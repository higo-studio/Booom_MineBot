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
        private readonly List<MiningProgressSnapshot> activeMiningProgressSnapshots = new List<MiningProgressSnapshot>();
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
        public event Action<IReadOnlyList<MiningProgressSnapshot>> MiningProgressUpdated;
        public event Action<IReadOnlyList<ScanReading>> PassiveHazardSenseUpdated;
        public event Action<RobotAutomationResult> RobotAutomationCompleted;
        public MineResolution LastMineResolution { get; private set; }
        public RobotAutomationResult LastRobotAutomationResult { get; private set; }
        public WorldPickupService WorldPickups => worldPickups;
        public IReadOnlyList<MiningProgressSnapshot> ActiveMiningProgressSnapshots => activeMiningProgressSnapshots;
        public float PlayerMiningTickIntervalSeconds => mining != null
            ? mining.PlayerMiningTickIntervalSeconds
            : MiningRules.DefaultPlayerMiningTickIntervalSeconds;
        public float MiningDisengageGraceSeconds => mining != null
            ? mining.MiningDisengageGraceSeconds
            : MiningRules.DefaultMiningDisengageGraceSeconds;

        public MineInteractionResult Move(GridPosition direction)
        {
            MineInteractionResult result = mining.Move(player, direction);
            return result;
        }

        public MineInteractionResult Mine(GridPosition target)
        {
            MineResolution resolution = mining.TryMineDetailed(player, target);
            LastMineResolution = resolution;
            SyncMiningProgressSnapshots();
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
                RefreshPassiveHazardSenseIfAffected(resolution.ClearedCells);
                StateChanged?.Invoke();
            }

            return result;
        }

        public bool TickMiningRecovery(float deltaTime)
        {
            if (mining == null || !mining.TickMiningRecovery(deltaTime))
            {
                return false;
            }

            return SyncMiningProgressSnapshots();
        }

        public IReadOnlyList<ScanReading> RefreshPassiveHazardSense()
        {
            IReadOnlyList<ScanReading> readings = hazards.ScanNearbyEmptyCells(
                player.Position,
                GetPassiveHazardSenseFrontierRange(),
                GetPassiveHazardSenseUsesEightWayNeighbors());
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
                if (robot == null || !robot.IsActive)
                {
                    continue;
                }

                RobotAutomationResult result = robotAutomation.TickRobot(robot, drillTier, mining, deltaTime, avoidDangerZones);
                if (!result.HasStateChange)
                {
                    continue;
                }

                SyncMiningProgressSnapshots();

                if (result.Kind == RobotAutomationResultKind.Mined)
                {
                    SpawnWorldPickupRewards(result.ClearedCells, WorldPickupSource.HelperRobotMining);
                    RefreshPassiveHazardSenseIfAffected(result.ClearedCells);
                }
                else if (result.Kind == RobotAutomationResultKind.TriggeredBomb)
                {
                    SpawnWorldPickupRewards(result.ClearedCells, WorldPickupSource.HelperRobotMining);
                    RefreshPassiveHazardSenseIfAffected(result.ClearedCells);
                    robot.Destroy("误挖炸药损毁");
                    if (robotRecycleDrop.Metal > 0 || robotRecycleDrop.Energy > 0 || robotRecycleDrop.Experience > 0)
                    {
                        SpawnWorldPickupReward(robot.Position, robotRecycleDrop, WorldPickupSource.RobotRecycle);
                    }

                    result = new RobotAutomationResult(
                        RobotAutomationResultKind.TriggeredBomb,
                        robot,
                        result.Target,
                        robotRecycleDrop,
                        "机器人误挖炸药并损毁。",
                        result.ClearedCells);
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

        private bool RefreshPassiveHazardSenseIfAffected(IReadOnlyList<MineClearedCell> clearedCells)
        {
            if (clearedCells == null || clearedCells.Count == 0)
            {
                return false;
            }

            GridPosition playerPosition = player.Position;
            int frontierRange = GetPassiveHazardSenseFrontierRange();
            bool usesEightWayNeighbors = GetPassiveHazardSenseUsesEightWayNeighbors();
            for (int i = 0; i < clearedCells.Count; i++)
            {
                if (!AffectsPassiveHazardSense(clearedCells[i].Position, playerPosition, frontierRange, usesEightWayNeighbors))
                {
                    continue;
                }

                RefreshPassiveHazardSense();
                return true;
            }

            return false;
        }

        private int GetPassiveHazardSenseFrontierRange()
        {
            return hazardRules != null
                ? hazardRules.ScanFrontierRange
                : HazardRules.DefaultScanFrontierRange;
        }

        private bool GetPassiveHazardSenseUsesEightWayNeighbors()
        {
            return hazardRules != null
                ? hazardRules.ScanUsesEightWayNeighbors
                : HazardRules.DefaultScanUsesEightWayNeighbors;
        }

        private static bool AffectsPassiveHazardSense(
            GridPosition clearedPosition,
            GridPosition playerPosition,
            int frontierRange,
            bool usesEightWayNeighbors)
        {
            // Passive scan samples the configured local neighborhood, and each reading depends on one extra ring
            // for nearby wall adjacency and 3x3 bomb counts.
            int dx = Math.Abs(clearedPosition.X - playerPosition.X);
            int dy = Math.Abs(clearedPosition.Y - playerPosition.Y);
            int influenceRadius = Math.Max(0, frontierRange) + 1;
            return usesEightWayNeighbors
                ? Math.Max(dx, dy) <= influenceRadius
                : dx + dy <= influenceRadius;
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

        private bool SyncMiningProgressSnapshots()
        {
            IReadOnlyList<MiningProgressSnapshot> source = mining != null
                ? mining.ActiveProgressSnapshots
                : Array.Empty<MiningProgressSnapshot>();

            bool changed = activeMiningProgressSnapshots.Count != source.Count;
            if (!changed)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    if (!AreEqual(activeMiningProgressSnapshots[i], source[i]))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed)
            {
                return false;
            }

            activeMiningProgressSnapshots.Clear();
            for (int i = 0; i < source.Count; i++)
            {
                activeMiningProgressSnapshots.Add(source[i]);
            }

            MiningProgressUpdated?.Invoke(activeMiningProgressSnapshots);
            return true;
        }

        private static bool AreEqual(MiningProgressSnapshot left, MiningProgressSnapshot right)
        {
            return left.Position.Equals(right.Position)
                && left.CurrentHealth == right.CurrentHealth
                && left.MaxHealth == right.MaxHealth;
        }
    }
}
