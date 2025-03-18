using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour
{
    public enum AutoLevelMode { Off, On }
    public GameEvent respawnEvent;
    public GameEvent gameOverEvent;
    private HealthComponent healthComponent;
    private HealthBar healthBar;
    private CameraController playerCamera;

    [Header("Movement Settings")]
    [SerializeField] private float currentSpeed;
    [SerializeField] private float accumulatedPitch = 0f;
    [SerializeField] private float accumulatedRoll = 0f;
    [SerializeField] private float accumulatedYaw = 0f;
    [SerializeField] private float minimumSpeed = 20f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float maxRollAngle = 45f;
    [SerializeField] private float maxYawAngle = 45f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float deceleration = 3f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float pitchAcceleration = 20f;
    [SerializeField] private float rollAcceleration = 20f;
    [SerializeField] private float yawAcceleration = 20f;
    [SerializeField] private float pitchDecayRate = 2f;
    [SerializeField] private float rollDecayRate = 2f;
    [SerializeField] private float yawDecayRate = 2f;
    [SerializeField] private bool isInverted = false;
    [SerializeField] private AutoLevelMode autoLevelMode = AutoLevelMode.Off;
    [SerializeField] private float autoLevelSpeed = 2f;
    //[SerializeField] private float brakeMultiplier = 0.5f;
    [SerializeField] private float barrelRollSpeed = 360f;
    //[SerializeField] private float quickTurnMultiplier = 1.5f;

    [Header("Crosshair Settings")]
    [SerializeField] private RectTransform crosshairUI;
    [SerializeField] private HealthComponent crosshairTarget;
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

    [Header("Wing Trails")]
    [SerializeField] private TrailRenderer leftWingTrail;
    [SerializeField] private TrailRenderer rightWingTrail;
    [SerializeField] private float trailActivationThreshold = 15f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip rollSound;
    [SerializeField] private AudioSource warningSpeakers;

    private float boostTimer;

    private Vector2 crosshairPosition = Vector2.zero;
    public bool isBoosting;
    private bool isMoving;
    private Rigidbody rb;
    private Coroutine crosshairRoutine;
    private const int MAX_TARGETS = 10;
    private RaycastHit[] assistHits = new RaycastHit[MAX_TARGETS];

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            StopAllCoroutines();
            return;
        }
        rb= GetComponent<Rigidbody>();
        healthBar = GetComponent<HealthBar>();
        healthComponent = GetComponent<HealthComponent>();
        SpawnManager.Instance.RegisterPlayer(this);
        FindLocalCamera();
        crosshairUI = GameObject.Find("Crosshair").GetComponent<RectTransform>();
        InitializeLocalGameManager();
        InitializeEvents();
        gameObject.name = MultiplayerManager.PlayerName;
        TeamManager.Instance.InitializeTeamScores();
    }

    private void InitializeLocalGameManager()
    {
        GameObject LGM = GameObject.Find("Local Game Manager");
        UIManager manager = LGM.GetComponent<UIManager>();
        manager.playerHealth = healthComponent;
        manager.enabled = true;
        healthBar.InitializeHealthBar(manager.playerHealthBar);

        RadarSystem radarSystem = LGM.GetComponent<RadarSystem>();
        radarSystem.player = transform;
        radarSystem.enabled = true;
        
        AltimeterSystem altimeterSystem = LGM.GetComponent<AltimeterSystem>();
        altimeterSystem.player = transform;
        altimeterSystem.warningSound = warningSpeakers;
        altimeterSystem.maxAltitude = maxAltitude;
        altimeterSystem.minAltitude = minAltitude;
        altimeterSystem.enabled = true;
    }

    private void InitializeEvents()
    {
        InputManager.Instance.OnStartMove += StartCrosshairMovement;
        InputManager.Instance.OnEndMove += StopCrosshairMovement;
        InputManager.Instance.OnBoost += StartBoost;
        InputManager.Instance.OnFirePrimaryWeapon += StartShooting;
        respawnEvent.OnGameObjectEventRaised += Respawn;
        gameOverEvent.OnEventRaised += DisablePlaneObject;
    }
    private void OnEnable() => InitializeEvents();
    private void OnDisable()
    {
        Debug.Log("removing all refs");
        InputManager.Instance.OnStartMove -= StartCrosshairMovement;
        InputManager.Instance.OnEndMove -= StopCrosshairMovement;
        InputManager.Instance.OnBoost -= StartBoost;
        InputManager.Instance.OnFirePrimaryWeapon -= StartShooting;
        respawnEvent.OnGameObjectEventRaised -= Respawn;
        gameOverEvent.OnEventRaised -= DisablePlaneObject;
        StopAllCoroutines();
    }

    private void FixedUpdate()
    {
        ApplyPhysicsMovement();
        ApplyPhysicsRotation();
        ApplyAccumulationDecay();
        if (autoLevelMode == AutoLevelMode.On && !isMoving)
            AutoLevel();
        HandleWingTrails();
    }

    private void ApplyPhysicsMovement()
    {
        float targetSpeed = isBoosting ? maxSpeed : minimumSpeed;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, (isBoosting ? acceleration : deceleration) * Time.fixedDeltaTime);

        rb.linearVelocity = transform.forward * currentSpeed;
    }

    private void ApplyPhysicsRotation()
    {
        float pitchInput = isInverted ? accumulatedPitch : -accumulatedPitch;

        Vector3 torque = Vector3.zero;
        torque += pitchInput * pitchAcceleration * transform.right;


        float currentYawAngle = Mathf.Rad2Deg * Mathf.Asin(transform.forward.x);
        if ((Mathf.Abs(currentYawAngle) < maxYawAngle) || (Mathf.Sign(accumulatedYaw) != Mathf.Sign(currentYawAngle)))
            torque += accumulatedYaw * yawAcceleration * transform.up;
        else
            accumulatedYaw = 0;

        float currentRollAngle = Mathf.Rad2Deg * Mathf.Asin(transform.right.y);
        if ((currentRollAngle < maxRollAngle && currentRollAngle > -maxRollAngle) ||
            (Mathf.Sign(accumulatedRoll) != Mathf.Sign(currentRollAngle)))
            torque += accumulatedRoll * rollAcceleration * transform.forward;
        else
            accumulatedRoll = 0;

        rb.AddTorque(torque, ForceMode.Acceleration);
    }

    private void StartCrosshairMovement()
    {
        if (crosshairRoutine != null)
            return;
        StartCoroutine(HandleCrosshairMovement());
        isMoving = true;
    }

    private void StopCrosshairMovement()
    {
        isMoving = false;
        if (crosshairRoutine != null)
            StopCoroutine(crosshairRoutine);
        crosshairRoutine = null;
    }

    private IEnumerator HandleCrosshairMovement()
    {
        while (true)
        {
            Vector2 inputVector = InputManager.Instance.CurrentMoveVector;
            if (inputVector.magnitude > 0)
            {
                Vector2 newCrosshairPosition = crosshairPosition + (crosshairSpeed * Time.fixedDeltaTime * inputVector);
                Vector2 adjustedPosition = ApplyCrosshairMagnetism(newCrosshairPosition);

                bool atLimitX = Mathf.Abs(adjustedPosition.x) >= crosshairRadius;
                bool atLimitY = Mathf.Abs(adjustedPosition.y) >= crosshairRadius;

                if (!atLimitX)
                    crosshairPosition.x = adjustedPosition.x;
                if (!atLimitY)
                    crosshairPosition.y = adjustedPosition.y;
                if (atLimitX)
                {
                    accumulatedRoll += -inputVector.x * rollAcceleration * Time.fixedDeltaTime;
                    accumulatedYaw += inputVector.x * yawAcceleration * Time.fixedDeltaTime;
                }
                if (atLimitY)
                {
                    float pitchInput = isInverted ? -inputVector.y : inputVector.y;
                    accumulatedPitch += pitchInput * pitchAcceleration * Time.fixedDeltaTime;
                }
                accumulatedPitch = Mathf.Clamp(accumulatedPitch, -1f, 1f);
                accumulatedRoll = Mathf.Clamp(accumulatedRoll, -1f, 1f);
            }
            GetCrosshairWorldPosition();
            crosshairUI.anchoredPosition = crosshairPosition;
            yield return null;
        }
    }
    private void ApplyAccumulationDecay()
    {
        if (Mathf.Abs(accumulatedPitch) > 0.01f)
            accumulatedPitch = Mathf.MoveTowards(accumulatedPitch, 0, pitchDecayRate * Time.fixedDeltaTime);
        if (Mathf.Abs(accumulatedRoll) > 0.01f)
            accumulatedRoll = Mathf.MoveTowards(accumulatedRoll, 0, rollDecayRate * Time.fixedDeltaTime);
        if (Mathf.Abs(accumulatedYaw) > 0.01f)
            accumulatedYaw = Mathf.MoveTowards(accumulatedYaw, 0, yawDecayRate * Time.fixedDeltaTime);
    }
    private void DisablePlaneObject()
    {
        Debug.Log("disabling plane");
        gameObject.SetActive(false);
    }

    private void StartBoost() => StartCoroutine(HandleBoost());
    private void StartShooting() => primaryWeapon.Fire(GetCrosshairWorldPosition());

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
            crosshairImage.color = Color.Lerp(crosshairImage.color, targetColor, Time.fixedDeltaTime * 10f);
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

            if (angle >= closestAngle)
                continue;
            closestAngle = angle;
            bestTarget = hit.transform;
            bestScreenPosition = screenPos;
        }

        if (bestTarget != null)
        {
            UpdateCrosshairColor(true);
            return Vector2.Lerp(currentPosition, bestScreenPosition, Time.fixedDeltaTime * magnetStrength);
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
        yield break;
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
        float roll = Mathf.Abs(InputManager.Instance.CurrentMoveVector.x * 30f);
        bool shouldActivate = roll > trailActivationThreshold;

        leftWingTrail.emitting = shouldActivate;
        rightWingTrail.emitting = shouldActivate;
    }

    private void AutoLevel()
    {
        Quaternion currentRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, autoLevelSpeed * Time.fixedDeltaTime);
    }

    public void ToggleInversion() => isInverted = !isInverted;

    private void FindLocalCamera()
    {
        playerCamera = Camera.main.GetComponent<CameraController>();
        if (playerCamera != null)
        {
            Debug.Log("Local Camera Found: " + playerCamera.gameObject.name);
            playerCamera.player = transform;
            playerCamera.enabled = true;
            playerCamera.TeleportCameraBehindPlayer();
        }
        else
            Debug.LogError("No Main Camera found for Player " + OwnerClientId);
    }
    public void Respawn(GameObject gameObject)
    {
        Team currentTeam = TeamManager.Instance.GetTeam(OwnerClientId);
        Vector3 newSpawnPosition = SpawnManager.Instance.GetRandomSpawnPoint(currentTeam);
        transform.SetPositionAndRotation(newSpawnPosition, Quaternion.identity);
        healthComponent.InitializeHealth();
    }
    public override void OnNetworkDespawn()
    {
        if (SpawnManager.Instance != null)
            SpawnManager.Instance.UnregisterPlayer(this);
    }
}
