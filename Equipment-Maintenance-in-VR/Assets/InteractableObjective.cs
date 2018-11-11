using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class InteractableObjective : MonoBehaviour, IObjectiveCommands {
    public event Action CompletionEvent;
    public bool requireTriggerPress = true;
    public bool hapticFeedback = true;
    // private Objective.ObjectiveStates objectiveState = Objective.ObjectiveStates.NotInProgress;
    public Objective.ObjectiveStates objectiveState = Objective.ObjectiveStates.NotInProgress;
    public void OnObjectiveFinish()
    {
        objectiveState = Objective.ObjectiveStates.NotInProgress;
        CompletionEvent();
    }

    public void OnObjectiveReset()
    {
        throw new NotImplementedException();
    }

    public void OnObjectiveStart()
    {
        objectiveState = Objective.ObjectiveStates.InProgress;
    }

    private void HandHoverUpdate(Hand hand)
    {
        if (objectiveState == Objective.ObjectiveStates.InProgress && (!requireTriggerPress || SteamVR_Input._default.inActions.GrabPinch.GetStateDown(hand.handType)))
        {
            //VibrateController(hand, 0.35f, 0.1f, 0.4f);
            if(hapticFeedback)
                hand.TriggerHapticPulse(0.05f, 90.0f, 0.7f);
            OnObjectiveFinish();
        }
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

}
