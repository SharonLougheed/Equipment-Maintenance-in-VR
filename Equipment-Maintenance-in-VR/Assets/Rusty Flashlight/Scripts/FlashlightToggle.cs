using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using Valve.VR;
using Valve.VR.InteractionSystem;


public class FlashlightToggle : MonoBehaviour
{
    public GameObject lightGO; //light gameObject to work with
    private bool isOn = true; //is flashlight on or off?
    public Hand left;
    public Hand right;


    // Use this for initialization
    void Start()
    {
        //set default off
        lightGO.SetActive(isOn);
    }

    // Update is called once per frame
    void Update()
    {

    }
    void HandAttachedUpdate(Hand hand)
    {
        //Debug.Log("holding the object now.");
        //toggle flashlight on key down
        if (SteamVR_Input._default.inActions.Teleport.GetStateDown(hand.handType))
        {
            //toggle light
            isOn = !isOn;
            //turn light on
            if (isOn)
            {
                lightGO.SetActive(true);
            }
            //turn light off
            else
            {
                lightGO.SetActive(false);

            }
        }
    }
}
