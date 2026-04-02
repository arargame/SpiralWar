using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core;

namespace SpiralWar.Entities
{
    /// <summary>
    /// HP yönetimi olan tüm oyun nesnelerinin tabanı.
    /// </summary>
    public abstract class Entity : GameObject
    {
        public int  MaxHealth     { get; private set; }
        public int  CurrentHealth { get; private set; }
        public bool IsAlive       => CurrentHealth > 0 && IsActive;

        public event EventHandler? Died;

        protected Entity(int maxHealth)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public virtual void TakeDamage(int amount)
        {
            if (!IsAlive) return;
            CurrentHealth = Math.Max(0, CurrentHealth - amount);
            if (CurrentHealth <= 0) OnDeath();
        }

        protected virtual void OnDeath()
        {
            IsActive = false;
            Died?.Invoke(this, EventArgs.Empty);
        }
    }
}
