using UnityEngine;

namespace GolemKin.ToonAirplaneController
{
    public class Destructible : MonoBehaviour
    {
       public float health = 100f;
       public int score = 10;
       public GameObject deathEffect;
       
       public void TakeDamage(float damage)
       {
           health -= damage;
           if (health <= 0)
           {
               Die();
           }
       }

       public void Die()
       {
           GameObject.FindAnyObjectByType<ScoreController>().AddScore(score);

              Instantiate(deathEffect, transform.position, Quaternion.identity);
              Destroy(gameObject);
       }
    }
}