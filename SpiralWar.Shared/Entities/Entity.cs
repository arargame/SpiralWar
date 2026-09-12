using System;
using SpiralWar.Core;

namespace SpiralWar.Entities
{
    /// <summary>
    /// Base for everything that owns hit points.
    ///
    /// Damage is routed through a single virtual method so subclasses can modify it in one
    /// place — armored rings halve it, the ship ignores it while invulnerable — and every
    /// listener still sees the amount that was ACTUALLY applied, which is what the floating
    /// damage numbers and the hit sounds are driven from.
    /// </summary>
    public abstract class Entity : GameObject
    {
        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0 && IsActive;

        public float HealthPercent => MaxHealth > 0
            ? CurrentHealth / (float)MaxHealth
            : 0f;

        /// <summary>Raised after damage lands. The payload is the amount actually applied.</summary>
        public event EventHandler<int>? Damaged;

        /// <summary>Raised after healing lands. The payload is the amount actually restored.</summary>
        public event EventHandler<int>? Healed;

        public event EventHandler? Died;

        protected Entity(int maxHealth)
        {
            if (maxHealth < 1) maxHealth = 1;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        /// <summary>Resets hit points, used when a pooled entity is reused.</summary>
        protected void ResetHealth(int maxHealth)
        {
            if (maxHealth < 1) maxHealth = 1;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        /// <summary>
        /// Applies damage after <see cref="ModifyIncomingDamage"/> has had a say.
        /// Returns the amount that was actually taken off, which may be 0.
        /// </summary>
        public virtual int TakeDamage(int amount)
        {
            if (!IsAlive || amount <= 0) return 0;

            int effective = ModifyIncomingDamage(amount);
            if (effective <= 0) return 0;

            int before = CurrentHealth;
            CurrentHealth = Math.Max(0, CurrentHealth - effective);
            int applied = before - CurrentHealth;

            if (applied > 0)
                Damaged?.Invoke(this, applied);

            if (CurrentHealth <= 0)
                OnDeath();

            return applied;
        }

        /// <summary>Hook for armor, resistances and invulnerability windows.</summary>
        protected virtual int ModifyIncomingDamage(int amount) => amount;

        public virtual int Heal(int amount)
        {
            if (!IsAlive || amount <= 0) return 0;

            int before = CurrentHealth;
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
            int applied = CurrentHealth - before;

            if (applied > 0)
                Healed?.Invoke(this, applied);

            return applied;
        }

        protected virtual void OnDeath()
        {
            Deactivate();
            Died?.Invoke(this, EventArgs.Empty);
        }
    }
}
