using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FanControl : MonoBehaviour {

    public bool turnOn = false;
    private bool isSpinning = false;
    Animator fanAnim;
    public bool turnOff = false;

	// Use this for initialization
	void Start () {
        fanAnim = GetComponent<Animator>();
        //fanAnim.SetTrigger("FanStandby");
        
	}
	
	// Update is called once per frame
	void Update () {

		if(turnOn)
        {
            // TODO
            // seems like FanStartUp state, then FanSpin state, then repeat.
            // our goal is to repeat on FanSpin, not coming back to FanStartUp.

            //fanAnim.Play("FanStartUp");
            //fanAnim.Play("FanSpin");
            fanAnim.ResetTrigger("FanOff");
            fanAnim.SetTrigger("FanOn");
            isSpinning = true;
        }

        if(isSpinning)
        {
            //fanAnim.Play("FanSpin");
        }

        if(turnOff)
        {
            fanAnim.ResetTrigger("FanOn");
            fanAnim.SetTrigger("FanOff");
            //fanAnim.Play("FanShutOff");
            isSpinning = false;
        }
	}
}
