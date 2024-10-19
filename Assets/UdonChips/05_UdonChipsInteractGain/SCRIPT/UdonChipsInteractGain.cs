
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UCS;

public class UdonChipsInteractGain : UdonSharpBehaviour
{
    [Header("----------------------System-------------------------")]
    [SerializeField] private AudioSource audioSource_ButtonHit;
    private UdonChips udonChips;
    [Space(20)]
    [Header("----------------------Reward-------------------------")]
    [SerializeField] private float moneyReward = 0.2f;


    void Start()
    {
        udonChips = GameObject.Find("UdonChips").GetComponent<UdonChips>();
    }

    public override void Interact()
    {
        ButtonPush();
    }


    private void ButtonPush()
    {
        udonChips.money = udonChips.money + moneyReward;

        if (audioSource_ButtonHit != null)
        {
            audioSource_ButtonHit.Play();
        }
    }
}
