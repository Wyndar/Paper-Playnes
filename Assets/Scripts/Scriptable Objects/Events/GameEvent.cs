using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Game Event", menuName = "Scriptable/Game Event")]
public class GameEvent : ScriptableObject
{
    public event Action<GameObject> OnEventRaised;

    public void RaiseEvent(GameObject obj) => OnEventRaised?.Invoke(obj);
}
