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
   
    public float acceptableDegreesFromEndPoint = 10f;
    public float acceptableMetersFromEndPoint = 0.1f;
    public bool snapAndDetach = true;
    public UnityEvent onAcceptablePlacement;

    private Transform transform;
    private Rigidbody rigidbody;
    private bool wasAcceptable = false;
    private Bounds selfGroupBounds;
    private Bounds otherGroupBounds;
    private Vector3 selfCenter;
    private Vector3 otherCenter;
    private bool selfBoundsExpired = true;
    private bool otherBoundsExpired = true;
    // Use this for initialization
    void Start () {
        interactable = GetComponent<Interactable>();
        transform = GetComponent<Transform>();
        rigidbody = GetComponent<Rigidbody>();
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
        // if true the pulse will happen in a sawtooth pattern like this /|/|/|/|
        // else it will happen opposite like this |\|\|\|\
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
            endPointTransform.gameObject.SetActive(true);
        }
        else if (isGrabEnding)
        {
            // Detach this object from the hand
            hand.DetachObject(gameObject);
            // Call this to undo HoverLock
            hand.HoverUnlock(interactable);
            // For snapping
            if (IsWithinRangeOfCenter(endPointTransform, acceptableMetersFromEndPoint)
                && IsWithinRangeOfRotation(gameObject.transform.rotation, endPointTransform.rotation, acceptableDegreesFromEndPoint))
            {
                OnAcceptablePlacement();
                wasAcceptable = true;
                // stay in place when placed correctly
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;

                // Move to End point transform
                transform.position = endPointTransform.position;
                transform.rotation = endPointTransform.rotation;
                endPointTransform.gameObject.SetActive(false);
                
            }
            else
            {
                wasAcceptable = false;
                rigidbody.useGravity = true;
                rigidbody.isKinematic = true;
            }
        }

        if (interactable.attachedToHand != null && !isGrabEnding && endPointTransform != null)
        {
           if(IsWithinRangeOfCenter(endPointTransform, acceptableMetersFromEndPoint)
                && IsWithinRangeOfRotation(gameObject.transform.rotation, endPointTransform.rotation, acceptableDegreesFromEndPoint))
            {
                if (!wasAcceptable)
                {
                    OnAcceptablePlacement();
                    wasAcceptable = true;
                    if (snapAndDetach)
                    {
                        rigidbody.useGravity = false;
                        rigidbody.isKinematic = true;
                        // Detach this object from the hand
                        hand.DetachObject(gameObject);
                        // Call this to undo HoverLock
                        hand.HoverUnlock(interactable);
                        // Move to End point transform
                        transform.position = endPointTransform.position;
                        transform.rotation = endPointTransform.rotation;
                        endPointTransform.gameObject.SetActive(false);
                    }
                }
                else
                {
                    endPointTransform.gameObject.SetActive(true);
                }
            }
            else
            {
                wasAcceptable = false;
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
