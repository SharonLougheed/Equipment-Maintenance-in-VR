using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class MovementObjective : MonoBehaviour, IObjectiveCommands {


    public Collider vrCollider;
    public TeleportPoint teleportPoint;
    public float colliderHeight = 2.0f;
    public float colliderRadius = 0.3065555f;
    public event Action CompletionEvent;
    private CapsuleCollider teleporterCollider;
    private bool teleportActiveState = false;
    private Objective.ObjectiveStates objectiveState = Objective.ObjectiveStates.NotInProgress;


    public void OnObjectiveReset()
    {
        throw new System.NotImplementedException();
    }

        
    public void OnObjectiveStart()
    {
        objectiveState = Objective.ObjectiveStates.InProgress;
        teleporterCollider = gameObject.AddComponent<CapsuleCollider>();
        teleporterCollider.center = new Vector3(teleportPoint.transform.localPosition.x, teleportPoint.transform.localPosition.y + colliderHeight / 2, teleportPoint.transform.localPosition.z);
        teleporterCollider.radius = colliderRadius;
        teleporterCollider.height = colliderHeight;
        teleporterCollider.direction = 1; // Y-axis
        teleporterCollider.isTrigger = true;
        teleportActiveState = true;
        teleportPoint.gameObject.SetActive(teleportActiveState);
        teleportPoint.markerActive = teleportActiveState;

    }


    public void OnObjectiveFinish()
    {
        Destroy(teleporterCollider);
        teleportActiveState = false;
        teleportPoint.markerActive = false;
        teleportPoint.gameObject.SetActive(false);
        objectiveState = Objective.ObjectiveStates.NotInProgress;
        if(CompletionEvent != null)
            CompletionEvent();
    }

    void Start()
    {
        teleportPoint.gameObject.SetActive(teleportActiveState);
        teleportPoint.markerActive = teleportActiveState;
    }


    void OnTriggerEnter(Collider other)
    {
        if(objectiveState == Objective.ObjectiveStates.InProgress)
        {
            if(other.GetInstanceID() == vrCollider.GetInstanceID()) {
                OnObjectiveFinish();
            }
        }
    }
}
