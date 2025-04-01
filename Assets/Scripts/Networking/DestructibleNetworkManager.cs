using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DestructibleNetworkManager : NetworkBehaviour
{
    public static DestructibleNetworkManager Instance { get; private set; }
    //add a way to keep track of damage and healing done and received here for each player
    public GameEvent HealthModificationEvent;
    public GameEvent RespawnEvent;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }
    private void OnEnable() => HealthModificationEvent.OnHealthModifiedEventRaised += HandleHealthChange;
    private void OnDisable() => HealthModificationEvent.OnHealthModifiedEventRaised -= HandleHealthChange;

    private void HandleHealthChange(HealthComponent component, HealthModificationType type, int amount, int previousHP, Controller modificationSource)
    {
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
    public void LocalPlayerDied(Controller entity, List<Controller> damageSources)
    {
        RespawnEvent.RaiseEvent(entity.gameObject);
        RequestGameObjectStateChangeAtServerRpc(entity.GetComponent<NetworkObject>(), false);
        Controller killer = damageSources.Count > 0 ? damageSources[^1] : null;
        if (killer != null)
        {
            Team killTeam = killer.Team;
            if (killTeam != Team.Undefined)
            {
                TeamData killTeamData = TeamManager.Instance.teamDataList.Find(t => t.team == killTeam);
                TeamManager.Instance.RequestScoreChangeServerRpc(killTeam, 1, killTeamData.teamScore);
                MessageFeedManger.Instance.RequestKillFeedBroadcastAtServerRpc(killer.name, killTeam, entity.name, entity.Team, "spitfire");
            }
        }
        else
            MessageFeedManger.Instance.RequestEnvironmentKillFeedBroadcastAtServerRpc(entity.name, entity.Team, "wall", "wall");
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestGameObjectStateChangeAtServerRpc(NetworkObjectReference entityRef, bool isActive) => ReceiveGameObjectStateChangeAtClientRpc(entityRef, isActive);

    [ClientRpc]
    private void ReceiveGameObjectStateChangeAtClientRpc(NetworkObjectReference entityRef, bool isActive)
    {
        if (!entityRef.TryGet(out NetworkObject networkObject))
            throw new MissingComponentException("If you're seeing this, something is wrong with Netcode and your code is working as intended.");
        networkObject.gameObject.SetActive(isActive);
    }
}
