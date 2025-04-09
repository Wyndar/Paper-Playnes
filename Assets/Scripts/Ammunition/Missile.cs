using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Missile : NetworkBehaviour
{
    [Header("Missile Settings")]
    public float speed = 50f;
    public float steeringStrength = 5f;
    public float maxRange = 1000f;
    public float explosionRadius = 10f;
    public int damage = 50;
    public float explosionForce = 500f;
    public bool isHoming = false;
    public LayerMask damageMask;
    public float proximityTriggerDistance = 5f;

    public float knockbackMultiplier = 1f;
    public float knockbackUpwardModifier = 0.1f;

    private Rigidbody missileRigidbody;
    public Vector3 targetPoint;
    public Vector3 launchPosition;
    public Transform targetTransform;
    private Controller owner;
    public float fixedUpdateTimer = 0f;
    private float correctionTime = -1f;
    private bool hasCorrected = false;
    private static readonly Collider[] explosionHits = new Collider[50];
    public bool hasExploded = false;

    public void Initialize(Transform lockOnTarget, Vector3 fallbackTarget, Controller owner)
    {
        if (!IsServer) return;
        hasExploded = false;
        fixedUpdateTimer = 0f;
        missileRigidbody = GetComponent<Rigidbody>();
        launchPosition = transform.position;
        this.owner = owner;
        if (owner != null && lockOnTarget == owner.transform)
            lockOnTarget = null;
        targetTransform = lockOnTarget;
        targetPoint = lockOnTarget 
            ? PredictFuturePosition(launchPosition, lockOnTarget.position, lockOnTarget.GetComponent<Rigidbody>().linearVelocity, speed) 
            : fallbackTarget;
        if (!isHoming && lockOnTarget != null)
        {
            Vector3 predictedPos = PredictFuturePosition(launchPosition, lockOnTarget.position,
                lockOnTarget.GetComponent<Rigidbody>().linearVelocity, speed);
            float estimatedTime = (predictedPos - launchPosition).magnitude / speed;
            correctionTime = Mathf.Max(0f, estimatedTime - 0.5f);
        }
    }

    private void FixedUpdate()
    {
        if (!missileRigidbody || !IsServer) return;
        fixedUpdateTimer += Time.fixedDeltaTime;

        if (targetTransform && (!targetTransform.TryGetComponent(out HealthComponent health) || health.IsDead))
        {
            targetTransform = null;
            targetPoint = transform.position + transform.forward * (maxRange - Vector3.Distance(launchPosition, transform.position));
        }
        if (isHoming && targetTransform)
            targetPoint = PredictFuturePosition(transform.position,targetTransform.position,
                targetTransform.GetComponent<Rigidbody>().linearVelocity,speed);
        else if (!isHoming && !hasCorrected && targetTransform && fixedUpdateTimer >= correctionTime)
        {
            targetPoint = PredictFuturePosition(transform.position, targetTransform.position,
                targetTransform.GetComponent<Rigidbody>().linearVelocity, speed);
            hasCorrected = true;
        }

        Vector3 desiredDirection = (targetPoint - transform.position).normalized;
        Vector3 currentDirection = transform.forward;

        float maxTurnAngle = steeringStrength * Time.fixedDeltaTime;
        Vector3 newDirection = Vector3.RotateTowards(currentDirection, desiredDirection, Mathf.Deg2Rad * maxTurnAngle, 0f);

        transform.rotation = Quaternion.LookRotation(newDirection);
        missileRigidbody.linearVelocity = transform.forward * speed;

        if (fixedUpdateTimer >= 5f)
            Explode();
        // Check fallback explosion distance if not already exploded
        if (!hasExploded && targetTransform)
        {
            float sqrDist = (targetPoint - transform.position).sqrMagnitude;
            if (sqrDist <= proximityTriggerDistance * proximityTriggerDistance)
                Explode();
        }
    }


    private void OnTriggerEnter(Collider collider)
    {
        Debug.Log($"hit {collider.name}");
        Explode();
    }

    private void Explode()
    {
        if (hasExploded || !IsServer)
            return;
        hasExploded = true;
        SpawnManager.Instance.SpawnExplosionVFXServerRpc(transform.position);
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, explosionHits, damageMask);
        for (int i = 0; i < hitCount; i++)
        {
            if (!explosionHits[i].TryGetComponent(out HealthComponent health) || health == GetComponent<HealthComponent>())
                continue;
            float distanceToBlastCentre = Vector3.Distance(transform.position, explosionHits[i].transform.position);
            float damageFactor = 1f - (distanceToBlastCentre / explosionRadius);
            damageFactor = Mathf.Clamp01(damageFactor);
            damageFactor = Mathf.Lerp(0.5f, 1f, damageFactor);
            int finalDamage = Mathf.CeilToInt(damage * damageFactor);
            health.ModifyHealth(HealthModificationType.Damage, finalDamage, owner);
        }
        float knockbackRadius = explosionRadius * 1.5f;
        int knockbackCount = Physics.OverlapSphereNonAlloc(transform.position, knockbackRadius, explosionHits, damageMask);
        for (int i = 0; i < knockbackCount; i++)
        {
            if (!explosionHits[i].TryGetComponent(out Rigidbody rb) || rb == missileRigidbody)
                continue;
            rb.AddExplosionForce(explosionForce * knockbackMultiplier, transform.position, knockbackRadius, knockbackUpwardModifier, ForceMode.Impulse);
        }
        missileRigidbody = null;
        SpawnManager.Instance.ReturnMissileToPool(gameObject);
    }

    private Vector3 PredictFuturePosition(Vector3 origin, Vector3 targetPosition, Vector3 targetVelocity, float missileSpeed)
    {
        Vector3 displacement = targetPosition - origin;
        float relativeSpeed = Mathf.Max(0.1f, missileSpeed - Vector3.Dot(targetVelocity, displacement.normalized));
        float estimatedInterceptTime = displacement.magnitude / relativeSpeed;
        float maxFlightTime = maxRange / missileSpeed;
        estimatedInterceptTime = isHoming ? Mathf.Min(estimatedInterceptTime, 5f) : Mathf.Min(estimatedInterceptTime, maxFlightTime);
        return targetPosition + targetVelocity * Mathf.Max(0f, estimatedInterceptTime);
    }

}
