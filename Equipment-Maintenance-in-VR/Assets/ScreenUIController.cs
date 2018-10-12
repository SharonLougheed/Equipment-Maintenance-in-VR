using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenUIController : MonoBehaviour {

    private Canvas screenCanvas;
    private Text topText;

	// Use this for initialization
	void Start () {
        screenCanvas = GameObject.Find("ScreenCanvas").GetComponent<Canvas>();
        topText = GameObject.Find("TopText").GetComponent<Text>();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void SetTopText(string text)
    {
        topText.text = text;
    }

    public void ClearTopText()
    {
        topText.text = "";
    }

    public void ShowTopText()
    {
        topText.gameObject.SetActive(false);
    }

    public void HideTopText()
    {
        topText.gameObject.SetActive(true);
    }

}
