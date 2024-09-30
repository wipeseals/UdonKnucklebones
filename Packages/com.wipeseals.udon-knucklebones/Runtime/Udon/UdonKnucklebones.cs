
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class UdonKnucklebones : UdonSharpBehaviour
{
    [Header("Udon Knucklebones")]
    [SerializeField, Tooltip("Debug Enabled")]
    public bool isDebug = true;

    [SerializeField, Tooltip("The dice object to roll")]
    public GameObject dice = null;

    /// <summary>
    /// Prints a message to the console
    /// </summary>
    /// <param name="msg"></param>
    void Log(string msg)
    {
        if (isDebug)
        {
            Debug.Log($"[UdonKnucklebones] {msg}");
        }
    }

    void Start()
    {
        Log("Udon Knucklebones is ready!");
    }
}
