using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PaperPlaynesManager playnesManager;
    public InputManager inputManager;
    private Rigidbody rb;
    public float thrust = 10f; // Forward speed
    public float lift = 2f; // Lift force to keep the plane in the air
    public float drag = 0.1f; // Air resistance
    public float turnSpeed = 50f; // Rotation speed for turning
    public float pitchSpeed = 30f; // Tilt up/down speed
    public float waveAmplitude = 0.5f; // Amplitude of the wave motion
    public float waveFrequency = 2f; // Frequency of the wave motion (Hz)
    public bool allowMove;
    private float timeElapsed;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Paper planes rely on lift, not gravity
    }

    private void OnEnable()
    {
        inputManager.OnStartTouch += TouchStart;
        inputManager.OnEndTouch += TouchEnd;
    }

    private void OnDisable()
    {
        inputManager.OnStartTouch -= TouchStart;
        inputManager.OnEndTouch -= TouchEnd;
    }

    private void TouchStart(Vector2 position, float time) => allowMove = true;
    private void TouchEnd(Vector2 position, float time) => allowMove = false;

    private void FixedUpdate()
    {
        if (!allowMove) return;
        
        timeElapsed += Time.fixedDeltaTime;
        
        Vector2 input = inputManager.CurrentFingerPosition;
        float turnDirection = GetTurnDirection(input.x);
        float pitchDirection = GetPitchDirection(input.y);
        
        // Dynamic lift based on forward velocity
        float dynamicLift = lift * Mathf.Clamp(rb.linearVelocity.z / thrust, 0.5f, 1.5f);
        rb.AddForce(Vector3.up * dynamicLift, ForceMode.Acceleration);
        
        // Apply drag to only horizontal movement
        rb.linearVelocity = new Vector3(rb.linearVelocity.x * (1 - drag * Time.fixedDeltaTime), rb.linearVelocity.y, rb.linearVelocity.z);
        
        // Apply rotation for turning and tilting
        Quaternion turnRotation = Quaternion.Euler(0, turnSpeed * turnDirection * Time.fixedDeltaTime, 0);
        Quaternion pitchRotation = Quaternion.Euler(-pitchSpeed * pitchDirection * Time.fixedDeltaTime, 0, 0);
        rb.MoveRotation(rb.rotation * turnRotation * pitchRotation);
        
        // Apply wavy motion as an additional force
        float verticalOffset = Mathf.Sin(timeElapsed * waveFrequency * Mathf.PI * 2) * waveAmplitude;
        rb.AddForce(Vector3.up * verticalOffset, ForceMode.Acceleration);
        
        // Update forward motion to follow the new rotation direction
        rb.linearVelocity = rb.rotation * Vector3.forward * thrust;
    }

    private float GetTurnDirection(float xInput)
    {
        float screenCenterX = Screen.width / 2;
        return (xInput - screenCenterX) / screenCenterX; // Normalize to -1, 1
    }

    private float GetPitchDirection(float yInput)
    {
        float screenCenterY = Screen.height / 2;
        return (screenCenterY - yInput) / screenCenterY; // Inverted for natural pitch
    }
}
