using UnityEngine;
using System.Collections;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class GarageDoorBig : MonoBehaviour {

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
        if (!opened)
        {
            // TODO
            if (SteamVR_Input._default.inActions.GrabPinch.GetStateDown(hand.handType))
            {
                opened = true;
                //MessageUI.gameObject.SetActive(false);
                doorAnim.Play("Open");
            }
        }
        else if (opened)
        {
            if (SteamVR_Input._default.inActions.GrabPinch.GetStateDown(hand.handType))
            {
                opened = false;
                //MessageUI.gameObject.SetActive(true);
                doorAnim.Play("Close");

            }
        }
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
