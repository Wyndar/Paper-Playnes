using UnityEngine;

namespace GolemKin.ToonAirplaneController
{
    public class CheckpointRing : MonoBehaviour
    {
        public string playerTag = "Player";
        public int score = 10;
        
        public GameObject passEffect;
        public void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                GameObject.FindAnyObjectByType<ScoreController>().AddScore(score);
                Instantiate(passEffect, transform.position, Quaternion.identity);
            }
        }
    }
}