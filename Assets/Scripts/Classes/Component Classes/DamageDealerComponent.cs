using UnityEngine;

public class DamageDealerComponent : MonoBehaviour
{
    public int damageAmount = 20;
    public Controller controller;
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.gameObject.name);
        if (collision.gameObject.TryGetComponent(out HealthComponent health))
            health.ModifyHealth(HealthModificationType.Damage, damageAmount, controller);
        Debug.Log(gameObject.transform.position);
    }
}

