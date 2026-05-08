using System;
using System.Collections.Generic;
using Minebot.GridMining;

namespace Minebot.Progression
{
    public sealed class UpgradeSelectionService
    {
        private readonly ExperienceService experience;
        private readonly PlayerMiningState playerMining;
        private readonly PlayerVitals vitals;
        private readonly UpgradePoolConfig upgradePool;
        private readonly int thresholdIncrease;
        private readonly Random random;
        private UpgradeDefinition[] cachedCandidates = Array.Empty<UpgradeDefinition>();

        public UpgradeSelectionService(
            ExperienceService experience,
            PlayerMiningState playerMining,
            PlayerVitals vitals,
            UpgradePoolConfig upgradePool,
            GameBalanceConfig balance,
            Random random = null)
        {
            this.experience = experience;
            this.playerMining = playerMining;
            this.vitals = vitals;
            this.upgradePool = upgradePool;
            this.thresholdIncrease = balance != null ? balance.UpgradeThresholdIncrease : 3;
            this.random = random ?? new Random(Environment.TickCount);
        }

        public bool HasPendingUpgrade => experience.HasPendingUpgrade;

        public UpgradeDefinition[] GetCandidates(int maxCount)
        {
            if (!experience.HasPendingUpgrade)
            {
                cachedCandidates = Array.Empty<UpgradeDefinition>();
                return Array.Empty<UpgradeDefinition>();
            }

            UpgradeDefinition[] source = upgradePool != null && upgradePool.Upgrades.Length > 0
                ? upgradePool.Upgrades
                : DefaultUpgrades();
            int count = Math.Min(Math.Max(1, maxCount), source.Length);
            if (cachedCandidates.Length != count)
            {
                cachedCandidates = RollCandidates(source, count);
            }

            var copy = new UpgradeDefinition[cachedCandidates.Length];
            Array.Copy(cachedCandidates, copy, cachedCandidates.Length);
            return copy;
        }

        public bool Select(UpgradeDefinition upgrade)
        {
            if (!experience.HasPendingUpgrade || upgrade == null)
            {
                return false;
            }

            if (cachedCandidates.Length > 0 && !ContainsCandidate(upgrade))
            {
                return false;
            }

            int drillTier = (int)playerMining.DrillTier + Math.Max(0, upgrade.drillTierDelta);
            playerMining.DrillTier = (HardnessTier)Math.Min(drillTier, (int)HardnessTier.UltraHard);
            playerMining.MiningDamageBonus += Math.Max(0, upgrade.miningDamageDelta);
            playerMining.MoveSpeedMultiplier += Math.Max(0f, upgrade.moveSpeedMultiplierDelta);
            playerMining.MarkerCapacity += Math.Max(0, upgrade.markerCapacityDelta);
            vitals.IncreaseMaxHealth(upgrade.maxHealthDelta);
            experience.ConfirmUpgrade(thresholdIncrease);
            cachedCandidates = Array.Empty<UpgradeDefinition>();
            return true;
        }

        private bool ContainsCandidate(UpgradeDefinition candidate)
        {
            for (int i = 0; i < cachedCandidates.Length; i++)
            {
                UpgradeDefinition current = cachedCandidates[i];
                if (ReferenceEquals(current, candidate)
                    || string.Equals(current.id, candidate.id, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private UpgradeDefinition[] RollCandidates(UpgradeDefinition[] source, int count)
        {
            var pool = new List<UpgradeDefinition>(source);
            var results = new UpgradeDefinition[count];
            for (int resultIndex = 0; resultIndex < count && pool.Count > 0; resultIndex++)
            {
                int totalWeight = 0;
                for (int i = 0; i < pool.Count; i++)
                {
                    totalWeight += Math.Max(1, pool[i].weight);
                }

                int roll = random.Next(0, Math.Max(1, totalWeight));
                int cursor = 0;
                int chosenIndex = pool.Count - 1;
                for (int i = 0; i < pool.Count; i++)
                {
                    cursor += Math.Max(1, pool[i].weight);
                    if (roll < cursor)
                    {
                        chosenIndex = i;
                        break;
                    }
                }

                results[resultIndex] = pool[chosenIndex];
                pool.RemoveAt(chosenIndex);
            }

            return results;
        }

        private static UpgradeDefinition[] DefaultUpgrades()
        {
            return new[]
            {
                new UpgradeDefinition { id = "health", displayName = "生命扩容", maxHealthDelta = 1, weight = 1 },
                new UpgradeDefinition { id = "drill", displayName = "钻头升级", drillTierDelta = 1, miningDamageDelta = 1, weight = 1 },
                new UpgradeDefinition { id = "move", displayName = "移动提速", moveSpeedMultiplierDelta = 0.15f, weight = 1 },
                new UpgradeDefinition { id = "marker", displayName = "标记扩容", markerCapacityDelta = 1, weight = 1 }
            };
        }
    }
}
