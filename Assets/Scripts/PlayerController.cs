using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public enum AutoLevelMode { Off, On }
    public InputManager inputManager;
    private HealthComponent healthComponent;
    private HealthBar healthBar;
    private DestructibleComponent destructibleComponent;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float maxTiltAngle = 45f;
    [SerializeField] private float turnAcceleration = 30f;
    [SerializeField] private float pitchAcceleration = 20f;
    [SerializeField] private float turnDecayRate = 2f;
    [SerializeField] private float pitchDecayRate = 2f;
    [SerializeField] private bool isInverted = false;
    [SerializeField] private AutoLevelMode autoLevelMode = AutoLevelMode.Off;
    [SerializeField] private float autoLevelSpeed = 2f;
    [SerializeField] private float brakeMultiplier = 0.5f;
    [SerializeField] private float barrelRollSpeed = 360f;
    [SerializeField] private float quickTurnMultiplier = 1.5f;

    [Header("Crosshair Settings")]
    [SerializeField] private RectTransform crosshairUI;
    [SerializeField] private HealthComponent crosshairTarget;
    [SerializeField] private Slider crosshairTargetHPBar;
    [SerializeField] private float crosshairRadius = 50f;
    [SerializeField] private float crosshairSpeed = 500f;
    [SerializeField] private float assistRadius = 1.5f; 
    [SerializeField] private float magnetStrength = 8f; 
    [SerializeField] private float maxAssistRange = 1000f; 

    [SerializeField] private Color crosshairColor;
    [SerializeField] private Color crosshairOnTargetColor;

    [Header("Boost Settings")]
    [SerializeField] private float boostMultiplier = 2f;
    [SerializeField] private float boostDuration = 2f;
    [SerializeField] private GameObject boostVFX;

    [Header("Altitude Settings")]
    [SerializeField] private float maxAltitude = 100f;
    [SerializeField] private float minAltitude = 10f;

    [Header("Weight Settings")]
    public int maxWeight;
    public int currentWeight;
    public float weightToSpeedReductionRatio;

    [Header("Weapon Positions")]
    [SerializeField] private Transform leftPrimaryWeaponObject;
    [SerializeField] private Transform rightPrimaryWeaponObject;
    [SerializeField] private Transform leftSecondaryWeaponObject;
    [SerializeField] private Transform rightSecondaryWeaponObject;
    [SerializeField] private Transform undersideWeaponObject;
    [SerializeField] private Transform cockpitWeaponObject;

    [Header("Equipped Weapon Settings")]
    [SerializeField] private Weapon primaryWeapon;
    [SerializeField] private List<Weapon> secondaryWeapons;
    public TMP_Text primaryWeaponAmmoCountText;
    public TMP_Text primaryWeaponMaxAmmoCountText;

    [Header("Wing Trails")]
    [SerializeField] private TrailRenderer leftWingTrail;
    [SerializeField] private TrailRenderer rightWingTrail;
    [SerializeField] private float trailActivationThreshold = 15f;

    [Header("Audio Clip")]
    [SerializeField] private AudioClip rollSound;

    private float boostTimer;
    private float accumulatedYaw = 0f;
    private float accumulatedPitch = 0f;
    private Vector2 crosshairPosition = Vector2.zero;
    public bool isBoosting;
    private bool isTurning;
    private Coroutine moveRoutine;
    private Coroutine shootRoutine;
    private Coroutine crosshairRoutine;
    private const int MAX_TARGETS = 10; 
    private RaycastHit[] assistHits = new RaycastHit[MAX_TARGETS];

    private void Start()
    {
        healthBar = GetComponent<HealthBar>();
        healthComponent = GetComponent<HealthComponent>();
        destructibleComponent = GetComponent<DestructibleComponent>();
    }

    private void OnEnable()
    {
        inputManager.OnStartMove += StartCrosshairMovement;
        inputManager.OnEndMove += StopCrosshairMovement;
        inputManager.OnBoost += StartBoost;
        inputManager.OnStartPrimaryWeapon += StartShooting;
        inputManager.OnEndPrimaryWeapon += StopShooting;
    }
    private void OnDisable()
    {
        inputManager.OnStartMove -= StartMove;
        inputManager.OnEndMove -= StopMove;
        inputManager.OnStartPrimaryWeapon -= StartShooting;
        inputManager.OnEndPrimaryWeapon -= StopShooting;
    }

    private void Update()
    {
        float moveSpeed = isBoosting ? speed * boostMultiplier : speed;
        transform.Translate(moveSpeed * Time.deltaTime * Vector3.forward);

        Vector3 position = transform.position;
        position.y = Mathf.Clamp(position.y, minAltitude, maxAltitude);
        transform.position = position;
        if (autoLevelMode == AutoLevelMode.On)
            AutoLevel();
        HandleWingTrails();
    }
    private void StartCrosshairMovement() => crosshairRoutine ??= StartCoroutine(HandleCrosshairMovement());

    private void StopCrosshairMovement()
    {
        if (crosshairRoutine != null)
            StopCoroutine(crosshairRoutine);
        crosshairRoutine = null;
        isTurning = false;
        StartCoroutine(DecayRotation());
    }

    private IEnumerator HandleCrosshairMovement()
    {
        while (true)
        {
            Vector2 inputVector = inputManager.CurrentMoveVector;

            if (inputVector.magnitude > 0)
            {
                isTurning = true;
                Vector2 newCrosshairPosition = crosshairPosition + (crosshairSpeed * Time.deltaTime * inputVector);
                Vector2 adjustedPosition = ApplyCrosshairMagnetism(newCrosshairPosition);

                bool atLimitX = Mathf.Abs(adjustedPosition.x) >= crosshairRadius;
                bool atLimitY = Mathf.Abs(adjustedPosition.y) >= crosshairRadius;

                if (!atLimitX)
                    crosshairPosition.x = adjustedPosition.x;
                if (!atLimitY)
                    crosshairPosition.y = adjustedPosition.y;
                if (atLimitX || atLimitY)
                {
                    accumulatedYaw += inputVector.x * turnAcceleration * Time.deltaTime;

                    float pitchInput = isInverted ? -inputVector.y : inputVector.y;
                    accumulatedPitch -= pitchInput * pitchAcceleration * Time.deltaTime;
                }
            }
            else
                isTurning = false;
            GetCrosshairWorldPosition();
            crosshairUI.anchoredPosition = crosshairPosition;

            RotatePlayer();
            yield return null;
        }
    }


    private IEnumerator DecayRotation()
    {
        while (!isTurning && (Mathf.Abs(accumulatedYaw) > 0.1f || Mathf.Abs(accumulatedPitch) > 0.1f))
        {
            accumulatedYaw = Mathf.Lerp(accumulatedYaw, 0, Time.deltaTime * turnDecayRate);
            accumulatedPitch = Mathf.Lerp(accumulatedPitch, 0, Time.deltaTime * pitchDecayRate);
            yield return null;
        }
        accumulatedYaw = 0f;
        accumulatedPitch = 0f;
    }
    private void RotatePlayer()
    {
        float pitchInput = isInverted ? -accumulatedPitch : accumulatedPitch;
        Quaternion yawRotation = Quaternion.AngleAxis(accumulatedYaw * rotationSpeed * Time.deltaTime, transform.up);
        Quaternion pitchRotation = Quaternion.AngleAxis(pitchInput * rotationSpeed * Time.deltaTime, transform.right);
        transform.rotation = yawRotation * pitchRotation * transform.rotation;
    }

    private void StartMove() => moveRoutine ??= StartCoroutine(Move());

    private void StopMove()
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);
        moveRoutine = null;
    }
    private void StartBoost() => StartCoroutine(HandleBoost());
    private IEnumerator Move()
    {
        while (true)
        {
            Vector2 inputVector = inputManager.CurrentMoveVector.normalized;
            float pitch = inputVector.y * (isInverted ? -1 : 1) * rotationSpeed * Time.deltaTime;
            float yaw = inputVector.x * rotationSpeed * Time.deltaTime;
            transform.Rotate(pitch, yaw, 0);
            yield return null;
        }
    }
    private void StartShooting() => shootRoutine ??= StartCoroutine(ShootPrimary());
    private void StopShooting()
    {
        if (shootRoutine != null)
            StopCoroutine(shootRoutine);
        shootRoutine = null;
    }

    private IEnumerator ShootPrimary()
    {
        while (true)
        {
            Vector3 target = GetCrosshairWorldPosition();
            primaryWeapon.Fire(target);
            yield return new WaitForSeconds(primaryWeapon.fireRate);
        }
    }
    private Vector3 GetCrosshairWorldPosition()
    {
        Vector3 screenPosition = crosshairUI.position;
        Camera cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            crosshairTarget = hit.collider.GetComponent<HealthComponent>();
            UpdateCrosshairColor(crosshairTarget != null); 
            return hit.point; 
        }
        UpdateCrosshairColor(false);
        return transform.position + transform.forward * 50f;
    }
    
    private void UpdateCrosshairColor(bool isTargetingEnemy)
    {
        if (crosshairUI.TryGetComponent(out Image crosshairImage))
        {
            Color targetColor = isTargetingEnemy ? crosshairOnTargetColor : crosshairColor;
            crosshairImage.color = Color.Lerp(crosshairImage.color, targetColor, Time.deltaTime * 10f);
        }
    }
    
    private Vector2 ApplyCrosshairMagnetism(Vector2 currentPosition)
    {
        Camera cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(currentPosition);

        int numHits = Physics.SphereCastNonAlloc(ray, assistRadius, assistHits, maxAssistRange);

        Transform bestTarget = null;
        float closestAngle = float.MaxValue;
        Vector2 bestScreenPosition = currentPosition;

        for (int i = 0; i < numHits; i++)
        {
            RaycastHit hit = assistHits[i];
            if (!hit.collider.TryGetComponent(out HealthComponent _)) continue;

            Vector3 worldPos = hit.collider.bounds.center;
            Vector2 screenPos = cam.WorldToScreenPoint(worldPos);

            float angle = Vector3.Angle(ray.direction, (worldPos - ray.origin).normalized);

            if (angle < closestAngle)
            {
                closestAngle = angle;
                bestTarget = hit.transform;
                bestScreenPosition = screenPos;
            }
        }

        if (bestTarget != null)
        {
            UpdateCrosshairColor(true);
            return Vector2.Lerp(currentPosition, bestScreenPosition, Time.deltaTime * magnetStrength);
        }

        UpdateCrosshairColor(false);
        return currentPosition;
    }


    private IEnumerator HandleBoost()
    {
        if (!isBoosting)
        {
            isBoosting = true;
            boostTimer = boostDuration;
            boostVFX.SetActive(true);
        }
        while (isBoosting)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0)
            {
                isBoosting = false;
                boostVFX.SetActive(false);
            }
            yield return null;
        }
    }

    private IEnumerator PerformBarrelRoll()
    {
        float rolled = 0f;
        while (rolled < 360f)
        {
            float rollStep = barrelRollSpeed * Time.deltaTime;
            transform.Rotate(0, 0, rollStep);
            rolled += rollStep;
            yield return null;
        }
    }
    private void HandleWingTrails()
    {
        if (isBoosting)
        {
            leftWingTrail.emitting = true;
            rightWingTrail.emitting = true;
            return;
        }
        float roll = Mathf.Abs(inputManager.CurrentMoveVector.x * 30f);
        bool shouldActivate = roll > trailActivationThreshold;

        leftWingTrail.emitting = shouldActivate;
        rightWingTrail.emitting = shouldActivate;
    }

    private void AutoLevel()
    {
        Quaternion currentRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, autoLevelSpeed * Time.deltaTime);
    }

    public void ToggleInversion() => isInverted = !isInverted;
}
