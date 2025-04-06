using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject projectilePrefab;
    public GameObject weaponObject;
    public GameObject VFXObject;

    public int weaponWeight;
    public float ADSTime;
    public float fireRate;
    public float reloadRate;
    public float range;
    public int damage;

    [Header("Events")]
    public GameEvent playerAmmoUpdateEvent;
    public GameEvent ammoPickUpEvent;

    [Header("Paired Weapon")]
    public bool isPairedWeapon;
    public bool isLeftWeapon;
    public bool firedLastShot;
    public Weapon pairedWeapon;

    [Header("Magazine Stats")]
    public int ammoInCurrentMagCount;
    public int maxAmmoInMag;
    public int magInHoldCount;
    public int maxMagInHold;
    public int magCountInAmmoPacks;

    [Header("OverHeat Stats")]
    //more work needs to be done here
    public bool canOverHeat;
    public bool isOverHeated;
    public int maxOverHeatCount;
    public float currentOverHeatCount;
    public float overHeatDecayRate;
    public Transform spawnTransform;

    public float timeSinceLastShot;
    public float timeSinceReloadStarted;
    private Coroutine cooldownRoutine;
    private Coroutine reloadRoutine;
    public void Initialize()
    {
        CapMagazine();
        playerAmmoUpdateEvent.RaiseEvent(ammoInCurrentMagCount, magInHoldCount);
        timeSinceLastShot = fireRate;
        timeSinceReloadStarted = 0;
        if (isPairedWeapon && isLeftWeapon)
        {
            pairedWeapon.gameObject.SetActive(true);
            pairedWeapon.Initialize();
            VerifyPair();
        }
    }

   // private void OnEnable() => ammoPickUpEvent.OnEventRaised += PickedUpAmmo;

    private void OnDisable()
    {
     //   ammoPickUpEvent.OnEventRaised -= PickedUpAmmo;
        if (isPairedWeapon && isLeftWeapon)
            pairedWeapon.gameObject.SetActive(false);
    }

    public void Fire(Vector3 targetPosition, Controller player)
    {
        if (cooldownRoutine != null || reloadRoutine != null || (ammoInCurrentMagCount <= 0 && magInHoldCount <= 0))
            return;
        if (firedLastShot && isPairedWeapon)
        {
            firedLastShot = false;
            pairedWeapon.Fire(targetPosition, player);
            return;
        }
        //we need to work on projectiles later
        //GameObject proj = Instantiate(projectilePrefab, spawnTransform.position, spawnTransform.rotation);
        //Projectile projectile = proj.GetComponent<Projectile>();
        //projectile.InitializeDisplay(targetPosition);
        // Perform a raycast instead of instantiating a projectile
        Vector3 shootDirection = (targetPosition - spawnTransform.position).normalized;

        if (Physics.Raycast(spawnTransform.position, shootDirection, out RaycastHit hit, range))
        {
            if (hit.collider.TryGetComponent(out HealthComponent health))
                health.ModifyHealth(HealthModificationType.Damage, damage, player);
            if (VFXObject != null)
                Instantiate(VFXObject, hit.point, Quaternion.LookRotation(hit.normal));
        }
        Instantiate(VFXObject, spawnTransform.position, spawnTransform.rotation);
        ammoInCurrentMagCount--;
        timeSinceLastShot = 0;
        firedLastShot = true;
        if (!player.IsBot)
            playerAmmoUpdateEvent.RaiseEvent(ammoInCurrentMagCount, magInHoldCount);
        if (ammoInCurrentMagCount <= 0)
        {
            Reload(player);
            return;
        }
        cooldownRoutine ??= StartCoroutine(Cooldown());
    }
    public void Reload(Controller player)
    {
        magInHoldCount--;
        ammoInCurrentMagCount = maxAmmoInMag;
        if (!player.IsBot)
            playerAmmoUpdateEvent.RaiseEvent(ammoInCurrentMagCount, magInHoldCount);
        reloadRoutine ??= StartCoroutine(ReloadCooldown());
    }

    private IEnumerator Cooldown()
    {
        while (timeSinceLastShot < fireRate)
        {
            timeSinceLastShot += Time.deltaTime;
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
            timeSinceReloadStarted += Time.deltaTime;
            yield return null;
        }
        if (timeSinceReloadStarted >= reloadRate)
        {
            timeSinceReloadStarted = 0;
            SyncPair();
            yield break;
        }
    }
    private void PickedUpAmmo()
    {
        magInHoldCount += magCountInAmmoPacks;
        CapMagazine();
    }
    private void CapMagazine() => magInHoldCount = magInHoldCount > maxMagInHold ? maxMagInHold : magInHoldCount;
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
        Debug.Assert(pairedWeapon.canOverHeat == canOverHeat);
        Debug.Assert(pairedWeapon.isOverHeated == isOverHeated);
        Debug.Assert(pairedWeapon.overHeatDecayRate == overHeatDecayRate);
        Debug.Assert(pairedWeapon.maxOverHeatCount == maxOverHeatCount);
        Debug.Assert(pairedWeapon.magInHoldCount == magInHoldCount);
        Debug.Assert(pairedWeapon.maxMagInHold == magInHoldCount);
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
        pairedWeapon.ammoInCurrentMagCount = ammoInCurrentMagCount;
        pairedWeapon.magInHoldCount = magInHoldCount;
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
