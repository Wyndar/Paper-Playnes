using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class BotController : Controller
{
    [SerializeField] private float decisionCooldown = 1f;
    [SerializeField] private float spawnerCooldown = 10f;
    [SerializeField] private float detectionRadius = 500f;
    [SerializeField] private float firingRange = 200f;
    [SerializeField] private float avoidanceStrength = 5f;
    [SerializeField] private float obstacleDetectionDistance = 50f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private Weapon primaryWeapon;
    
    private Transform target;
#pragma warning disable IDE0044 // Add readonly modifier
    private static Collider[] detectedColliders = new Collider[50];
#pragma warning restore IDE0044 // Add readonly modifier

    public void OnEnable()
    {
        if (spawnerCooldown <= 0)
            StartCoroutine(BotBehaviorLoop());
    }
    public void OnDisable() => StopAllCoroutines();
    public override void Initialize(Team team)
    {
        if (!IsServer) return;
        ulong ownerId = NetworkManager.ServerClientId;
        //this should be set by rpc
        gameObject.name = SpawnManager.Instance.GetBotName();
        InitializeEntity(true, ownerId, team);
        StartCoroutine(BotBehaviorLoop());
    }

    private IEnumerator BotBehaviorLoop()
    {
        spawnerCooldown -= Time.deltaTime;
        while (spawnerCooldown <= 0)
        {
            FindNearestTarget();
            MoveTowardsTarget();
            AvoidObstacles();
            //ShootAtTarget();
            yield return new WaitForSeconds(decisionCooldown);
        }
        yield return null;
    }

    private void FindNearestTarget()
    {
        int numTargets = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, detectedColliders);
        Transform closestEnemy = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < numTargets; i++)
        {
            Collider closestCollider = detectedColliders[i];
            if (closestCollider.TryGetComponent(out HealthComponent enemyHealth) && enemyHealth != healthComponent && !enemyHealth.IsDead)
            {
                float distance = Vector3.Distance(transform.position, closestCollider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = closestCollider.transform;
                }
            }
        }
        target = closestEnemy;
    }

    private void MoveTowardsTarget()
    {
        if (target == null) return;

        Vector3 direction = (target.position - transform.position).normalized;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 2f);
        rb.linearVelocity = transform.forward * maxSpeed;
    }

    private void AvoidObstacles()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, obstacleDetectionDistance))
        {
            Vector3 avoidanceDirection = Vector3.Reflect(transform.forward, hit.normal);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(avoidanceDirection), Time.deltaTime * avoidanceStrength);
        }
    }
}
