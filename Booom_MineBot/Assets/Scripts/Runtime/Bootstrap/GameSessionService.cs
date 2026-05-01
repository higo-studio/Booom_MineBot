using System;
using System.Collections.Generic;
using Minebot.Automation;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.HazardInference;
using Minebot.Progression;
using Minebot.WaveSurvival;
using UnityEngine;

namespace Minebot.Bootstrap
{
    public sealed class GameSessionService
    {
        private readonly PlayerMiningState player;
        private readonly MiningService mining;
        private readonly HazardService hazards;
        private readonly HazardRules hazardRules;
        private readonly PlayerEconomy economy;
        private readonly ExperienceService experience;
        private readonly WorldPickupService worldPickups;
        private readonly PlayerVitals vitals;
        private readonly RobotAutomationService robotAutomation;
        private readonly IList<RobotState> robots;
        private readonly WaveSurvivalService waves;
        private readonly ResourceAmount robotRecycleDrop;
        private readonly bool robotUsesPlayerDrillTier;
        private readonly HardnessTier robotFixedDrillTier;
        private readonly List<ScanReading> lastPassiveHazardSenseReadings = new List<ScanReading>();
        private float passiveHazardSenseElapsed;

        public GameSessionService(
            PlayerMiningState player,
            MiningService mining,
            HazardService hazards,
            HazardRules hazardRules,
            PlayerEconomy economy,
            ExperienceService experience,
            WorldPickupService worldPickups,
            PlayerVitals vitals,
            RobotAutomationService robotAutomation,
            IList<RobotState> robots,
            WaveSurvivalService waves,
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
            this.worldPickups = worldPickups;
            this.vitals = vitals;
            this.robotAutomation = robotAutomation;
            this.robots = robots;
            this.waves = waves;
            this.robotRecycleDrop = robotRecycleDrop;
            this.robotUsesPlayerDrillTier = robotUsesPlayerDrillTier;
            this.robotFixedDrillTier = robotFixedDrillTier;
        }

        public event Action StateChanged;
        public event Action<ResourceAmount> RewardGranted;
        public event Action<IReadOnlyList<ScanReading>> PassiveHazardSenseUpdated;
        public event Action<RobotAutomationResult> RobotAutomationCompleted;
        public MineResolution LastMineResolution { get; private set; }
        public RobotAutomationResult LastRobotAutomationResult { get; private set; }
        public WorldPickupService WorldPickups => worldPickups;

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
            MineResolution resolution = mining.TryMineDetailed(player, target);
            LastMineResolution = resolution;
            MineInteractionResult result = resolution.Result;
            if (result == MineInteractionResult.Mined || result == MineInteractionResult.TriggeredBomb)
            {
                SpawnWorldPickupRewards(resolution.ClearedCells, WorldPickupSource.PlayerMining);
            }

            if (result == MineInteractionResult.TriggeredBomb)
            {
                int damage = hazardRules != null ? hazardRules.DirectBombDamage : HazardRules.DefaultDirectBombDamage;
                vitals.Damage(damage);
            }

            if (result == MineInteractionResult.Mined || result == MineInteractionResult.TriggeredBomb)
            {
                StateChanged?.Invoke();
            }

            return result;
        }

        public IReadOnlyList<ScanReading> RefreshPassiveHazardSense()
        {
            int frontierRange = hazardRules != null ? hazardRules.ScanFrontierRange : HazardRules.DefaultScanFrontierRange;
            IReadOnlyList<ScanReading> readings = hazards.ScanFrontierWalls(player.Position, frontierRange);
            lastPassiveHazardSenseReadings.Clear();
            if (readings != null)
            {
                for (int i = 0; i < readings.Count; i++)
                {
                    lastPassiveHazardSenseReadings.Add(readings[i]);
                }
            }

            passiveHazardSenseElapsed = 0f;
            PassiveHazardSenseUpdated?.Invoke(lastPassiveHazardSenseReadings);
            return lastPassiveHazardSenseReadings;
        }

        public bool TickPassiveHazardSense(float deltaTime)
        {
            float interval = hazardRules != null
                ? hazardRules.PassiveHazardSenseIntervalSeconds
                : HazardRules.DefaultPassiveHazardSenseIntervalSeconds;
            passiveHazardSenseElapsed += Mathf.Max(0f, deltaTime);
            if (passiveHazardSenseElapsed < interval)
            {
                return false;
            }

            passiveHazardSenseElapsed = Mathf.Max(0f, passiveHazardSenseElapsed - interval);
            RefreshPassiveHazardSense();
            return true;
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
            bool avoidDangerZones = waves == null || waves.IsWarningWindowActive;
            foreach (RobotState robot in robots)
            {
                RobotAutomationResult result = robotAutomation.TickRobot(robot, drillTier, mining, deltaTime, avoidDangerZones);
                if (!result.HasStateChange)
                {
                    continue;
                }

                if (result.Kind == RobotAutomationResultKind.Mined)
                {
                    SpawnWorldPickupRewards(result.ClearedCells, WorldPickupSource.HelperRobotMining);
                }
                else if (result.Kind == RobotAutomationResultKind.TriggeredBomb)
                {
                    SpawnWorldPickupRewards(result.ClearedCells, WorldPickupSource.HelperRobotMining);
                    robot.Destroy("误挖炸药损毁");
                    if (robotRecycleDrop.Metal > 0 || robotRecycleDrop.Energy > 0 || robotRecycleDrop.Experience > 0)
                    {
                        SpawnWorldPickupReward(robot.Position, robotRecycleDrop, WorldPickupSource.RobotRecycle);
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

        public bool TickWorldPickups(float deltaTime, Vector2 playerWorldPosition)
        {
            if (worldPickups == null)
            {
                return false;
            }

            ResourceAmount reward = worldPickups.TickAndCollect(deltaTime, playerWorldPosition);
            if (reward.Metal <= 0 && reward.Energy <= 0 && reward.Experience <= 0)
            {
                return false;
            }

            GrantCollectedReward(reward);
            StateChanged?.Invoke();
            return true;
        }

        public void SpawnWorldPickupReward(GridPosition origin, ResourceAmount reward, WorldPickupSource source)
        {
            if (worldPickups == null)
            {
                GrantCollectedReward(reward);
                return;
            }

            worldPickups.SpawnReward(origin, reward, source);
        }

        private void SpawnWorldPickupRewards(IReadOnlyList<MineClearedCell> clearedCells, WorldPickupSource source)
        {
            if (clearedCells == null)
            {
                return;
            }

            for (int i = 0; i < clearedCells.Count; i++)
            {
                SpawnWorldPickupReward(clearedCells[i].Position, clearedCells[i].Reward, source);
            }
        }

        private void GrantCollectedReward(ResourceAmount reward)
        {
            if (reward.Metal <= 0 && reward.Energy <= 0 && reward.Experience <= 0)
            {
                return;
            }

            economy.Add(new ResourceAmount(reward.Metal, reward.Energy, 0));
            experience.AddExperience(reward.Experience);
            RewardGranted?.Invoke(reward);
        }
    }
}
