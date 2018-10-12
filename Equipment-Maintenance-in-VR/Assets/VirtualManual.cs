using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace Valve.VR.InteractionSystem
{

    public class VirtualManual : MonoBehaviour {

        public SteamVR_Action_Boolean manualAction;

        public Hand hand;

        private Canvas manualCanvasObject;
       
        public bool activeOnStart = false;

        // TODO Add functionality to change page of manual

        private void OnEnable()
        {
            
            if (hand == null)
                hand = this.GetComponent<Hand>();

            if (manualAction== null)
            {
                Debug.LogError("No action assigned");
                return;
            }

            manualAction.AddOnChangeListener(OnManualActionChange, hand.handType);
        }

        private void OnDisable()
        {
            if (manualAction != null)
                manualAction.RemoveOnChangeListener(OnManualActionChange, hand.handType);
        }

        private void OnManualActionChange(SteamVR_Action_In actionIn)
        {
            if (manualAction.GetStateDown(hand.handType))
            {
                Debug.Log("Opening Virtual Manual");
                manualCanvasObject.gameObject.SetActive(!manualCanvasObject.isActiveAndEnabled);
            }
        }


        // Use this for initialization
        void Start () {
            // TODO use better method of getting the manual canvas
            manualCanvasObject = GameObject.Find("ManualCanvas").GetComponent<Canvas>();
            manualCanvasObject.gameObject.SetActive(activeOnStart);

        }
	
	    // Update is called once per frame
	    void Update () {
		
	    }
    }
}