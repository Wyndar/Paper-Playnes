using UnityEngine;
using Unity.Netcode;
using System.Collections;
using log4net.Util;
using Unity.Netcode.Components;

public class Controller : NetworkBehaviour
{
    public bool IsBot;
    public ulong? OwnerId;
    public Team Team;
    public HealthComponent healthComponent;
    public HealthBar healthBar;
    public PickUpHandler pickUpHandler;
    public Rigidbody rb;

    public virtual void Initialize() => Debug.LogError("Failed virtual override");
    public void InitializeEntity(bool isBot, ulong? ownerId, Team team)
    {
        IsBot = isBot;
        OwnerId = ownerId;
        Team = team;
        SpawnManager.Instance.RegisterController(this);
        rb = GetComponent<Rigidbody>();
        healthBar = GetComponent<HealthBar>();
        pickUpHandler = GetComponent<PickUpHandler>();
        healthComponent = GetComponent<HealthComponent>();
        healthBar.enabled = true;
        pickUpHandler.enabled = true;
        healthComponent.enabled = true;
        Respawn(gameObject);
    }
    public void Respawn(GameObject _)
    {
        Team currentTeam = TeamManager.Instance.GetTeam(this);
        Vector3 newSpawnPosition = SpawnManager.Instance.GetRandomSpawnPoint(currentTeam);

        if (TryGetComponent(out NetworkTransform networkTransform))
            networkTransform.Teleport(newSpawnPosition, Quaternion.identity,transform.localScale);
        else
            throw new MissingComponentException("Attach a Network Transform");
        healthComponent.InitializeHealth();
    }
}
