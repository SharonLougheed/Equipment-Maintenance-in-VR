using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;

public class InteractablePart : Throwable, IObjectiveCommands {

    [Flags] public enum Axis_t
    {
        //None = 0,
        X = 1 << 0,
        Y = 1 << 1,
        Z = 1 << 2,
    }
    [Flags] public enum RigidbodySettings
    {
        //None = 0,
        UseGravity = 1 << 0,
        IsKinematic = 1 << 1,
        ModeDefault = 1 << 2,
    }
    [Flags] public enum PartObjectiveSettings
    {
        //None = 0,
        RequireHandAttached = 1 << 0,
        RequireColliderOverlap = 1 << 1,
    }
    public Transform gripAttachOffset;
    public Transform pinchAttachOffset;
    public Objective.PartObjectiveTypes ObjectiveType = Objective.PartObjectiveTypes.MoveToLocation;
    private Objective.ObjectiveStates objectiveState = Objective.ObjectiveStates.NotInProgress;
    [Header("Move To Location Settings")]
    //[EnumFlags] public RigidbodySettings beginningKinematicState = RigidbodySettings.UseGravity;
    //[EnumFlags] public RigidbodySettings endingKinematicState = RigidbodySettings.IsKinematic;
    public bool useGravityBefore = true;
    public bool isKinematicBefore = false;
    public bool showEndPointOutline = true;
    public bool useGravityAfter = false;
    public bool isKinematicAfter = true;
    public Transform endingLocation;
    public float acceptableDegreesFromEndPoint = 5f;
    public float acceptableMetersFromEndPoint = 0.1f;
    //[EnumFlags] public Axis_t rotationAxisToCheck = Axis_t.X | Axis_t.Y | Axis_t.Z;
    public bool checkXaxis = true;
    public bool checkYaxis = true;
    public bool checkZaxis = true;
    //[EnumFlags] public PartObjectiveSettings partObjectiveSettings = PartObjectiveSettings.RequireColliderOverlap | PartObjectiveSettings.RequireColliderOverlap;
    public bool requireHandAttached = true;
    public bool requireColliderOverlap = true;
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
    private Dictionary<int, Collider> endPointColliders;
    private enum PlacementStates { DefaultPlaced, DefaultHeld, UnacceptableHover, AcceptableHoverCanDetach, AcceptableHoverNoDetach, AcceptablePlaced, UnacceptablePlaced };
    private PlacementStates currentPlacementState = PlacementStates.DefaultPlaced;
    private bool isTouchingEndPoint = false;
    private bool endPointActiveState = false;
    private bool highlightOnHover = false;
    private Vector3 endingPosition;
    private Quaternion endingRotation;

    public event Action CompletionEvent;

    // Loads in materials used to indicate the accuracy of the attempted object placement
    protected override void Awake()
    {
        base.Awake();
        defaultOutlineMaterial = Resources.Load("Materials/OutlineMatOrange") as Material;
        acceptablePlacementMaterial = Resources.Load("Materials/OutlineMatGreen") as Material;
        unacceptablePlacementMaterial = Resources.Load("Materials/OutlineMatRed") as Material;
    }

    // Start by setting the interactable to not highlight unless the objective has started in which the variable will hold true
    // and copy ending location transform data incase it moves during runtime
    void Start () {
        interactable.highlightOnHover = highlightOnHover;
        SetStatic(gameObject, false);
        if(endingLocation != null)
        {
            endingPosition = endingLocation.position;
            endingRotation = endingLocation.rotation;
        }
    }

    // Sets all GameObjects at or below obj to or fom static
    private void SetStatic(GameObject obj, bool isStatic)
    {
        List<GameObject> allObjects = GetAllGameObjectsAtOrBelow(obj);
        for(int i = 0; i < allObjects.Count; i++)
        {
            allObjects[i].isStatic = isStatic;
        }
    }

    // Gets all the children and self of a gameobject
    private List<GameObject> GetAllGameObjectsAtOrBelow(GameObject start)
    {
        List<GameObject> gameObjects = new List<GameObject>();
        Transform[] transforms = start.GetComponentsInChildren<Transform>();
        foreach (Transform childTransform in transforms)
            gameObjects.Add(childTransform.gameObject);
        return gameObjects;
    }

   // Applies a material to all gameobjects in a list
    private void ApplyMaterialToList(List<GameObject> gameObjects, Material mat)
    {
        for (int i = 0; i < gameObjects.Count; i++)
        {
            Renderer renderer = gameObjects[i].GetComponent<Renderer>();
            if (renderer != null)
                renderer.materials = GetArrayOfMaterial(mat, renderer.materials.Length);
        }
    }

    // For some reason the size of the previous material array must be matched in order to show up
    private Material[] GetArrayOfMaterial(Material mat, int size)
    {
        Material[] materials = new Material[size];
        for (int i = 0; i < size; i++)
        {
            materials[i] = mat;
        }
        return materials;
    }

    // Create the ending location gameobject and outline by duplicating this one and removing its scripts
    private void InitializeEndPoint()
    {
        if (endingLocation != null)
        {
            
            endPointGameObject = Instantiate(gameObject, endingPosition, endingRotation);
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
           
            SetEndPointVisibility(showEndPointOutline);
            endPointGameObject.SetActive(endPointActiveState);
        }
    }

    // Sets this visibility of all of the endpoints renderers
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

    // Checks whether two rotations are within some 90 degree rotation from each other
    // NOTE The mod 90 was only added because sometimes the angles are very similar but appear to be nearly 180 degrees off
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
        if (checkYaxis)
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

    // Checks whether another transform is within range of this one's center
    // This transform is recalculated every frame since it appears to not work otherwise when being moved by a hand
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

    // Calculates a single bounds by aggregating several bounds
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

    // Sets all colliders to the boolean trigger state in a gameobject
    private void SetAllTriggers(GameObject obj, bool isOn)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].isTrigger = isOn;
        }
    }
    
    // Do not react until it is this objects objective
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
        // Allowing this before checking Objective state so that Throwable can cleanup easier
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


        if(objectiveState == Objective.ObjectiveStates.InProgress)
        {
            // when in an objective 

            GrabTypes startingGrabType = hand.GetGrabStarting();
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
                    UpdatePlacementState(PlacementStates.DefaultPlaced);
                }
            }
        }
        else if(objectiveState == Objective.ObjectiveStates.NotInProgress)
        {
            // when outside of an objective
            // TODO make obtion for acting throwable
        }
    }

    /* Updates the placement state to a new state only if it is a different state
     *  This involves two main components, (1) changing the interactables rigidbody settings
     *  and (2) changing the ending location's appearance
     */
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
                    rigidbody.isKinematic = requireHandAttached ?  isKinematicBefore : isKinematicAfter;
                    rigidbody.useGravity = requireHandAttached ? useGravityBefore : useGravityAfter;
                    break;
                case PlacementStates.AcceptablePlaced:
                    rigidbody.isKinematic = isKinematicAfter;
                    rigidbody.useGravity = useGravityAfter;
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

    /* Used to determine if this object is colliding with its ending location
     * This is useful because it drastically cuts down on computations for checking
     * rotation and distance from the centers
     */
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

    /* Used to determine if this object is colliding with its ending location
     * This is useful because it drastically cuts down on computations for checking
     * rotation and distance from the centers
     */
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

        hand.HoverUnlock(interactable);

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


    /* Called every Update() while this GameObject is attached to the hand
     * This function is responsible for changing between states VERY IMPORTANT
     */
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

            if (objectiveState == Objective.ObjectiveStates.InProgress)
            {
                if (ObjectiveType == Objective.PartObjectiveTypes.MoveToLocation)
                {
                    // First test if they are at least overlapping
                    if (requireColliderOverlap && isTouchingEndPoint  || (!showEndPointOutline && endPointGameObject != null))
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
                    UpdatePlacementState(PlacementStates.AcceptablePlaced);
                    OnObjectiveFinish();
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
                    if (requireColliderOverlap &&  isTouchingEndPoint || (!showEndPointOutline && endPointGameObject != null))
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
                                    UpdatePlacementState(PlacementStates.AcceptableHoverCanDetach);
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

    /* This is used to evaluate whether this object is in proximity of the ending location when it is not required to be attached to the hand.
     * This is because it will no longer be recieving hand attached updates so it must be done in the update function!
     */
    void Update()
    {
        if(objectiveState == Objective.ObjectiveStates.InProgress && endPointGameObject != null && !base.attached && !requireHandAttached && IsWithinRangeOfCenter(endPointGameObject.transform, acceptableMetersFromEndPoint)
                        && IsWithinRangeOfRotation(gameObject.transform.rotation, endPointGameObject.transform.rotation, acceptableDegreesFromEndPoint))
        {
            UpdatePlacementState(PlacementStates.AcceptablePlaced);
            OnObjectiveFinish();
        }
    }

    /* Initializes the this object with the settings of the objective and resets any state variables
     */
    public void OnObjectiveStart()
    {   
        objectiveState = Objective.ObjectiveStates.InProgress;
        highlightOnHover = true;
        interactable.highlightOnHover = true;
        isTouchingEndPoint = false;

        currentPlacementState = PlacementStates.DefaultPlaced;
        if (ObjectiveType == Objective.PartObjectiveTypes.MoveToLocation)
        {
            rigidbody.isKinematic = isKinematicBefore;
            rigidbody.useGravity = useGravityBefore;
            InitializeEndPoint();
            endPointActiveState = true;
            if (endPointGameObject != null)
            {
                endPointGameObject.SetActive(endPointActiveState);
                SetEndPointVisibility(showEndPointOutline);
            }
            
        }
        else if(ObjectiveType == Objective.PartObjectiveTypes.MoveFromLocation)
        {
            SetEndPointVisibility(false);
            rigidbody.isKinematic = isKinematicBefore;
            rigidbody.useGravity = useGravityBefore;
        }
        
    }

    public void OnObjectiveReset()
    {
        throw new NotImplementedException();
    }


    /* Called when the objective is completed and applies ending states to the object
     */
    public void OnObjectiveFinish()
    {
        rigidbody.isKinematic = isKinematicAfter;
        rigidbody.useGravity = useGravityAfter;
        CompletionEvent();
        objectiveState = Objective.ObjectiveStates.NotInProgress;
        interactable.highlightOnHover = false;
        SetEndPointVisibility(false);
        if(endPointGameObject != null)
        {
            Destroy(endPointGameObject);
        }
    }
}
