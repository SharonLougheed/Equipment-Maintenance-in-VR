using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Objective : MonoBehaviour {
    
    [Tooltip("Select the GameObject that is the subject of this objective")]
    public GameObject interactableObject;
    public UnityEvent StartConditions;
    public UnityEvent FinishConditions;

    private InteractablePart part;


    void Start () {
        StartObjective();
	}
	

    void StartObjective()
    {
        ApplyStartConditions();
        if (interactableObject == null)
        {
            Debug.Log("Error. Objective subject is null");
            return;
        }
        InteractablePart part = interactableObject.GetComponent<InteractablePart>();
        part.CompletionEvent += OnTaskCompleted;
    }


    private void OnTaskCompleted()
    {
        part.CompletionEvent -= OnTaskCompleted;
        ApplyFinishConditions();
    }


    private void ApplyStartConditions()
    {
        StartConditions.Invoke();
    }


    private void ApplyFinishConditions()
    {
        FinishConditions.Invoke();
    }

}
