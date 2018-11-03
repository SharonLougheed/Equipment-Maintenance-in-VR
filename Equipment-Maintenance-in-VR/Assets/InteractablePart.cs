using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;

public class InteractablePart : Throwable {

    public Objective.ObjectiveTypes ObjectiveType = Objective.ObjectiveTypes.MoveToLocation;
    public Transform endPointTransform;
    public bool showEndPointOutline = true;
    public float acceptableDegreesFromEndPoint = 10f;
    public float acceptableMetersFromEndPoint = 0.1f;
    public bool snapAndDetach = true;
    public UnityEvent onAcceptablePlacement;
    [Tooltip("Material used for showing where replacement part is supposed to go. Default is OrangeOutline")]
    public Material defaultOutlineMaterial;
    [Tooltip("Material used for showing the user's replacement part is acceptable. Default is GreenOutline")]
    public Material acceptablePlacementMaterial;
    [Tooltip("Material used for showing the user's replacement part is not acceptable. Default is RedOutline")]
    public Material unacceptablePlacementMaterial;

    private ObjectiveSubject objectiveSubject;

    private Bounds selfGroupBounds;
    private Bounds otherGroupBounds;
    private Vector3 selfCenter;
    private Vector3 otherCenter;
    private bool selfBoundsExpired = true;
    private bool otherBoundsExpired = true;
    private List<GameObject> endPointGameObjects;
    private Dictionary<int, Material[]> endPointOriginalMaterials;
    private Dictionary<int, Collider> endPointColliders;
    private enum PlacementStates { DefaultPlaced, DefaultHeld, UnacceptableHover, AcceptableHoverCanDetach, AcceptableHoverNoDetach, AcceptablePlaced, UnacceptablePlaced };
    private PlacementStates currentPlacementState = PlacementStates.DefaultPlaced;
    private bool isTouchingEndPoint = false;

    protected override void Awake()
    {
        base.Awake();

        if (defaultOutlineMaterial == null)
        {
            defaultOutlineMaterial = Resources.Load("Materials/OutlineMatOrange") as Material;
        }
        if (acceptablePlacementMaterial == null)
        {
            acceptablePlacementMaterial = Resources.Load("Materials/OutlineMatGreen") as Material;
        }
        if (unacceptablePlacementMaterial == null)
        {
            unacceptablePlacementMaterial = Resources.Load("Materials/OutlineMatRed") as Material;
        }
    }


    void Start () {
        ObjectiveSubject objectiveSubject = GetComponent<ObjectiveSubject>();
        SetStatic(gameObject, false);
        InitializeEndPoint();
    }

    
    private void SetStatic(GameObject obj, bool isStatic)
    {
        List<GameObject> allObjects = GetAllGameObjectsAtOrBelow(obj);
        for(int i = 0; i < allObjects.Count; i++)
        {
            allObjects[i].isStatic = isStatic;
        }
    }


    private Dictionary<int, Material[]> SaveOriginalMaterials(List<GameObject> gameObjects)
    {
        Dictionary<int, Material[]> ogMaterials = new Dictionary<int, Material[]>();
        foreach (GameObject obj in gameObjects)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
                ogMaterials.Add(obj.GetInstanceID(), renderer.materials);
        }
        return ogMaterials;
    }


    private List<GameObject> GetAllGameObjectsAtOrBelow(GameObject start)
    {
        List<GameObject> gameObjects = new List<GameObject>();
        Transform[] transforms = start.GetComponentsInChildren<Transform>();
        foreach (Transform childTransform in transforms)
            gameObjects.Add(childTransform.gameObject);
        return gameObjects;
    }

   
    private void ApplyMaterialToList(List<GameObject> gameObjects, Material mat)
    {
        for (int i = 0; i < gameObjects.Count; i++)
        {
            Renderer renderer = gameObjects[i].GetComponent<Renderer>();
            if (renderer != null)
                renderer.materials = GetArrayOfMaterial(mat, renderer.materials.Length);
        }
    }


    private Material[] GetArrayOfMaterial(Material mat, int size)
    {
        Material[] materials = new Material[size];
        for (int i = 0; i < size; i++)
        {
            materials[i] = mat;
        }
        return materials;
    }

    private void InitializeEndPoint()
    {
        if(endPointTransform != null)
        {
            endPointGameObjects = GetAllGameObjectsAtOrBelow(endPointTransform.gameObject);
            endPointOriginalMaterials = SaveOriginalMaterials(endPointGameObjects);
            if (showEndPointOutline)
            {
                ApplyMaterialToList(endPointGameObjects, defaultOutlineMaterial);
            }
            else
            {
                SetEndPointVisibility(false);
            }
            endPointColliders = new Dictionary<int, Collider>();
            for (int i = 0; i < endPointGameObjects.Count; i++)
            {
                Collider collider = endPointGameObjects[i].GetComponent<Collider>();
                if (collider != null)
                {
                    collider.isTrigger = true;
                    endPointColliders.Add(collider.GetInstanceID(), collider);
                }
            }
            //endPointTransform.gameObject.SetActive(false);
        }
    }


    private void SetEndPointVisibility(bool isVisible)
    {
        Renderer[] renderers = endPointTransform.gameObject.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = isVisible;
        }
    }

    private void OnAcceptablePlacement()
    {
        onAcceptablePlacement.Invoke();
        // if an objective recently added it
        if(objectiveSubject == null)
        {
            objectiveSubject = GetComponent<ObjectiveSubject>();
        }
        if(objectiveSubject != null)
        {
            objectiveSubject.NotifyCompletion();
        }
    }


    public void VibrateController(Hand hand, float durationSec, float frequency, float amplitude)
    {
        StartCoroutine(VibrateControllerContinuous(hand, durationSec, frequency, amplitude));
    }


    IEnumerator VibrateControllerContinuous(Hand hand, float durationSec, float frequency, float amplitude)
    {
        hand.TriggerHapticPulse(durationSec, frequency, amplitude);
        yield break;
    }


    private bool IsWithinRangeOfRotation(Quaternion rot1, Quaternion rot2, float limit)
    {
        // Debug.Log("Rotation between objects: " + Quaternion.Angle(rot1, rot2));
        return Quaternion.Angle(rot1, rot2) <= limit ? true : false;
    }

    private bool IsWithinRangeOfCenter(Transform otherTransform, float limit)
    {
        if (selfBoundsExpired)
        {
            selfGroupBounds = CalculateGroupBounds(this.transform);
            //selfBoundsExpired = false;
        }
        if (otherBoundsExpired)
        {
            otherGroupBounds = CalculateGroupBounds(otherTransform);
            otherBoundsExpired = false;
        }
        // self bounds expire every time so that the new center is always recalculated
        selfBoundsExpired = true;
        selfCenter = selfGroupBounds.center;
        otherCenter = otherGroupBounds.center;
        //Debug.Log("Distance: " + Vector3.Distance(selfCenter, otherCenter));
        return Vector3.Distance(selfCenter, otherCenter) <= limit ? true : false;
    }


    private Bounds CalculateGroupBounds(params Transform[] aObjects)
    {
        Bounds b = new Bounds();
        foreach (var o in aObjects)
        {
            var renderers = o.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (b.size == Vector3.zero)
                    b = r.bounds;
                else
                    b.Encapsulate(r.bounds);
            }
            var colliders = o.GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
            {
                if (b.size == Vector3.zero)
                    b = c.bounds;
                else
                    b.Encapsulate(c.bounds);
            }
        } 
        return b;
    }


    private void SetAllTriggers(GameObject obj, bool isOn)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].isTrigger = isOn;
        }
    }


    protected override void HandHoverUpdate(Hand hand)
    {
        if (attachEaseIn)
        {
            float t = Util.RemapNumberClamped(Time.time, attachTime, attachTime + snapAttachEaseInTime, 0.0f, 1.0f);
            if (t < 1.0f)
            {
                t = snapAttachEaseInCurve.Evaluate(t);
                transform.position = Vector3.Lerp(attachPosition, attachEaseInTransform.position, t);
                transform.rotation = Quaternion.Lerp(attachRotation, attachEaseInTransform.rotation, t);
            }
            else if (!snapAttachEaseInCompleted)
            {
                gameObject.SendMessage("OnThrowableAttachEaseInCompleted", hand, SendMessageOptions.DontRequireReceiver);
                snapAttachEaseInCompleted = true;
            }
        }

        GrabTypes startingGrabType = hand.GetGrabStarting();
        bool isGrabEnding = hand.IsGrabEnding(this.gameObject);

        if (interactable.attachedToHand == null && startingGrabType != GrabTypes.None)
        {
            // Attach this object to the hand
            hand.AttachObject(gameObject, startingGrabType, base.attachmentFlags);
            hand.HideGrabHint();
            if (ObjectiveType == Objective.ObjectiveTypes.MoveToLocation)
            {
                // Do nothing
            }
            else if (ObjectiveType == Objective.ObjectiveTypes.MoveFromLocation)
            {
                UpdatePlacementState(PlacementStates.DefaultHeld);
            }
        }

        //if (hand.IsGrabEnding(this.gameObject))
        //{
        //    hand.DetachObject(gameObject, restoreOriginalParent);

        //    // Uncomment to detach ourselves late in the frame.
        //    // This is so that any vehicles the player is attached to
        //    // have a chance to finish updating themselves.
        //    // If we detach now, our position could be behind what it
        //    // will be at the end of the frame, and the object may appear
        //    // to teleport behind the hand when the player releases it.
        //    //StartCoroutine( LateDetach( hand ) );
        //}
    }


    private void UpdatePlacementState(PlacementStates newState)
    {
        PlacementStates oldState = currentPlacementState;
        currentPlacementState = newState;

        if(currentPlacementState != oldState)
        {
            // Make changes on Interactable Part to reflect state change
            switch (currentPlacementState)
            {
                case PlacementStates.UnacceptablePlaced:
                case PlacementStates.DefaultPlaced:
                    SetAllTriggers(gameObject, false);
                    rigidbody.isKinematic = false;
                    rigidbody.useGravity = true;
                    break;
                case PlacementStates.AcceptablePlaced:
                    rigidbody.isKinematic = true;
                    rigidbody.useGravity = false;
                    SetAllTriggers(gameObject, false);
                    break;
                case PlacementStates.DefaultHeld:
                case PlacementStates.UnacceptableHover:
                case PlacementStates.AcceptableHoverCanDetach:
                case PlacementStates.AcceptableHoverNoDetach:
                    SetAllTriggers(gameObject, true);
                    break;
            }
            // Make changes on End Point Part to reflect state change (only if its going to be visible)
            if (endPointTransform != null && showEndPointOutline)
            {
                // change conditions coming out of specific states
                switch (oldState)
                {
                    case PlacementStates.AcceptablePlaced:
                        endPointTransform.gameObject.SetActive(true);
                        break;
                }
                // change conditions going to specifc states
                switch (currentPlacementState)
                {
                    case PlacementStates.DefaultPlaced:
                    case PlacementStates.DefaultHeld:
                        ApplyMaterialToList(endPointGameObjects, defaultOutlineMaterial);
                        break;
                    case PlacementStates.UnacceptableHover:
                    case PlacementStates.UnacceptablePlaced:
                        ApplyMaterialToList(endPointGameObjects, unacceptablePlacementMaterial);
                        break;
                    case PlacementStates.AcceptableHoverCanDetach:
                    case PlacementStates.AcceptableHoverNoDetach:
                        ApplyMaterialToList(endPointGameObjects, acceptablePlacementMaterial);
                        break;
                    case PlacementStates.AcceptablePlaced:
                        endPointTransform.gameObject.SetActive(false);
                        break;
                }
            }
        }
        //Debug.Log("Placement State: " + currentPlacementState.ToString());
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("OnTriggerEnter");
        if( showEndPointOutline
            && other != null
            && endPointTransform != null
            && endPointColliders.ContainsKey(other.GetInstanceID()))
        {
            isTouchingEndPoint = true;
        }           
    }


    private void OnTriggerExit(Collider other)
    {
        //Debug.Log("OnTriggerExit");
        if (showEndPointOutline
           && other != null
           && endPointTransform != null
           && endPointColliders.ContainsKey(other.GetInstanceID()))
        {
            isTouchingEndPoint = false;
        }
    }


    //-------------------------------------------------
    // Called when this GameObject is detached from the hand
    //-------------------------------------------------
    protected override void OnDetachedFromHand(Hand hand)
    {
        attached = false;

        onDetachFromHand.Invoke();

        hand.HoverUnlock(null);

        base.rigidbody.interpolation = hadInterpolation;

        if (currentPlacementState != PlacementStates.AcceptablePlaced)
        {
            Vector3 velocity;
            Vector3 angularVelocity;

            GetReleaseVelocities(hand, out velocity, out angularVelocity);

            rigidbody.velocity = velocity;
            rigidbody.angularVelocity = angularVelocity;
        }
    }


    //-------------------------------------------------
    // Called every Update() while this GameObject is attached to the hand
    //-------------------------------------------------
    protected override void HandAttachedUpdate(Hand hand)
    {
        //Throwable will detach the object
        //base.HandAttachedUpdate(hand);

        if (hand.IsGrabEnding(this.gameObject))
        {
            // Detach this object from the hand
            hand.DetachObject(gameObject, restoreOriginalParent);

            // Call this to undo HoverLock
            //hand.HoverUnlock(interactable);
            if (ObjectiveType == Objective.ObjectiveTypes.MoveToLocation)
            {
                // First test if they are at least overlapping
                if (isTouchingEndPoint)
                {
                    if (endPointTransform != null
                    && IsWithinRangeOfCenter(endPointTransform, acceptableMetersFromEndPoint)
                    && IsWithinRangeOfRotation(gameObject.transform.rotation, endPointTransform.rotation, acceptableDegreesFromEndPoint))
                    {
                        // Move to End point transform
                        gameObject.transform.position = endPointTransform.position;
                        gameObject.transform.rotation = endPointTransform.rotation;
                        UpdatePlacementState(PlacementStates.AcceptablePlaced);
                        OnAcceptablePlacement();
                    }
                    else
                    {
                        UpdatePlacementState(PlacementStates.UnacceptablePlaced);
                    }
                }
                else
                {
                    UpdatePlacementState(PlacementStates.DefaultPlaced);
                }
            }
            else if (ObjectiveType == Objective.ObjectiveTypes.MoveFromLocation)
            {
                // For now dropping it anywhere implies a successful movement away from a location
                OnAcceptablePlacement();
                UpdatePlacementState(PlacementStates.DefaultPlaced);
            }
        }

        if (endPointTransform != null
            && interactable.attachedToHand != null
            )
        {
            if (ObjectiveType == Objective.ObjectiveTypes.MoveToLocation)
            {
                // First check if they are at least overlapping
                if (isTouchingEndPoint)
                {
                    // Then if they are relatively close in position and rotation
                    if (IsWithinRangeOfCenter(endPointTransform, acceptableMetersFromEndPoint)
                    && IsWithinRangeOfRotation(gameObject.transform.rotation, endPointTransform.rotation, acceptableDegreesFromEndPoint))
                    {
                        switch (currentPlacementState)
                        {
                            case PlacementStates.AcceptableHoverCanDetach:
                                // Detach this object from the hand
                                hand.DetachObject(gameObject, restoreOriginalParent);
                                // Move to End point transform
                                gameObject.transform.position = endPointTransform.position;
                                gameObject.transform.rotation = endPointTransform.rotation;
                                UpdatePlacementState(PlacementStates.AcceptablePlaced);
                                OnAcceptablePlacement();
                                break;
                            case PlacementStates.AcceptablePlaced:
                                UpdatePlacementState(PlacementStates.AcceptableHoverNoDetach);
                                break;
                            case PlacementStates.UnacceptableHover:
                                if (snapAndDetach)
                                {
                                    UpdatePlacementState(PlacementStates.AcceptableHoverCanDetach);
                                }
                                else
                                {
                                    UpdatePlacementState(PlacementStates.AcceptableHoverNoDetach);
                                }
                                break;
                        }
                    }
                    else
                    {
                        UpdatePlacementState(PlacementStates.UnacceptableHover);
                    }
                }
                else
                {
                    UpdatePlacementState(PlacementStates.DefaultHeld);
                }
            }
            else if (ObjectiveType == Objective.ObjectiveTypes.MoveFromLocation)
            {
                // Do nothing
            }

        }


    }


    //-------------------------------------------------
    // Called when this attached GameObject becomes the primary attached object
    //-------------------------------------------------
    private void OnHandFocusAcquired(Hand hand)
    {
    }


    //-------------------------------------------------
    // Called when another attached GameObject becomes the primary attached object
    //-------------------------------------------------
    private void OnHandFocusLost(Hand hand)
    {
    }
}
