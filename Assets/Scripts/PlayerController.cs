using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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
    [SerializeField] private bool isInverted = false;
    [SerializeField] private AutoLevelMode autoLevelMode = AutoLevelMode.Off;
    [SerializeField] private float autoLevelSpeed = 2f;
    [SerializeField] private float brakeMultiplier = 0.5f;
    [SerializeField] private float barrelRollSpeed = 360f;
    [SerializeField] private float quickTurnMultiplier = 1.5f;

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

    [Header("Audio Clip")]
    [SerializeField] private AudioClip rollSound;

    private float boostTimer;
    public bool isBoosting;
    private Coroutine moveRoutine;
    private Coroutine shootRoutine;

    private void Start()
    {
        healthBar = GetComponent<HealthBar>();
        healthComponent = GetComponent<HealthComponent>();
        destructibleComponent = GetComponent<DestructibleComponent>();
    }

    private void OnEnable()
    {
        inputManager.OnStartMove += StartMove;
        inputManager.OnEndMove += StopMove;
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
    private void StartShooting()=>shootRoutine??=StartCoroutine(ShootPrimary());
    private void StopShooting()
    {
       if(shootRoutine != null)
            StopCoroutine(shootRoutine);
       shootRoutine = null;
    }

    private IEnumerator ShootPrimary()
    {
        while (true)
        {
            primaryWeapon.Fire();
            yield return new WaitForSeconds (primaryWeapon.fireRate);
        }
    }

    //private void HandleMissileSystem()
    //{
    //    // Lock on to a target if no target is locked
    //    if (currentTarget == null)
    //    {
    //        LockOnTarget();
    //    }

    //    // Fire missile if target is locked and cooldown is complete
    //    if (Input.GetKeyDown(fireMissileKey) && missileCooldownTimer <= 0)
    //    {
    //        FireMissile();
    //        missileCooldownTimer = missileCooldown;
    //    }

    //    // Reduce cooldown timer
    //    if (missileCooldownTimer > 0)
    //    {
    //        missileCooldownTimer -= Time.deltaTime;
    //    }
    //}

    //private void LockOnTarget()
    //{

    //    if (Physics.OverlapSphere(missileSpawnPoint.transform.position, lockOnRange, targetLayer).Length > 0)
    //    {
    //        Transform nearestTarget = null;
    //        float shortestDistance = float.MaxValue;

    //        foreach (Collider target in Physics.OverlapSphere(missileSpawnPoint.transform.position, lockOnRange, targetLayer))
    //        {
    //            if (!target.CompareTag(targetTag)) continue;

    //            Vector3 directionToTarget = (target.transform.position - missileSpawnPoint.position).normalized;
    //            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

    //            // Check if the target is within the 40-degree cone
    //            if (angleToTarget > 20f) continue; // 20 degrees on either side of forward

    //            float distance = Vector3.Distance(missileSpawnPoint.position, target.transform.position);
    //            if (distance < shortestDistance)
    //            {
    //                shortestDistance = distance;
    //                nearestTarget = target.transform;
    //            }
    //        }

    //        currentTarget = nearestTarget;
    //    }
    //    else
    //    {
    //        currentTarget = null;
    //    }
    //}


    //private void OnDrawGizmos()
    //{
    //    if (missileSpawnPoint == null) return;

    //    // Draw the search radius
    //    Gizmos.color = new Color(1, 0, 0, 0.3f); // Semi-transparent red
    //    Gizmos.DrawSphere(missileSpawnPoint.position, lockOnRange);

    //    // Draw the forward arc for valid targets
    //    Vector3 forward = transform.forward * lockOnRange;
    //    Vector3 leftBoundary = Quaternion.Euler(0, -45, 0) * forward; // Adjust angle as needed
    //    Vector3 rightBoundary = Quaternion.Euler(0, 45, 0) * forward;

    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawRay(missileSpawnPoint.position, leftBoundary);
    //    Gizmos.DrawRay(missileSpawnPoint.position, rightBoundary);
    //}



    //private void FireMissile()
    //{
    //    // Instantiate a missile
    //    GameObject missile = Instantiate(missilePrefab, missileSpawnPoint.position, missileSpawnPoint.rotation);
    //    //Missile missileScript = missile.GetComponent<Missile>();

    //    // Instantiate the missile VFX prefab at the missile spawn point
    //    GameObject missileVFX = Instantiate(missleVFXPrefab, missileSpawnPoint.position + missileSpawnPoint.forward * 2, missileSpawnPoint.rotation);

    //    if (missileScript != null)
    //    {
    //        if (currentTarget != null)
    //        {
    //            // Set target if available
    //            missileScript.SetTarget(currentTarget);
    //        }
    //        else
    //        {
    //            // Fire forward if no target is locked
    //            missileScript.SetForward();
    //        }
    //    }
    //}
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

    //private void HandleAirBrake()
    //{
    //    if (Input.GetKey(airBrakeKey))
    //    {
    //        // Gradually reduce speed when braking
    //        speed = Mathf.Lerp(speed, speed * brakeMultiplier, Time.deltaTime * 2f);
    //    }
    //    else
    //    {
    //        speed = Mathf.Lerp(speed, 20f, Time.deltaTime);
    //    }
    //}

    //private void HandleBarrelRoll()
    //{
    //    if (Input.GetKeyDown(barrelRollKey))
    //    {
    //        GetComponent<AudioSource>().PlayOneShot(rollSound);
    //        StartCoroutine(PerformBarrelRoll());
    //    }
    //}

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

    //private void HandleQuickTurn()
    //{
    //    if (Input.GetKey(quickTurnKey))
    //    {
    //        float yaw = Input.GetAxis("Horizontal") * quickTurnMultiplier * rotationSpeed * Time.deltaTime;
    //        transform.Rotate(0, yaw, 0);
    //    }
    //}

    private void HandleWingTrails()
    {
        if (isBoosting)
        {
            leftWingTrail.emitting = true;
            rightWingTrail.emitting = true;
            return;
        }
        float roll = Mathf.Abs(Input.GetAxis("Horizontal") * 30f);
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
