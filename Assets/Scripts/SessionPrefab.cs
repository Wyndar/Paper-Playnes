using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

public class SessionPrefab : MonoBehaviour
{
    public TMP_Text sessionNameText;
    public TMP_Text gameTypeText;
    public TMP_Text playerCountText;
    public TMP_Text sessionTypeText;
    public ISessionInfo sessionInfo;
    private Image backgroundImage;

    private Color defaultColor;
    public Color highlightColor = Color.yellow;

    private void OnEnable()
    {
        backgroundImage = GetComponent<Image>();
        defaultColor = backgroundImage.color;
    }

    public void SetSession(ISessionInfo sessionInfo)
    {
        this.sessionInfo = sessionInfo;
        sessionNameText.text = sessionInfo.Name;
        playerCountText.text = $"{sessionInfo.MaxPlayers - sessionInfo.AvailableSlots} / {sessionInfo.MaxPlayers}";
        sessionTypeText.text = sessionInfo.HasPassword ? "Locked" : "Open";
        SetHighlighted(false);
    }

    public void SetHighlighted(bool isHighlighted)
    {
        backgroundImage.color = isHighlighted ? highlightColor : defaultColor;
    }
}
