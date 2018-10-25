using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenUIController : MonoBehaviour {

    public GameObject trackingObject;
    private Canvas screenCanvas;
    private Text topText;
    public float textDist = .75f;
    public float verticalDist = 3.0f;
    public float horizontalDist = 0.0f;
	public TextMesh text3D;
	public bool testing3D = false;

	// Use this for initialization
	void Start () {
        
        screenCanvas = GameObject.Find("ScreenCanvas").GetComponent<Canvas>();
        topText = GameObject.Find("TopText").GetComponent<Text>();
		if (testing3D)
			topText.gameObject.SetActive(false);
	}
	
	// Update is called once per frame
	void Update () {
		if (!testing3D)
		{
			screenCanvas.transform.position = trackingObject.transform.position + trackingObject.transform.forward * textDist + trackingObject.transform.up * -(verticalDist) + trackingObject.transform.right * horizontalDist ;
			screenCanvas.transform.rotation = trackingObject.transform.rotation;
		}
    }

    public void SetTopText(string text)
    {
		if (testing3D)
		{
			text3D.text = text;
			text3D.transform.position = trackingObject.transform.position + trackingObject.transform.forward * textDist + trackingObject.transform.up * -(verticalDist) + trackingObject.transform.right * horizontalDist;
			text3D.transform.rotation = trackingObject.transform.rotation;
		}
		else
			topText.text = text;
		ShowTopText();
    }

    public void ClearTopText()
    {
		if (testing3D)
			text3D.text = "";
		else
			topText.text = "";
		HideTopText();
    }

    public void ShowTopText()
    {
		if (testing3D)
			text3D.gameObject.SetActive(true);
		else
			topText.gameObject.SetActive(true);	
	}

    public void HideTopText()
    {
		if (testing3D)
			text3D.gameObject.SetActive(false);
		else
			topText.gameObject.SetActive(false);
	}

}
