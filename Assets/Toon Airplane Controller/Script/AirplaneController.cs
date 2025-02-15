using System;
using UnityEngine;
using System.Collections;

namespace GolemKin.ToonAirplaneController
{

public class AirplaneController : MonoBehaviour
{
    public enum AutoLevelMode { Off, On }

    [Header("Movement Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private bool isInverted = false; // Toggle for inversion
    [SerializeField] private AutoLevelMode autoLevelMode = AutoLevelMode.Off;

    [Header("Boost Settings")]
    [SerializeField] private KeyCode boostKey = KeyCode.B;
    [SerializeField] private float boostMultiplier = 2f;
    [SerializeField] private float boostDuration = 2f;
    [SerializeField] private GameObject boostVFX;
    
    [Header("Air Brake Settings")]
    [SerializeField] private KeyCode airBrakeKey = KeyCode.Space;
    [SerializeField] private float brakeMultiplier = 0.5f;

    [Header("Barrel Roll Settings")]
    [SerializeField] private KeyCode barrelRollKey = KeyCode.R;
    [SerializeField] private float barrelRollSpeed = 360f;

    [Header("Quick Turn Settings")]
    [SerializeField] private KeyCode quickTurnKey = KeyCode.LeftShift;
    [SerializeField] private float quickTurnMultiplier = 1.5f;

    [Header("Altitude Settings")]
    [SerializeField] private float maxAltitude = 100f;
    [SerializeField] private float minAltitude = 10f;
    
    [Header("Missile System")]
    [SerializeField] private KeyCode fireMissileKey = KeyCode.M; // Key to fire missiles
    [SerializeField] private GameObject missilePrefab; // Missile prefab
    [SerializeField] private GameObject missleVFXPrefab; // Prefab for the bullets
    [SerializeField] private Transform missileSpawnPoint; // Where the missile is fired from
    [SerializeField] private float missileCooldown = 1f; // Cooldown time between missile launches
    [SerializeField] private LayerMask targetLayer; // Layer to identify valid targets
    [SerializeField] private string targetTag = "Enemy"; // Tag to identify valid targets
    [SerializeField] private float lockOnRange = 100f; // Range within which targets can be locked
    [SerializeField] private Transform currentTarget; // The currently locked target

    private float missileCooldownTimer;
    
    
    [Header("Machine Gun Settings")]
    [SerializeField] private KeyCode fireGunKey = KeyCode.Mouse1; // Key to fire the machine gun
    [SerializeField] private GameObject bulletPrefab; // Prefab for the bullets
    [SerializeField] private GameObject bulletVFXPrefab; // Prefab for the bullets
    [SerializeField] private Transform leftGunSpawnPoint; // Left gun position
    [SerializeField] private Transform rightGunSpawnPoint; // Right gun position
    [SerializeField] private float fireRate = 0.1f; // Time between consecutive shots
    [SerializeField] private float bulletSpeed = 50f; // Speed of the bullets
    [SerializeField] private float bulletLifeTime = 2f; // Time before the bullet is destroyed

    private float fireCooldown; // Internal cooldown timer for the gun
    private bool useLeftGun = true; // Toggle to alternate between guns


    

    [Header("Wing Trails")]
    [SerializeField] private TrailRenderer leftWingTrail;
    [SerializeField] private TrailRenderer rightWingTrail;
    [SerializeField] private float trailActivationThreshold = 15f;

    [Header("Auto Level Settings")]
    [SerializeField] private float autoLevelSpeed = 2f; // Speed at which the plane levels itself

    [Header("Audio Clip")] [SerializeField]
    private AudioClip rollSound;
    
    private float boostTimer;
    public bool isBoosting;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Disable gravity
    }

    private void Update()
    {
        HandleMovement();
        HandleBoost();
        HandleAirBrake();
        HandleBarrelRoll();
        HandleQuickTurn();
        HandleWingTrails();
        LimitAltitude();
        HandleMissileSystem(); // Add this line
        HandleMachineGun();
        if (autoLevelMode == AutoLevelMode.On)
        {
            AutoLevel();
        }
    }
    
    private void HandleMachineGun()
    {
        // Check if the fire key is pressed and cooldown is complete
        if (Input.GetKey(fireGunKey) && fireCooldown <= 0f)
        {
            FireBullet();
            fireCooldown = fireRate; // Reset cooldown
        }
    
        // Reduce the cooldown timer
        if (fireCooldown > 0f)
        {
            fireCooldown -= Time.deltaTime;
        }
    }
    
    private void FireBullet()
    {
        // Determine which gun to fire from
        Transform selectedGunSpawnPoint = useLeftGun ? leftGunSpawnPoint : rightGunSpawnPoint;
    
        // Instantiate the bullet prefab at the selected gun position
        GameObject bullet = Instantiate(bulletPrefab, selectedGunSpawnPoint.position, selectedGunSpawnPoint.rotation);
        
        // Instantiate the bullet VFX prefab at the selected gun position
        GameObject bulletVFX = Instantiate(bulletVFXPrefab, selectedGunSpawnPoint.position, selectedGunSpawnPoint.rotation);
    
        // Add forward velocity to the bullet
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = selectedGunSpawnPoint.forward * bulletSpeed;
        }
    
        // Destroy the bullet after a set lifetime
        Destroy(bullet, bulletLifeTime);
    
        // Alternate guns for the next shot
        useLeftGun = !useLeftGun;
    }

    
    private void HandleMissileSystem()
    {
        // Lock on to a target if no target is locked
        if (currentTarget == null)
        {
            LockOnTarget();
        }

        // Fire missile if target is locked and cooldown is complete
        if (Input.GetKeyDown(fireMissileKey) && missileCooldownTimer <= 0 )
        {
            FireMissile();
            missileCooldownTimer = missileCooldown;
        }

        // Reduce cooldown timer
        if (missileCooldownTimer > 0)
        {
            missileCooldownTimer -= Time.deltaTime;
        }
    }
    
    private void LockOnTarget()
    {
        Collider[] potentialTargets = Physics.OverlapSphere(missileSpawnPoint.transform.position, lockOnRange, targetLayer);

        if (potentialTargets.Length > 0)
        {
            Transform nearestTarget = null;
            float shortestDistance = float.MaxValue;

            foreach (var target in potentialTargets)
            {
                if (!target.CompareTag(targetTag)) continue;

                Vector3 directionToTarget = (target.transform.position - missileSpawnPoint.position).normalized;
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

                // Check if the target is within the 40-degree cone
                if (angleToTarget > 20f) continue; // 20 degrees on either side of forward

                float distance = Vector3.Distance(missileSpawnPoint.position, target.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestTarget = target.transform;
                }
            }

            currentTarget = nearestTarget;
        }
        else
        {
            currentTarget = null;
        }
    }
        
    
    private void OnDrawGizmos()
    {
        if (missileSpawnPoint == null) return;

        // Draw the search radius
        Gizmos.color = new Color(1, 0, 0, 0.3f); // Semi-transparent red
        Gizmos.DrawSphere(missileSpawnPoint.position, lockOnRange);

        // Draw the forward arc for valid targets
        Vector3 forward = transform.forward * lockOnRange;
        Vector3 leftBoundary = Quaternion.Euler(0, -45, 0) * forward; // Adjust angle as needed
        Vector3 rightBoundary = Quaternion.Euler(0, 45, 0) * forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(missileSpawnPoint.position, leftBoundary);
        Gizmos.DrawRay(missileSpawnPoint.position, rightBoundary);
    }


    
    private void FireMissile()
    {
        // Instantiate a missile
        GameObject missile = Instantiate(missilePrefab, missileSpawnPoint.position, missileSpawnPoint.rotation);
        Missile missileScript = missile.GetComponent<Missile>();
        
        // Instantiate the missile VFX prefab at the missile spawn point
        GameObject missileVFX = Instantiate(missleVFXPrefab, missileSpawnPoint.position + missileSpawnPoint.forward * 2, missileSpawnPoint.rotation);

        if (missileScript != null)
        {
            if (currentTarget != null)
            {
                // Set target if available
                missileScript.SetTarget(currentTarget);
            }
            else
            {
                // Fire forward if no target is locked
                missileScript.SetForward();
            }
        }
    }


    private void HandleMovement()
    {
        // Apply inversion based on the isInverted toggle
        float pitch = Input.GetAxis("Vertical") * (isInverted ? -1 : 1) * rotationSpeed * Time.deltaTime;
        float yaw = Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime;
        transform.Rotate(pitch, yaw, 0);

        float moveSpeed = isBoosting ? speed * boostMultiplier : speed;
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }

    private void HandleBoost()
    {
        if (Input.GetKeyDown(boostKey) && !isBoosting)
        {
            isBoosting = true;
            boostTimer = boostDuration;
        }

        if (isBoosting)
        {
            boostVFX.SetActive(true);
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0)
            {
                isBoosting = false;
                boostVFX.SetActive(false);

            }
        }
    }

    private void HandleAirBrake()
    {
        if (Input.GetKey(airBrakeKey))
        {
            // Gradually reduce speed when braking
            speed = Mathf.Lerp(speed, speed * brakeMultiplier, Time.deltaTime * 2f);
        }
        else
        {
            speed = Mathf.Lerp(speed, 20f, Time.deltaTime);
        }
    }

    private void HandleBarrelRoll()
    {
        if (Input.GetKeyDown(barrelRollKey))
        {
            GetComponent<AudioSource>().PlayOneShot(rollSound);
            StartCoroutine(PerformBarrelRoll());
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

    private void HandleQuickTurn()
    {
        if (Input.GetKey(quickTurnKey))
        {
            float yaw = Input.GetAxis("Horizontal") * quickTurnMultiplier * rotationSpeed * Time.deltaTime;
            transform.Rotate(0, yaw, 0);
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
        float roll = Mathf.Abs(Input.GetAxis("Horizontal") * 30f);
        bool shouldActivate = roll > trailActivationThreshold;

        leftWingTrail.emitting = shouldActivate;
        rightWingTrail.emitting = shouldActivate;
    }

    private void LimitAltitude()
    {
        Vector3 position = transform.position;
        position.y = Mathf.Clamp(position.y, minAltitude, maxAltitude);
        transform.position = position;
    }

    private void AutoLevel()
    {
        // Gradually level the airplane on the X (pitch) and Z (roll) axes
        Quaternion currentRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, autoLevelSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Toggle the inversion state for vertical controls.
    /// </summary>
    public void ToggleInversion()
    {
        isInverted = !isInverted;
    }
}
    
}