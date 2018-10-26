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
    public GameObject interactableObject;
    [Tooltip("Actions applied before this objective is completed")]
    public UnityEvent PreConditions;
    [Tooltip("Actions applied after this objective is completed")]
    public UnityEvent PostConditions;

    private InteractablePart part;
    private List<Objective> childObjectives;
    private int currentObjectiveIndex = 0;
    private event Action CompletionEvent;

    void Awake()
    {
        childObjectives = GetChildObjectives();
        if(interactableObject != null)
            part = interactableObject.GetComponent<InteractablePart>();
    }


    void Start () {
        Objective[] parentObjectives = GetComponentsInParent<Objective>();

        if (parentObjectives != null && parentObjectives.Length > 1)
            return;
        // Must be parent objective
        StartNextObjective();
	}
	
    private List<Objective> GetChildObjectives()
    {
        List<Objective> objectives = new List<Objective>();
        foreach(var child in GetComponentsInChildren<Objective>())
        {
            if(child.gameObject != gameObject && child.gameObject != gameObject.transform.parent)
            {
                objectives.Add(child);
                Debug.Log("Child: " + child.title);
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
            if (interactableObject != null && part != null)
            {
                ApplyPreConditions();
                Debug.Log(title + " objective started!");
                part.CompletionEvent += OnObjectiveCompleted;
            }
            else
            {
                //Debug.Log("Error. Objective subject is null");
                // FOR TESTING
                Debug.Log(title + " objective started! (no object)");
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
        if(part != null)
            part.CompletionEvent -= OnObjectiveCompleted;
        ApplyPostConditions();
        if(CompletionEvent != null)
            CompletionEvent();
    }


    private void ApplyPreConditions()
    {
        PreConditions.Invoke();
    }


    private void ApplyPostConditions()
    {
        Debug.Log(title + " objective completed!");
        PostConditions.Invoke();
    }

}
