using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    public int damageAmount = 20;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<HealthComponent>(out var health))
            health.TakeDamage(damageAmount);
    }
}

