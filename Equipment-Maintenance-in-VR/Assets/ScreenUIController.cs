using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenUIController : MonoBehaviour {

    public GameObject trackingObject;
    private Canvas screenCanvas;
    private Text topText;
    private bool isActive = false;
    public float textDist = .75f;
    public float verticalDist = 3.0f;
    public float horizontalDist = 0.0f;
    
	// Use this for initialization
	void Start () {
        
        screenCanvas = GameObject.Find("ScreenCanvas").GetComponent<Canvas>();
        topText = GameObject.Find("TopText").GetComponent<Text>();
    }
	
	// Update is called once per frame
	void Update () {
 
        screenCanvas.transform.position =  trackingObject.transform.position + trackingObject.transform.forward * textDist + trackingObject.transform.up * -(verticalDist) + trackingObject.transform.right * horizontalDist ;
        screenCanvas.transform.rotation = trackingObject.transform.rotation;
    }

    public void SetTopText(string text)
    {
        topText.text = text;
        ShowTopText();
    }

    public void ClearTopText()
    {
        topText.text = "";
        HideTopText();
    }

    public void ShowTopText()
    {
        topText.gameObject.SetActive(true);
    }

    public void HideTopText()
    {
        topText.gameObject.SetActive(false);
    }

}
