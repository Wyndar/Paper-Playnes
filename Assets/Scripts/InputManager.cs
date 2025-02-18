using UnityEngine;
using UnityEngine.InputSystem;


public class InputManager : MonoBehaviour
{
    public delegate void PressEvent();

    public event PressEvent OnStartMove;
    public event PressEvent OnEndMove;
    public event PressEvent OnStartPrimaryWeapon;
    public event PressEvent OnEndPrimaryWeapon;
    public event PressEvent OnReload;
    public event PressEvent OnBoost;

    private PlayerInputList playerInputs;

    private void Awake() => playerInputs = new PlayerInputList();

    private void OnEnable() => playerInputs.Enable();

    private void OnDisable() => playerInputs.Disable();

    private void Start()
    {
        playerInputs.Player.Movement.started += context => StartedMoveVectorInput();
        playerInputs.Player.Movement.canceled += context => EndedMoveVectorInput();
        playerInputs.Player.FirePrimary.started += context => StartedPrimaryWeapon();
        playerInputs.Player.FirePrimary.canceled += context => EndedPrimaryWeapon();
        playerInputs.Player.Reload.started += context => TriggerReload();
        playerInputs.Player.Boost.started += context => TriggerBoost();
    }

    private void StartedMoveVectorInput() => OnStartMove?.Invoke();

    private void EndedMoveVectorInput() => OnEndMove?.Invoke();

    private void StartedPrimaryWeapon() => OnStartPrimaryWeapon?.Invoke();
    private void EndedPrimaryWeapon() => OnEndPrimaryWeapon?.Invoke();
    private void TriggerReload() => OnReload?.Invoke();
    private void TriggerBoost() => OnBoost?.Invoke();
    public Vector2 CurrentMoveVector => playerInputs.Player.Movement.ReadValue<Vector2>();  
}



