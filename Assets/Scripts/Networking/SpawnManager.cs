using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance { get; private set; }
    public GameObject playerNetworkPrefab;
    public GameObject destBoxPrefab;
    public GameObject puBoxPrefab;
    public GameObject birdsPrefab;
    public GameObject minesPrefab;
    public int spawnBoxCount;
    public Renderer spawnRenderer;

    public string[] botNames = new string[20];
    public List<Controller> activeControllers = new();
    private int botNamesTaken = 0;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    //private void Start()
    //{
    //    if (!IsServer) return;
    //    InstantiateBoxes();
    //    RearrangeBoxes();
    //}

    private void InstantiateBoxes()
    {
        for (int i = 0; i < spawnBoxCount; i++)
        {
            SpawnShit(puBoxPrefab);
            SpawnShit(minesPrefab);
            SpawnShit(birdsPrefab);
            SpawnShit(destBoxPrefab);
        }
    }
    private void SpawnShit(GameObject prefab)
    {
        GameObject pu = Instantiate(prefab);
        pu.GetComponent<NetworkObject>().SpawnWithOwnership(NetworkManager.ServerClientId);
        pu.transform.SetParent(transform);
        if (pu.TryGetComponent(out BirdAI bird))
            bird.flightArea = spawnRenderer;
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

    public (Vector3, Quaternion) GetRandomSpawnPoint(Team team)
    {
        Renderer spawnArea = TeamManager.Instance.teamDataList.Find(x => x.team == team).teamSpawnArea;
        Bounds bounds = spawnArea.bounds;
        Vector3 vector = new(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
        Quaternion quaternion = spawnArea.transform.rotation;
        return (vector, quaternion);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestPlayerSpawnServerRpc(ulong clientId) => SpawnEntity(clientId, false);

    [ServerRpc(RequireOwnership = false)]
    public void RequestBotSpawnServerRpc() => SpawnEntity();
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
    private void SendSpawnedPlayerCallbackClientRpc(NetworkObjectReference entityObject, bool isBot) => StartCoroutine(ResolveSpawnedPlayerCallback(entityObject, isBot));

    private IEnumerator ResolveSpawnedPlayerCallback(NetworkObjectReference entityObject, bool isBot)
    {
        if (!entityObject.TryGet(out NetworkObject netObj))
            yield break;
        Destroy(isBot ? netObj.gameObject.GetComponent<PlayerController>() : netObj.gameObject.GetComponent<BotController>());
        Controller controller = isBot ? netObj.gameObject.GetComponent<BotController>() : netObj.gameObject.GetComponent<PlayerController>();
        controller.enabled = true;
        RegisterController(controller);

        //to ensure we don't call the wrong initialize on a destroyed controller, skip a few frames
        yield return new WaitForSeconds(0.2f);
        if (NetworkManager.Singleton.LocalClientId == netObj.OwnerClientId)
            TeamManager.Instance.RequestTeamAssignmentServerRpc(entityObject);
        yield break;
    }
   
    public void TriggerControllerInitialize(Controller controller, Team team)
    {
        if (!IsServer) return;
        InitializeControllerClientRpc(controller.GetComponent<NetworkObject>(), team);
    }
    [ClientRpc]
    private void InitializeControllerClientRpc(NetworkObjectReference networkObjectReference, Team team)
    {
        if (networkObjectReference.TryGet(out NetworkObject netObj) && netObj.TryGetComponent(out Controller component))    
            component.Initialize(team);
    }
    public string GetBotName()
    {
        string returnString = botNames[botNamesTaken];
        botNamesTaken++;
        return returnString;
    }
}
