
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UCS;



public class UdonChipsMoveSpeed : UdonSharpBehaviour
{
    private UdonChips udonChips;
    [HideInInspector] public VRCPlayerApi localPlayer;
    private float runSpeed = 1.0f;
    private float lastRunSpeed = 0;

    [SerializeField] private float[] speedKeyCoin = { 0f, 50f, 100f, 150f, 250f, 400f, 1000f, 2000f, 10000f, 100000f };
    [SerializeField] private float[] speedValue = { 0.5f, 1.0f, 1.5f, 2.0f, 2.5f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f };
    
    void Start()
    {
        localPlayer = Networking.LocalPlayer;
        udonChips = GameObject.Find("UdonChips").GetComponent<UdonChips>();
    }

    void Update()
    {
        if (localPlayer == null)
        {
            return;
        }

        runSpeed = CalcSpeed();
        
        if(lastRunSpeed != runSpeed)
        {
            SetSpeed();
        }
        lastRunSpeed = runSpeed;

    }
    float CalcSpeed()
    {
        //最初と最後の判定
        if (udonChips.money <= speedKeyCoin[0])
        {
            return speedValue[0];
        }

        int length = speedKeyCoin.Length;

        if (speedKeyCoin[length - 1] <= udonChips.money)
        {
            return speedValue[length - 1];
        }

        for (int i = 0; i < length; i++)
        {
            if (speedKeyCoin[i] >= udonChips.money)
            {
                var interval = speedKeyCoin[i] - speedKeyCoin[i-1];
                var rate = (udonChips.money - speedKeyCoin[i-1]) / interval;
                return Mathf.Lerp(speedValue[i - 1], speedValue[i], rate);
            }
        }
        return 1;
    }
    private void SetSpeed()
    {
        float walkSpeed = runSpeed / 2;
        float strafeSpeed = runSpeed;

        localPlayer.SetWalkSpeed(walkSpeed);
        localPlayer.SetRunSpeed(runSpeed);
        localPlayer.SetStrafeSpeed(strafeSpeed);
    }
}
