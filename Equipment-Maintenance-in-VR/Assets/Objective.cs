using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Objective : MonoBehaviour {

    public string title;
    public string description;
    public bool isCompleted = false;
    public enum ObjectiveTypes {MoveToLocation, MoveFromLocation, None};
    public enum ObjectiveStates { InProgress, NotInProgress };
    [Tooltip("Select the GameObject that is the subject of this objective")]
    public GameObject subjectGameObject;
    [Tooltip("Actions applied as the objective starts")]
    public UnityEvent PreConditions;
    [Tooltip("Actions applied after this objective is completed")]
    public UnityEvent PostConditions;
    
    
    private Text clipboardCanvasText; 
    private bool isParentObjective;
    private static List<Objective> allObjectives;
    private List<Objective> childObjectives;
    private int currentObjectiveIndex = 0;
    private event Action CompletionEvent;
    private IObjectiveCommands objectiveCommands;

    void Awake()
    {
        childObjectives = GetChildObjectives();
        GameObject canvasGameObject = GameObject.Find("ClipboardCanvas");
        if (canvasGameObject != null)
        {
            clipboardCanvasText = canvasGameObject.GetComponentInChildren<Text>();
        }
        if (subjectGameObject != null)
        {
            objectiveCommands = subjectGameObject.GetComponent<IObjectiveCommands>();
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
        if (childObjectives.Count > 0 && currentObjectiveIndex < childObjectives.Count)
        {
            childObjectives[currentObjectiveIndex].CompletionEvent += OnChildObjectiveCompleted;
            childObjectives[currentObjectiveIndex].StartNextObjective();
        }
        else // This scripts objective
        {
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
                Debug.Log("Error: Objective \"" + title + "\" Objective Subject is null. Completing immediately");
                OnObjectiveCompleted();
            }
        }
        
    }


    private void OnChildObjectiveCompleted()
    {
        childObjectives[currentObjectiveIndex].CompletionEvent -= OnChildObjectiveCompleted;
        currentObjectiveIndex++;
        StartNextObjective();
    }


    private void OnObjectiveCompleted()
    {
        isCompleted = true;
        Debug.Log("Completed objective " + title);
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
        if(clipboardCanvasText != null && allObjectives != null)
        {
            String outputText = "Generator Repair Task List:\n";
            foreach (var objective in allObjectives)
            {
                if (objective != null && (!hideEmptyObjectives || objective.subjectGameObject != null))
                {
                    outputText += (objective.isCompleted ? "☑" : "☐") + " " + objective.title + "\n";
                }
            }
            Debug.Log(outputText);
            clipboardCanvasText.text = outputText;
            
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
}
