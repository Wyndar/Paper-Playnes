using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TeamManager : NetworkBehaviour
{
    public GameEvent UpdateTeamScoreEvent;
    public static TeamManager Instance { get; private set; }

#pragma warning disable IDE0044 // Add readonly modifier
    private Dictionary<Controller, Team> entityTeams = new();
    private Dictionary<Team, int> teamScores = new();
    private List<Controller> allEntities = new();
#pragma warning restore IDE0044 // Add readonly modifier
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        teamScores.Clear();
    }

    public Team AssignTeam(Controller entity)
    {
        if (!IsServer) return Team.Undefined;

        if (!entityTeams.ContainsKey(entity))
        {
            Team assignedTeam = GetSmallerTeam();
            entityTeams[entity] = assignedTeam;
            allEntities.Add(entity);

            Debug.Log($"Assigned {entity.name} to {assignedTeam}");
        }
        return entityTeams[entity];
    }

    private Team GetSmallerTeam()
    {
        int redCount = 0, blueCount = 0;

        foreach (var team in entityTeams.Values)
        {
            if (team == Team.RedTeam) redCount++;
            else if (team == Team.BlueTeam) blueCount++;
        }

        return (redCount <= blueCount) ? Team.RedTeam : Team.BlueTeam;
    }

    public Team GetTeam(Controller entity) => entityTeams.TryGetValue(entity, out Team team) ? team : Team.Undefined;

    [ServerRpc(RequireOwnership = false)]
    public void RequestTeamAssignmentServerRpc(NetworkObjectReference entityRef)
    {
        if (entityRef.TryGet(out NetworkObject entityObj) && entityObj.TryGetComponent(out Controller controller))
            SpawnManager.Instance.DesignateSpawnPoint(controller, AssignTeam(controller));
    }

    public void LocalPlayerDied(Controller entity)
    {
        Team playerTeam = GetTeam(entity);
        if (playerTeam == Team.Undefined) return;

        Team enemyTeam = (playerTeam == Team.RedTeam) ? Team.BlueTeam : Team.RedTeam;
        RequestScoreChangeServerRpc(enemyTeam, 1, teamScores[enemyTeam]);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestScoreChangeServerRpc(Team teamToChangeScore, int scoreChangeAmount, int scoreAtRequestingClientBeforeChange) => UpdateTeamScore(teamToChangeScore, scoreChangeAmount, scoreAtRequestingClientBeforeChange);

    private void UpdateTeamScore(Team teamToChangeScore, int scoreChangeAmount, int scoreAtRequestingClientBeforeChange)
    {
        if (!IsServer) return;

        if (!teamScores.TryGetValue(teamToChangeScore, out int score)) return;

        teamScores[teamToChangeScore] = score + scoreChangeAmount;
        ReceiveTeamScoreAtClientRpc(teamToChangeScore, teamScores[teamToChangeScore], score);
    }

    public void InitializeTeamScores()
    {
        ReceiveTeamScoreAtClientRpc(Team.RedTeam, teamScores.GetValueOrDefault(Team.RedTeam, 0), 0);
        ReceiveTeamScoreAtClientRpc(Team.BlueTeam, teamScores.GetValueOrDefault(Team.BlueTeam, 0), 0);
    }

    [ClientRpc]
    private void ReceiveTeamScoreAtClientRpc(Team teamToChangeScore, int scoreAtServerAfterChange, int scoreAtServerBeforeChange)
    {
        if (!teamScores.ContainsKey(teamToChangeScore))
        {
            teamScores[teamToChangeScore] = scoreAtServerAfterChange;
        }
        else if (teamScores[teamToChangeScore] < scoreAtServerAfterChange)
        {
            teamScores[teamToChangeScore] = scoreAtServerAfterChange;
        }
        Debug.Log(teamScores[teamToChangeScore]);
        UpdateTeamScoreEvent.RaiseEvent(teamToChangeScore, scoreAtServerAfterChange, scoreAtServerBeforeChange);
    }
}
