using UnityEngine;
using Unity.Netcode;

public class Controller : NetworkBehaviour
{
    public bool IsBot { get; private set; }
    public ulong? OwnerId { get; private set; } 
    public Team Team { get; private set; }

    public virtual void Initialize() => Debug.LogError("Failed virtual override");
    public void InitializeEntity(bool isBot, ulong? ownerId, Team team)
    {
        IsBot = isBot;
        OwnerId = ownerId;
        Team = team;
    }
}
