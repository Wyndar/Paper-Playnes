using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField]
    private Transform target; // The airplane to follow

    [Header("Camera Offset")]
    [SerializeField]
    private Vector3 offset = new Vector3(0, 5, -10); // Default offset from the airplane

    [SerializeField] private float followSpeed = 5f; // Speed at which the camera follows the airplane

    [Header("Dynamic FOV")]
    [SerializeField]
    private float baseFOV = 60f;

    [SerializeField] private float boostFOV = 75f;
    [SerializeField] private float fovTransitionSpeed = 2f;

    private Camera mainCamera;
    private bool isBoosting;

    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("No Camera component found on this GameObject!");
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        FollowTarget();
        AdjustFOV();
    }

    private void FollowTarget()
    {
        // Smoothly move the camera to follow the target
        Vector3 desiredPosition = target.position + target.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // Smoothly rotate the camera to look at the target
        Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, followSpeed * Time.deltaTime);
    }

    private void AdjustFOV()
    {
        if (mainCamera == null) return;

        // Adjust FOV dynamically based on the airplane's boost state
        float targetFOV = target.GetComponent<PlayerController>() ? boostFOV : baseFOV;
        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
    }
}
