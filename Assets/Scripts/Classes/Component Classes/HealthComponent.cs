using UnityEngine;
using System;
using Unity.Netcode;

public class HealthComponent : NetworkBehaviour
{
    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }
    public bool IsDead { get; private set; }

    public Destructible destructible;
    public GameEvent healthEvent;
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
    public void ModifyHealth(HealthModificationType type, int amount)
    {
        if (IsDead) return;
        healthEvent.RaiseEvent(this, type, amount, CurrentHP);
    }

    public void HandleHealthUpdate(int currentHP, int maxHP)
    {
        CurrentHP = currentHP;
        MaxHP = maxHP;
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        if (currentHP <= 0) Die();
    }
    private void Die()
    {
        if (IsDead) return;
        IsDead = true;
        OnDeath?.Invoke(IsDead);
        destructible.Die(gameObject);
    }
}

