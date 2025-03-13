using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Game Event", menuName = "Scriptable/Game Event")]
public class GameEvent : ScriptableObject
{
    public event Action OnEventRaised;
    public event Action<GameObject> OnGameObjectEventRaised;
    public event Action<int, int> OnStatEventRaised;
    public event Action<Team, int, int> OnTeamEventRaised;
    public event Action<HealthComponent, HealthModificationType, int, int> OnHealthModified;

    [Header("Assign the corresponding PickUpType if it is a pick up Event")]
    public PickUpType pickupType;

    private static readonly Dictionary<PickUpType, WeakReference<GameEvent>> _eventRegistry = new();

    private void OnEnable() => RegisterEvent();

    private void OnDisable() => UnregisterEvent();

    private void RegisterEvent()
    {
        if (pickupType == PickUpType.Undefined) return;

        if (!_eventRegistry.ContainsKey(pickupType))
            _eventRegistry[pickupType] = new WeakReference<GameEvent>(this);
    }

    private void UnregisterEvent()
    {
        if (_eventRegistry.ContainsKey(pickupType))
            _eventRegistry.Remove(pickupType);
    }
    public void RaiseEvent() => OnEventRaised?.Invoke();
    public void RaiseEvent(GameObject obj) => OnGameObjectEventRaised?.Invoke(obj);
    public void RaiseEvent(int currentStat, int maxStat) => OnStatEventRaised?.Invoke(currentStat, maxStat);
    public void RaiseEvent(Team updateTeam, int currentStat, int maxOrPreviousStat) 
        => OnTeamEventRaised?.Invoke(updateTeam, currentStat, maxOrPreviousStat);
    public void RaiseEvent(HealthComponent component, HealthModificationType modificationType, int amount, int previousHP)
        => OnHealthModified?.Invoke(component, modificationType, amount, previousHP);

    public static bool TryGetEvent(PickUpType type, out GameEvent gameEvent)
    {
        if (_eventRegistry.TryGetValue(type, out WeakReference<GameEvent> weakRef) && weakRef.TryGetTarget(out gameEvent))
            return true;

        LoadEventIfMissing(type);

        if (_eventRegistry.TryGetValue(type, out weakRef) && weakRef.TryGetTarget(out gameEvent))
            return true;

        gameEvent = null;
        return false;
    }

    private static void LoadEventIfMissing(PickUpType type)
    {
        if (!_eventRegistry.ContainsKey(type))
        {
            GameEvent eventAsset = Resources.Load<GameEvent>($"GameEvents/{type}");

            if (eventAsset != null)
                _eventRegistry[type] = new WeakReference<GameEvent>(eventAsset);
        }
    }
}
