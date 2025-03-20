using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class GameOverManager : NetworkBehaviour
{
    public GameObject gameOverPanel;
    private bool gameOverTriggered = false;

    public void TriggerGameOver()
    {
        if (!IsServer || gameOverTriggered) return;

        gameOverTriggered = true;
        Debug.Log("Game Over triggered! Despawning all players...");

        StartCoroutine(DespawnPlayersAndFinalizeGameOver());
    }

    private IEnumerator DespawnPlayersAndFinalizeGameOver()
    {
        for (int i = SpawnManager.Instance.activeControllers.Count - 1; i >= 0; i--)
        {
            Controller player = SpawnManager.Instance.activeControllers[i];
            if (player != null && player.TryGetComponent(out NetworkObject networkObject))
            {
                networkObject.Despawn();
                Debug.Log($"Despawning player: {player.gameObject.name}");
            }
        }
        yield return new WaitForEndOfFrame();
        FinalizeGameOver();
    }

    private void FinalizeGameOver()
    {
        Debug.Log("Notifying all clients of Game Over...");
        NotifyClientsGameOverClientRpc();
    }

    [ClientRpc]
    private void NotifyClientsGameOverClientRpc() => gameOverPanel.SetActive(true);
}
