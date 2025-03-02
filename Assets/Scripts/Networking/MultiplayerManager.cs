using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.CloudSave;
using Unity.Services.Multiplayer;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerManager : MonoBehaviour
{
    private const string PlayerNameString = "PlayerName";
    private const string PlayerLevelString = "PlayerLevel";
    private const string ProfilePictureString = "ProfilePicture";
    private IHostSession currentSession;
    public static string PlayerName { get; private set; }
    public static int PlayerLevel { get; private set; }
    public static string ProfilePicture { get; private set; }
    public static string PlayerID { get; private set; }
    public static MultiplayerManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }
    async void Start()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        PlayerID = AuthenticationService.Instance.PlayerId;
        Debug.Log($"Player Authenticated with ID: {PlayerID}");
        await LoadPlayerData();
        ProfileManager.Instance.Initialize();
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }
    public static async Task SetPlayerInfo(string name, int level)
    {
        PlayerName = name;
        PlayerLevel = level;

        Dictionary<string, object> data = new()
        {
            { PlayerNameString, name },
            { PlayerLevelString, level }
        };

        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
    }

    private static async Task LoadPlayerData()
    {
        try
        {
            var savedData = await CloudSaveService.Instance.Data.Player.LoadAsync
                (new HashSet<string> { PlayerNameString, PlayerLevelString, ProfilePictureString });

            if (savedData.ContainsKey(PlayerNameString))
                PlayerName = savedData[PlayerNameString].Value.GetAsString();
            else
                PlayerName = "Guest";

            if (savedData.ContainsKey(PlayerLevelString))
                PlayerLevel = int.Parse(savedData[PlayerLevelString].Value.GetAsString());
            else
                PlayerLevel = 1;
            if (savedData.ContainsKey(ProfilePictureString))
                ProfilePicture = savedData[ProfilePictureString].Value.GetAsString();
            else
                ProfilePicture = "ammo";
        }
        catch
        {
            PlayerName = "Guest";
            PlayerLevel = 1;
            ProfilePicture = "ammo";
        }
    }
    public async void SavePlayerData(string playerName, int playerLevel, string selectedProfilePicture)
    {
        PlayerName = playerName;
        PlayerLevel = playerLevel;
        ProfilePicture = selectedProfilePicture;
        Dictionary<string, object> data = new()
        {
            { PlayerNameString, playerName },
            { PlayerLevelString, playerLevel },
            { ProfilePictureString, selectedProfilePicture }
        };
        await CloudSaveService.Instance.Data.Player.SaveAsync(data);
        Debug.Log("Profile Saved!");
    }
    public async Task CreateGameSession()
    {
        try
        {
            var sessionOptions = new SessionOptions
            {
                MaxPlayers = 10,
                IsPrivate = false,
                IsLocked = false
            };

            currentSession = await MultiplayerService.Instance.CreateSessionAsync(sessionOptions);
            Debug.Log($"Game session created! Session ID: {currentSession.Id}");
            NetworkManager.Singleton.StartHost();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create session: {e.Message}");
        }
    }

    public async Task JoinGameSession(string sessionId)
    {
        try
        {
            await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
            Debug.Log($"Joined game session: {sessionId}");
            NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join session: {e.Message}");
        }
    }

    public async Task<IList<ISessionInfo>> ListAvailableSessions()
    {
        try
        {
            IList<ISessionInfo> sessions = (await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions())).Sessions;
            Debug.Log($"Found {sessions.Count} available sessions.");
            return sessions;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to list sessions: {e.Message}");
            return null;
        }
    }

    public async Task LeaveGameSession()
    {
        try
        {
            if (currentSession != null)
            {
                await currentSession.DeleteAsync();
                Debug.Log($"Session {currentSession.Id} closed.");
                currentSession = null;

                if (NetworkManager.Singleton.IsServer)
                    NetworkManager.Singleton.Shutdown();
                else if (NetworkManager.Singleton.IsClient)
                    NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to leave session: {e.Message}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameSessionServerRpc()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Starting game session, changing scene...");
            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
    }
    public void StartGameSession()
    {
        if (NetworkManager.Singleton.IsServer)
            StartGameSessionServerRpc();
    }
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected.");
        if (clientId == NetworkManager.ServerClientId)
        {
            Debug.Log("Host disconnected!");
            //AssignNewHost();
        }
        if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.ServerClientId,
            out NetworkClient hostClient) && hostClient.PlayerObject != null && hostClient.PlayerObject.TryGetComponent(out PlayerNetworkData playerData))
            playerData.UpdatePlayerListRpc();
    }

    //private void AssignNewHost()
    //{
    //    if (NetworkManager.Singleton.IsServer) return; 

    //    var newHostClientId = NetworkManager.Singleton.ConnectedClients.Keys
    //        .Where(clientId => clientId != NetworkManager.ServerClientId) 
    //        .OrderBy(id => id) 
    //        .FirstOrDefault();

    //    if (newHostClientId == 0) return; 

    //    Debug.Log($"New host selected: {newHostClientId}");
    //    NetworkManager.Singleton.Shutdown(); 
    //    NetworkManager.Singleton.StartHost();
    //}

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected.");
        if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.ServerClientId,
            out NetworkClient hostClient) && hostClient.PlayerObject != null && hostClient.PlayerObject.TryGetComponent(out PlayerNetworkData playerData))
            playerData.UpdatePlayerListRpc();
    }

    public void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }
}
