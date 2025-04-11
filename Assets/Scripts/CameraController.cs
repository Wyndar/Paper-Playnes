using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform player;
    [SerializeField] private Vector3 offset = new(0, 2f, -8f);

    [Header("Camera Speed Settings")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float catchUpMultiplier = 1.5f;

    [Header("FOV Settings")]
    public float flightFOV = 120f;
    public float boostFOV = 150f, smallADSFOV = 90f, midADSFOV = 60f, largeADSFOV = 30f;

    //[Header("Screen Positioning")]
    //[SerializeField] private float verticalScreenOffset = 0.3f;

    //private void LateUpdate()
    //{
    //    if (!player) return;

    //    AdjustCameraPosition();
    //    AdjustCameraRotation();
    //}

    private void AdjustCameraPosition()
    {
        Vector3 desiredPosition = player.position + (player.forward * offset.z) + (Vector3.up * offset.y);
        float playerSpeed = player.GetComponent<Rigidbody>().linearVelocity.magnitude;
        float dynamicSpeed = followSpeed + (playerSpeed * catchUpMultiplier);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, dynamicSpeed * Time.deltaTime);
    }

    private void AdjustCameraRotation()
    {
        Quaternion targetRotation = Quaternion.LookRotation(player.forward, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
    }
    public void TeleportCameraBehindPlayer()
    {
        if (player == null)
        {
            Debug.LogWarning("Player not assigned to CameraController.");
            return;
        }

        transform.SetParent(player);
        transform.SetLocalPositionAndRotation(offset, Quaternion.LookRotation(Vector3.forward, Vector3.up));
    }

    public void RemoveCameraFromPlayer()
    {
        transform.SetParent(null);
        player = null;
        Debug.Log("Camera removed from the player.");
    }
}
