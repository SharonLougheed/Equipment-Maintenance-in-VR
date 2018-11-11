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
    
    
    private Text[] clipboardCanvasTextItems;
    private bool isParentObjective;
    private static List<Objective> allObjectives;
    private List<Objective> childObjectives;
    private int currentChildObjectiveIndex = 0;
    private event Action CompletionEvent;
    private IObjectiveCommands objectiveCommands;

    void Awake()
    {
        childObjectives = GetChildObjectives();
        GameObject canvasGameObject = GameObject.Find("ClipboardCanvas");
        if (canvasGameObject != null)
        {
            clipboardCanvasTextItems = canvasGameObject.GetComponentsInChildren<Text>();
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
        if (childObjectives.Count > 0 && currentChildObjectiveIndex < childObjectives.Count)
        {
            childObjectives[currentChildObjectiveIndex].CompletionEvent += OnChildObjectiveCompleted;
            childObjectives[currentChildObjectiveIndex].StartNextObjective();
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
                Debug.Log("Empty Objective: \"" + title + "\" Completing immediately");
                OnObjectiveCompleted();
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
}
