using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Objective : MonoBehaviour {

    public string title;
    public string description;
    public bool isCompleted;
    public enum ObjectiveTypes {MoveToLocation, MoveFromLocation, None};
    public enum ObjectiveStates { InProgress, NotInProgress };
    [Tooltip("Select the GameObject that is the subject of this objective")]
    public GameObject subjectGameObject;
    [Tooltip("Actions applied as the objective starts")]
    public UnityEvent PreConditions;
    [Tooltip("Actions applied after this objective is completed")]
    public UnityEvent PostConditions;
    
    
    private Canvas clipboardCanvas; 
    private bool isParentObjective;
    private static Objective[] allObjectives;
    private List<Objective> childObjectives;
    private int currentObjectiveIndex = 0;
    private event Action CompletionEvent;
    private IObjectiveCommands objectiveCommands;

    void Awake()
    {
        childObjectives = GetChildObjectives();
        if(subjectGameObject != null)
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
            allObjectives = gameObject.GetComponentsInChildren<Objective>();
            Debug.Log("All Objectives: " + allObjectives.Length);
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
                DisplayObjectives();
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
        Debug.Log("Completed objective " + title);
        if (objectiveCommands != null)
        {
            objectiveCommands.CompletionEvent -= OnObjectiveCompleted;
        }
        ApplyPostConditions();
        if(CompletionEvent != null)
            CompletionEvent();
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

    private void DisplayObjectives()
    {
        if(allObjectives != null)
        {
            foreach (var objective in allObjectives)
            {
                Debug.Log(objective.title + ": " + (isCompleted ? "Completed" : "Incomplete"));
            }
        }       
    }
}
