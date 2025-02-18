using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject projectilePrefab;
    public GameObject weaponObject;
    public GameObject VFXObject;
    public int weaponWeight;
    public float fireRate;
    public float reloadRate;
    
    [Header("Paired Weapon")]
    public bool isPairedWeapon;
    public bool isLeftWeapon;
    public bool firedLastShot;
    public Weapon pairedWeapon;

    [Header("Magazine Stats")]
    public int magazineAmmoCount;
    public int maxMagazineAmmoCount;
    public int magazineHoldCount;
    public int maxMagazineHoldCount;

    [Header("OverHeat Stats")]
    //more work needs to be done here
    public bool overHeat;
    public int maxOverHeatCount;
    public float currentOverHeatCount;
    public float overHeatDecayRate;
    public Transform spawnTransform;

    public float timeSinceLastShot;
    public float timeSinceReloadStarted;
    private Coroutine cooldownRoutine;
    private Coroutine reloadRoutine;
    public void Start()
    {
        CapMagazine();
        timeSinceLastShot = fireRate;
        timeSinceReloadStarted = 0;
        if(isPairedWeapon)
            VerifyPair();
    }
    public void Fire()
    {
        if (cooldownRoutine != null || reloadRoutine != null)
            return;
        if (isPairedWeapon && firedLastShot)
        {
            firedLastShot = false;
            pairedWeapon.Fire();
            return;
        }
        Instantiate(projectilePrefab, spawnTransform.position, spawnTransform.rotation)
            .GetComponent<Projectile>().spawnerWeapon = this;
        Instantiate(VFXObject, spawnTransform.position, spawnTransform.rotation);
        magazineAmmoCount--;
        timeSinceLastShot = 0;
        firedLastShot = true;
        if (magazineAmmoCount <= 0)
        {
            Reload();
            return;
        }
        cooldownRoutine ??= StartCoroutine(Cooldown());
    }

    private void Reload()
    {
        magazineHoldCount--;
        magazineAmmoCount = maxMagazineAmmoCount;
        reloadRoutine ??= StartCoroutine(ReloadCooldown());
    }

    private IEnumerator Cooldown()
    {
        while (timeSinceLastShot < fireRate)
        {
            timeSinceLastShot += Time.time;
            yield return null;
        }
        if (timeSinceLastShot >= fireRate)
        {
            SyncPair();
            yield break;
        }
    }

    private IEnumerator ReloadCooldown()
    {
        while (timeSinceReloadStarted < reloadRate)
        {
            timeSinceReloadStarted += Time.time;
            yield return null;
        }
        if (timeSinceReloadStarted >= reloadRate)
        {
            timeSinceReloadStarted = 0;
            SyncPair();
            yield break;
        }
    }

    private void CapMagazine() => magazineHoldCount = magazineHoldCount > maxMagazineHoldCount ? maxMagazineHoldCount : magazineHoldCount;
    private void VerifyPair()
    {
        if (!isPairedWeapon)
            return;
        Debug.Assert(pairedWeapon.isPairedWeapon);
        Debug.Assert(pairedWeapon.pairedWeapon == this);
        Debug.Assert(pairedWeapon.isLeftWeapon == !isLeftWeapon);
        Debug.Assert(pairedWeapon.firedLastShot == !firedLastShot);
        Debug.Assert(pairedWeapon.weaponWeight == weaponWeight);
        Debug.Assert(pairedWeapon.projectilePrefab ==  projectilePrefab);
        Debug.Assert(pairedWeapon.overHeat == overHeat);
        Debug.Assert(pairedWeapon.overHeatDecayRate == overHeatDecayRate);
        Debug.Assert(pairedWeapon.maxOverHeatCount == maxOverHeatCount);
        Debug.Assert(pairedWeapon.magazineHoldCount == magazineHoldCount);
        Debug.Assert(pairedWeapon.maxMagazineHoldCount == magazineHoldCount);
    }
    private void SyncPair()
    {
        if (!isPairedWeapon)
            return;
        if(cooldownRoutine != null)
            StopCoroutine(cooldownRoutine);
        if (reloadRoutine != null)
            StopCoroutine(reloadRoutine);
        cooldownRoutine = null;
        reloadRoutine = null;
        pairedWeapon.timeSinceLastShot = timeSinceLastShot;
        pairedWeapon.magazineAmmoCount = magazineAmmoCount;
        pairedWeapon.magazineHoldCount = magazineHoldCount;
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
