using UnityEngine;

[RequireComponent(typeof(AudioListener))]
public class ListenerHandler : MonoBehaviour
{
    private AudioListener listener;
    public GameEvent toggleListenerEvent;

    public void Awake() => listener = GetComponent<AudioListener>();
    public void OnEnable() => toggleListenerEvent.OnToggleEventRaised += ToggleListener;
    public void OnDisable() => toggleListenerEvent.OnToggleEventRaised -= ToggleListener;
    public void ToggleListener(bool toggle)=>listener.enabled = toggle;

    //attach this to main listeners that are expected to deliver audio all the time, we need to temporarily disable them for cross scene audio listening in the load panels
}
