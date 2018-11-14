using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Interactable))]
public class ToolObjective : MonoBehaviour, IObjectiveCommands {

    // Use this for initialization
    public enum RotationDirections { Clockwise, Counterclockwise};
    public Transform rotationPoint;
    public RotationDirections rotationDirection = RotationDirections.Clockwise;
    public int numberOfRotatations = 1;
    public float requiredDegreesOfRotation = 90;

    private Transform startingTransform;
    private float totalDegreesRotated = 0;
    public Objective.ObjectiveStates objectiveState = Objective.ObjectiveStates.NotInProgress;
    public event Action CompletionEvent;
    private bool isGrabbingTool;
    private bool finishedRotationRound = false;
    private bool highlightOnHover = true;
    private int rotationCount = 0;
    private GameObject dummyObject;
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
        dummyObject = new GameObject();
        startingTransform = transform;
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
     
        if (objectiveState == Objective.ObjectiveStates.InProgress)
        {

            if (finishedRotationRound || hand.IsGrabEnding(dummyObject))
            {
                //Debug.Log("Ending Grab");
                finishedRotationRound = false;
                isGrabbingTool = false;
                hand.HoverUnlock(GetComponent<Interactable>());
                hand.DetachObject(dummyObject);
            }
            GrabTypes startingGrabType = hand.GetGrabStarting();

            if(startingGrabType == GrabTypes.Pinch)
            {
                //Debug.Log("Starting Grab");
                hand.HoverLock(GetComponent<Interactable>());
                hand.AttachObject(dummyObject, startingGrabType);
                isGrabbingTool = true;
            }

            if (isGrabbingTool)
            {
                // is grabbing
                //Debug.Log("Grabbing");
                if (rotationPoint != null)
                {
                    Vector3 handPosition = hand.transform.position;
                    Vector3 toolPosition = transform.position;
                    handPosition.y = toolPosition.y;
                    Vector3 handToTool = handPosition - toolPosition;
                    Debug.DrawRay(handPosition, -handToTool);
                    float angle = Vector3.Angle(handToTool, -transform.right);
                    Vector3 cross = Vector3.Cross(handToTool, -transform.right);
                    if (angle > 1)
                    {
                        if (cross.y > 0) angle = -angle;
                        Debug.Log("Angle between hand and tool: " + angle);
                        transform.RotateAround(rotationPoint.position, Vector3.up, angle * Time.deltaTime);
                        totalDegreesRotated += angle * Time.deltaTime;
                    }
                    if (totalDegreesRotated < requiredDegreesOfRotation)
                    {
                        
                    }
                    else
                    {
                        rotationCount += 1;
                        if(rotationCount < numberOfRotatations)
                        {
                            // reset position to start
                            totalDegreesRotated = 0;
                            isGrabbingTool = false;
                            finishedRotationRound = true;
                            transform.position = startingTransform.position;
                            transform.rotation = startingTransform.rotation;
                        }
                        else
                        {
                            OnObjectiveFinish();
                        }
                        
                    }
                        
                }
            }
        }
        //Debug.Log(Valve.VR.SteamVR_Input._default.inActions.GrabPinch.GetStateDown(hand.handType));
    }

    void OnHandHoverEnd(Hand hand)
    {

    }
	// Update is called once per frame
	void Update () {

        //if (rotationPoint != null)
        //{
        //    Vector3 handPosition = hand.position;
        //    Vector3 toolPosition = transform.position;
        //    handPosition.y= toolPosition.y;
        //    Vector3 handToTool = handPosition - toolPosition;
        //    Debug.DrawRay(handPosition, -handToTool);
        //    float angle = Vector3.Angle(handToTool, -transform.right);
        //    Vector3 cross = Vector3.Cross(handToTool, -transform.right);
        //    if(totalDegreesRotated < requiredDegreesOfRotation)
        //    if(angle > 1)
        //    {
        //        if (cross.y > 0) angle = -angle;
        //        Debug.Log("Angle between hand and tool: " + angle);

        //        transform.RotateAround(rotationPoint.position, Vector3.up, angle * Time.deltaTime);
        //        totalDegreesRotated += angle * Time.deltaTime;
        //    }
        //}

        //if (true || objectiveState == Objective.ObjectiveStates.InProgress)
        //{
        //    if(handRef == null)
        //    {
        //        return;
        //    }

        //    if (!isGrabbingTool && Valve.VR.SteamVR_Input._default.inActions.GrabPinch.GetStateDown(handRef.handType))
        //    {
        //        // start to grab the tool
        //        Debug.Log("Starting Grab");
        //        handRef.HoverLock(GetComponent<Interactable>());
        //        isGrabbingTool = true;

        //    }
        //    else if (isGrabbingTool && !Valve.VR.SteamVR_Input._default.inActions.GrabPinch.GetStateDown(handRef.handType))
        //    {
        //        // was grabbing and is letting go now
        //        Debug.Log("Ending Grab");
        //        isGrabbingTool = false;
        //        handRef.HoverUnlock(GetComponent<Interactable>());
        //    }

        //    if (isGrabbingTool && Valve.VR.SteamVR_Input._default.inActions.GrabPinch.GetStateDown(handRef.handType))
        //    {
        //        // is grabbing 
        //        Debug.Log("Grabbing");
        //        if (rotationPoint != null)
        //        {
        //            Vector3 handPosition = handRef.transform.position;
        //            Vector3 toolPosition = transform.position;
        //            handPosition.y = toolPosition.y;
        //            Vector3 handToTool = handPosition - toolPosition;
        //            Debug.DrawRay(handPosition, -handToTool);
        //            float angle = Vector3.Angle(handToTool, -transform.right);
        //            Vector3 cross = Vector3.Cross(handToTool, -transform.right);
        //            if (totalDegreesRotated < requiredDegreesOfRotation)
        //                if (angle > 1)
        //                {
        //                    if (cross.y > 0) angle = -angle;
        //                    Debug.Log("Angle between hand and tool: " + angle);

        //                    transform.RotateAround(rotationPoint.position, Vector3.up, angle * Time.deltaTime);
        //                    totalDegreesRotated += angle * Time.deltaTime;
        //                }
        //        }
        //    }
        //}
    }
}
