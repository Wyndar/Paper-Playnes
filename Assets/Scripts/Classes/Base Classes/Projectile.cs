using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : DamageDealerComponent
{
    public float travelSpeed;
    public float maxLifetime = 10f;
    private float currentLifetime;
    private Rigidbody rb;
    private Vector3 direction;

    public void Initialize(Vector3 shootDirection)
    {
        currentLifetime = 0f;
        rb = GetComponent<Rigidbody>();
        direction = shootDirection;
        rb.linearVelocity = direction * travelSpeed;
        Debug.Log(shootDirection);
    }

    private void Update()
    {
        currentLifetime += Time.deltaTime;

        if (currentLifetime > maxLifetime)
        {
            Destroy(gameObject);
        }
    }
}
