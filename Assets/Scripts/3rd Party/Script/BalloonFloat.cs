using UnityEngine;

namespace GolemKin.ToonAirplaneController
{
    public class BalloonFloat : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField, Header("Floating Settings")] [Tooltip("The upward force applied to the balloon.")]
        private float floatForce = 5f;

        [SerializeField, Tooltip("The speed of rotation to make the balloon more dynamic.")]
        private float rotationSpeed = 20f;

        [SerializeField, Tooltip("The maximum height offset from the original position.")]
        private float maxHeightOffset = 5f;

        [SerializeField, Tooltip("The minimum height offset from the original position.")]
        private float minHeightOffset = 1f;

        #endregion

        #region Private Fields

        private Rigidbody rb;
        private float originalY;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            // InitializeDisplay Rigidbody
            rb = GetComponentInChildren<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("Rigidbody is missing on the balloon object. Adding one now.");
                rb = gameObject.AddComponent<Rigidbody>();
            }

            // Set Rigidbody properties
            rb.useGravity = false;

            // Store the original Y position
            originalY = transform.position.y;
        }

        private void FixedUpdate()
        {
            HandleFloating();
        }

        #endregion

        #region Private Methods

        private void HandleFloating()
        {
            float currentY = transform.position.y;
            float maxY = originalY + maxHeightOffset;
            float minY = originalY - minHeightOffset;

            // Apply upward or downward force based on position
            if (currentY < maxY && rb.linearVelocity.y <= 0)
            {
                rb.AddForce(Vector3.up * floatForce, ForceMode.Acceleration);
            }
            else if (currentY > minY && rb.linearVelocity.y >= 0)
            {
                rb.AddForce(Vector3.down * floatForce, ForceMode.Acceleration);
            }

            // Apply slight rotation for dynamic movement
            transform.Rotate(rotationSpeed * Time.deltaTime * Vector3.up);
        }

        #endregion
    }
}