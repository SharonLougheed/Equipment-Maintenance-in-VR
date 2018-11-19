using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenTextController : MonoBehaviour {

    public Canvas screenCanvas;
    public Text screenText;
    public string defaultDisplayMessage = "";
    public float blinkTimerSeconds = 1.0f;
    private bool textShowing = false;
    public bool blinkScreenText = false;
	// Use this for initialization
	void Start () {
     
        if(screenCanvas == null || screenText == null)
        {
            Debug.Log("Could not find sceen text in canvas");
        }
        else
        {
            screenText.text = defaultDisplayMessage;
            StartCoroutine(BlinkText());
        }

	}
	
    public void ClearScreenText()
    {
        screenText.text = "";
    }

    public void ShowScreenText(string text)
    {
        if(screenText != null)
        {
            screenText.text = text;
        }
    }

    IEnumerator BlinkText()
    {
        while (blinkScreenText)
        {
            screenCanvas.gameObject.SetActive(!screenCanvas.isActiveAndEnabled);
            yield return new WaitForSeconds(blinkTimerSeconds);
        }
    }
	// Update is called once per frame
	void Update () {
        if (screenText != null)
        {
            // TODO do something more useful here
        }
	}
}
