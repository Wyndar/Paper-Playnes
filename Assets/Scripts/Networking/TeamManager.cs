using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TeamManager : NetworkBehaviour
{
    public static TeamManager Instance { get; private set; }
    private Dictionary<ulong, Team> playerTeams = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AssignTeam(ulong clientId)
    {
        if (!IsServer) return;

        if (!playerTeams.ContainsKey(clientId))
        {
            Team assignedTeam = GetSmallerTeam();
            playerTeams[clientId] = assignedTeam;

            Debug.Log($"Assigned Player {clientId} to {assignedTeam}");
        }
        ConfirmTeamAssignmentClientRpc(clientId);
    }

    private Team GetSmallerTeam()
    {
        int teamACount = 0, teamBCount = 0;

        foreach (var team in playerTeams.Values)
        {
            if (team == Team.RedTeam) teamACount++;
            else if (team == Team.BlueTeam) teamBCount++;
        }

        return (teamACount <= teamBCount) ? Team.RedTeam : Team.BlueTeam;
    }

    public Team GetTeam(ulong clientId) => playerTeams.TryGetValue(clientId, out Team team) ? team : Team.None;

    [ServerRpc(RequireOwnership = false)]
    public void RequestTeamAssignmentServerRpc(ulong clientId) => AssignTeam(clientId);
    [ClientRpc]
    private void ConfirmTeamAssignmentClientRpc(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"Client {clientId} team assigned. Requesting spawn...");
            SpawnManager.Instance.RequestSpawnServerRpc(clientId);
        }
    }
}

public enum Team
{
    None, 
    RedTeam,
    BlueTeam 
}
