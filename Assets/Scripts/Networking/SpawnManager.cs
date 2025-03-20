using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance { get; private set; }
    public Renderer redTeamSpawnArea;
    public Renderer blueTeamSpawnArea;
    public GameObject playerNetworkPrefab;
    public GameObject destBoxPrefab;
    public GameObject puBoxPrefab;
    public GameObject birdsPrefab;
    public GameObject minesPrefab;
    public int spawnBoxCount;
    public Renderer spawnRenderer;

    public List<Controller> activeControllers = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InstantiateBoxes();
        RearrangeBoxes();
    }

    private void InstantiateBoxes()
    {
        for (int i = 0; i < spawnBoxCount; i++)
        {
            Instantiate(puBoxPrefab).transform.SetParent(transform);
            Instantiate(minesPrefab).transform.SetParent(transform);
            Instantiate(destBoxPrefab).transform.SetParent(transform);
            GameObject bird = Instantiate(birdsPrefab);
            bird.transform.SetParent(transform);
            bird.GetComponent<BirdAI>().flightArea = spawnRenderer;
        }
    }

    public void RearrangeBoxes()
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).SetPositionAndRotation(GetRandomPositionWithinBounds(), transform.rotation);
    }

    public Vector3 GetRandomPositionWithinBounds()
    {
        Bounds bounds = spawnRenderer.bounds;
        Vector3 boxSize = puBoxPrefab.GetComponentInChildren<Renderer>().bounds.size;

        float x = Random.Range(bounds.min.x + boxSize.x / 2, bounds.max.x - boxSize.x / 2);
        float y = Random.Range(bounds.min.y + boxSize.y / 2, bounds.max.y - boxSize.y / 2);
        float z = Random.Range(bounds.min.z + boxSize.z / 2, bounds.max.z - boxSize.z / 2);

        return new Vector3(x, y, z);
    }

    public void RegisterController(Controller controller)
    {
        if (!activeControllers.Contains(controller))
            activeControllers.Add(controller);
    }

    public void UnregisterController(Controller controller)
    {
        if (activeControllers.Contains(controller))
            activeControllers.Remove(controller);
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

    private void SpawnEntity(ulong clientId = NetworkManager.ServerClientId, bool isBot = true)
    {
        if (!IsServer) return;

        GameObject entityObject = Instantiate(playerNetworkPrefab);

        if (entityObject.TryGetComponent(out NetworkObject networkObject))
        {
            networkObject.SpawnWithOwnership(clientId);
            SendSpawnedPlayerCallbackClientRpc(networkObject, isBot);
        }
        else
            Debug.LogError("Player prefab does not have a NetworkObject component!");
    }

    [ClientRpc]
    private void SendSpawnedPlayerCallbackClientRpc(NetworkObjectReference entityObject, bool isBot)
    {
        if (!entityObject.TryGet(out NetworkObject netObj))
            return;
        Destroy(isBot ? netObj.gameObject.GetComponent<PlayerController>() : netObj.gameObject.GetComponent<BotController>());
        Controller controller = isBot ? netObj.gameObject.GetComponent<BotController>() : netObj.gameObject.GetComponent<PlayerController>();
        RegisterController(controller);
        if (NetworkManager.Singleton.LocalClientId == netObj.OwnerClientId)
            TeamManager.Instance.RequestTeamAssignmentServerRpc(entityObject);
    }
   
    public void DesignateSpawnPoint(Controller controller, Team team)
    {
        if (!IsServer) return;
        Vector3 spawnPoint = GetRandomSpawnPoint(team);
        controller.transform.SetPositionAndRotation(spawnPoint, Quaternion.identity);
        InitializeControllerClientRpc(controller.GetComponent<NetworkObject>());
    }
    [ClientRpc]
    private void InitializeControllerClientRpc(NetworkObjectReference networkObjectReference)
    {
        if (networkObjectReference.TryGet(out NetworkObject netObj) && netObj.TryGetComponent(out Controller component))
            component.Initialize();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestPlayerSpawnServerRpc(ulong clientId) => SpawnEntity(clientId, false);

    [ServerRpc(RequireOwnership = false)]
    public void RequestBotSpawnServerRpc() => SpawnEntity();
}
