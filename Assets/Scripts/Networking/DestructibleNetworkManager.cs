using Unity.Netcode;
using UnityEngine;

public class DestructibleNetworkManager : NetworkBehaviour
{
    //add a way to keep track of damage and healing done and received here for each player
    public GameEvent HealthModificationEvent;

    private void OnEnable() => HealthModificationEvent.OnHealthModifiedEventRaised += HandleHealthChange;
    private void OnDisable() => HealthModificationEvent.OnHealthModifiedEventRaised -= HandleHealthChange;

    private void HandleHealthChange(HealthComponent component, HealthModificationType type, int amount, int previousHP, Controller modificationSource)
    {
        if (!IsServer) return;

        int newHP = previousHP;
        int maxHP = component.MaxHP;

        switch (type)
        {
            case HealthModificationType.Damage:
                newHP = Mathf.Clamp(previousHP - amount, 0, maxHP);
                break;
            case HealthModificationType.Heal:
                newHP = Mathf.Clamp(previousHP + amount, 0, maxHP);
                break;
            case HealthModificationType.MaxHPIncrease:
                maxHP += amount;
                newHP += amount;
                break;
            case HealthModificationType.MaxHPDecrease:
                maxHP = Mathf.Max(1, maxHP - amount);
                newHP = Mathf.Clamp(newHP, 0, maxHP);
                break;
        }
        NetworkObjectReference reference = modificationSource != null ? modificationSource.NetworkObject : null;
        UpdateHealthClientRpc(component.NetworkObject, newHP, maxHP, reference);
    }

    [ClientRpc]
    private void UpdateHealthClientRpc(NetworkObjectReference modifiedHealth, int newHP, int maxHP, NetworkObjectReference modificationSource)
    {
        if (modifiedHealth.TryGet(out NetworkObject netObj) && netObj.TryGetComponent(out HealthComponent component))
        {
            Controller controller = null;
            if (modificationSource.TryGet(out NetworkObject modifierObj) && modifierObj.TryGetComponent(out Controller c))
                controller = c;
            component.HandleHealthUpdate(newHP, maxHP, controller);
        }
    }
}
