using System;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.HazardInference;
using Minebot.Progression;

namespace Minebot.Bootstrap
{
    public readonly struct ScanResult
    {
        public ScanResult(bool success, int bombCount)
        {
            Success = success;
            BombCount = bombCount;
        }

        public bool Success { get; }
        public int BombCount { get; }
    }

    public sealed class GameSessionService
    {
        private readonly PlayerMiningState player;
        private readonly MiningService mining;
        private readonly HazardService hazards;
        private readonly HazardRules hazardRules;
        private readonly PlayerEconomy economy;
        private readonly ExperienceService experience;
        private readonly PlayerVitals vitals;

        public GameSessionService(
            PlayerMiningState player,
            MiningService mining,
            HazardService hazards,
            HazardRules hazardRules,
            PlayerEconomy economy,
            ExperienceService experience,
            PlayerVitals vitals)
        {
            this.player = player;
            this.mining = mining;
            this.hazards = hazards;
            this.hazardRules = hazardRules;
            this.economy = economy;
            this.experience = experience;
            this.vitals = vitals;
        }

        public event Action StateChanged;
        public event Action<ResourceAmount> RewardGranted;
        public event Action<int> ScanCompleted;

        public MineInteractionResult Move(GridPosition direction)
        {
            MineInteractionResult result = mining.Move(player, direction);
            if (result == MineInteractionResult.Moved)
            {
                StateChanged?.Invoke();
            }

            return result;
        }

        public MineInteractionResult Mine(GridPosition target)
        {
            MineInteractionResult result = mining.TryMine(player, target, out ResourceAmount reward);
            if (result == MineInteractionResult.Mined || result == MineInteractionResult.TriggeredBomb)
            {
                GrantMiningReward(reward);
            }

            if (result == MineInteractionResult.TriggeredBomb)
            {
                int radius = hazardRules != null ? hazardRules.ExplosionRadius : 1;
                int damage = hazardRules != null ? hazardRules.DirectBombDamage : 1;
                ExplosionResolution resolution = hazards.ResolveExplosion(target, radius, damage);
                vitals.Damage(resolution.DirectDamage);
            }

            if (result == MineInteractionResult.Mined || result == MineInteractionResult.TriggeredBomb)
            {
                StateChanged?.Invoke();
            }

            return result;
        }

        public ScanResult Scan(GridPosition origin)
        {
            int cost = hazardRules != null ? hazardRules.ScanEnergyCost : 1;
            if (!economy.TrySpend(new ResourceAmount(0, cost, 0)))
            {
                return new ScanResult(false, 0);
            }

            bool eightWay = hazardRules == null || hazardRules.ScanUsesEightWayNeighbors;
            int bombCount = hazards.ScanBombCount(origin, eightWay);
            ScanCompleted?.Invoke(bombCount);
            StateChanged?.Invoke();
            return new ScanResult(true, bombCount);
        }

        public bool ToggleMarker(GridPosition position)
        {
            bool marked = hazards.ToggleMarker(position);
            StateChanged?.Invoke();
            return marked;
        }

        private void GrantMiningReward(ResourceAmount reward)
        {
            economy.Add(new ResourceAmount(reward.Metal, reward.Energy, 0));
            experience.AddExperience(reward.Experience);
            RewardGranted?.Invoke(reward);
        }
    }
}
