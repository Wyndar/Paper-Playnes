using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class TeamManager : NetworkBehaviour
{
    public GameEvent UpdateTeamScoreEvent;
    public static TeamManager Instance { get; private set; }
    public List<TeamData> teamDataList = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private Team AssignTeam(Controller entity)
    {
        if (!IsServer) return Team.Undefined;

        TeamData smallestTeam = teamDataList.OrderBy(t => t.teamMembers.Count).First();
        smallestTeam.teamMembers.Add(entity);
        return smallestTeam.team;
    }

    public Team GetTeam(Controller entity)
    {
        foreach (var teamData in teamDataList)
            if (teamData.teamMembers.Contains(entity))
                return teamData.team;
        return Team.Undefined;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestTeamAssignmentServerRpc(NetworkObjectReference entityRef)
    {
        if (entityRef.TryGet(out NetworkObject entityObj) && entityObj.TryGetComponent(out Controller controller))
            SpawnManager.Instance.TriggerControllerInitialize(controller, AssignTeam(controller));
    }

    public void LocalPlayerDied(Controller entity, List<Controller> damageSources)
    {
        Controller killer = damageSources.Count > 0 ? damageSources[^1] : null;
        if (killer != null)
        {
            Team killTeam = GetTeam(killer);
            if (killTeam != Team.Undefined)
            {
                TeamData killTeamData = teamDataList.Find(t => t.team == killTeam);
                RequestScoreChangeServerRpc(killTeam, 1, killTeamData.teamScore);
                //request message broadcast here for kills and assists
            }
        }
        RequestGameObjectStateChangeAtServerRpc(entity.GetComponent<NetworkObject>(), false);
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

    [ServerRpc(RequireOwnership = false)]
    public void RequestScoreChangeServerRpc(Team teamToChangeScore, int scoreChangeAmount, int scoreAtRequestingClientBeforeChange)
    {
        if (!IsServer) return;

        TeamData targetTeamData = teamDataList.Find(t => t.team == teamToChangeScore);
        if (targetTeamData == null) return;

        int previousScore = targetTeamData.teamScore;
        int newScore = targetTeamData.teamScore + scoreChangeAmount;

        ReceiveTeamScoreAtClientRpc(teamToChangeScore, newScore, previousScore);
    }

    public void InitializeTeamScores()
    {
        foreach (var teamData in teamDataList)
            ReceiveTeamScoreAtClientRpc(teamData.team, teamData.teamScore, 0);
    }

    [ClientRpc]
    private void ReceiveTeamScoreAtClientRpc(Team teamToChangeScore, int scoreAtServerAfterChange, int scoreAtServerBeforeChange)
    {
        TeamData teamData = teamDataList.Find(t => t.team == teamToChangeScore);
        if (teamData == null) return;

        if (teamData.teamScore < scoreAtServerAfterChange)
            teamData.teamScore = scoreAtServerAfterChange;

        UpdateTeamScoreEvent.RaiseEvent(teamToChangeScore, scoreAtServerAfterChange, scoreAtServerBeforeChange);
    }
}
