using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TeamData : MonoBehaviour
{
    public Team team;
    public List<Controller> teamMembers = new();
    public Renderer teamSpawnArea;
    public int teamScore;
    public List<PlayerScoreEntry> playerScores = new();
}
