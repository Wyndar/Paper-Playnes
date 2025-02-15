using System;
using UnityEngine;
namespace GolemKin.ToonAirplaneController
{
public class Bullet : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private GameObject impactEffect;
    public string enemyTag = "Enemy";
    public string groundTag = "Ground";

    private void OnTriggerEnter(Collider other)
    {
        // Check if the bullet hits a target
        if (other.CompareTag(enemyTag))
        {
           other.GetComponent<Destructible>().TakeDamage(damage);
           Instantiate(impactEffect, transform.position, transform.rotation);
        
           // Destroy the bullet on impact
           Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        // Check if the bullet hits a target
        if (other.collider.CompareTag(groundTag))
        {
            // Handle damage logic
            Debug.Log("Hit Enemy! Apply " + damage + " damage.");
            
        }
        Instantiate(impactEffect, transform.position, transform.rotation);


        // Destroy the bullet on impact
        Destroy(gameObject);
    }
}
    
}