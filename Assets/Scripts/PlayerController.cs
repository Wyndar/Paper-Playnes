using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PaperPlaynesManager playnesManager;
    public InputManager inputManager;
    public TagHandle spawnerTag;
    public Rigidbody rb;
    public int speed;
    public float rotationSpeed;
    public float maxRotationAngle;
    private bool allowMove;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
        inputManager.OnStartTouch += TouchStart;
        inputManager.OnEndTouch += TouchEnd;
    }
    public void OnDisable()
    {
        inputManager.OnStartTouch -= TouchStart;
        inputManager.OnEndTouch -= TouchEnd;
    }
    private void TouchStart(Vector2 position, float time) => allowMove = true;
    private void TouchEnd(Vector2 position, float time) => allowMove = false;

    void FixedUpdate()
    {
        float currentY = rb.rotation.eulerAngles.y;
        float currentZ = rb.rotation.eulerAngles.z;
        if (currentY > 180) currentY -= 360;
        if (currentZ > 180) currentZ -= 360;

        int direction = GetXDirection();
        float targetY = allowMove ? GetTargetEulerAngle(currentY, direction) : GetDefaultEulerAngle(currentY);
        float targetZ = allowMove ? GetTargetEulerAngle(currentZ, direction * -1) : GetDefaultEulerAngle(currentZ);

        rb.MoveRotation(Quaternion.Euler(0, targetY, targetZ));
        rb.MovePosition(transform.position + speed * Time.fixedDeltaTime * transform.forward);
    }

    private int GetXDirection() => inputManager.CurrentFingerPosition.x > Screen.width / 2 ? 1 : -1;
    private float GetTargetEulerAngle(float originalRotation, int direction) =>
        Mathf.Clamp(originalRotation + (rotationSpeed * direction * Time.fixedDeltaTime), -maxRotationAngle, maxRotationAngle);
    private float GetDefaultEulerAngle(float originalRotation) => Mathf.MoveTowards(originalRotation, 0, rotationSpeed * Time.fixedDeltaTime);
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(spawnerTag))
        {
            Spawner spawner = gameObject.GetComponent<Spawner>();
            if (spawner.boxLocation != BoxLocation.Centre)
                playnesManager.ChangeBoxLocations(spawner);
        }
    }
}
