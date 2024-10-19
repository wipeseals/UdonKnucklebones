
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class UdonSpeaker : UdonSharpBehaviour
{
    [Tooltip("The audio source to control")]
    public AudioSource TargetAudioSource = null;

    [Tooltip("Should the audio source play on start")]
    public bool IsPlayOnStart = true;

    void Start()
    {
        if (IsPlayOnStart && TargetAudioSource != null)
        {
            TargetAudioSource.Play();
        }
    }

    public override void Interact()
    {
        if (TargetAudioSource != null)
        {
            if (TargetAudioSource.isPlaying)
            {
                TargetAudioSource.Stop();
            }
            else
            {
                TargetAudioSource.Play();
            }
        }
    }

}
