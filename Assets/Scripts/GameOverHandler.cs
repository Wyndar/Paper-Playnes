using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

public class GameOverPanelHandler : MonoBehaviour
{
    private Button panelButton;

    private void Awake()
    {
        panelButton = GetComponent<Button>();
        _ = ButtonCheck();
    }

    private void OnEnable()
    {
        if (ButtonCheck())
            panelButton.onClick.AddListener(HandleGameOverClick);
    }

    private void OnDisable() => panelButton.onClick.RemoveAllListeners();
    private bool ButtonCheck()
    {
        if (panelButton == null)
        {
            Debug.LogError("GameOverPanelHandler: Button component not found!");
            return false;
        }
        return true;
    }
    private void HandleGameOverClick()
    {
        Debug.Log("Game Over panel clicked! Disconnecting...");
        StartCoroutine(DisconnectAndReturnToHome());
    }

    private IEnumerator DisconnectAndReturnToHome()
    {
        if (NetworkManager.Singleton.IsConnectedClient)
            yield return MultiplayerManager.Instance.LeaveGameSession();
        SceneLoadingManager.Instance.LoadScene(LoadingMode.Local, "Home");
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
        yield break;
    }
}
