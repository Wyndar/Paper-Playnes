using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject projectilePrefab;
    public GameObject weaponObject;
    public GameObject VFXObject;
    public AmmoClass ammoClass;

    public int weaponWeight;
    public float ADSTime;
    public float fireRate;
    public float reloadRate;
    public float range;
    public int damage;
    public Transform spawnTransform;

    [Header("Paired Weapon")]
    [Tooltip("Many weapons are visually shown on two wings so we need two alternating points")]
    public bool isPairedWeapon;
    public GameObject pairedWeaponObject;
    public bool firedLastShot;
    public Transform pairedWeaponSpawnTransform;

    [Header("Magazine Stats")]
    public int ammoInCurrentMagCount;
    public int maxAmmoInMag;

    [Header("OverHeat Stats")]
    //more work needs to be done here
    public bool canOverHeat;
    public bool isOverHeated;
    public int maxOverHeatCount;
    public float currentOverHeatCount;
    public float overHeatDecayRate;

    public float timeSinceLastShot;
    public float timeSinceReloadStarted;
    private Coroutine cooldownRoutine;
    private Coroutine reloadRoutine;
    private PlaneStorageHandler planeStorage;
    public void Initialize(PlaneStorageHandler planeStorageHandler, Controller player)
    {
        planeStorage = planeStorageHandler;
        timeSinceLastShot = fireRate;
        timeSinceReloadStarted = 0;
        int ammoLeftInHold = player.IsBot ? 999 : planeStorage.GetAmmoInStorageCount(ammoClass);
        ammoInCurrentMagCount = ammoLeftInHold >= maxAmmoInMag ? maxAmmoInMag : ammoLeftInHold;
    }

    private void OnEnable()
    {
        if (isPairedWeapon)
            pairedWeaponObject.SetActive(true);
    }
    private void OnDisable()
    {
        if (isPairedWeapon)
            pairedWeaponObject.SetActive(false);
        StopAllCoroutines();
        cooldownRoutine = null;
        reloadRoutine = null;
    }

    public void Fire(Vector3 targetPosition, Controller player)
    {
        if (cooldownRoutine != null || reloadRoutine != null)
        {
            if (timeSinceLastShot < fireRate || timeSinceReloadStarted < reloadRate) return;
            Debug.LogWarning("A coroutine wasn't cleaned up properly or you're on cooldown");
            return;
        }
        int ammoCount = player.IsBot ? 999 : planeStorage.GetAmmoInStorageCount(ammoClass);
        if (ammoCount <= 0 || ammoInCurrentMagCount <= 0)
        {
            Reload(player);
            return;
        }
        Vector3 shootPosition = firedLastShot && isPairedWeapon ? pairedWeaponSpawnTransform.position : spawnTransform.position;
        firedLastShot = !firedLastShot;
        Vector3 shootDirection = (targetPosition - shootPosition).normalized;
        NetworkObjectReference target = player.NetworkObject;
        if (Physics.Raycast(shootPosition, shootDirection, out RaycastHit hit, range))
        {
            if (hit.collider.TryGetComponent(out HealthComponent health))
                if (projectilePrefab == null)
                {
                    health.ModifyHealth(HealthModificationType.Damage, damage, player);
                    if (VFXObject != null)
                        Instantiate(VFXObject, hit.point, Quaternion.LookRotation(hit.normal));
                }
                else
                    target = health.NetworkObject;
        }
        if (projectilePrefab != null)
            SpawnManager.Instance.SpawnMissileServerRpc(projectilePrefab.name, shootPosition,
                Quaternion.Euler(shootDirection), target, targetPosition, player.NetworkObject);
        if (VFXObject != null)
            Instantiate(VFXObject, spawnTransform.position, spawnTransform.rotation);
        ammoInCurrentMagCount--;
        timeSinceLastShot = 0;
        if (!player.IsBot)
            planeStorage.UpdateWeaponAmmo(-1, 0);
        if (ammoInCurrentMagCount <= 0)
        {
            Reload(player);
            return;
        }
        cooldownRoutine ??= StartCoroutine(Cooldown());
    }

    public void Reload(Controller player) => reloadRoutine ??= StartCoroutine(ReloadCooldown(player));

    private IEnumerator Cooldown()
    {
        while (timeSinceLastShot < fireRate)
        {
            timeSinceLastShot += Time.deltaTime;
            yield return null;
        }
        cooldownRoutine = null;
        yield break;
    }

    private IEnumerator ReloadCooldown(Controller player)
    {
        while (timeSinceReloadStarted < reloadRate)
        {
            timeSinceReloadStarted += Time.deltaTime;
            yield return null;
        }
        timeSinceReloadStarted = 0;
        int ammoLeftInHold = player.IsBot ? 999 : planeStorage.GetAmmoInStorageCount(ammoClass);
        ammoInCurrentMagCount = ammoLeftInHold >= maxAmmoInMag ? maxAmmoInMag : ammoLeftInHold;
        if (!player.IsBot)
            planeStorage.UpdateWeaponAmmo(0, ammoInCurrentMagCount);
        reloadRoutine = null;
        yield break;
    }
  
    //    private void HandleMachineGun()
    //    {
    //        // Check if the fire key is pressed and cooldown is complete
    //        if (Input.GetKey(fireGunKey) && fireCooldown <= 0f)
    //        {
    //            FireBullet();
    //            fireCooldown = fireRate; // Reset cooldown
    //        }

    //        // Reduce the cooldown timer
    //        if (fireCooldown > 0f)
    //        {
    //            fireCooldown -= Time.deltaTime;
    //        }
    //    }

    //    private void FireBullet()
    //    {
    //        // Determine which gun to fire from
    //        Transform selectedGunSpawnPoint = useLeftGun ? leftGunSpawnPoint : rightGunSpawnPoint;

    //        // Instantiate the bullet prefab at the selected gun position
    //        GameObject bullet = Instantiate(bulletPrefab, selectedGunSpawnPoint.position, selectedGunSpawnPoint.rotation);

    //        // Instantiate the bullet VFX prefab at the selected gun position
    //        _ = Instantiate(bulletVFXPrefab, selectedGunSpawnPoint.position, selectedGunSpawnPoint.rotation);

    //        // Add forward velocity to the bullet
    //        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
    //        if (bulletRb != null)
    //        {
    //            bulletRb.linearVelocity = selectedGunSpawnPoint.forward * bulletSpeed;
    //        }

    //        // Destroy the bullet after a set lifetime
    //        Destroy(bullet, bulletLifeTime);

    //        // Alternate guns for the next shot
    //        useLeftGun = !useLeftGun;
    //    }
}
