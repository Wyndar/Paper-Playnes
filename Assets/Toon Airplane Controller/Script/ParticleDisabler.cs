using System.Collections;
using UnityEngine;

namespace GolemKin.ToonAirplaneController
{
    /// <summary>
    /// Disables the GameObject once the ParticleSystem has finished playing using an IEnumerator.
    /// </summary>
    public class ParticleDisabler : MonoBehaviour
    {
        #region Fields
        
        public float deactivateTime = 1f;

        #endregion

        #region Unity Methods

        private void Awake()
        {
        }

        private void OnEnable()
        {
            StartCoroutine(WaitForParticlesToFinish());
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Waits for the particle system to finish playing, then disables the GameObject.
        /// </summary>
        private IEnumerator WaitForParticlesToFinish()
        {
            yield return new WaitForSeconds(deactivateTime);

            // Disable the GameObject
            Destroy(gameObject);
        }

        #endregion
    }
}