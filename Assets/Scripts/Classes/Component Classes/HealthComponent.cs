using UnityEngine;
using System;

[RequireComponent(typeof(DestructibleComponent))]
public class HealthComponent : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP;
    public bool IsDead { get; private set; } = false;

    public event Action<int, int> OnHealthChanged;
    public event Action<bool> OnDeath;
    public DestructibleComponent destructibleComponent;

    void Start()
    {
        currentHP = maxHP;
        OnHealthChanged?.Invoke(currentHP, maxHP); 
        destructibleComponent = GetComponent<DestructibleComponent>();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead)
            return;

        currentHP -= amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        OnHealthChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (IsDead) return;

        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        OnHealthChanged?.Invoke(currentHP, maxHP);
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
        if (IsDead) return;
        IsDead = true;
        OnDeath?.Invoke(IsDead);
        destructibleComponent.Destroy();
    }
}

