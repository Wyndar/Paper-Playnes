using UnityEngine;

public class BirdAI : MonoBehaviour
{
    public Renderer flightArea;
    public float speed = 3f;
    public float turnSpeed = 2f;
    public float minPauseTime = 1f;
    public float maxPauseTime = 3f;
    public float minAltitude = 2f;
    public float maxAltitude = 10f;

    private Vector3 targetPosition;
    private bool isMoving = false;

    private void Start()
    {
        if (flightArea == null)
        {
            Debug.LogError("No flight area assigned! Please assign a MeshRenderer.");
            enabled = false;
            return;
        }

        PickNewTarget();
    }

    private void Update()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            isMoving = false;
            Invoke(nameof(PickNewTarget), Random.Range(minPauseTime, maxPauseTime));
        }
    }

    private void PickNewTarget()
    {
        Bounds bounds = flightArea.bounds;

        targetPosition = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Mathf.Clamp(Random.Range(bounds.min.y, bounds.max.y), minAltitude, maxAltitude),
            Random.Range(bounds.min.z, bounds.max.z)
        );

        isMoving = true;
    }
}
