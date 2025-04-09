using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : Controller
{
    public PlaneState planeState = PlaneState.Flight;
    private CameraController playerCamera;

    [Header("Movement Settings")]
    [SerializeField] private float currentSpeed;
    [SerializeField] private float accumulatedPitch = 0f, accumulatedRoll = 0f, accumulatedYaw = 0f;
    [SerializeField] private float minimumSpeed = 20f, rotationSpeed = 100f, maxSpeed = 100f;
    [SerializeField] private float maxRollAngle = 45f, maxYawAngle = 45f;
    [SerializeField] private float acceleration = 5f, deceleration = 3f;
    [SerializeField] private float pitchAcceleration = 20f, rollAcceleration = 20f, yawAcceleration = 20f;
    [SerializeField] private float pitchInertia = 2f, rollInertia = 2f, yawInertia = 2f;
    [SerializeField] private bool isInverted = false;
    [SerializeField] private AutoLevelMode autoLevelMode = AutoLevelMode.Off;
    [SerializeField] private float autoLevelSpeed = 2f;
    //[SerializeField] private float brakeMultiplier = 0.5f;
    [SerializeField] private float barrelRollSpeed = 360f;
    [SerializeField] private float lateralForceMultiplier = 0.1f;
    //[SerializeField] private float quickTurnMultiplier = 1.5f;

    [Header("Crosshair Settings")]
    [SerializeField] private RectTransform crosshairUI;
    [SerializeField] private float crosshairRadius = 50f;
    [SerializeField] private float crosshairSpeed = 500f;
    [SerializeField] private float assistRadius = 1.5f;
    [SerializeField] private float sweepAngle = 30f;
    [SerializeField] private float maxAssistRange = 1000f;
    [SerializeField] private LayerMask destructibleLayerMask;
    [SerializeField] private Color crosshairColor;
    [SerializeField] private Color crosshairOnTargetColor;

    [Header("Boost Settings")]
    [SerializeField] private float boostMultiplier = 2f;
    [SerializeField] private float boostDrainRate = 25f, boostRechargeRate = 0.5f, boostRechargeDelay = 1f, boostRechargeDelayTimer = 0f;
    [SerializeField] private GameObject boostVFX;
    [SerializeField] private Slider boostBar;
    [SerializeField] private float boostTransitionDuration = 5f;
    [SerializeField] private float maxBoostCharge = 100, boostCharge = 0;

    [Header("Altitude Settings")]
    [SerializeField] private float maxAltitude = 100f;
    [SerializeField] private float minAltitude = 10f;

    [Header("Weight Settings")]
    public int maxWeight;
    public int currentWeight;
    public float weightToSpeedReductionRatio;

    [Header("Weapon Positions")]
    [SerializeField] private Transform leftWeaponSlot;
    [SerializeField] private Transform rightWeaponSlot;
    [Header("Equipped Weapon Settings")]
    [SerializeField] private Weapon selectedWeapon;
    [SerializeField] private List<Weapon> weaponList;
    [SerializeField] private GameEvent weaponChangedEvent;

    [Header("Wing Trails")]
    [SerializeField] private TrailRenderer leftWingTrail, rightWingTrail;
    [SerializeField] private float trailActivationThreshold = 15f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip rollSound;
    [SerializeField] private AudioSource warningSpeakers;

    private Vector2 crosshairPosition = Vector2.zero;
    private Vector3 magnetWorldHitPoint;
    private HealthComponent magnetTarget;
    private HealthComponent previousMagnetTarget;
    private const int MAX_TARGETS = 50;
    private const float accelerationAccumulationLimit = 0.25f;
#pragma warning disable IDE0044 // Add readonly modifier
    private RaycastHit[] assistHits = new RaycastHit[MAX_TARGETS];
#pragma warning restore IDE0044 // Add readonly modifier
    private AltimeterSystem altimeterSystem;
    private RadarSystem radarSystem;
    private UIManager uiManager;
    private PlaneStorageHandler planeStorageHandler;
    private bool hasInitialized;
    private Coroutine firingCoroutine;
    private Coroutine fovTransitionCoroutine;
    private Coroutine boostCoroutine;
    private Coroutine barrelRollCoroutine;
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
        uiManager.InitializeResources();
        healthBar.InitializeHealthBar(uiManager.playerHealthBar);
        crosshairUI = uiManager.crosshairUI;
        boostBar = uiManager.boostSlider;
        boostBar.maxValue = maxBoostCharge;

        radarSystem = LGM.GetComponent<RadarSystem>();
        radarSystem.player = transform;
        radarSystem.enabled = true;

        altimeterSystem = LGM.GetComponent<AltimeterSystem>();
        altimeterSystem.player = transform;
        altimeterSystem.warningSound = warningSpeakers;
        altimeterSystem.maxAltitude = maxAltitude;
        altimeterSystem.minAltitude = minAltitude;
        altimeterSystem.enabled = true;

        planeStorageHandler = LGM.GetComponent<PlaneStorageHandler>();
        planeStorageHandler.enabled = true;
        planeStorageHandler.Initialize();
        LGM.GetComponent<DamageDirectionUIManager>().Initialize(transform);
    }

    private void InitializeEvents()
    {
        if (!hasInitialized) return;
        InputManager.Instance.OnStartBoost += StartBoost;
        InputManager.Instance.OnEndBoost += EndBoost;
        InputManager.Instance.OnStartFireWeapon += OnStartFiringWeapon;
        InputManager.Instance.OnEndFireWeapon += OnStopFiringWeapon;
        InputManager.Instance.OnReload += ReloadWeapon;
        InputManager.Instance.OnBarrelRoll += StartBarrelRoll;
        InputManager.Instance.OnCycleWeaponUp += CycleWeaponUp;
        InputManager.Instance.OnCycleWeaponDown += CycleWeaponDown;
        FindLocalCamera();
        GetComponent<HealthComponent>().OnDeath += playerCamera.RemoveCameraFromPlayer;
        planeState = PlaneState.Flight;
        playerCamera.GetComponent<Camera>().fieldOfView = playerCamera.flightFOV;
        boostCharge = maxBoostCharge / 4;
        boostBar.value = boostCharge;
        magnetTarget = null;
        if (weaponList.Count < 0)
            return;
        selectedWeapon = weaponList[2];
        selectedWeapon.gameObject.SetActive(true);
        weaponChangedEvent.RaiseEvent(selectedWeapon);
        selectedWeapon.Initialize(planeStorageHandler, this);
        planeStorageHandler.UpdateWeaponAmmo(100, 0);
    }
    private void OnEnable() => InitializeEvents();
    private void OnDisable() => CleanupEventsAndRoutines();

    private void CleanupEventsAndRoutines()
    {
        if (!hasInitialized) return;
        InputManager.Instance.OnStartBoost -= StartBoost;
        InputManager.Instance.OnStartBoost -= EndBoost;
        InputManager.Instance.OnStartFireWeapon -= OnStartFiringWeapon;
        InputManager.Instance.OnEndFireWeapon -= OnStopFiringWeapon;
        InputManager.Instance.OnReload -= ReloadWeapon;
        InputManager.Instance.OnBarrelRoll -= StartBarrelRoll;
        InputManager.Instance.OnCycleWeaponUp -= CycleWeaponUp;
        InputManager.Instance.OnCycleWeaponDown -= CycleWeaponDown;
        GetComponent<HealthComponent>().OnDeath -= playerCamera.RemoveCameraFromPlayer;
        StopAllCoroutines();
        CancelInvoke();
    }

    private void FixedUpdate()
    {
        if (!hasInitialized || planeState == PlaneState.BarrelRoll) return;
        HandleCrosshairMovement();
        ApplyPhysicsMovement();
        ApplyPhysicsRotation();
        ApplyAccelerationAccumulationDecay();
        if (autoLevelMode == AutoLevelMode.On)
            AutoLevel();
        HandleWingTrails();
        ApplyBoostCharge();
    }

    private void ApplyPhysicsMovement()
    {
        float targetSpeed = planeState == PlaneState.Boost ? maxSpeed : minimumSpeed;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, (planeState == PlaneState.Boost ? acceleration : deceleration) * Time.fixedDeltaTime);
        planeRigidbody.linearVelocity = transform.forward * currentSpeed;
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

        planeRigidbody.AddTorque(torque, ForceMode.Acceleration);

        Vector3 localAngularVelocity = transform.InverseTransformDirection(planeRigidbody.angularVelocity);

        if (Mathf.Abs(accumulatedYaw) < 0.01f)
            localAngularVelocity.y = 0f;
        if (Mathf.Abs(accumulatedRoll) < 0.01f || planeState != PlaneState.BarrelRoll)
            localAngularVelocity.z = 0f;

        planeRigidbody.angularVelocity = transform.TransformDirection(localAngularVelocity);
    }

    private void HandleCrosshairMovement()
    {
        Vector2 inputVector = InputManager.Instance.CurrentMoveVector;
        if (inputVector.magnitude > 0)
        {
            Vector2 newCrosshairPosition = crosshairPosition + (crosshairSpeed * Time.fixedDeltaTime * inputVector);
            ClampCrosshairAtBoundary(inputVector, newCrosshairPosition);
            accumulatedPitch = Mathf.Clamp(accumulatedPitch, -accelerationAccumulationLimit, accelerationAccumulationLimit);
            accumulatedRoll = Mathf.Clamp(accumulatedRoll, -accelerationAccumulationLimit, accelerationAccumulationLimit);
            accumulatedYaw = Mathf.Clamp(accumulatedYaw, -accelerationAccumulationLimit, accelerationAccumulationLimit);
        }
        if (RunCrosshairAssistRaycast(out Vector2 targetScreenPos, out float angularDist) && InputManager.Instance.IsFiringWeapon())
        {
            RectTransform canvasRect = crosshairUI.root as RectTransform;
            Camera uiCam = playerCamera.GetComponent<Camera>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetScreenPos, uiCam, out Vector2 localPos))
                crosshairPosition = localPos;
        }
        crosshairUI.anchoredPosition = crosshairPosition;
        UpdateCrosshairColor(angularDist);
    }

    private void ClampCrosshairAtBoundary(Vector2 inputVector, Vector2 newCrosshairPosition)
    {
        crosshairUI.anchoredPosition = newCrosshairPosition;
        bool atLimitX = Mathf.Abs(newCrosshairPosition.x) >= crosshairRadius;
        bool atLimitY = Mathf.Abs(newCrosshairPosition.y) >= crosshairRadius;

        if (atLimitX)
        {
            accumulatedRoll += -inputVector.x * rollAcceleration * Time.fixedDeltaTime;
            accumulatedYaw += inputVector.x * yawAcceleration * Time.fixedDeltaTime;
            crosshairPosition.x = Mathf.Sign(newCrosshairPosition.x) * crosshairRadius;
        }
        else
            crosshairPosition.x = newCrosshairPosition.x;
        if (atLimitY)
        {
            float pitchInput = isInverted ? -inputVector.y : inputVector.y;
            accumulatedPitch += pitchInput * pitchAcceleration * Time.fixedDeltaTime;
            crosshairPosition.y = Mathf.Sign(newCrosshairPosition.y) * crosshairRadius;
        }
        else
            crosshairPosition.y = newCrosshairPosition.y;
    }

    private void ApplyAccelerationAccumulationDecay()
    {
        if (Mathf.Abs(accumulatedPitch) > 0.01f)
            accumulatedPitch = Mathf.MoveTowards(accumulatedPitch, 0, pitchInertia * Time.fixedDeltaTime);
        if (Mathf.Abs(accumulatedRoll) > 0.01f)
            accumulatedRoll = Mathf.MoveTowards(accumulatedRoll, 0, rollInertia * Time.fixedDeltaTime);
        if (Mathf.Abs(accumulatedYaw) > 0.01f)
            accumulatedYaw = Mathf.MoveTowards(accumulatedYaw, 0, yawInertia * Time.fixedDeltaTime);
    }

    private void ReloadWeapon() => selectedWeapon.Reload(this);
    private void OnStartFiringWeapon()
    {
        if (planeState != PlaneState.Flight)
            return;
        planeState = PlaneState.ADS;
        if (fovTransitionCoroutine != null)
            StopCoroutine(fovTransitionCoroutine);
        fovTransitionCoroutine = StartCoroutine(FOVTransition(playerCamera.ADSFOV, selectedWeapon.ADSTime, TriggerPrimaryFiring));
    }

    private void OnStopFiringWeapon()
    {
        if (firingCoroutine != null)
            StopCoroutine(firingCoroutine);
        if (fovTransitionCoroutine != null)
            StopCoroutine(fovTransitionCoroutine);
        fovTransitionCoroutine = StartCoroutine(FOVTransition(playerCamera.flightFOV, selectedWeapon.ADSTime, FinishPrimaryFiring));
    }

    private void TriggerPrimaryFiring()
    {
        if (fovTransitionCoroutine != null)
            StopCoroutine(fovTransitionCoroutine);
        if (firingCoroutine != null)
            StopCoroutine(firingCoroutine);
        firingCoroutine = StartCoroutine(HandlePrimaryFiring());
    }

    private void FinishPrimaryFiring()
    {
        if (fovTransitionCoroutine != null)
            StopCoroutine(fovTransitionCoroutine);
        planeState = PlaneState.Flight;
    }
    private IEnumerator HandlePrimaryFiring()
    {
        yield return new WaitForSeconds(selectedWeapon.ADSTime);
        while (InputManager.Instance.IsFiringWeapon())
        {
            selectedWeapon.Fire(GetCrosshairWorldPosition(), this);
            yield return new WaitForSeconds(selectedWeapon.fireRate);
        }
    }

    private void CycleWeaponDown()
    {
        if (weaponList.Count == 0) return;
        int currentIndex = weaponList.IndexOf(selectedWeapon);
        selectedWeapon.gameObject.SetActive(false);
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = weaponList.Count - 1;
        selectedWeapon = weaponList[currentIndex];
        selectedWeapon.gameObject.SetActive(true);
        weaponChangedEvent.RaiseEvent(selectedWeapon);
        selectedWeapon.Initialize(planeStorageHandler, this);
        planeStorageHandler.UpdateWeaponAmmo(100, 0);
    }
    private void CycleWeaponUp()
    {
        if (weaponList.Count == 0) return;
        int currentIndex = weaponList.IndexOf(selectedWeapon);
        selectedWeapon.gameObject.SetActive(false);
        currentIndex = (currentIndex + 1) % weaponList.Count;
        selectedWeapon = weaponList[currentIndex];
        selectedWeapon.gameObject.SetActive(true);
        weaponChangedEvent.RaiseEvent(selectedWeapon);
        selectedWeapon.Initialize(planeStorageHandler, this);
        planeStorageHandler.UpdateWeaponAmmo(100, 0);
    }
    private Vector3 GetCrosshairWorldPosition()
    {
        if (magnetTarget != null)
            return magnetWorldHitPoint;
        Ray ray = new(playerCamera.transform.position, (crosshairUI.position - playerCamera.transform.position).normalized);
        if (Physics.Raycast(ray, out RaycastHit hit, maxAssistRange))
            return hit.point;
        return ray.origin + ray.direction * selectedWeapon.range;
    }

    private bool RunCrosshairAssistRaycast(out Vector2 targetScreenPos, out float angularDistanceToCrosshair)
    {
        Camera cam = playerCamera.GetComponent<Camera>();
        Ray targetRay = new(playerCamera.transform.position, (crosshairUI.position - playerCamera.transform.position).normalized);
        int hitCount = Physics.SphereCastNonAlloc(targetRay, assistRadius, assistHits, maxAssistRange, destructibleLayerMask);

        angularDistanceToCrosshair = sweepAngle;
        previousMagnetTarget = magnetTarget;
        magnetTarget = null;
        magnetWorldHitPoint = Vector3.zero;
        targetScreenPos = crosshairUI.position;
        for (int i = 0; i < hitCount; i++)
        {
            var hit = assistHits[i];
            if (!hit.collider.TryGetComponent(out HealthComponent health)) continue;
            if (!health.TryGetComponent(out Controller controller) || controller.Team == Team) continue;
            Vector3 worldPos = hit.point;
            Vector3 hitScreenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            if (hitScreenPos.z < 0f) continue;
            float newHitAngle = Vector3.Angle(targetRay.direction, (worldPos - targetRay.origin).normalized);
            if (newHitAngle < angularDistanceToCrosshair)
            {
                angularDistanceToCrosshair = newHitAngle;
                magnetTarget = health;
                magnetWorldHitPoint = worldPos;
                targetScreenPos = hitScreenPos;
            }
        }
        if (magnetTarget != null && uiManager.activeDamageableMarkers.TryGetValue(magnetTarget, out HUDMarker targetMaarker))
            targetMaarker.TriggerLockOnSequence();
        if (previousMagnetTarget != null && previousMagnetTarget != magnetTarget &&
            uiManager.activeDamageableMarkers.TryGetValue(previousMagnetTarget, out HUDMarker previousTargetMarker))
            previousTargetMarker.CancelLockOnSequence();
        return magnetTarget != null;
    }

    private void UpdateCrosshairColor(float angularDistance)
    {
        if (crosshairUI.TryGetComponent(out Image crosshairImage))
        {
            float t = Mathf.InverseLerp(sweepAngle, 0f, angularDistance);
            crosshairImage.color = Color.Lerp(crosshairColor, crosshairOnTargetColor, t);
        }
    }

    private void ApplyBoostCharge()
    {
        if (boostCharge >= maxBoostCharge || planeState != PlaneState.Flight) return;
        if (boostRechargeDelayTimer > 0)
        {
            boostRechargeDelayTimer -= Time.deltaTime;
            return;
        }
        boostCharge += boostRechargeRate * Time.deltaTime;
        boostCharge = Mathf.Min(maxBoostCharge, boostCharge);
        boostBar.value = boostCharge;
    }
    private void StartBoost()
    {
        if (boostCharge <= 10f || planeState != PlaneState.Flight) return;
        planeState = PlaneState.Boost;
        boostVFX.SetActive(true);
        if (fovTransitionCoroutine != null)
            StopCoroutine(fovTransitionCoroutine);
        fovTransitionCoroutine = StartCoroutine(FOVTransition(playerCamera.boostFOV, boostTransitionDuration, OnBoostFOVComplete));
    }

    private void OnBoostFOVComplete()
    {
        if (boostCoroutine != null)
            StopCoroutine(boostCoroutine);
        boostCoroutine = StartCoroutine(BoostRoutine());
    }

    private IEnumerator BoostRoutine()
    {
        while (boostCharge > 2.5f && InputManager.Instance.IsBoosting())
        {
            boostCharge -= boostDrainRate * Time.deltaTime;
            boostCharge = Mathf.Max(0f, boostCharge);
            boostBar.value = boostCharge;
            yield return null;
        }
        EndBoost();
        yield break;
    }

    private void EndBoost()
    {
        if (boostCoroutine != null)
            StopCoroutine(boostCoroutine);
        if (fovTransitionCoroutine != null)
            StopCoroutine(fovTransitionCoroutine);
        if (isActiveAndEnabled)
            fovTransitionCoroutine = StartCoroutine(FOVTransition(playerCamera.flightFOV, boostTransitionDuration, OnEndBoostFOVOver));
    }
    private void OnEndBoostFOVOver()
    {
        boostVFX.SetActive(false);
        planeState = PlaneState.Flight;
        boostRechargeDelayTimer = boostRechargeDelay;
    }
    private IEnumerator FOVTransition(float toFOV, float duration, Action onComplete = null)
    {
        Camera cam = playerCamera.GetComponent<Camera>();
        float fromFOV = cam.fieldOfView;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cam.fieldOfView = Mathf.Lerp(fromFOV, toFOV, elapsed / duration);
            yield return null;
        }

        cam.fieldOfView = toFOV;
        onComplete?.Invoke();
    }

    private void StartBarrelRoll()
    {
        if (barrelRollCoroutine != null || boostCharge < 25 || planeState != PlaneState.Flight)
            return;
        barrelRollCoroutine = StartCoroutine(PerformBarrelRoll());
    }
    private IEnumerator PerformBarrelRoll()
    {
        planeState = PlaneState.BarrelRoll;
        boostCharge -= 25f;
        boostBar.value = boostCharge;

        float totalRoll = 0f;
        float desiredRotation = 360f;
        float inputX = InputManager.Instance.CurrentMoveVector.x;
        float rollDirection = inputX == 0f ? 1f : Mathf.Sign(inputX);

        while (totalRoll < desiredRotation)
        {
            float rollStep = barrelRollSpeed * Time.fixedDeltaTime;
            float remainingRoll = desiredRotation - totalRoll;
            float step = Mathf.Min(rollStep, remainingRoll);

            Vector3 rollTorque = rollDirection * step * transform.forward;
            planeRigidbody.AddTorque(rollTorque, ForceMode.VelocityChange);
            Vector3 lateralForce = lateralForceMultiplier * rollDirection * step * transform.right;
            planeRigidbody.AddForce(lateralForce, ForceMode.VelocityChange);

            totalRoll += step;
            yield return new WaitForFixedUpdate();
        }
        boostRechargeDelayTimer = boostRechargeDelay;
        planeState = PlaneState.Flight;
        barrelRollCoroutine = null;
    }


    private void HandleWingTrails()
    {
        if (planeState == PlaneState.Boost)
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
        if (planeState == PlaneState.BarrelRoll)
            return;
        Quaternion currentRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, autoLevelSpeed * Time.fixedDeltaTime);
    }
    public void ToggleInversion() => isInverted = !isInverted;

    private void FindLocalCamera()
    {
        if (playerCamera == null)
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
