
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class EventEmitter : UdonSharpBehaviour
{
    #region Inspector Variables
    [Tooltip("The UdonBehaviours to send events to.")]
    public UdonSharpBehaviour listener = null;

    [Tooltip("The event to send to the listeners.")]
    public string eventName = "OnReceiveEvent";

    private bool _isEventSendable = true;
    public bool IsEventSendable
    {
        get
        {
            return _isEventSendable;
        }
        set
        {
            // コライダーもセットで調整
            _isEventSendable = value;
            this.gameObject.GetComponent<Collider>().enabled = value;
        }
    }

    #endregion

    #region Toggle Event

    /// <summary>
    /// Called when the object is enabled.
    /// </summary>
    public void SetEnable()
    {
        IsEventSendable = true;
    }

    /// <summary>
    /// Called when the object is disabled.
    /// </summary>
    public void OnDisable()
    {
        IsEventSendable = false;
    }
    #endregion

    #region Emit

    /// <summary>
    /// Sends the event to all listeners.
    /// </summary>
    public void Emit()
    {
        // If the event is disabled, do not send it.
        if (!_isEventSendable || listener == null)
        {
            return;
        }
        listener.SendCustomEvent(eventName);
    }

    #endregion

    void Start()
    {
    }

    public override void Interact()
    {
        Emit();
    }
}