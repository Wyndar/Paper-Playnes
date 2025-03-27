using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : Controller
{
    public enum AutoLevelMode { Off, On }
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
    private Vector3 magnetWorldHitPoint;
    private HealthComponent magnetTarget;
    public bool isBoosting;
    private const int MAX_TARGETS = 10;
#pragma warning disable IDE0044 // Add readonly modifier
    private RaycastHit[] assistHits = new RaycastHit[MAX_TARGETS];
#pragma warning restore IDE0044 // Add readonly modifier
    private AltimeterSystem altimeterSystem;
    private RadarSystem radarSystem;
    private UIManager uiManager;
    private bool hasInitialized;

    public override void Initialize(Team team)
    {
        if (!IsOwner)
        {
            enabled = false;
            StopAllCoroutines();
            return;
        }
        ulong ownerId = NetworkManager.Singleton.LocalClientId;
        //we need to broadcast names over network too
        gameObject.name = MultiplayerManager.PlayerName;
        InitializeEntity(false, ownerId, team);
        FindLocalCamera();
        crosshairUI = GameObject.Find("Crosshair").GetComponent<RectTransform>();
        hasInitialized = true;
        InitializeLocalGameManager();
        InitializeEvents();
        TeamManager.Instance.InitializeTeamScores();
    }

    public override void OnNetworkDespawn()
    {
        if (SpawnManager.Instance != null)
            SpawnManager.Instance.UnregisterController(this);
        playerCamera.RemoveCameraFromPlayer();
        altimeterSystem.enabled = false;
        radarSystem.enabled = false;
        uiManager.enabled = false;
        CleanupEventsAndRoutines();
        Destroy(gameObject);
    }

    private void InitializeLocalGameManager()
    {
        GameObject LGM = GameObject.Find("Local Game Manager");
        uiManager = LGM.GetComponent<UIManager>();
        uiManager.playerHealth = healthComponent;
        uiManager.enabled = true;
        healthBar.InitializeHealthBar(uiManager.playerHealthBar);

        radarSystem = LGM.GetComponent<RadarSystem>();
        radarSystem.player = transform;
        radarSystem.enabled = true;
        
        altimeterSystem = LGM.GetComponent<AltimeterSystem>();
        altimeterSystem.player = transform;
        altimeterSystem.warningSound = warningSpeakers;
        altimeterSystem.maxAltitude = maxAltitude;
        altimeterSystem.minAltitude = minAltitude;
        altimeterSystem.enabled = true;
    }

    private void InitializeEvents()
    {
        if (!hasInitialized) return;
        InputManager.Instance.OnBoost += StartBoost;
        InputManager.Instance.OnFirePrimaryWeapon += StartShooting;
    }
    private void OnEnable() => InitializeEvents();
    private void OnDisable() => CleanupEventsAndRoutines();

    private void CleanupEventsAndRoutines()
    {
        if (!hasInitialized) return;
        InputManager.Instance.OnBoost -= StartBoost;
        InputManager.Instance.OnFirePrimaryWeapon -= StartShooting;
        StopAllCoroutines();
    }

    private void FixedUpdate()
    {
        if (!hasInitialized) return;
        HandleCrosshairMovement();
        ApplyPhysicsMovement();
        ApplyPhysicsRotation();
        ApplyAccumulationDecay();
        ApplyAutoRealign();
        if (autoLevelMode == AutoLevelMode.On)
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

        float currentYawAngle = Vector3.SignedAngle(Vector3.forward, transform.forward, Vector3.up);
        if (Mathf.Abs(currentYawAngle) < maxYawAngle || Mathf.Sign(accumulatedYaw) != Mathf.Sign(currentYawAngle))
            torque += accumulatedYaw * yawAcceleration * transform.up;
        else
            accumulatedYaw = 0;

        float currentRollAngle = Vector3.SignedAngle(Vector3.up, transform.up, transform.forward);
        if (Mathf.Abs(currentRollAngle) < maxRollAngle || Mathf.Sign(accumulatedRoll) != Mathf.Sign(currentRollAngle))
            torque += accumulatedRoll * rollAcceleration * transform.forward;
        else
            accumulatedRoll = 0;

        rb.AddTorque(torque, ForceMode.Acceleration);

        Vector3 localAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity);

        if (Mathf.Abs(accumulatedYaw) < 0.01f)
            localAngularVelocity.y = 0f;
        if (Mathf.Abs(accumulatedRoll) < 0.01f)
            localAngularVelocity.z = 0f;

        rb.angularVelocity = transform.TransformDirection(localAngularVelocity);
    }


    private void HandleCrosshairMovement()
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
            accumulatedPitch = Mathf.Clamp(accumulatedPitch, -0.4f, 0.4f);
            accumulatedRoll = Mathf.Clamp(accumulatedRoll, -0.4f, 0.4f);
            accumulatedYaw = Mathf.Clamp(accumulatedYaw, -0.4f, 0.4f);
        }
        GetCrosshairWorldPosition();
        crosshairUI.anchoredPosition = crosshairPosition;
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

    private void StartBoost() => StartCoroutine(HandleBoost());
    private void StartShooting() => primaryWeapon.Fire(GetCrosshairWorldPosition(), this);
    private Vector3 GetCrosshairWorldPosition()
    {
        if (magnetTarget != null)
            return magnetWorldHitPoint;
        Ray ray = playerCamera.GetComponent<Camera>().ScreenPointToRay(crosshairUI.position);

        if (Physics.Raycast(ray, out RaycastHit hit, maxAssistRange))
            return hit.point;

        return ray.origin + ray.direction * maxAssistRange;
    }

    private bool RunCrosshairAssistRaycast(Vector2 screenPos, out Vector2 targetScreenPos)
    {
        Camera cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(screenPos);
        int hitCount = Physics.SphereCastNonAlloc(ray, assistRadius, assistHits, maxAssistRange);

        float closestAngle = float.MaxValue;
        magnetTarget = null;
        magnetWorldHitPoint = Vector3.zero;
        targetScreenPos = screenPos;

        for (int i = 0; i < hitCount; i++)
        {
            var hit = assistHits[i];
            if (!hit.collider.TryGetComponent(out HealthComponent health)) continue;

            Vector3 worldPos = hit.point;
            Vector2 hitScreenPos = cam.WorldToScreenPoint(worldPos);
            float distanceToCrosshair = Vector2.Distance(hitScreenPos, screenPos);
            if (distanceToCrosshair > crosshairRadius) continue;

            float angle = Vector3.Angle(ray.direction, (worldPos - ray.origin).normalized);
            if (angle < closestAngle)
            {
                closestAngle = angle;
                magnetTarget = health;
                magnetWorldHitPoint = worldPos;
                targetScreenPos = hitScreenPos;
            }
        }

        return magnetTarget != null;
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
        if (RunCrosshairAssistRaycast(currentPosition, out Vector2 magnetizedPos))
        {
            UpdateCrosshairColor(true);
            return Vector2.Lerp(currentPosition, magnetizedPos, Time.fixedDeltaTime * magnetStrength);
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
    private void ApplyAutoRealign()
    {
        if (crosshairPosition.magnitude < 0.1f)
            return;

        Camera cam = playerCamera.GetComponent<Camera>();
        Ray currentRay = cam.ScreenPointToRay(crosshairUI.position);
        Vector3 currentDir = currentRay.direction.normalized;
        Ray zeroRay = cam.ScreenPointToRay(cam.pixelRect.center);
        Vector3 zeroDir = zeroRay.direction.normalized;

        Quaternion rotationOffset = Quaternion.FromToRotation(zeroDir, currentDir);
        Quaternion currentRotation = transform.rotation;
        Quaternion rawTargetRotation = rotationOffset * currentRotation;
        Vector3 currentEuler = currentRotation.eulerAngles;
        Vector3 targetEuler = rawTargetRotation.eulerAngles;

        float pitch = currentEuler.x;
        if (Mathf.Abs(accumulatedPitch) < 0.01f)
            pitch = targetEuler.x;
        float yaw = targetEuler.y;
        float roll = targetEuler.z;

        Quaternion constrainedRotation = Quaternion.Euler(pitch, yaw, roll);
        transform.rotation = Quaternion.Slerp(currentRotation, constrainedRotation, autoLevelSpeed * Time.fixedDeltaTime);
        crosshairPosition = Vector2.Lerp(crosshairPosition, Vector2.zero, autoLevelSpeed * Time.fixedDeltaTime);
        crosshairUI.anchoredPosition = crosshairPosition;
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
}
