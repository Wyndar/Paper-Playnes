using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using Unity.Services.Multiplayer;
using TMPro;
using System.Collections;
using System;
using Unity.Netcode;

public class LobbyUIManager : MonoBehaviour
{
    public Button hostButton, joinButton, refreshButton, startGameButton;
    public TMP_InputField sessionIdInput;
    public Transform sessionListContainer, playerListContainer;
    public GameObject sessionPrefab, playerEntryPrefab;

    public GameObject loadingPanel, roomPanel, messagePanel;
    public TMP_Text loadingText, messageText, roomHeaderText;

    private List<SessionPrefab> sessionPool = new();
    private List<PlayerEntryPrefab> playerEntries = new();
    private SessionPrefab selectedSession;

    public static LobbyUIManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDisable() => InputManager.Instance.OnTouch -= HandleTap;

    private void Start()
    {
        InputManager.Instance.OnTouch += HandleTap;
        startGameButton.gameObject.SetActive(false);
        foreach (Transform t in sessionListContainer.transform)
            sessionPool.Add(t.GetComponent<SessionPrefab>());
        foreach(Transform t in playerListContainer.transform)
            playerEntries.Add(t.GetComponent<PlayerEntryPrefab>());
    }

    public void OnHostButtonClicked() => _ = HostGame();
    public void OnJoinButtonClicked() => _ = JoinSelectedSession();
    public void OnRefreshButtonClicked() => _ = RefreshSessionList();
    public void OnLeaveLobbyButtonClicked() => _ = LeaveRoom();
    public void OnStartGameButtonClicked() => MultiplayerManager.Instance.StartGameSession();

    private async Task HostGame()
    {
        ShowLoading("Creating session...");
        try
        {
            await MultiplayerManager.Instance.CreateGameSession();
            roomPanel.SetActive(true);
            ShowMessage("Session Created Successfully!", false);
        }
        catch (Exception e)
        {
            ShowMessage($"Error: {e.Message}", true);
            _ = RefreshSessionList();
        }
        finally
        {
            HideLoading();
        }
    }

    private async Task JoinSelectedSession()
    {
        if (selectedSession == null)
        {
            ShowMessage("Please select a session before joining.", true);
            return;
        }

        ShowLoading("Joining session...");
        try
        {
            await MultiplayerManager.Instance.JoinGameSession(selectedSession.sessionInfo.Id);
            roomPanel.SetActive(true);
            ShowMessage("Joined session successfully!", false);
        }
        catch (Exception e)
        {
            ShowMessage($"Error: {e.Message}", true);
            _ = RefreshSessionList();
        }
        finally
        {
            HideLoading();
        }
    }

    private async Task RefreshSessionList()
    {
        ShowLoading("Loading available sessions...");
        try
        {
            foreach (var session in sessionPool)
                session.gameObject.SetActive(false);

            IList<ISessionInfo> sessions = await MultiplayerManager.Instance.ListAvailableSessions();

            if (sessions.Count == 0)
                ShowMessage("No available sessions found.", true);

            for (int i = 0; i < sessions.Count; i++)
            {
                SessionPrefab sessionInstance;

                if (i < sessionPool.Count)
                    sessionInstance = sessionPool[i];
                else
                {
                    GameObject sessionObj = Instantiate(sessionPrefab, sessionListContainer);
                    sessionInstance = sessionObj.GetComponent<SessionPrefab>();
                    sessionPool.Add(sessionInstance);
                }
                sessionInstance.gameObject.SetActive(true);
                sessionInstance.SetSession(sessions[i]);
                Button button = sessionInstance.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectSession(sessionInstance));
            }
        }
        catch (Exception e)
        {
            ShowMessage($"Error: {e.Message}", true);
        }
        finally
        {
            HideLoading();
        }
    }
    private void HandleTap() => StartCoroutine(DelayedTapCheck());

    private IEnumerator DelayedTapCheck()
    {
        yield return new WaitForEndOfFrame();

        if (EventSystem.current.IsPointerOverGameObject())
        {
            GameObject clickedObject = EventSystem.current.currentSelectedGameObject;
            if (clickedObject != null && clickedObject == joinButton.gameObject)
                yield break;

            if (clickedObject != null && clickedObject.TryGetComponent(out SessionPrefab session))
            {
                SelectSession(session);
                yield break;
            }
        }

        DeselectSession();
        yield break;
    }

    private void SelectSession(SessionPrefab session)
    {
        if (selectedSession != null)
            selectedSession.SetHighlighted(false);

        selectedSession = session;
        selectedSession.SetHighlighted(true);
    }

    private void DeselectSession()
    {
        if (selectedSession != null)
        {
            selectedSession.SetHighlighted(false);
            selectedSession = null;
        }
    }
    public void UpdatePlayerList()
    {
        int index = 0;
        
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId == NetworkManager.ServerClientId)
                roomHeaderText.text = client.PlayerObject.GetComponent<PlayerNetworkData>().PlayerName.Value.ToString()+ "'s Room";
            PlayerEntryPrefab playerEntry;

            if (index < playerEntries.Count)
                playerEntry = playerEntries[index];
            else
            {
                playerEntry = Instantiate(playerEntryPrefab, playerListContainer).GetComponent<PlayerEntryPrefab>();
                playerEntries.Add(playerEntry);
            }
            playerEntry.gameObject.SetActive(true);
            if (client.PlayerObject.TryGetComponent(out PlayerNetworkData playerData))
                playerEntry.SetPlayer(playerData);
            index++;
        }
        for (int i = index; i < playerEntries.Count; i++)
            playerEntries[i].gameObject.SetActive(false);
        startGameButton.gameObject.SetActive(NetworkManager.Singleton.IsServer);
    }


    private async Task LeaveRoom()
    {
        ShowLoading("Leaving room...");

        try
        {
            if (NetworkManager.Singleton.IsHost)
            {
                await MultiplayerManager.Instance.LeaveGameSession();
            }
            else
            {
                NetworkManager.Singleton.Shutdown();
            }
            roomPanel.SetActive(false);
            startGameButton.gameObject.SetActive(false); 
        }
        catch (Exception e)
        {
            ShowMessage($"Error: {e.Message}", true);
        }
        finally
        {
            HideLoading();
        }
    }

    private void ShowLoading(string message)
    {
        loadingText.text = message;
        loadingPanel.SetActive(true);
    }

    private void HideLoading() => loadingPanel.SetActive(false);

    private void ShowMessage(string message, bool isError)
    {
        messageText.text = message;
        messageText.color = isError ? Color.red : Color.green;
        messagePanel.SetActive(true);
    }
}
