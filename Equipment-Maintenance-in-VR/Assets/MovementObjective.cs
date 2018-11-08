using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class MovementObjective : MonoBehaviour, ObjectiveCommands {

    public Camera vrCamera;
    public TeleportPoint teleportPoint;
    public Hand hand;
    public SteamVR_Action_Boolean teleportAction;
    public int frameSkip = 15;
    public float distance = 0.2f;
    private int frameCount = 0;

    private bool playerDetected = false;
    private bool teleportActiveState = false;
    private ObjectiveSubject objectiveSubject;

    public void OnObjectiveReset()
    {
        throw new System.NotImplementedException();
    }

        
    public void OnObjectiveStart()
    {
        teleportActiveState = true;
        teleportPoint.gameObject.SetActive(teleportActiveState);
        if(teleportAction != null)
        {
            teleportAction.AddOnChangeListener(OnTeleportActionChange, hand.handType);
        }
    }

    private void OnTeleportActionChange(SteamVR_Action_In actionIn)
    {
        var tempTransform1 = vrCamera.transform.position;
        tempTransform1.y = 0;
        var tempTransform2 = teleportPoint.transform.position;
        tempTransform2.y = 0;

        if (Vector3.Distance(tempTransform1, tempTransform2) <= distance)
        {
            Debug.Log("Player detected on teleport point");
            playerDetected = true;
            GetComponent<ObjectiveSubject>().NotifyCompletion();
            teleportAction.RemoveOnChangeListener(OnTeleportActionChange, hand.handType);
        }
    }
    public void OnObjectiveFinish()
    {
        if(teleportAction != null)
        {
            teleportAction.RemoveOnChangeListener(OnTeleportActionChange, hand.handType);
        }
        throw new System.NotImplementedException();
    }

    
    // Use this for initialization
    void Start () {
        ObjectiveSubject objectiveSubject = GetComponent<ObjectiveSubject>();
        teleportPoint.gameObject.SetActive(teleportActiveState);
        
    }
    
    void Update()
    {
        // TODO move this from the update to an event, triggering everytime the user releases the teleport buttton
        //if (GetComponent<ObjectiveSubject>() != null && GetComponent<ObjectiveSubject>().objectiveState == Objective.ObjectiveStates.InProgress)
        //{
        //    if (!playerDetected && frameCount++ % frameSkip == 0)
        //    {
        //        frameCount = 1;
        //        var tempTransform1 = vrCamera.transform.position;
        //        tempTransform1.y = 0;
        //        var tempTransform2 = teleportPoint.transform.position;
        //        tempTransform2.y = 0;

        //        if (Vector3.Distance(tempTransform1, tempTransform2) <= distance)
        //        {
        //            Debug.Log("Player detected on teleport point");
        //            playerDetected = true;
        //            GetComponent<ObjectiveSubject>().NotifyCompletion();
        //        }
        //    }
        //}
    }
}
