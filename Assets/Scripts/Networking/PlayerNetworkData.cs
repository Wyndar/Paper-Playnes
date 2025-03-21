using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkData : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> PlayerName = new("Guest", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> PlayerLevel = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString32Bytes> ProfilePicture = new("ammo", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> IsReady = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            PlayerName.Value = MultiplayerManager.PlayerName;
            PlayerLevel.Value = MultiplayerManager.PlayerLevel;
            ProfilePicture.Value = MultiplayerManager.ProfilePicture;
            gameObject.name = MultiplayerManager.PlayerName + "'s data.";
        }
    }

    public void SetReadyStatus(bool ready)
    {
        if (IsOwner)
            IsReady.Value = ready;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void UpdatePlayerListRpc()
    {
        Debug.Log(NetworkManager.Singleton.LocalClientId);
        LobbyUIManager.Instance.UpdatePlayerList();
    }
}
