using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FanControl : MonoBehaviour {

    public bool turnOn = false;
    private bool isSpinning = false;
    public Animator fanAnim;
    public AudioSource FanSound;
    public AudioClip FanStarting;
    public AudioClip FanSpinning;
    public AudioClip FanStopping;


	void Start () {
        fanAnim = GetComponent<Animator>();
        FanSound = GetComponent<AudioSource>();
	}

    bool playSound = true;
	void Update () {

		if(turnOn && !isSpinning)
        {
            // TODO
            // seems like FanStartUp state, then FanSpin state, then repeat.
            // our goal is to repeat on FanSpin, not coming back to FanStartUp.
            if(!FanSound.isPlaying)
                FanSound.PlayOneShot(FanStarting, 0.7f);
            fanAnim.SetBool("FanOn", true);
            //PlaySound(FanSound, FanStarting);
            //fanAnim.Play("FanStartUp");
            //fanAnim.Play("FanSpin");
            //fanAnim.SetTrigger("FanOn");
            isSpinning = true;
        }

        
        if(isSpinning)
        {
            //FanSound.PlayOneShot(FanSpinning);
            if(!FanSound.isPlaying)
                FanSound.PlayOneShot(FanSpinning, 0.7f);
            //PlaySound(FanSound, FanSpinning);
        }
        

        if(!turnOn && isSpinning)
        {
            //if (!FanSound.isPlaying)
            FanSound.Stop();
            FanSound.PlayOneShot(FanStopping, 0.7f);
            fanAnim.SetBool("FanOn", false);
            //PlaySound(FanSound, FanStopping);
            //fanAnim.Play("FanShutOff");
            isSpinning = false;
        }
	}

    public void ChangeFanState(bool turnFanOn)
    {
        turnOn = turnFanOn;
    }

    public void ToggleFanState()
    {
        turnOn = !turnOn;
    }
}
