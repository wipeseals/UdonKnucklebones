
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UCS;

public class UdonChipsAutoGain : UdonSharpBehaviour
{
    private UdonChips udonChips;
    [SerializeField] float gainPerSec = 0.1f;
    [SerializeField] float gainMax = 100000;
    void Start()
    {
        udonChips = GameObject.Find("UdonChips").GetComponent<UdonChips>();
    }

    private void Update()
    {
        if (udonChips.money <= gainMax)
        {
            udonChips.money += gainPerSec * Time.deltaTime;
        }
    }
}
