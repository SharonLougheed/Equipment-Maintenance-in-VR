using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;

public class Objective : MonoBehaviour {

    public string title;
    public string description;
    public bool isCompleted = false;
    public enum PartObjectiveTypes {MoveToLocation, MoveFromLocation, None};
    public enum AllObjetiveTypes { None, MoveToLocation, MoveFromLocation, Interaction, PlayerMovement, ToolMotion};
    public enum ObjectiveStates { InProgress, NotInProgress };
    [Tooltip("Select the GameObject that is the subject of this objective")]
    public GameObject subjectGameObject;
    [Tooltip("Specifies the type of script that the objective will look for")]
    public AllObjetiveTypes objectiveType = AllObjetiveTypes.None;
    [Header("Move To Location Settings")]
    public Transform endingLocation;
    public bool requireColliderOverlap = true;
    public bool useGravityBefore = true;
    public bool isKinematicBefore = false;
    public bool showEndPointOutline = true;
    public bool useGravityAfter = false;
    public bool isKinematicAfter = true;
    public float acceptableDegreesFromEndPoint = 20f;
    public float acceptableMetersFromEndPoint = 1f;
    public bool checkXaxis = true;
    public bool checkYaxis = true;
    public bool checkZaxis = true;
    public bool requireHandAttached = true;
    [Header("Move From Location Settings")]
    public bool onlyCompleteAfterRelease = true;
    [Header("Interaction Settings")]
    public bool requireTriggerPress = true;
    public bool hapticFeedback = true;
    [Header("Player Movement Settings")]
    public Collider vrCollider;
    public TeleportPoint teleportPoint;
    public float colliderHeight = 2.0f;
    public float colliderRadius = 0.3065555f;
    [Header("Tool Motion Settings")]
    public Transform rotationPoint;
    public ToolObjective.RotationDirections rotationDirection = ToolObjective.RotationDirections.Clockwise;
    public float requiredDegreesOfRotation = 90;
    public int numberOfRotatations = 1;
    public float rotationSpeed = 2;

    [Tooltip("Actions applied as the objective starts")]
    public UnityEvent PreConditions;
    [Tooltip("Actions applied after this objective is completed")]
    public UnityEvent PostConditions;
    
    
    private Text[] clipboardCanvasTextItems;
    private bool isParentObjective;
    private static List<Objective> allObjectives;
    private List<Objective> childObjectives;
    private int currentChildObjectiveIndex = 0;
    private event Action CompletionEvent;
    private IObjectiveCommands[] objectiveCommandsList;
    private IObjectiveCommands objectiveCommands;

    void Awake()
    {
        childObjectives = GetChildObjectives();
        GameObject canvasGameObject = GameObject.Find("ClipboardCanvas");
        if (canvasGameObject != null)
        {
            clipboardCanvasTextItems = canvasGameObject.GetComponentsInChildren<Text>();
        }
    }

    void Start () {
        Objective[] parentObjectives = GetComponentsInParent<Objective>();

        if (parentObjectives != null && parentObjectives.Length > 1)
        {
            isParentObjective = false;
            return;
        }
        else
        {
            //Must be parent objective
            isParentObjective = true;
            allObjectives = GetOrderedObjectives();
            DisplayObjectives();
            StartNextObjective();
        }
    }
	
    private List<Objective> GetChildObjectives()
    {
        List<Objective> objectives = new List<Objective>();
        foreach(var child in GetComponentsInChildren<Objective>())
        {
            if(child.gameObject != gameObject && child.gameObject != gameObject.transform.parent)
            {
                objectives.Add(child);
            }
        }
        return objectives;
    }
    public void StartNextObjective()
    {
        /* Go through each child objective, only move to the next one when the current one is complete
         * Else there are no child objective left to complete so start this objective
         */
        if (childObjectives.Count > 0 && currentChildObjectiveIndex < childObjectives.Count)
        {
            childObjectives[currentChildObjectiveIndex].CompletionEvent += OnChildObjectiveCompleted;
            childObjectives[currentChildObjectiveIndex].StartNextObjective();
        }
        else // This scripts objective
        {
            ApplyObjectiveSettings();

            if (objectiveCommands != null)
            {
                Debug.Log(title + " objective started!");
                objectiveCommands.OnObjectiveStart();
                ApplyPreConditions();
                objectiveCommands.CompletionEvent += OnObjectiveCompleted;
            }
            else
            {
                ApplyPreConditions();
                Debug.Log("Empty Objective: \"" + title + "\" Completing immediately");
                OnObjectiveCompleted();
            }
        }
    }

    private void ApplyObjectiveSettings()
    {
        if (subjectGameObject != null)
        {
            switch (objectiveType)
            {
                case AllObjetiveTypes.MoveToLocation:
                    InteractablePart partMoveToLocation = subjectGameObject.GetComponent<InteractablePart>();
                    if (partMoveToLocation == null)
                        partMoveToLocation = subjectGameObject.AddComponent<InteractablePart>();
                    partMoveToLocation.ObjectiveType = PartObjectiveTypes.MoveToLocation;
                    partMoveToLocation.endingLocation = endingLocation;
                    partMoveToLocation.showEndPointOutline = showEndPointOutline;
                    partMoveToLocation.useGravityBefore = useGravityBefore;
                    partMoveToLocation.isKinematicBefore = isKinematicBefore;
                    partMoveToLocation.isKinematicAfter = isKinematicAfter;
                    partMoveToLocation.useGravityAfter = useGravityAfter;

                    partMoveToLocation.acceptableDegreesFromEndPoint = acceptableDegreesFromEndPoint;
                    partMoveToLocation.acceptableMetersFromEndPoint = acceptableMetersFromEndPoint;
                    partMoveToLocation.requireColliderOverlap = requireColliderOverlap;
                    partMoveToLocation.checkXaxis = checkXaxis;
                    partMoveToLocation.checkYaxis = checkYaxis;
                    partMoveToLocation.checkZaxis = checkZaxis;
                    partMoveToLocation.requireHandAttached = requireHandAttached;
                    objectiveCommands = partMoveToLocation;
                    break;
                case AllObjetiveTypes.MoveFromLocation:
                    InteractablePart partMoveFromLocation = subjectGameObject.GetComponent<InteractablePart>();
                    if (partMoveFromLocation == null)
                        partMoveFromLocation = subjectGameObject.AddComponent<InteractablePart>();
                    partMoveFromLocation.ObjectiveType = PartObjectiveTypes.MoveFromLocation;
                    partMoveFromLocation.endingLocation = endingLocation;
                    partMoveFromLocation.showEndPointOutline = showEndPointOutline;
                    partMoveFromLocation.useGravityBefore = useGravityBefore;
                    partMoveFromLocation.isKinematicBefore = isKinematicBefore;
                    partMoveFromLocation.isKinematicAfter = isKinematicAfter;
                    partMoveFromLocation.useGravityAfter = useGravityAfter;

                    partMoveFromLocation.acceptableDegreesFromEndPoint = acceptableDegreesFromEndPoint;
                    partMoveFromLocation.acceptableMetersFromEndPoint = acceptableMetersFromEndPoint;
                    partMoveFromLocation.requireColliderOverlap = requireColliderOverlap;
                    partMoveFromLocation.checkXaxis = checkXaxis;
                    partMoveFromLocation.checkYaxis = checkYaxis;
                    partMoveFromLocation.checkZaxis = checkZaxis;
                    partMoveFromLocation.requireHandAttached = requireHandAttached;
                    
                    objectiveCommands = partMoveFromLocation;
                    break;
                case AllObjetiveTypes.Interaction:
                    InteractableObjective interactableObjective = subjectGameObject.GetComponent<InteractableObjective>();
                    if (interactableObjective == null)
                        interactableObjective = subjectGameObject.AddComponent<InteractableObjective>();
                    interactableObjective.requireTriggerPress = requireTriggerPress;
                    interactableObjective.hapticFeedback = hapticFeedback;
                    objectiveCommands = interactableObjective;
                    break;
                case AllObjetiveTypes.PlayerMovement:
                    MovementObjective movementObjective = subjectGameObject.GetComponent<MovementObjective>();
                    if (movementObjective == null)
                        movementObjective = subjectGameObject.AddComponent<MovementObjective>();
                    movementObjective.vrCollider = vrCollider == null ? GameObject.Find("HeadCollider").GetComponent<Collider>() : vrCollider;
                    movementObjective.teleportPoint = teleportPoint;
                    movementObjective.colliderRadius = colliderRadius;
                    movementObjective.colliderHeight = colliderHeight;
                    objectiveCommands = movementObjective;
                    break;
                case AllObjetiveTypes.ToolMotion:
                    ToolObjective toolObjective = subjectGameObject.GetComponent<ToolObjective>();
                    if (toolObjective == null)
                        toolObjective = subjectGameObject.AddComponent<ToolObjective>();
                    toolObjective.rotationPoint = rotationPoint;
                    toolObjective.rotationDirection = rotationDirection;
                    toolObjective.requiredDegreesOfRotation = requiredDegreesOfRotation;
                    toolObjective.numberOfRotatations = numberOfRotatations;
                    toolObjective.rotationSpeed = rotationSpeed;
                    objectiveCommands = toolObjective;
                    break;
                case AllObjetiveTypes.None:
                default:
                    Debug.Log("Objective " + title + " has no type.");
                    objectiveCommands = null;
                    break;
            }
        }
    }
    private void OnChildObjectiveCompleted()
    {
        childObjectives[currentChildObjectiveIndex].CompletionEvent -= OnChildObjectiveCompleted;
        currentChildObjectiveIndex++;
        StartNextObjective();
    }


    private void OnObjectiveCompleted()
    {
        isCompleted = true;
        Debug.Log("Completed objective " + title);
        DestroyHighlights();
        if (objectiveCommands != null)
        {
            objectiveCommands.CompletionEvent -= OnObjectiveCompleted;
        }
        ApplyPostConditions();
        if(CompletionEvent != null)
            CompletionEvent();
        DisplayObjectives();
    }


    private void ApplyPreConditions()
    {
        //Debug.Log(title + " Applying pre-conditions");
        PreConditions.Invoke();
    }


    private void ApplyPostConditions()
    {
        //Debug.Log(title + " Applying post-conditions");
        PostConditions.Invoke();
    }

    private void DisplayObjectives(bool hideEmptyObjectives = true)
    {
        if (clipboardCanvasTextItems != null && clipboardCanvasTextItems.Length > 0 && allObjectives != null)
        {
            int numVisibleObjectives = clipboardCanvasTextItems.Length;
            int currentObjectiveIndex = 0;
            String debugClipboardText = "Debug Clipboard Text:\n";

            ClearClipboard();
            while (allObjectives[currentObjectiveIndex].isCompleted && currentObjectiveIndex < allObjectives.Count - 1)
                currentObjectiveIndex++;
            int outputStartingIndex = ((currentObjectiveIndex) / numVisibleObjectives) * numVisibleObjectives;
            for (int i = outputStartingIndex, clipboardItemIndex = 0; i < Math.Min(allObjectives.Count, outputStartingIndex + numVisibleObjectives); i++, clipboardItemIndex++)
            {
                if (allObjectives[i] != null && (!hideEmptyObjectives || allObjectives[i].subjectGameObject != null))
                { 
                    clipboardCanvasTextItems[clipboardItemIndex].text =  (allObjectives[i].isCompleted ? "☑" : "☐") + " " + allObjectives[i].title;
                    debugClipboardText += (allObjectives[i].isCompleted ? "☑" : "☐") + " " + allObjectives[i].title + "\n";
                }
            }
            //Debug.Log(debugClipboardText);
        }       
    }

    private void ClearClipboard()
    {
        if(clipboardCanvasTextItems != null)
        {
            foreach (Text item in clipboardCanvasTextItems)
                item.text = "";
        }
    }
    private List<Objective> GetOrderedObjectives()
    {
        Stack<Transform> stack = new Stack<Transform>();
        List<Objective> objectiveList = new List<Objective>();
        Dictionary<int, bool> visited = new Dictionary<int, bool>();

        stack.Push(this.transform);
        while (stack.Count != 0)
        {
            Transform parentTransform = stack.Pop();
            bool isVisited = false;
            visited.TryGetValue(parentTransform.GetInstanceID(), out isVisited);
            if (!isVisited)
            {
                visited.Add(parentTransform.GetInstanceID(), true);
                Objective objective = parentTransform.gameObject.GetComponent<Objective>();
                if(objective != null)
                {
                    objectiveList.Insert(0, objective);
                }
                foreach (Transform child in parentTransform)
                {
                    if (child.GetComponent<Objective>() != null)
                        stack.Push(child);
                }
            }

        }
        return objectiveList;
    }

    void DestroyHighlights()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach(var obj in rootObjects)
        {
            if(obj.name == "Highlighter")
            {
                Destroy(obj);
            }
        }
    }
}
