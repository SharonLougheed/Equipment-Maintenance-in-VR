using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioQueue : MonoBehaviour
{
    public AudioSource audioSource;
    private AudioSource nextAudioSource;
    public bool muteOnStart = true;
    private Queue<AudioClip> audioClipsQueue = new Queue<AudioClip>();
    private bool isPaused = false;
    private bool waitingToChangeSource = false;

    // Use this for initialization
    void Start ()
    {
        audioSource.volume = 0.5f;
        audioSource.mute = muteOnStart;
    }
	
	// Update is called once per frame
	void Update ()
    {
		if(!waitingToChangeSource && audioSource != null && audioClipsQueue.Count > 0 && !isPaused && !audioSource.isPlaying)
        {
            
                audioSource.clip = audioClipsQueue.Dequeue();
                audioSource.Play();
            
        }
	}

    public void AddAudioClipToQueue(AudioClip audioClip)
    {
        audioClipsQueue.Enqueue(audioClip);
    }

    public void AddAudioClipToQueue(AudioClip audioClip, bool stopCurrentAudio = false)
    {
        audioClipsQueue.Enqueue(audioClip);
        if (audioSource != null && audioClipsQueue.Count > 0 && !isPaused && !audioSource.isPlaying)
        {
            audioSource.clip = audioClipsQueue.Dequeue();
            audioSource.Play();
        }
    }
    
    public void ChangeAudioSource(AudioSource newAudioSource)
    {
        StartCoroutine(WaitToChangeSource(newAudioSource));
    }

    private IEnumerator WaitToChangeSource(AudioSource newAudioSource)
    {
        yield return new WaitWhile(() => !audioSource.isPlaying);
        waitingToChangeSource = true;

        yield return new WaitWhile(() => audioSource.isPlaying);

        audioSource = newAudioSource;
        waitingToChangeSource = false;
    }

    public void PlayOneShot(AudioClip audioClip, float volume = 1.0f)
    {
        if (audioSource != null)
            audioSource.PlayOneShot(audioClip, volume);
    }

    public void StopAudio()
    {
        audioSource.Stop();
    }

    public void PlayAudio()
    {
        audioSource.Play();
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

    public void setVolume(float vol)
    {
        if(audioSource != null)
        {
            audioSource.volume = vol;
        }
    }

    public void ClearAudioQueue()
    {
        if (audioSource != null)
        {
            audioClipsQueue.Clear();
        }

    }
}
