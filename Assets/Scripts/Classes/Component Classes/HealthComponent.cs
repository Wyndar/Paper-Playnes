using UnityEngine;
using System;

public class HealthComponent : MonoBehaviour
{
    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }
    public bool IsDead { get; private set; }

    public Destructible destructible;
    public event Action<int, int> OnHealthChanged;
    public event Action<bool> OnDeath;

    private void Start() => InitializeHealth();

    public void InitializeHealth()
    {
        MaxHP = destructible.maxHP;
        CurrentHP = MaxHP;
        IsDead = false;
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
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
        destructible.Die(gameObject);
    }
}

