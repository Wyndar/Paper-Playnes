using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance { get; private set; }

    [Header("Profile UI Elements")]
    public TMP_InputField nameInputField;
    public TMP_Text levelText, nameText;
    public Image selectedProfileImage;
    public Image profileEditScreenImage;
    public GameObject profilePanel, profilePictureContainer;
    public Button saveProfileButton;
    public Button closeProfileButton;

    private string selectedProfilePicture;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        if (MultiplayerManager.Instance != null)
            Initialize();
    }
    public void Initialize()
    {
        LoadPlayerProfile();
        saveProfileButton.onClick.AddListener(SavePlayerProfile);
        closeProfileButton.onClick.AddListener(CloseProfilePanel);
    }

    private void LoadPlayerProfile()
    {
        nameInputField.text = MultiplayerManager.PlayerName;
        nameText.text = MultiplayerManager.PlayerName;
        levelText.text = MultiplayerManager.PlayerLevel.ToString();
        selectedProfilePicture = MultiplayerManager.ProfilePicture;
        UpdateProfilePictureUI(selectedProfilePicture);
    }

    private void SavePlayerProfile()
    {
        string playerName = nameInputField.text;
        nameText.text = nameInputField.text;
        int playerLevel = int.Parse(levelText.text.Replace("Level ", ""));
        MultiplayerManager.Instance.SavePlayerData(playerName, playerLevel, selectedProfilePicture);
    }

    public void SelectProfilePicture(string pictureName)
    {
        selectedProfilePicture = pictureName;
        UpdateProfilePictureUI(pictureName);
    }

    private void UpdateProfilePictureUI(string pictureName)
    {
        Sprite profileSprite = Resources.Load<Sprite>($"Sprites/{pictureName}");
        if (profileSprite != null)
        {
            selectedProfileImage.sprite = profileSprite;
            profileEditScreenImage.sprite = profileSprite;
            profilePictureContainer.SetActive(false);
        }
    }

    private void CloseProfilePanel() => profilePanel.SetActive(false);
}
