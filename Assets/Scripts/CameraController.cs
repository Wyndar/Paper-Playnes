using UnityEngine;

public class CameraController : MonoBehaviour
{
    public PlayerController playerController;
    private const float maxSpeed = 50f;
    public Vector3 offset;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        offset = transform.position - playerController.transform.position;
    }

    // Update is called once per frame
    public void Update()
    {
        //float speed = Mathf.Clamp(Vector3.Distance(transform.position, playerController.transform.position) * 2, minSpeed, maxSpeed);
        //if (Vector3.Distance(transform.position, playerController.transform.position) > offset.magnitude)
        //{
            transform.position = Vector3.MoveTowards(transform.position, playerController.transform.position + offset, maxSpeed * Time.deltaTime);
        //}
    }
}
