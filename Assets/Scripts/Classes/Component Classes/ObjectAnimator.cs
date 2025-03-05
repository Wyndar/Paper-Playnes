using UnityEngine;
public class ObjectAnimator : MonoBehaviour
{
    [Header("Rotation Settings")]
    public bool enableRotation = true;
    public float rotationSpeed = 30f;

    [Header("Pulse Settings")]
    public bool enablePulse = true;
    public float pulseSpeed = 1f;      
    public float pulseIntensity = 0.2f; 
    public float pulsePauseDuration = 0.3f;

    [Header("Float Settings")]
    public bool enableFloat = true;
    public float floatSpeed = 1f;    
    public float floatHeight = 0.5f; 

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private float pulseTimer = 0f;
    private bool isExpanding = true;
    private float pauseTimer = 0f;

    void Start()
    {
        originalScale = transform.localScale;
        originalPosition = transform.position;
    }

    void Update()
    {
        if (enableRotation)
            RotateObject();

        if (enablePulse)
            PulseObject();

        if (enableFloat)
            FloatObject();
    }

    private void RotateObject() => transform.Rotate(rotationSpeed * Time.deltaTime * Vector3.up);
    

    private void PulseObject()
    {
        if (pauseTimer > 0)
        {
            pauseTimer -= Time.deltaTime;
            return;
        }

        // Time-based progression from 0 to 1 (smooth pulse cycle)
        pulseTimer += Time.deltaTime * pulseSpeed;
        float progress = isExpanding ? pulseTimer : 1 - pulseTimer;

        // Use an easing function to accelerate and decelerate growth
        float easedProgress = Mathf.SmoothStep(0, 1, progress);
        float scaleMultiplier = 1f + easedProgress * pulseIntensity;

        transform.localScale = originalScale * scaleMultiplier;

        // Check if the phase is complete
        if (pulseTimer >= 1f)
        {
            pulseTimer = 0f;
            isExpanding = !isExpanding; // Switch between expanding & shrinking
            pauseTimer = pulsePauseDuration; // Brief pause
        }
    }


    private void FloatObject()
    {
        float newY = originalPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(originalPosition.x, newY, originalPosition.z);
    }
}
