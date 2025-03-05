using UnityEngine;
using System.Collections;

public class Mine : MonoBehaviour
{
    public MineData mineData;

    private bool isTriggered = false;
    private static Collider[] hitObjects = new Collider[10];

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isTriggered)
        {
            isTriggered = true;
            StartCoroutine(Detonate());
        }
    }

    private IEnumerator Detonate()
    {
        yield return new WaitForSeconds(mineData.detonationTime);
        Explode();
    }

    private void Explode()
    {
        Debug.Log("Sonic Mine Exploded!");

        // Play sound effect
        if (mineData.explosionSound)
            AudioSource.PlayClipAtPoint(mineData.explosionSound, transform.position);

        // Spawn shockwave effect (to be created in Step 3)
        Instantiate(mineData.explosionEffect, transform.position, Quaternion.identity);

        // Apply knockback force
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, mineData.explosionRadius, hitObjects);
        for (int i = 0; i < hitCount; i++)
        {
            Rigidbody rb = hitObjects[i].attachedRigidbody;
            if (rb)
            {
                Vector3 forceDirection = (rb.transform.position - transform.position).normalized;
                rb.AddForce(forceDirection * mineData.knockbackForce, ForceMode.Impulse);
            }
        }

        //// Apply post-processing effect (Step 4)
        //if (Camera.main.TryGetComponent(out ShockwaveEffect effect))
        //{
        //    effect.TriggerEffect(mineData.shockwaveDuration);
        //}

        Destroy(gameObject); // Destroy the mine after explosion
    }
}
