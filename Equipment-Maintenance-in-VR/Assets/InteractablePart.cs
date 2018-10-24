using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Interactable))]
public class InteractablePart : MonoBehaviour {

    // Interactable related
    private Hand.AttachmentFlags attachmentFlags = Hand.defaultAttachmentFlags & (~Hand.AttachmentFlags.SnapOnAttach) & (~Hand.AttachmentFlags.DetachOthers) & (~Hand.AttachmentFlags.VelocityMovement);
    private Interactable interactable;


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

    private Transform transform;
    private Rigidbody rigidbody;
    private Bounds selfGroupBounds;
    private Bounds otherGroupBounds;
    private Vector3 selfCenter;
    private Vector3 otherCenter;
    private bool selfBoundsExpired = true;
    private bool otherBoundsExpired = true;
    private List<GameObject> endPointGameObjects;
    private Dictionary<int, Material[]> endPointOriginalMaterials;
    private enum PlacementStates { Default, UnacceptableHover, AcceptableHover, AcceptablePlaced };
    private PlacementStates currentPlacementState = PlacementStates.Default;

    void Awake()
    {
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
        interactable = GetComponent<Interactable>();
        transform = GetComponent<Transform>();
        rigidbody = GetComponent<Rigidbody>();
        InitializeEndPoint();
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
            for (int i = 0; i < endPointGameObjects.Count; i++)
            {
                Collider collider = endPointGameObjects[i].GetComponent<Collider>();
                if (collider != null)
                {
                    collider.isTrigger = true;
                }
            }
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
        //Debug.Log(this.name  + "Position: " + this.transform.position + "Center: " + selfCenter );
        //Debug.Log(otherTransform.gameObject.name + "Position: " + otherTransform.position + "Center: " + otherCenter);
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


    //-------------------------------------------------
    // Called when a Hand starts hovering over this object
    //-------------------------------------------------
    private void OnHandHoverBegin(Hand hand)
    {
        VibrateController(hand, 0.15f, 5f, 1f);
    }


    //-------------------------------------------------
    // Called when a Hand stops hovering over this object
    //-------------------------------------------------
    private void OnHandHoverEnd(Hand hand)
    {
    }


    //-------------------------------------------------
    // Called every Update() while a Hand is hovering over this object
    //-------------------------------------------------
    private void HandHoverUpdate(Hand hand)
    {
        GrabTypes startingGrabType = hand.GetGrabStarting();
        bool isGrabEnding = hand.IsGrabEnding(this.gameObject);

        if (interactable.attachedToHand == null && startingGrabType != GrabTypes.None)
        {
            // Call this to continue receiving HandHoverUpdate messages,
            // and prevent the hand from hovering over anything else
            hand.HoverLock(interactable);
            // Attach this object to the hand
            hand.AttachObject(gameObject, startingGrabType, attachmentFlags);
        }
        else if (isGrabEnding)
        {
            // Detach this object from the hand
            hand.DetachObject(gameObject);
            // Call this to undo HoverLock
            hand.HoverUnlock(interactable);
            // For snapping
            if (endPointTransform != null
                && IsWithinRangeOfCenter(endPointTransform, acceptableMetersFromEndPoint)
                && IsWithinRangeOfRotation(gameObject.transform.rotation, endPointTransform.rotation, acceptableDegreesFromEndPoint))
            {
                // Move to End point transform
                transform.position = endPointTransform.position;
                transform.rotation = endPointTransform.rotation;
                endPointTransform.gameObject.SetActive(false);
                UpdatePlacementState(PlacementStates.AcceptablePlaced);
                OnAcceptablePlacement();
            }
            else
            {
                UpdatePlacementState(PlacementStates.Default);
            }
        }

        if (endPointTransform != null
            && interactable.attachedToHand != null
            && !isGrabEnding)
        {
           if(IsWithinRangeOfCenter(endPointTransform, acceptableMetersFromEndPoint)
                && IsWithinRangeOfRotation(gameObject.transform.rotation, endPointTransform.rotation, acceptableDegreesFromEndPoint))
            {
                if (currentPlacementState != PlacementStates.AcceptableHover)
                {
                    if (snapAndDetach)
                    {
                        // Detach this object from the hand
                        hand.DetachObject(gameObject);
                        // Call this to undo HoverLock
                        hand.HoverUnlock(interactable);
                        // Move to End point transform
                        transform.position = endPointTransform.position;
                        transform.rotation = endPointTransform.rotation;

                        UpdatePlacementState(PlacementStates.AcceptablePlaced);
                    }
                    else
                    {
                        UpdatePlacementState(PlacementStates.AcceptableHover);
                    }
                    OnAcceptablePlacement();
                }
                else
                {
                    endPointTransform.gameObject.SetActive(true);
                }
            }
            else
            {
                UpdatePlacementState(PlacementStates.UnacceptableHover);
            }

        }

    }


    private void UpdatePlacementState(PlacementStates newState)
    {
        if(newState != currentPlacementState)
        {
            // Make changes on Interactable Part to reflect state change
            switch (newState)
            {
                case PlacementStates.Default:
                    rigidbody.isKinematic = false;
                    rigidbody.useGravity = true;
                    break;
                case PlacementStates.AcceptablePlaced:
                    rigidbody.isKinematic = true;
                    rigidbody.useGravity = false;
                    break;
                case PlacementStates.UnacceptableHover:
                case PlacementStates.AcceptableHover:
                    break;
            }
            // Make changes on End Point Part to reflect state change (only if its going to be visible)
            if (endPointTransform != null && showEndPointOutline)
            {
                if(currentPlacementState == PlacementStates.AcceptablePlaced)
                {
                    endPointTransform.gameObject.SetActive(true);
                }
                switch (newState)
                {
                    case PlacementStates.Default:
                        ApplyMaterialToList(endPointGameObjects, defaultOutlineMaterial);
                        break;
                    case PlacementStates.UnacceptableHover:
                        ApplyMaterialToList(endPointGameObjects, unacceptablePlacementMaterial);
                        break;
                    case PlacementStates.AcceptableHover:
                        ApplyMaterialToList(endPointGameObjects, acceptablePlacementMaterial);
                        break;
                    case PlacementStates.AcceptablePlaced:
                        endPointTransform.gameObject.SetActive(false);
                        break;
                }
            }
        }
    }

    //-------------------------------------------------
    // Called when this GameObject becomes attached to the hand
    //-------------------------------------------------
    private void OnAttachedToHand(Hand hand)
    {
    }


    //-------------------------------------------------
    // Called when this GameObject is detached from the hand
    //-------------------------------------------------
    private void OnDetachedFromHand(Hand hand)
    {
    }


    //-------------------------------------------------
    // Called every Update() while this GameObject is attached to the hand
    //-------------------------------------------------
    private void HandAttachedUpdate(Hand hand)
    {
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
