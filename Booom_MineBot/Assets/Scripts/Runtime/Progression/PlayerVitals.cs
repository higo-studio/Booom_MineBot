using System;

namespace Minebot.Progression
{
    public sealed class PlayerVitals
    {
        public PlayerVitals(int maxHealth)
        {
            MaxHealth = Math.Max(1, maxHealth);
            CurrentHealth = MaxHealth;
        }

        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        public void Damage(int amount)
        {
            CurrentHealth = Math.Max(0, CurrentHealth - Math.Max(0, amount));
        }

        public void RepairToFull()
        {
            CurrentHealth = MaxHealth;
        }

        public void IncreaseMaxHealth(int amount)
        {
            MaxHealth += Math.Max(0, amount);
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + Math.Max(0, amount));
        }
    }
}
