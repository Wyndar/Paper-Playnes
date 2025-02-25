using UnityEngine;

public class DamageDealerComponent : MonoBehaviour
{
    public int damageAmount = 20;

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.gameObject.name);
        if (collision.gameObject.TryGetComponent<HealthComponent>(out var health))
            health.TakeDamage(damageAmount);
        Debug.Log(gameObject.transform.position);
    }
}

