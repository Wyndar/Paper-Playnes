using UnityEngine;
using Unity.Netcode;

public class Controller : NetworkBehaviour
{
    public bool IsBot;
    public ulong? OwnerId; 
    public Team Team;

    public virtual void Initialize() => Debug.LogError("Failed virtual override");
    public void InitializeEntity(bool isBot, ulong? ownerId, Team team)
    {
        IsBot = isBot;
        OwnerId = ownerId;
        Team = team;
    }
}
