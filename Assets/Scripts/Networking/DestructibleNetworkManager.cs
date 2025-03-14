using Unity.Netcode;
using UnityEngine;

public class DestructibleNetworkManager : NetworkBehaviour
{
    public GameEvent HealthModificationEvent;

    private void OnEnable() => HealthModificationEvent.OnHealthModifiedEventRaised += HandleHealthChange;
    private void OnDisable() => HealthModificationEvent.OnHealthModifiedEventRaised -= HandleHealthChange;

    private void HandleHealthChange(HealthComponent component, HealthModificationType type, int amount, int previousHP)
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

        UpdateHealthClientRpc(component.NetworkObject, newHP, maxHP);
    }

    [ClientRpc]
    private void UpdateHealthClientRpc(NetworkObjectReference componentRef, int newHP, int maxHP)
    {
        if (componentRef.TryGet(out NetworkObject netObj) && netObj.TryGetComponent(out HealthComponent component))
            component.HandleHealthUpdate(newHP, maxHP);
    }
}
