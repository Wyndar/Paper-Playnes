using UnityEngine;

public class InputManager : MonoBehaviour
{
    public delegate void PressEvent();

    public event PressEvent OnStartMove;
    public event PressEvent OnEndMove;
    public event PressEvent OnFirePrimaryWeapon;
    public event PressEvent OnReload;
    public event PressEvent OnBoost;
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
        playerInputs = new PlayerInputList();
    }

    private void OnEnable() => playerInputs.Enable();

    private void OnDisable() => playerInputs?.Disable();

    private void Start()
    {
        playerInputs.Player.Movement.started += context => StartedMoveVectorInput();
        playerInputs.Player.Movement.canceled += context => EndedMoveVectorInput();
        playerInputs.Player.FirePrimary.started += context => FiredPrimaryWeapon();
        playerInputs.Player.Reload.started += context => TriggerReload();
        playerInputs.Player.Boost.started += context => TriggerBoost();
        playerInputs.Player.Tap.started += context => TouchedScreen();
    }

    private void StartedMoveVectorInput() => OnStartMove?.Invoke();

    private void EndedMoveVectorInput() => OnEndMove?.Invoke();

    private void FiredPrimaryWeapon() => OnFirePrimaryWeapon?.Invoke();
    private void TriggerReload() => OnReload?.Invoke();
    private void TriggerBoost() => OnBoost?.Invoke();

    private void TouchedScreen() => OnTouch?.Invoke();

    public Vector2 GetCurrentTouchPosition => playerInputs.Player.Position.ReadValue<Vector2>();
    public Vector2 CurrentMoveVector => playerInputs.Player.Movement.ReadValue<Vector2>();  

}



