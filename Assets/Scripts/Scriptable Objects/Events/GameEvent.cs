using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Game Event", menuName = "Scriptable/Game Event")]
public class GameEvent : ScriptableObject
{
    public event Action<GameObject> OnGameObjectEventRaised;
    public event Action<int, int> OnUpdateStatEventRaised;

    public void RaiseEvent(GameObject obj) => OnGameObjectEventRaised?.Invoke(obj);
    public void RaiseEvent(int currentStat, int maxStat)=>OnUpdateStatEventRaised?.Invoke(currentStat, maxStat);
}
