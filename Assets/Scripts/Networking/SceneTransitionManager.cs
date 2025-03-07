using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : NetworkBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }
    private int clientsLoaded = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoaded;
    }

    private void OnSceneLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        Debug.Log($"Client {clientId} finished loading scene: {sceneName}");

        if (IsServer)
        {
            TeamManager.Instance.RequestTeamAssignmentServerRpc(clientId);
            ClientLoadedScene(clientId);
        }
    }

    public void ClientLoadedScene(ulong clientId)
    {
        clientsLoaded++;
        Debug.Log($"Client {clientId} loaded. {clientsLoaded}/{NetworkManager.Singleton.ConnectedClientsList.Count} clients ready.");

        if (clientsLoaded == NetworkManager.Singleton.ConnectedClientsList.Count)
            Debug.Log("All clients have loaded the scene.");
    }

    [ServerRpc]
    public void NotifyServerLoadedSceneServerRpc(ulong clientId) => ClientLoadedScene(clientId);
}
