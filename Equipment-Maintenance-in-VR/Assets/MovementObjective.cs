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
    public float distance = 0.2f;
    private int frameCount = 0;

    private bool teleportActiveState = false;
    private ObjectiveSubject objectiveSubject;

    // TODO Find out why distance between vrCamera and teleportPoint is > 1m 
    // TODO Optionally disable teleportArea that covers entire ground to clearly indicate where they can go

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
}
