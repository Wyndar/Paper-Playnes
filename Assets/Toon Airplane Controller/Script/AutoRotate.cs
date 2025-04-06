using UnityEngine;

namespace GolemKin.ToonAirplaneController
{
    public class AutoRotate : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField, Header("Rotation Speed (Degrees per Second)")]
        private float rotationSpeed = 100f;

        [SerializeField, Header("Enable Rotation")]
        private bool isRotating = true;

        #endregion

        #region Unity Methods

        private void Update()
        {
            if (isRotating)
            {
                RotateObject();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Rotates the object around the Z-axis at the specified speed.
        /// </summary>
        private void RotateObject()
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the rotation.
        /// </summary>
        public void StartRotation()
        {
            isRotating = true;
        }

        /// <summary>
        /// Stops the rotation.
        /// </summary>
        public void StopRotation()
        {
            isRotating = false;
        }

        /// <summary>
        /// Toggles the rotation state.
        /// </summary>
        public void ToggleRotation()
        {
            isRotating = !isRotating;
        }

        #endregion
    }
}