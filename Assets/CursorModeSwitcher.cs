using UnityEngine;
using UnityEngine.InputSystem;

public class CursorModeSwitcher : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private GameObject floatingCursorObject;
    [SerializeField] private bool debug = false;

    private void OnEnable()
    {
        playerInput.onControlsChanged += OnControlsChanged;
        UpdateCursorForScheme(playerInput.currentControlScheme);
    }

    private void OnDisable() => playerInput.onControlsChanged -= OnControlsChanged;

    private void OnControlsChanged(PlayerInput input) => UpdateCursorForScheme(input.currentControlScheme);

    private void UpdateCursorForScheme(string scheme)
    {
        if (debug) Debug.Log("Control Scheme changed to: " + scheme);

        bool isGamepad = scheme == "Gamepad";

        floatingCursorObject.SetActive(isGamepad);
        Cursor.visible = !isGamepad;
        Cursor.lockState = isGamepad ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
