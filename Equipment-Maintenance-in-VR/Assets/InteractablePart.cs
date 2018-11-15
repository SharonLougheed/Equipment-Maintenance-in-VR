using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;

public class InteractablePart : Throwable, IObjectiveCommands {

    public Transform gripAttachOffset;
    public Transform pinchAttachOffset;
    public Objective.PartObjectiveTypes ObjectiveType = Objective.PartObjectiveTypes.MoveToLocation;
    private Objective.ObjectiveStates objectiveState = Objective.ObjectiveStates.NotInProgress;
    [Header("Move To Location Settings")]
    public Transform endingLocation;
    public bool showEndPointOutline = true;
    public float acceptableDegreesFromEndPoint = 5f;
    public float acceptableMetersFromEndPoint = 0.1f;
    public bool checkXaxis = true;
    public bool checkYaxis = true;
    public bool checkZaxis = true;
    public bool detachAndSnap = true;
    [Header("Move From Location Settings")]
    public bool onlyCompleteAfterRelease = true;
    public UnityEvent onAcceptablePlacement;

    private Material defaultOutlineMaterial;
    private Material acceptablePlacementMaterial;
    private Material unacceptablePlacementMaterial;
    private Transform endPointTransform;
    private GameObject endPointGameObject;
    private Bounds selfGroupBounds;
    private Bounds otherGroupBounds;
    private Vector3 selfCenter;
    private Vector3 otherCenter;
    private bool selfBoundsExpired = true;
    private bool otherBoundsExpired = true;
    private List<GameObject> endPointObjectList;
    private Dictionary<int, Material[]> endPointOriginalMaterials;
    private Dictionary<int, Collider> endPointColliders;
    private enum PlacementStates { DefaultPlaced, DefaultHeld, UnacceptableHover, AcceptableHoverCanDetach, AcceptableHoverNoDetach, AcceptablePlaced, UnacceptablePlaced };
    private PlacementStates currentPlacementState = PlacementStates.DefaultPlaced;
    private bool isTouchingEndPoint = false;
    private bool endPointActiveState = false;
    private bool highlightOnHover = false;


    public event Action CompletionEvent;

    protected override void Awake()
    {
        base.Awake();
        defaultOutlineMaterial = Resources.Load("Materials/OutlineMatOrange") as Material;
        acceptablePlacementMaterial = Resources.Load("Materials/OutlineMatGreen") as Material;
        unacceptablePlacementMaterial = Resources.Load("Materials/OutlineMatRed") as Material;
    }


    void Start () {
        interactable.highlightOnHover = false;
        SetStatic(gameObject, false);
    }

    
    private void SetStatic(GameObject obj, bool isStatic)
    {
        List<GameObject> allObjects = GetAllGameObjectsAtOrBelow(obj);
        for(int i = 0; i < allObjects.Count; i++)
        {
            allObjects[i].isStatic = isStatic;
        }
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
        if (endingLocation != null)
        {

            endPointGameObject = Instantiate(gameObject, endingLocation.position, endingLocation.rotation);
            Destroy(endPointGameObject.GetComponent<InteractablePart>());
            Destroy(endPointGameObject.GetComponent<Rigidbody>());
            Destroy(endPointGameObject.GetComponent<Throwable>());
            Destroy(endPointGameObject.GetComponent<VelocityEstimator>());
            Destroy(endPointGameObject.GetComponent<InteractableObjective>());
            Destroy(endPointGameObject.GetComponent<ToolObjective>());
            Destroy(endPointGameObject.GetComponent<InteractableHoverEvents>());
            Destroy(endPointGameObject.GetComponent<Interactable>());

            endPointObjectList = GetAllGameObjectsAtOrBelow(endPointGameObject);
            ApplyMaterialToList(endPointObjectList, defaultOutlineMaterial);

            endPointColliders = new Dictionary<int, Collider>();
            for (int i = 0; i < endPointObjectList.Count; i++)
            {
                Collider collider = endPointObjectList[i].GetComponent<Collider>();
                if (collider != null)
                {
                    collider.isTrigger = true;
                    endPointColliders.Add(collider.GetInstanceID(), collider);
                }
            }
            if (!showEndPointOutline)
            {
                SetEndPointVisibility(false);
            }
            endPointGameObject.SetActive(endPointActiveState);
            //Debug.Log("endpoint: " + endingLocation.gameObject.name);
        }
    }


    private void SetEndPointVisibility(bool isVisible)
    {
        if(endPointGameObject != null)
        {
            Renderer[] renderers = endPointGameObject.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = isVisible;
            }
        }
    }

    public void ShowEndPointIfApplicable()
    {
        if (showEndPointOutline)
        {
            endPointActiveState = true;
            if (endPointGameObject != null)
            {
                endPointGameObject.SetActive(endPointActiveState);
                SetEndPointVisibility(true);
            }
        }
    }


    private void OnAcceptablePlacement()
    {
        //onAcceptablePlacement.Invoke();
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
        if (checkXaxis)
        {
            float angle = Math.Abs(rot1.eulerAngles.x - rot2.eulerAngles.x) % 360;
            angle = angle > 180 ? 360 - angle : angle;
            //Debug.Log("X angle: " + angle);
            angle = angle % 90;
            if (angle > limit)
                return false;
        }
        if (false && checkYaxis)
        {
            float angle = Math.Abs(rot1.eulerAngles.y - rot2.eulerAngles.y) % 360;
            angle = angle > 180 ? 360 - angle : angle;
            angle = angle % 90;
            //Debug.Log("Y angle: " + angle);
            if (angle > limit)
                return false;
        }
        if (checkZaxis)
        {
            float angle = Math.Abs(rot1.eulerAngles.z - rot2.eulerAngles.z) % 360;
            angle = angle > 180 ? 360 - angle : angle;
            angle = angle % 90;
            //Debug.Log("Z angle: " + angle);
            if (angle > limit)
                return false;
        }
        return true;
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
        Debug.DrawRay(selfCenter, otherCenter);

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

    protected override void OnHandHoverBegin(Hand hand)
    {
        if (objectiveState == Objective.ObjectiveStates.InProgress)
        {
            base.OnHandHoverBegin(hand);
        }
        else if (objectiveState == Objective.ObjectiveStates.NotInProgress)
        {
            // outside of objective
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

        if(objectiveState == Objective.ObjectiveStates.InProgress)
        {
            // when in an objective 

            if (interactable.attachedToHand == null && startingGrabType != GrabTypes.None)
            {
                // Attach this object to the hand

                if (startingGrabType == GrabTypes.Pinch)
                {
                    hand.AttachObject(gameObject, startingGrabType, base.attachmentFlags, pinchAttachOffset);
                }
                else if (startingGrabType == GrabTypes.Grip)
                {
                    hand.AttachObject(gameObject, startingGrabType, base.attachmentFlags, gripAttachOffset);
                }
                else
                {
                    hand.AttachObject(gameObject, startingGrabType, base.attachmentFlags, base.attachmentOffset);
                }

                hand.HideGrabHint();
                if (ObjectiveType == Objective.PartObjectiveTypes.MoveToLocation)
                {
                    // Do nothing
                }
                else if (ObjectiveType == Objective.PartObjectiveTypes.MoveFromLocation)
                {
                    UpdatePlacementState(PlacementStates.DefaultHeld);
                }
            }
        }
        else
        {
            // when outside of an objective
        }
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
            if (endPointGameObject != null && showEndPointOutline)
            {
                // change conditions coming out of specific states
                switch (oldState)
                {
                    case PlacementStates.AcceptablePlaced:
                        endPointGameObject.SetActive(true);
                        break;
                }
                // change conditions going to specifc states
                switch (currentPlacementState)
                {
                    case PlacementStates.DefaultPlaced:
                    case PlacementStates.DefaultHeld:
                        ApplyMaterialToList(endPointObjectList, defaultOutlineMaterial);
                        break;
                    case PlacementStates.UnacceptableHover:
                    case PlacementStates.UnacceptablePlaced:
                        ApplyMaterialToList(endPointObjectList, unacceptablePlacementMaterial);
                        break;
                    case PlacementStates.AcceptableHoverCanDetach:
                    case PlacementStates.AcceptableHoverNoDetach:
                        ApplyMaterialToList(endPointObjectList, acceptablePlacementMaterial);
                        break;
                    case PlacementStates.AcceptablePlaced:
                        endPointGameObject.SetActive(false);
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
            && endPointGameObject != null
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
           && endPointGameObject != null
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

        //onDetachFromHand.Invoke();

        hand.HoverUnlock(null);

        base.rigidbody.interpolation = hadInterpolation;
        Debug.Log("State: " + currentPlacementState);
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
        bool isFallbackHand = hand.name == "FallbackHand" ? true : false;
        if (isFallbackHand)
        {
            return;
        }
        if (hand.IsGrabEnding(this.gameObject))
        {
            // Detach this object from the hand
            hand.DetachObject(gameObject, restoreOriginalParent);

            // Call this to undo HoverLock
            //hand.HoverUnlock(interactable);
            if (objectiveState == Objective.ObjectiveStates.InProgress)
            {
                if (ObjectiveType == Objective.PartObjectiveTypes.MoveToLocation)
                {
                    // First test if they are at least overlapping
                    if (isTouchingEndPoint || (!showEndPointOutline && endPointGameObject != null))
                    {
                        if (endPointGameObject != null
                        && IsWithinRangeOfCenter(endPointGameObject.transform, acceptableMetersFromEndPoint)
                        && IsWithinRangeOfRotation(gameObject.transform.rotation, endPointGameObject.transform.rotation, acceptableDegreesFromEndPoint))
                        {
                            // Move to End point transform
                            gameObject.transform.position = endPointGameObject.transform.position;
                            gameObject.transform.rotation = endPointGameObject.transform.rotation;
                            UpdatePlacementState(PlacementStates.AcceptablePlaced);
                            OnAcceptablePlacement();
                            OnObjectiveFinish();
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
                else if (ObjectiveType == Objective.PartObjectiveTypes.MoveFromLocation)
                {
                    // For now dropping it anywhere implies a successful movement away from a location
                    OnAcceptablePlacement();
                    OnObjectiveFinish();
                    UpdatePlacementState(PlacementStates.DefaultPlaced);
                }
            }
            else
            {
                UpdatePlacementState(PlacementStates.DefaultPlaced);
            }
            
        }

        if (endPointGameObject != null
            && interactable.attachedToHand != null
            )
        {
            if (objectiveState == Objective.ObjectiveStates.InProgress)
            {
                if (ObjectiveType == Objective.PartObjectiveTypes.MoveToLocation)
                {
                    // First check if they are at least overlapping
                    if (isTouchingEndPoint || (!showEndPointOutline && endPointGameObject != null))
                    {
                        // Then if they are relatively close in position and rotation
                        if (IsWithinRangeOfCenter(endPointGameObject.transform, acceptableMetersFromEndPoint)
                        && IsWithinRangeOfRotation(gameObject.transform.rotation, endPointGameObject.transform.rotation, acceptableDegreesFromEndPoint))
                        {
                            switch (currentPlacementState)
                            {
                                case PlacementStates.AcceptableHoverCanDetach:
                                    // Detach this object from the hand
                                    hand.DetachObject(gameObject, restoreOriginalParent);
                                    // Move to End point transform
                                    gameObject.transform.position = endPointGameObject.transform.position;
                                    gameObject.transform.rotation = endPointGameObject.transform.rotation;
                                    UpdatePlacementState(PlacementStates.AcceptablePlaced);
                                    OnAcceptablePlacement();
                                    OnObjectiveFinish();
                                    break;
                                case PlacementStates.AcceptablePlaced:
                                    UpdatePlacementState(PlacementStates.AcceptableHoverNoDetach);
                                    break;
                                case PlacementStates.UnacceptableHover:
                                    if (detachAndSnap)
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
                else if (ObjectiveType == Objective.PartObjectiveTypes.MoveFromLocation)
                {
                    // Do nothing
                }
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

    public void OnObjectiveStart()
    {   
        objectiveState = Objective.ObjectiveStates.InProgress;
        highlightOnHover = true;
        interactable.highlightOnHover = true;
        if(ObjectiveType == Objective.PartObjectiveTypes.MoveToLocation)
        {
            if (showEndPointOutline)
            {
                InitializeEndPoint();
                endPointActiveState = true;
                if (endPointGameObject != null)
                {
                    endPointGameObject.SetActive(endPointActiveState);
                    SetEndPointVisibility(true);
                }
            }
        }
        else if(ObjectiveType == Objective.PartObjectiveTypes.MoveFromLocation)
        {
            SetEndPointVisibility(false);
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }
        
    }

    public void OnObjectiveReset()
    {
        throw new NotImplementedException();
    }

    public void OnObjectiveFinish()
    {
        CompletionEvent();
        objectiveState = Objective.ObjectiveStates.NotInProgress;
        interactable.highlightOnHover = false;
        SetEndPointVisibility(false);
    }
}
