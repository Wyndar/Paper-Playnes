using UnityEngine;
using System.Collections;

public class Mine : DamageDealerComponent
{
    public MineData mineData;

    private bool isTriggered = false;
#pragma warning disable IDE0044
    private static Collider[] hitObjects = new Collider[10];
#pragma warning restore IDE0044

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerController _) && !isTriggered)
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

        if (mineData.explosionSound)
            AudioSource.PlayClipAtPoint(mineData.explosionSound, transform.position);

        Instantiate(mineData.explosionEffect, transform.position, Quaternion.identity);
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, mineData.explosionRadius, hitObjects);
        for (int i = 0; i < hitCount; i++)
        {
            //Rigidbody rb = hitObjects[i].attachedRigidbody;
            //if (rb)
            //{
            //    Vector3 forceDirection = (rb.transform.position - transform.position).normalized;
            //    rb.AddForce(forceDirection * mineData.knockbackForce, ForceMode.Impulse);
            //}
            if (hitObjects[i].TryGetComponent(out HealthComponent health))
                health.ModifyHealth(HealthModificationType.Damage,mineData.damage, controller);
        }

        //if (Camera.main.TryGetComponent(out ShockwaveEffect effect))
        //{
        //    effect.TriggerEffect(mineData.shockwaveDuration);
        //}

        Destroy(gameObject);
    }
}
