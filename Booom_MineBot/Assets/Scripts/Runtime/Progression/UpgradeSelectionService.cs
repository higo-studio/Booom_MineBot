using System;
using Minebot.GridMining;

namespace Minebot.Progression
{
    public sealed class UpgradeSelectionService
    {
        private readonly ExperienceService experience;
        private readonly PlayerMiningState playerMining;
        private readonly PlayerVitals vitals;
        private readonly UpgradePoolConfig upgradePool;

        public UpgradeSelectionService(
            ExperienceService experience,
            PlayerMiningState playerMining,
            PlayerVitals vitals,
            UpgradePoolConfig upgradePool)
        {
            this.experience = experience;
            this.playerMining = playerMining;
            this.vitals = vitals;
            this.upgradePool = upgradePool;
        }

        public bool HasPendingUpgrade => experience.HasPendingUpgrade;

        public UpgradeDefinition[] GetCandidates(int maxCount)
        {
            if (!experience.HasPendingUpgrade)
            {
                return Array.Empty<UpgradeDefinition>();
            }

            UpgradeDefinition[] source = upgradePool != null && upgradePool.Upgrades.Length > 0
                ? upgradePool.Upgrades
                : DefaultUpgrades();
            int count = Math.Min(Math.Max(1, maxCount), source.Length);
            var candidates = new UpgradeDefinition[count];
            Array.Copy(source, candidates, count);
            return candidates;
        }

        public bool Select(UpgradeDefinition upgrade)
        {
            if (!experience.HasPendingUpgrade || upgrade == null)
            {
                return false;
            }

            int drillTier = (int)playerMining.DrillTier + Math.Max(0, upgrade.drillTierDelta);
            playerMining.DrillTier = (HardnessTier)Math.Min(drillTier, (int)HardnessTier.UltraHard);
            vitals.IncreaseMaxHealth(upgrade.maxHealthDelta);
            experience.ConfirmUpgrade(3);
            return true;
        }

        private static UpgradeDefinition[] DefaultUpgrades()
        {
            return new[]
            {
                new UpgradeDefinition { id = "drill", displayName = "钻头强化", drillTierDelta = 1, weight = 1 },
                new UpgradeDefinition { id = "armor", displayName = "加固外壳", maxHealthDelta = 1, weight = 1 }
            };
        }
    }
}
