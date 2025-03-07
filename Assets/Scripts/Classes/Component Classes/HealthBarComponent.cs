using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(HealthComponent))]
public class HealthBar : MonoBehaviour
{
    private Slider healthSlider;
    private HealthComponent health;

    public void InitializeHealthBar(Slider slider)
    {
        health = GetComponent<HealthComponent>();
        healthSlider = slider;
        health.OnHealthChanged += UpdateHealthBar;
        UpdateHealthBar(health.CurrentHP, health.MaxHP);
    }
    public void UpdateHealthBar(int currentHP, int maxHP)
    {
        healthSlider.maxValue = maxHP;
        healthSlider.value = currentHP;
    }
}

