using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveSubject : MonoBehaviour {

    
    public Objective.ObjectiveStates objectiveState = Objective.ObjectiveStates.NotInProgress;
    public event Action CompletionEvent;

    public bool NotifyCompletion()
    {
        if(CompletionEvent != null)
        {
            CompletionEvent();
            return true;
        }
        return false;
    }
}
