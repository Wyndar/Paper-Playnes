using TMPro;
using UnityEngine;

namespace GolemKin.ToonAirplaneController
{
    public class ScoreController : MonoBehaviour
    {
        private int totalScore = 0;
        public TextMeshProUGUI scoreText;
        public void AddScore(int score)
        {
            totalScore += score;
            scoreText.text = totalScore.ToString("00");

        }
    }
}