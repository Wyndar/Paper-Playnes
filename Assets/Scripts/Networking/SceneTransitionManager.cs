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
            SpawnManager.Instance.RequestPlayerSpawnServerRpc(clientId);
            ClientLoadedScene(clientId);
            //this magic number should be gotten from gamemode settings later
            int botCount = 20 - NetworkManager.Singleton.ConnectedClientsList.Count;
            for (int i = botCount - 1; i >= 0; i--)
                SpawnManager.Instance.RequestBotSpawnServerRpc();
        }
    }

    public void ClientLoadedScene(ulong clientId)
    {
        clientsLoaded++;
        Debug.Log($"Client {clientId} loaded. {clientsLoaded}/{NetworkManager.Singleton.ConnectedClientsList.Count} clients ready.");
        //this should be a proper broadcast as well that notifies player joining and leaving
        if (clientsLoaded == NetworkManager.Singleton.ConnectedClientsList.Count)
            Debug.Log("All clients have loaded the scene.");
    }

    [ServerRpc]
    public void NotifyServerLoadedSceneServerRpc(ulong clientId) => ClientLoadedScene(clientId);

}
