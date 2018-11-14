using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Interactable))]
public class TestRotation : MonoBehaviour, IObjectiveCommands {

    // Use this for initialization
    public Transform rotationPoint;
    public Transform hand;
    public float requiredDegreesOfRotation = 90;
    private Vector3 startingPosition;
    private float totalDegreesRotated = 0;
    public Objective.ObjectiveStates objectiveState = Objective.ObjectiveStates.NotInProgress;
    public event Action CompletionEvent;
    
    private bool highlightOnHover = false;
    
    public void OnObjectiveFinish()
    {
        objectiveState = Objective.ObjectiveStates.NotInProgress;
        highlightOnHover = false;
        GetComponent<Interactable>().highlightOnHover = false;
    }

    public void OnObjectiveReset()
    {
        throw new NotImplementedException();
    }

    public void OnObjectiveStart()
    {
        highlightOnHover = true;
        GetComponent<Interactable>().highlightOnHover = true;
        objectiveState = Objective.ObjectiveStates.InProgress;
    }

    void Start () {
        startingPosition = transform.position;
        GetComponent<Interactable>().highlightOnHover = highlightOnHover;
        foreach (Transform child in transform)
        {
            if (child.name == "rotationPoint")
            {
                rotationPoint = child;
                break;
            }
        }
	}

    void HandHoverUpdate(Hand hand)
    {
        GrabTypes startingGrabType = hand.GetGrabStarting();
        if (objectiveState == Objective.ObjectiveStates.InProgress)
        {

        }
    }
	// Update is called once per frame
	void Update () {
     
        if (rotationPoint != null)
        {
            Vector3 handPosition = hand.position;
            Vector3 toolPosition = transform.position;
            handPosition.y= toolPosition.y;
            Vector3 handToTool = handPosition - toolPosition;
            Debug.DrawRay(handPosition, -handToTool);
            float angle = Vector3.Angle(handToTool, -transform.right);
            Vector3 cross = Vector3.Cross(handToTool, -transform.right);
            if(totalDegreesRotated < requiredDegreesOfRotation)
            if(angle > 1)
            {
                if (cross.y > 0) angle = -angle;
                Debug.Log("Angle between hand and tool: " + angle);

                transform.RotateAround(rotationPoint.position, Vector3.up, angle * Time.deltaTime);
                totalDegreesRotated += angle * Time.deltaTime;
            }
        }
	}
}
