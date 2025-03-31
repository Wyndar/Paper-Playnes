using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class TeamManager : NetworkBehaviour
{
    public GameEvent UpdateTeamScoreEvent;
    public static TeamManager Instance { get; private set; }
    public List<TeamData> teamDataList = new();
    [Header("Team Colors")]
    public Color redColor;
    public Color blueColor;
    public Color yellowColor;
    public Color greenColor;
    public Color defaultColor = Color.white;
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
    public Color GetTeamColor(Team team) => team switch
    {
        Team.RedTeam => redColor,
        Team.BlueTeam => blueColor,
        Team.YellowTeam => yellowColor,
        Team.GreenTeam => greenColor,
        _ => defaultColor
    };
    [ServerRpc(RequireOwnership = false)]
    public void RequestTeamAssignmentServerRpc(NetworkObjectReference entityRef)
    {
        if (entityRef.TryGet(out NetworkObject entityObj) && entityObj.TryGetComponent(out Controller controller))
            SpawnManager.Instance.TriggerControllerInitialize(controller, AssignTeam(controller));
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
            RequestScoreChangeServerRpc(teamData.team, teamData.teamScore, 0);
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
