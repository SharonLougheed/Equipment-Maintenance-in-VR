using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Text3DTags : MonoBehaviour
{

   // public GameObject playerTrackingObject;
    public TextMesh text3D;
    public string[] textTags;
    public GameObject[] textLocations;
    public float textDist = .75f;
    public float verticalDist = 3.0f;
    public float horizontalDist = 0.0f;

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void SetTopText(int tagNum)
    {
        //if (textLocations[tagNum] != null)
        //{
            GameObject trackingObject = textLocations[tagNum];
            text3D.transform.position = trackingObject.transform.position;
            text3D.transform.rotation = trackingObject.transform.rotation;
            Debug.Log("test1");
        //}
        //else
        //{
         //   text3D.transform.position = playerTrackingObject.transform.position + playerTrackingObject.transform.forward * textDist + playerTrackingObject.transform.up * -(verticalDist) + playerTrackingObject.transform.right * horizontalDist;
         //   text3D.transform.rotation = playerTrackingObject.transform.rotation;
         //   Debug.Log("test1");
        //}
        text3D.text = textTags[tagNum];
        ShowTopText();
    }

    public void ClearTopText()
    {
        text3D.text = "";
        HideTopText();
    }

    public void ShowTopText()
    {
        text3D.gameObject.SetActive(true);
    }

    public void HideTopText()
    {
        text3D.gameObject.SetActive(false);
    }

}
