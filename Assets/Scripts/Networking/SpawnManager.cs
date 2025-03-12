using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance { get; private set; }
    public Renderer redTeamSpawnArea; 
    public Renderer blueTeamSpawnArea; 
    public GameObject playerNetworkPrefab;

    public List<PlayerController> activePlayers = new();
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }
    public void RegisterPlayer(PlayerController player)
    {
        if (!activePlayers.Contains(player))
            activePlayers.Add(player);
    }

    public void UnregisterPlayer(PlayerController player)
    {
        if (activePlayers.Contains(player))
            activePlayers.Remove(player);
    }
    public Vector3 GetRandomSpawnPoint(Team team)
    {
        Renderer spawnArea = (team == Team.RedTeam) ? redTeamSpawnArea : blueTeamSpawnArea;
        Bounds bounds = spawnArea.bounds;

        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }

    public void SpawnPlayerObject(ulong clientId)
    {
        if (!IsServer) return; 

        Team team = TeamManager.Instance.GetTeam(clientId);
        if (team == Team.Undefined) return; 

        Vector3 spawnPosition = GetRandomSpawnPoint(team);
        GameObject playerObject = Instantiate(playerNetworkPrefab, spawnPosition, Quaternion.identity);
        
        if (playerObject.TryGetComponent(out NetworkObject networkObject))
        {
            networkObject.SpawnWithOwnership(clientId);
            Debug.Log($"Spawned {team} player-controlled object for Client {clientId} at {spawnPosition}");
        }
        else
            Debug.LogError("Player prefab does not have a NetworkObject component!");
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnServerRpc(ulong clientId) => SpawnPlayerObject(clientId);
}
