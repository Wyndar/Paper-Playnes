using Unity.Netcode;
using UnityEngine;

public class GameOverManager : NetworkBehaviour
{
    public GameEvent gameOverEvent;

    private bool gameOverTriggered = false;

    public void TriggerGameOver()
    {
        if (!IsServer || gameOverTriggered) return;

        gameOverTriggered = true;
        Debug.Log("Game Over triggered! Notifying all clients...");
        gameOverEvent.RaiseEvent();
        NotifyClientsGameOverClientRpc();
    }

    [ClientRpc]
    private void NotifyClientsGameOverClientRpc() => gameOverEvent.RaiseEvent();
}
