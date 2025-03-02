using UnityEngine;
using UnityEngine.UI;

public class ProfilePictureButton : MonoBehaviour
{
    public string profilePictureName;
    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
        profilePictureName=GetComponent<Image>().sprite.name;
        button.onClick.AddListener(() => ProfileManager.Instance.SelectProfilePicture(profilePictureName));
    }
}
