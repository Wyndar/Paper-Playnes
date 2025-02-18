using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider healthSlider;
    private HealthComponent health;

    public void Start()
    { 
        health = GetComponent<HealthComponent>();
        health.OnHealthChanged += UpdateHealthBar;
        healthSlider.maxValue = health.maxHP;
        healthSlider.value = health.maxHP;
    }

    public void UpdateHealthBar(int currentHP, int maxHP) => healthSlider.value = currentHP;
}

