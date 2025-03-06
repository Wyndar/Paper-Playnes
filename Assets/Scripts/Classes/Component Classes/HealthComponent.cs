using UnityEngine;
using System;

[RequireComponent(typeof(DestructibleComponent))]
public class HealthComponent : MonoBehaviour
{
    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }
    public bool IsDead { get; private set; }

    public Destructible destructible;
    public event Action<int, int> OnHealthChanged;
    public event Action<bool> OnDeath;
    public DestructibleComponent destructibleComponent;

    public void Start()
    {
        MaxHP = destructible.maxHealth;
        CurrentHP = MaxHP;
        IsDead = false;
        OnHealthChanged?.Invoke(CurrentHP, MaxHP); 
        destructibleComponent = GetComponent<DestructibleComponent>();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        CurrentHP -= amount;
        CurrentHP = Mathf.Clamp(CurrentHP, 0, MaxHP);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        if (CurrentHP <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (IsDead) return;

        CurrentHP += amount;
        CurrentHP = Mathf.Clamp(CurrentHP, 0, MaxHP);
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
    }
    public void MaxHPIncrease(int amount)
    {
        MaxHP += amount;
        Heal(amount);
    }
    public void MaxHPDecrease(int amount)
    {
        MaxHP -= amount;
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

