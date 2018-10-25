using UnityEngine;
using System.Collections;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class GarageDoorSmall : MonoBehaviour {

    //public Transform	MessageUI;
    public Hand left;
    public Hand right;

    Animation doorAnim;
	bool opened=false; 
	bool allowOpen;

	// Use this for initialization
	void Start () {
		doorAnim=GetComponent<Animation>();
		//MessageUI.gameObject.SetActive(false);
	}

    // Update is called once per frame
    void Update()
    {

    }

    private void HandHoverUpdate(Hand hand)
    {
        //Debug.Log(hand.name + " is touching it...HandHoverUpdate");
        if (!opened)
        {
            if (SteamVR_Input._default.inActions.GrabPinch.GetStateDown(left.handType) || SteamVR_Input._default.inActions.GrabPinch.GetStateDown(right.handType))
            {
                opened = true;
                //MessageUI.gameObject.SetActive(false);
                doorAnim.Play("Open");
            }
        }
        else if (opened)
        {
            if (SteamVR_Input._default.inActions.GrabPinch.GetStateDown(left.handType) || SteamVR_Input._default.inActions.GrabPinch.GetStateDown(right.handType))
            {
                opened = false;
                //MessageUI.gameObject.SetActive(true);
                doorAnim.Play("Close");

            }
        }
    }

    private void OnHandHoverEnd(Hand hand)
    {
        //Debug.Log(hand.name + " is no longer touching it.");
    }

    /*
	void OnTriggerEnter(Collider other) {
		allowOpen=true;
		if (!opened) {
			MessageUI.gameObject.SetActive(true);
		}
	}

	void OnTriggerExit(Collider other) {
		allowOpen=false;
		if (!opened) {
			MessageUI.gameObject.SetActive(false);
		}
	}
    */
}
