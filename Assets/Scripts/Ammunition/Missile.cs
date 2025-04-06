using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Missile : NetworkBehaviour
{
    [Header("Missile Settings")]
    public float acceleration = 200f;
    public float maxRange = 1000f;
    public float explosionRadius = 10f;
    public int damage = 50;
    public float explosionForce = 500f;
    public LayerMask damageMask;

    [Header("Effects")]
    public AudioSource missileTravelSFX;

    private Rigidbody missileRigidbody;
    private Vector3 targetPoint;
    private Vector3 launchPosition;
    private Controller owner;

    private static readonly Collider[] explosionHits = new Collider[50];

    public void Initialize(Transform lockOnTarget, Vector3 fallbackTarget, Vector3 targetVelocity, Controller owner)
    {
        missileRigidbody = GetComponent<Rigidbody>();
        launchPosition = transform.position;
        this.owner = owner;
        if (missileTravelSFX) missileTravelSFX.Play();

        targetPoint = lockOnTarget 
            ? PredictFuturePosition(launchPosition, lockOnTarget.position, targetVelocity, acceleration) 
            : fallbackTarget;
    }

    private void FixedUpdate()
    {
        Vector3 dir = (targetPoint - transform.position).normalized;
        missileRigidbody.AddForce(acceleration * dir, ForceMode.Acceleration);
        if (Vector3.Distance(launchPosition, transform.position) >= maxRange)
            Explode();
    }

    private void OnCollisionEnter(Collision collision) => Explode();

    private void Explode()
    {
        if (!IsServer)
            return;
        SpawnManager.Instance.SpawnExplosionVFXServerRpc(transform.position);
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, explosionHits, damageMask);
        for (int i = 0; i < hitCount; i++)
            if (explosionHits[i].TryGetComponent(out HealthComponent health))
                health.ModifyHealth(HealthModificationType.Damage, damage, owner);

        Destroy(gameObject);
    }

    private Vector3 PredictFuturePosition(Vector3 origin, Vector3 targetPos, Vector3 targetVel, float missileAccel)
    {
        Vector3 displacement = targetPos - origin;
        float targetSpeedAlongDir = Vector3.Dot(targetVel, displacement.normalized);
        float estTime = displacement.magnitude / (missileAccel - targetSpeedAlongDir);
        return targetPos + targetVel * Mathf.Max(0f, estTime);
    }
}
