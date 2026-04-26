using System;
using System.Collections.Generic;
using Minebot.Automation;
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
        private readonly RobotAutomationService robotAutomation;
        private readonly IList<RobotState> robots;
        private readonly ResourceAmount robotRecycleDrop;
        private readonly bool robotUsesPlayerDrillTier;
        private readonly HardnessTier robotFixedDrillTier;

        public GameSessionService(
            PlayerMiningState player,
            MiningService mining,
            HazardService hazards,
            HazardRules hazardRules,
            PlayerEconomy economy,
            ExperienceService experience,
            PlayerVitals vitals,
            RobotAutomationService robotAutomation,
            IList<RobotState> robots,
            ResourceAmount robotRecycleDrop,
            bool robotUsesPlayerDrillTier,
            HardnessTier robotFixedDrillTier)
        {
            this.player = player;
            this.mining = mining;
            this.hazards = hazards;
            this.hazardRules = hazardRules;
            this.economy = economy;
            this.experience = experience;
            this.vitals = vitals;
            this.robotAutomation = robotAutomation;
            this.robots = robots;
            this.robotRecycleDrop = robotRecycleDrop;
            this.robotUsesPlayerDrillTier = robotUsesPlayerDrillTier;
            this.robotFixedDrillTier = robotFixedDrillTier;
        }

        public event Action StateChanged;
        public event Action<ResourceAmount> RewardGranted;
        public event Action<int> ScanCompleted;
        public event Action<RobotAutomationResult> RobotAutomationCompleted;
        public RobotAutomationResult LastRobotAutomationResult { get; private set; }

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
                int radius = hazardRules != null ? hazardRules.ExplosionRadius : HazardRules.DefaultExplosionRadius;
                int damage = hazardRules != null ? hazardRules.DirectBombDamage : HazardRules.DefaultDirectBombDamage;
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
            int cost = hazardRules != null ? hazardRules.ScanEnergyCost : HazardRules.DefaultScanEnergyCost;
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

        public bool TickRobots(float deltaTime)
        {
            if (robotAutomation == null || robots == null || robots.Count == 0)
            {
                return false;
            }

            if (vitals.IsDead || experience.HasPendingUpgrade)
            {
                foreach (RobotState robot in robots)
                {
                    if (robot.IsActive)
                    {
                        robot.SetActivity(RobotActivity.Idle, "自动模式暂停");
                    }
                }

                return false;
            }

            bool changed = false;
            HardnessTier drillTier = robotUsesPlayerDrillTier ? player.DrillTier : robotFixedDrillTier;
            foreach (RobotState robot in robots)
            {
                RobotAutomationResult result = robotAutomation.TickRobot(robot, drillTier, mining, deltaTime);
                if (!result.HasStateChange)
                {
                    continue;
                }

                if (result.Kind == RobotAutomationResultKind.Mined)
                {
                    GrantMiningReward(result.Reward);
                }
                else if (result.Kind == RobotAutomationResultKind.TriggeredBomb)
                {
                    int radius = hazardRules != null ? hazardRules.ExplosionRadius : HazardRules.DefaultExplosionRadius;
                    hazards.ResolveExplosion(result.Target, radius, 0);
                    robot.Destroy("误挖炸药损毁");
                    if (robotRecycleDrop.Metal > 0 || robotRecycleDrop.Energy > 0 || robotRecycleDrop.Experience > 0)
                    {
                        economy.Add(robotRecycleDrop);
                    }

                    result = new RobotAutomationResult(
                        RobotAutomationResultKind.Destroyed,
                        robot,
                        result.Target,
                        robotRecycleDrop,
                        "机器人误挖炸药并损毁。");
                }

                LastRobotAutomationResult = result;
                RobotAutomationCompleted?.Invoke(result);
                changed = true;

                if (experience.HasPendingUpgrade || vitals.IsDead)
                {
                    break;
                }
            }

            if (changed)
            {
                StateChanged?.Invoke();
            }

            return changed;
        }

        private void GrantMiningReward(ResourceAmount reward)
        {
            economy.Add(new ResourceAmount(reward.Metal, reward.Energy, 0));
            experience.AddExperience(reward.Experience);
            RewardGranted?.Invoke(reward);
        }
    }
}
