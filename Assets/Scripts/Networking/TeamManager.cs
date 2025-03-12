using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TeamManager : NetworkBehaviour
{
    public GameEvent UpdateTeamScoreEvent;
    public static TeamManager Instance { get; private set; }

#pragma warning disable IDE0044 
    private Dictionary<ulong, Team> playerTeams = new();
    private Dictionary<Team, int> teamScores = new();
#pragma warning restore IDE0044
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

    private void AssignTeam(ulong clientId)
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

    public Team GetTeam(ulong clientId) => playerTeams.TryGetValue(clientId, out Team team) ? team : Team.Undefined;

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

    //we need better logic incase there more than two teams but that's another can of worms entirely
    public void LocalPlayerDied()
    {
        Team playerTeam = GetTeam(NetworkManager.Singleton.LocalClientId);
        Team enemyTeam;
        if (playerTeam == Team.RedTeam)
            enemyTeam = Team.BlueTeam;
        else
            enemyTeam= Team.RedTeam;
            RequestScoreChangeServerRpc(enemyTeam, 1, teamScores[enemyTeam]);
    }
    [ServerRpc(RequireOwnership = false)]
    public void RequestScoreChangeServerRpc(Team teamToChangeScore, int scoreChangeAmount, int scoreAtRequestingClientBeforeChange) 
        => UpdateTeamScore(teamToChangeScore, scoreChangeAmount, scoreAtRequestingClientBeforeChange);
    private void UpdateTeamScore(Team teamToChangeScore, int scoreChangeAmount, int scoreAtRequestingClientBeforeChange)
    {
        if (!IsServer) return;

        if (!teamScores.TryGetValue(teamToChangeScore, out int score))
            return;
        if (score < scoreAtRequestingClientBeforeChange)
        {
            //if(Mathf.Abs(score - scoreAtRequestingClientBeforeChange)>scoreChangeAmount)
            //...I'll be honest, I can't think of a way to reconcile scores if someone mistakenly requests twice or get's updated twice 
            //I'll just ignore it for now
            Debug.Log("Scores do not match");
        }
        teamScores[teamToChangeScore] = score + scoreChangeAmount;
        ReceiveTeamScoreAtClientRpc(teamToChangeScore, teamScores[teamToChangeScore], score);
    }
    public void InitializeTeamScores()
    {
        ReceiveTeamScoreAtClientRpc(Team.RedTeam, teamScores.Count > 1 ? teamScores[Team.RedTeam] : 0, 0);
        ReceiveTeamScoreAtClientRpc(Team.BlueTeam, teamScores.Count > 1 ? teamScores[Team.BlueTeam] : 0, 0);
    }
    [ClientRpc]
    private void ReceiveTeamScoreAtClientRpc(Team teamToChangeScore, int scoreAtServerAfterChange, int scoreAtServerBeforeChange)
    {
        //if we don't find the team, add it.
        if(!teamScores.TryGetValue(teamToChangeScore,out int score))
            teamScores.Add(teamToChangeScore, scoreAtServerAfterChange);
        else if(score < scoreAtServerAfterChange)
            teamScores[teamToChangeScore] = score;
        Debug.Log(teamScores[teamToChangeScore]);
        UpdateTeamScoreEvent.RaiseEvent(teamToChangeScore, scoreAtServerAfterChange, scoreAtServerBeforeChange);
    }
}
