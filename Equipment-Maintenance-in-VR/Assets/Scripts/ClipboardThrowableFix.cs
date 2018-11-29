using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class ClipboardThrowableFix : MonoBehaviour {

    public void MakeNormalThrowable()
    {
        InteractablePart interactablePart = gameObject.GetComponent<InteractablePart>();
        if(interactablePart != null)
        {
            Destroy(interactablePart);
            Throwable throwable = gameObject.AddComponent<Throwable>();
            gameObject.GetComponent<Interactable>().highlightOnHover = true;
        }
    }

}
