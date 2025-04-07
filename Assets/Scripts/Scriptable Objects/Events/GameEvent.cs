using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Game Event", menuName = "Scriptable/Game Event")]
public class GameEvent : ScriptableObject
{
    public GameEventType selectedEventType;
    [Header("Assign the corresponding PickUpType if it is a pick up Event")]
    public PickUpType pickupType;

    public event Action OnEventRaised;
    public event Action<bool> OnToggleEventRaised;
    public event Action<Weapon> OnWeaponEventRaised;
    public event Action<Vector3> OnLocationEventRaised;
    public event Action<GameObject> OnGameObjectEventRaised;
    public event Action<int, int> OnStatEventRaised;
    public event Action<Team, int, int> OnTeamEventRaised;
    public event Action<HealthComponent, HealthModificationType, int, int, Controller> OnHealthModifiedEventRaised;
    

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
    public void RaiseEvent(bool isOn) => OnToggleEventRaised?.Invoke(isOn);
    public void RaiseEvent(Weapon weapon) => OnWeaponEventRaised?.Invoke(weapon);
    public void RaiseEvent(Vector3 location) => OnLocationEventRaised?.Invoke(location);
    public void RaiseEvent(GameObject obj) => OnGameObjectEventRaised?.Invoke(obj);
    public void RaiseEvent(int currentStatOrChangeAmount, int maxStat) => OnStatEventRaised?.Invoke(currentStatOrChangeAmount, maxStat);
    public void RaiseEvent(Team updateTeam, int currentStat, int maxOrPreviousStat) 
        => OnTeamEventRaised?.Invoke(updateTeam, currentStat, maxOrPreviousStat);
    public void RaiseEvent(HealthComponent component, HealthModificationType modificationType, int amount, int previousHP, Controller modificationSource)
        => OnHealthModifiedEventRaised?.Invoke(component, modificationType, amount, previousHP, modificationSource);

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
    public bool HasSubscribers() => OnEventRaised != null;
    public bool HasToggleSubscribers() => OnToggleEventRaised != null;
    public bool HasWeaponSubscribers() => OnWeaponEventRaised != null;
    public bool HasLocationSubscribers() => OnLocationEventRaised != null;
    public bool HasGameObjectSubscribers() => OnGameObjectEventRaised != null;
    public bool HasStatSubscribers() => OnStatEventRaised != null;
    public bool HasTeamSubscribers() => OnTeamEventRaised != null;
    public bool HasHealthModifiedSubscribers() => OnHealthModifiedEventRaised != null;

}
