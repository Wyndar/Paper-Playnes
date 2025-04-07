using UnityEngine;

public class InputManager : MonoBehaviour
{
    public delegate void PressEvent();

    public event PressEvent OnStartMove;
    public event PressEvent OnEndMove;
    public event PressEvent OnStartFireWeapon;
    public event PressEvent OnEndFireWeapon;
    public event PressEvent OnReload;
    public event PressEvent OnBarrelRoll;
    public event PressEvent OnCycleWeaponUp;
    public event PressEvent OnCycleWeaponDown;
    public event PressEvent OnUseItem;
    public event PressEvent OnStartBoost;
    public event PressEvent OnEndBoost;
    public event PressEvent OnTouch;

    private PlayerInputList playerInputs;
    public static InputManager Instance { get; private set; }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        playerInputs ??= new PlayerInputList();
    }

    private void OnEnable() => playerInputs?.Enable();

    private void OnDisable() => playerInputs?.Disable();

    private void Start()
    {
        playerInputs.Player.Movement.started += context => StartedMoveVectorInput();
        playerInputs.Player.Movement.canceled += context => EndedMoveVectorInput();
        playerInputs.Player.FireWeapon.started += context => StartedFiringPrimaryWeapon();
        playerInputs.Player.FireWeapon.canceled += context => EndedFiringPrimaryWeapon();
        playerInputs.Player.Reload.started += context => TriggerReload();
        playerInputs.Player.Roll.started += context => TriggerBarrelRoll();
        playerInputs.Player.CycleWeaponUp.started += context => CycleWeaponUp();
        playerInputs.Player.CycleWeaponDown.started += context => CycleWeaponDown();
        playerInputs.Player.UseItem.started += context => TriggerItemUse();
        playerInputs.Player.Boost.started += context => StartedBoost();
        playerInputs.Player.Boost.canceled += context => EndedBoost();
        playerInputs.Player.Tap.started += context => TouchedScreen();
    }

    private void StartedMoveVectorInput() => OnStartMove?.Invoke();
    private void EndedMoveVectorInput() => OnEndMove?.Invoke();
    private void StartedFiringPrimaryWeapon() => OnStartFireWeapon?.Invoke();
    private void EndedFiringPrimaryWeapon() => OnEndFireWeapon?.Invoke();
    public bool IsFiringPrimaryWeapon() => playerInputs.Player.FireWeapon.IsInProgress();
    private void TriggerReload() => OnReload?.Invoke();
    private void TriggerBarrelRoll() => OnBarrelRoll?.Invoke();
    private void CycleWeaponUp() => OnCycleWeaponUp?.Invoke();
    private void CycleWeaponDown() => OnCycleWeaponDown?.Invoke();
    private void TriggerItemUse() => OnUseItem?.Invoke();
    private void StartedBoost() => OnStartBoost?.Invoke();
    private void EndedBoost() => OnEndBoost?.Invoke();
    public bool IsBoosting() => playerInputs.Player.Boost.IsInProgress();
    private void TouchedScreen() => OnTouch?.Invoke();
    public Vector2 GetCurrentTouchPosition => playerInputs.Player.Position.ReadValue<Vector2>();
    public Vector2 CurrentMoveVector => playerInputs.Player.Movement.ReadValue<Vector2>();  
}