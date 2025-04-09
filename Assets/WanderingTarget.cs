using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WanderingTarget : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float directionChangeInterval = 2f;
    public float turnSpeed = 2f;

    private Rigidbody rb;
    private Vector3 targetDirection;
    private float changeTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        PickNewDirection();
    }

    private void FixedUpdate()
    {
        changeTimer -= Time.fixedDeltaTime;
        if (changeTimer <= 0f)
            PickNewDirection();

        SteerTowardTarget();
        rb.linearVelocity = transform.forward * moveSpeed;
    }

    private void PickNewDirection()
    {
        changeTimer = directionChangeInterval;
        Vector3 randomDir = Random.insideUnitSphere;
        randomDir.y = 0f; // stay on horizontal plane
        targetDirection = randomDir.normalized;
    }

    private void SteerTowardTarget()
    {
        if (targetDirection == Vector3.zero) return;

        Quaternion targetRot = Quaternion.LookRotation(targetDirection, Vector3.up);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
    }
}
