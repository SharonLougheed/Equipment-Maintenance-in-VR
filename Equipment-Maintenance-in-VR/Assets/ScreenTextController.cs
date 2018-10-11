using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenTextController : MonoBehaviour {

    public Canvas screenCanvas;
    public Text screenText;
    public string defaultText = @"Fault Code:
2343 - 
Fuel Filter Pressure High Above Normal";
    public float blinkTimerSeconds = 1.0f;
    private bool textShowing = false;

	// Use this for initialization
	void Start () {
       
        print("hello");
        if(screenCanvas == null || screenText == null)
        {
            Debug.Log("Could not find sceen text in canvas");
        }
        else
        {
            screenText.text = defaultText;
            StartCoroutine(BlinkText());
        }

	}
	
    IEnumerator BlinkText()
    {
        while (true)
        {
            screenCanvas.gameObject.SetActive(!screenCanvas.isActiveAndEnabled);
            print("BLINK");
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
