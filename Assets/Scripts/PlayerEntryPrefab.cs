using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerEntryPrefab : MonoBehaviour
{
    public TMP_Text playerNameText;
    public TMP_Text playerLevelText;
    public Image playerImage;
    public Toggle readyToggle;
    private PlayerNetworkData playerData;

    public void SetPlayer(PlayerNetworkData data)
    {
        playerData = data;
        string v = playerData.OwnerClientId == NetworkManager.ServerClientId ? " (Host)" : "";
        playerNameText.text = data.PlayerName.Value.ToString() + v;
        playerLevelText.text = $"Level {data.PlayerLevel.Value}";
        playerImage.sprite = Resources.Load<Sprite>($"Sprites/{data.ProfilePicture.Value}");
        readyToggle.isOn = data.IsReady.Value;

        data.PlayerName.OnValueChanged += (oldValue, newValue) => playerNameText.text = newValue.ToString();
        data.PlayerLevel.OnValueChanged += (oldValue, newValue) => playerLevelText.text = $"Level {newValue}";
        data.IsReady.OnValueChanged += (oldValue, newValue) => readyToggle.isOn = newValue;

        readyToggle.onValueChanged.RemoveAllListeners();
        readyToggle.interactable = playerData.IsOwner;
        readyToggle.onValueChanged.AddListener(value =>
        {
            if (playerData.IsOwner)
                playerData.SetReadyStatus(value);
        });
    }
}
