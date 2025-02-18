using UnityEngine;

[RequireComponent(typeof(DestructibleComponent))]
[RequireComponent (typeof(GameObject))]
public class Projectile : DamageDealer
{
    public GameObject spawnProjectile;
    public float travelSpeed;
    public float splashRadius;
    public float splashDamage;
    public float maxLifetime;
    public float currentLifetime;
    public int ammoWeight;
    public DestructibleComponent destructibleComponent;
    public Rigidbody rb;
    public Weapon spawnerWeapon;
    public void OnEnable()
    {
        currentLifetime = 0f;
        destructibleComponent = GetComponent<DestructibleComponent>();
        rb = GetComponent<Rigidbody>();
        rb.detectCollisions = false;
    }
    public void Update()
    {
        currentLifetime += Time.deltaTime;
        rb.linearVelocity = spawnerWeapon.transform.up * travelSpeed;
        if (currentLifetime > 0.5f)
            rb.detectCollisions = true;
        //trigger splash dmg here before destroying;
        if (currentLifetime > maxLifetime)
            Destroy(gameObject);
    }
}
