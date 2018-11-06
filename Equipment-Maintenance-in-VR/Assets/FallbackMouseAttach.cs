using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
public class FallbackMouseAttach : MonoBehaviour {

    private Hand fallbackHand;
    public bool restoreOriginalParent = false;
    public const Hand.AttachmentFlags fallbackAttachmentFlags = Hand.AttachmentFlags.ParentToHand |
                                                              //Hand.AttachmentFlags.DetachOthers |
                                                              //Hand.AttachmentFlags.DetachFromOtherHand |
                                                                Hand.AttachmentFlags.TurnOnKinematic |
                                                              Hand.AttachmentFlags.SnapOnAttach;
    private bool wasJustAttached = false;
    // Use this for initialization
    void Start () {
        fallbackHand = GetComponent<Hand>();
        // TODO set Hand.otherHand
	}
	
	// Update is called once per frame
	void Update () {
		if(fallbackHand != null)
        {
            if (Input.GetMouseButtonDown(0) && !wasJustAttached)  // Primary mouse button
            {
                Debug.Log("FallbackNoVR: " + fallbackHand.noSteamVRFallbackCamera);
                if (fallbackHand.AttachedObjects.Count == 0 && fallbackHand.hoveringInteractable != null)
                {
                    Debug.Log("mock attach: " + fallbackHand.hoveringInteractable.gameObject.name);
                    fallbackHand.AttachObject(fallbackHand.hoveringInteractable.gameObject, GrabTypes.Scripted, fallbackAttachmentFlags);
                    wasJustAttached = true;
                }
            }
            else if(Input.GetMouseButtonDown(0))
            {
                wasJustAttached = false;
                if(fallbackHand.AttachedObjects.Count > 0)
                {
                    Debug.Log("mock detach");
                    fallbackHand.DetachObject(fallbackHand.AttachedObjects[0].attachedObject, restoreOriginalParent);
                }
            }
            
        }
	}
}
