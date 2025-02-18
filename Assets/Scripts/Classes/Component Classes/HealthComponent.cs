using UnityEngine;
using System;

public class HealthComponent : MonoBehaviour
{
    public int maxHP = 100;
    private int currentHP;
    public bool isDead { get; private set; } = false;

    public event Action<int, int> OnHealthChanged; // Event for UI updates
    public event Action OnDeath; // Event for death behavior

    void Start()
    {
        currentHP = maxHP;
        OnHealthChanged?.Invoke(currentHP, maxHP); // Initialize UI
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHP -= amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        Debug.Log($"{gameObject.name} took {amount} damage! Current HP: {currentHP}");

        OnHealthChanged?.Invoke(currentHP, maxHP); // Update UI

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        Debug.Log($"{gameObject.name} healed {amount} HP! Current HP: {currentHP}");

        OnHealthChanged?.Invoke(currentHP, maxHP); // Update UI
    }
    public void MaxHPIncrease(int amount)
    {
        maxHP += amount;
        Heal(amount);
    }
    public void MaxHPDecrease(int amount)
    {
        maxHP -= amount;
        TakeDamage(amount);
    }
    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log($"{gameObject.name} has died!");
        OnDeath?.Invoke(); // Trigger death event
    }
}

