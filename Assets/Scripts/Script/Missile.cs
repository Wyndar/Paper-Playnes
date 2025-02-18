using UnityEngine;

namespace GolemKin.ToonAirplaneController
{
    public class Missile : MonoBehaviour
    {
        [SerializeField] private float speed = 50f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float explosionRadius = 5f;
        [SerializeField] private float damage = 50f;

        [SerializeField] private GameObject impactEffect;
        public string groundTag = "Untagged";
        public string enemyTag = "Untagged";
        private Transform target;
        private bool isForwardMode = false;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetForward()
        {
            isForwardMode = true;
        }

        private void Update()
        {
            if (isForwardMode)
            {
                // Move straight forward
                transform.Translate(Vector3.forward * speed * Time.deltaTime);
                return;
            }

            if (target == null)
            {
                Destroy(gameObject); // Destroy if no target and not in forward mode
                return;
            }

            // Move towards the target
            Vector3 direction = (target.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isForwardMode || other.transform == target)
            {
                // Explode and deal damage
                Collider[] hitObjects = Physics.OverlapSphere(transform.position, explosionRadius);

                foreach (var obj in hitObjects)
                {
                    // Handle damage logic here (e.g., reduce health of the target)
                }
                
                if (other.CompareTag(enemyTag))
                {
                    other.GetComponent<Destructible>().TakeDamage(damage);
                    Instantiate(impactEffect, transform.position, transform.rotation);
                }

                Destroy(gameObject); // Destroy the missile after impact
            }
            else
            {
                Debug.LogWarning("Missile hit an unintended target: " + other.name);
                // Check if the bullet hits a target
                if (other.CompareTag(enemyTag))
                {
                    other.GetComponent<Destructible>().TakeDamage(damage);
                    Instantiate(impactEffect, transform.position, transform.rotation);
                }
            }
        }



        private void OnCollisionEnter(Collision other)
        {
            // Check if the bullet
            if (other.collider.CompareTag(groundTag))
            {

            }

            Instantiate(impactEffect, transform.position, transform.rotation);


            // Destroy the bullet on impact
            Destroy(gameObject);
        }
    }
}