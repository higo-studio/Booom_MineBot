using System;

namespace Minebot.Progression
{
    public sealed class ExperienceService
    {
        public ExperienceService(int firstThreshold)
        {
            NextThreshold = Math.Max(1, firstThreshold);
        }

        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int NextThreshold { get; private set; }
        public bool HasPendingUpgrade { get; private set; }

        public void AddExperience(int amount)
        {
            Experience += Math.Max(0, amount);
            if (Experience >= NextThreshold)
            {
                HasPendingUpgrade = true;
            }
        }

        public void ConfirmUpgrade(int nextThresholdIncrease)
        {
            if (!HasPendingUpgrade)
            {
                return;
            }

            Level++;
            Experience -= NextThreshold;
            NextThreshold += Math.Max(1, nextThresholdIncrease);
            HasPendingUpgrade = Experience >= NextThreshold;
        }
    }
}
