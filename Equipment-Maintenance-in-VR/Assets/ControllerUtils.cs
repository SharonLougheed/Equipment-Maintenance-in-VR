
using System.Collections;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class ControllerUtils: MonoBehaviour {

    


    public static IEnumerator VibrateControllerContinuous(Hand hand, float durationSec, float frequency, float amplitude)
    {
        hand.TriggerHapticPulse(durationSec, frequency, amplitude);
        yield break;
    }
}
