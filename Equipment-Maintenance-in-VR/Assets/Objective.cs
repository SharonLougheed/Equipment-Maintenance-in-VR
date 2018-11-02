using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Objective : MonoBehaviour {

    public string title;
    public string description;
    public enum ObjectiveTypes {MoveToLocation, MoveFromLocation, None};
    [Tooltip("Select the GameObject that is the subject of this objective")]
    public GameObject subjectGameObject;
    [Tooltip("Actions applied as the objective starts")]
    public UnityEvent PreConditions;
    [Tooltip("Actions applied after this objective is completed")]
    public UnityEvent PostConditions;

    private bool isParentObjective;
    private InteractablePart part;
    private List<Objective> childObjectives;
    private int currentObjectiveIndex = 0;
    private event Action CompletionEvent;
    private ObjectiveSubject objectiveSubject;

    void Awake()
    {
        childObjectives = GetChildObjectives();
        if(subjectGameObject != null)
        {
            objectiveSubject = subjectGameObject.AddComponent<ObjectiveSubject>();
            part = subjectGameObject.GetComponent<InteractablePart>();
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
        // TODO change child selection logic
        if (childObjectives.Count > 0 && currentObjectiveIndex < childObjectives.Count)
        {
            childObjectives[currentObjectiveIndex].CompletionEvent += OnChildObjectiveCompleted;
            childObjectives[currentObjectiveIndex].StartNextObjective();
        }
        else // This scripts objective
        {
            ApplyPreConditions();
            if (objectiveSubject != null)
            {
                Debug.Log(title + " objective started!");
                objectiveSubject.CompletionEvent += OnObjectiveCompleted;
            }
            else
            {
                Debug.Log("Error: Objective \"" + title + "\" ObjectiveSubject is null. Completing immediately");
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
        if(objectiveSubject != null)
            objectiveSubject.CompletionEvent -= OnObjectiveCompleted;
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

}
