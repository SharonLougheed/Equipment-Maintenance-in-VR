using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioQueue : MonoBehaviour {

    public AudioSource audioSource;
    private Queue<AudioClip> audioClipsQueue = new Queue<AudioClip>();
    private bool isPaused = false;
    
    // Use this for initialization
    void Start () {

    }
	
	// Update is called once per frame
	void Update () {
		if(audioSource != null && audioClipsQueue.Count > 0 && !isPaused && !audioSource.isPlaying)
        {
            audioSource.clip = audioClipsQueue.Dequeue();
            audioSource.Play();
        }
	}

    public void AddAudioClipToQueue(AudioClip audioClip)
    {
        audioClipsQueue.Enqueue(audioClip);
    }

    public void PlayOneShot(AudioClip audioClip)
    {
        if (audioSource != null)
            audioSource.PlayOneShot(audioClip);
    }

    public void PauseQueue()
    {
        isPaused = true;
        audioSource.Pause();
    }

    public void UnPauseQueue()
    {
        isPaused = false;
        audioSource.UnPause();
    }
}
