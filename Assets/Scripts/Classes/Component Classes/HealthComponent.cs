using System;
using System.Collections.Generic;
using Unity.Netcode;

public class HealthComponent : NetworkBehaviour
{
    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }
    public bool IsDead { get; private set; } = true;

    public Destructible destructible;
    public GameEvent healthEvent;
    public GameEvent damageSourceLocationEvent;
    public event Action<int, int> OnHealthChanged;
    public event Action<bool> OnDeath;
    public List<Controller> damageSources;

    private void Start() => InitializeHealth();

    public void InitializeHealth()
    {
        MaxHP = destructible.maxHP;
        CurrentHP = MaxHP;
        IsDead = false;
        damageSources = new List<Controller>();
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
    }
    public void ModifyHealth(HealthModificationType type, int amount, Controller modificationSource)
    {
        if (IsDead) return;
        healthEvent.RaiseEvent(this, type, amount, CurrentHP, modificationSource);
    }

    public void HandleHealthUpdate(int currentHP, int maxHP, Controller modificationSource)
    {
        CurrentHP = currentHP;
        MaxHP = maxHP;
        OnHealthChanged?.Invoke(CurrentHP, MaxHP);
        if (currentHP <= 0) Die();
        if (currentHP == maxHP) damageSources.Clear();
        if (currentHP < maxHP) damageSources.Add(modificationSource);
        if (TryGetComponent(out PlayerController playerController) && playerController.IsOwner)
            damageSourceLocationEvent.RaiseEvent(modificationSource.transform.position);
    }
    private void Die()
    {
        if (IsDead) return;
        IsDead = true;
        OnDeath?.Invoke(IsDead);
        destructible.Die(gameObject, damageSources);
    }
}

