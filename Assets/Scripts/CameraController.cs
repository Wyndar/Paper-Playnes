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
        transform.position = player.position + (player.forward * offset.z) + (player.up * offset.y);
        transform.LookAt(player.position + (Vector3.up * 1.5f));
        //comment this if using update follow
        transform.SetParent(player);
        Debug.Log("Camera teleported behind the player.");
    }
}
