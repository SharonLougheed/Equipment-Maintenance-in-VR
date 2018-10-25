using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenUIController : MonoBehaviour {

    public GameObject trackingObject;
    private Canvas screenCanvas;
    private Text topText;
    private Quaternion originalRotationOf3DText;
    public float textDist = .75f;
    public float verticalDist = 3.0f;
    public float horizontalDist = 0.0f;
	public TextMesh text3D;
	public bool using3DText = false;

	// Use this for initialization
	void Start () {
        
        screenCanvas = GameObject.Find("ScreenCanvas").GetComponent<Canvas>();
        topText = GameObject.Find("TopText").GetComponent<Text>();
        if (using3DText)
        {
            topText.gameObject.SetActive(false);
            originalRotationOf3DText = text3D.transform.rotation;
        }
	}
	
	// Update is called once per frame
	void Update () {
		if (!using3DText)
		{
			screenCanvas.transform.position = trackingObject.transform.position + trackingObject.transform.forward * textDist + trackingObject.transform.up * -(verticalDist) + trackingObject.transform.right * horizontalDist ;
			screenCanvas.transform.rotation = trackingObject.transform.rotation;
		}
    }

    public void SetTopText(string text)
    {
		if (using3DText)
		{
			text3D.text = text;
			text3D.transform.position = trackingObject.transform.position + trackingObject.transform.forward * textDist + trackingObject.transform.up * -(verticalDist) + trackingObject.transform.right * horizontalDist;
            text3D.transform.Rotate(0, trackingObject.transform.rotation.eulerAngles.y, 0);
		}
		else
			topText.text = text;
		ShowTopText();
    }

    public void ClearTopText()
    {
		if (using3DText)
			text3D.text = "";
		else
			topText.text = "";
		HideTopText();
    }

    public void ShowTopText()
    {
		if (using3DText)
			text3D.gameObject.SetActive(true);
		else
			topText.gameObject.SetActive(true);	
	}

    public void HideTopText()
    {
        if (using3DText)
        {
            text3D.gameObject.SetActive(false);
            text3D.transform.rotation = originalRotationOf3DText;
        }
        else
            topText.gameObject.SetActive(false);
	}

}
