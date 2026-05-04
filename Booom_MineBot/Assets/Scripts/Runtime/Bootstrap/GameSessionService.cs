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
    public enum WaveResolutionPhase
    {
        None,
        DetonatePerimeterBombs,
        ReevaluateDangerZones,
        CollapseDangerZones
    }

    public readonly struct WaveResolutionState
    {
        public WaveResolutionState(
            bool isActive,
            WaveResolutionPhase phase,
            int targetWave,
            int targetDangerRadius,
            int totalPerimeterBombs,
            int detonatedPerimeterBombs,
            float phaseElapsedSeconds)
        {
            IsActive = isActive;
            Phase = phase;
            TargetWave = Mathf.Max(0, targetWave);
            TargetDangerRadius = Mathf.Max(0, targetDangerRadius);
            TotalPerimeterBombs = Mathf.Max(0, totalPerimeterBombs);
            DetonatedPerimeterBombs = Mathf.Clamp(detonatedPerimeterBombs, 0, TotalPerimeterBombs);
            PhaseElapsedSeconds = Mathf.Max(0f, phaseElapsedSeconds);
        }

        public bool IsActive { get; }
        public WaveResolutionPhase Phase { get; }
        public int TargetWave { get; }
        public int TargetDangerRadius { get; }
        public int TotalPerimeterBombs { get; }
        public int DetonatedPerimeterBombs { get; }
        public float PhaseElapsedSeconds { get; }
    }

    public sealed class GameSessionService
    {
        private readonly LogicalGridState grid;
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
        private readonly List<GridPosition> perimeterBombOrigins = new List<GridPosition>();
        private WaveResolutionPlan activeWaveResolutionPlan;
        private WaveResolutionPhase waveResolutionPhase;
        private float waveResolutionPhaseElapsedSeconds;
        private bool waveResolutionPhaseApplied;
        private int detonatedPerimeterBombCount;
        private float passiveHazardSenseElapsed;

        public GameSessionService(
            LogicalGridState grid,
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
            this.grid = grid;
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
        public WaveResolution LastWaveResolution { get; private set; }
        public WorldPickupService WorldPickups => worldPickups;
        public IReadOnlyList<MiningProgressSnapshot> ActiveMiningProgressSnapshots => activeMiningProgressSnapshots;
        public float PlayerMiningTickIntervalSeconds => mining != null
            ? mining.PlayerMiningTickIntervalSeconds
            : MiningRules.DefaultPlayerMiningTickIntervalSeconds;
        public float MiningDisengageGraceSeconds => mining != null
            ? mining.MiningDisengageGraceSeconds
            : MiningRules.DefaultMiningDisengageGraceSeconds;
        public bool IsWaveResolutionActive => waveResolutionPhase != WaveResolutionPhase.None;
        public WaveResolutionState CurrentWaveResolutionState => new(
            IsWaveResolutionActive,
            waveResolutionPhase,
            activeWaveResolutionPlan.WaveNumber,
            activeWaveResolutionPlan.DangerRadius,
            perimeterBombOrigins.Count,
            detonatedPerimeterBombCount,
            waveResolutionPhaseElapsedSeconds);

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
            if (IsWaveResolutionActive)
            {
                return false;
            }

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
            if (IsWaveResolutionActive)
            {
                return false;
            }

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

        public bool BeginWaveResolution()
        {
            if (waves == null || IsWaveResolutionActive)
            {
                return false;
            }

            activeWaveResolutionPlan = waves.PrepareWaveResolution();
            perimeterBombOrigins.Clear();
            if (hazards != null)
            {
                IReadOnlyList<GridPosition> candidates = hazards.CollectPerimeterBombOrigins();
                for (int i = 0; i < candidates.Count; i++)
                {
                    perimeterBombOrigins.Add(candidates[i]);
                }
            }

            detonatedPerimeterBombCount = 0;
            SetWaveResolutionPhase(perimeterBombOrigins.Count > 0
                ? WaveResolutionPhase.DetonatePerimeterBombs
                : WaveResolutionPhase.ReevaluateDangerZones);
            PauseActiveRobots("地震结算暂停");
            StateChanged?.Invoke();
            return true;
        }

        public bool TickWaveResolution(float deltaTime)
        {
            if (!IsWaveResolutionActive)
            {
                return false;
            }

            waveResolutionPhaseElapsedSeconds += Mathf.Max(0f, deltaTime);
            bool changed = false;
            int guard = 0;
            while (IsWaveResolutionActive && guard++ < 8)
            {
                if (!waveResolutionPhaseApplied)
                {
                    ApplyWaveResolutionPhase();
                    waveResolutionPhaseApplied = true;
                    changed = true;
                }

                if (waveResolutionPhaseElapsedSeconds < HoldSecondsForCurrentPhase())
                {
                    break;
                }

                AdvanceWaveResolutionPhase();
                changed = true;
            }

            if (changed)
            {
                StateChanged?.Invoke();
            }

            return changed;
        }

        public bool TickRobots(float deltaTime)
        {
            if (robotAutomation == null || robots == null || robots.Count == 0)
            {
                return false;
            }

            if (IsWaveResolutionActive || vitals.IsDead || experience.HasPendingUpgrade)
            {
                string pauseReason = IsWaveResolutionActive ? "地震结算暂停" : "自动模式暂停";
                PauseActiveRobots(pauseReason);
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
            if (IsWaveResolutionActive)
            {
                return false;
            }

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

        private void ApplyWaveResolutionPhase()
        {
            switch (waveResolutionPhase)
            {
                case WaveResolutionPhase.DetonatePerimeterBombs:
                    ApplyPerimeterBombPhase();
                    break;
                case WaveResolutionPhase.ReevaluateDangerZones:
                    waves.EvaluateDangerZones(activeWaveResolutionPlan.DangerRadius);
                    break;
                case WaveResolutionPhase.CollapseDangerZones:
                    ApplyCollapseDangerZonePhase();
                    break;
            }
        }

        private void ApplyPerimeterBombPhase()
        {
            if (hazards == null)
            {
                return;
            }

            int explosionRadius = hazardRules != null
                ? hazardRules.ExplosionRadius
                : HazardRules.DefaultExplosionRadius;
            for (int i = 0; i < perimeterBombOrigins.Count; i++)
            {
                GridPosition origin = perimeterBombOrigins[i];
                if (!grid.IsInside(origin))
                {
                    continue;
                }

                GridCellState cell = grid.GetCell(origin);
                if (!cell.IsMineable || !cell.HasBomb)
                {
                    continue;
                }

                hazards.ResolveExplosion(origin, explosionRadius, directDamage: 0);
                detonatedPerimeterBombCount++;
            }

            SyncMiningProgressSnapshots();
        }

        private void ApplyCollapseDangerZonePhase()
        {
            LastWaveResolution = waves.FinalizeWaveResolution(activeWaveResolutionPlan, player.Position, vitals, robots);
            if (LastWaveResolution.DroppedResources.Metal > 0
                || LastWaveResolution.DroppedResources.Energy > 0
                || LastWaveResolution.DroppedResources.Experience > 0)
            {
                SpawnWorldPickupReward(player.Position, LastWaveResolution.DroppedResources, WorldPickupSource.WaveRecycle);
            }

            waves.EvaluateDangerZones();
            SyncMiningProgressSnapshots();
            RefreshPassiveHazardSense();
        }

        private void AdvanceWaveResolutionPhase()
        {
            switch (waveResolutionPhase)
            {
                case WaveResolutionPhase.DetonatePerimeterBombs:
                    SetWaveResolutionPhase(WaveResolutionPhase.ReevaluateDangerZones);
                    return;
                case WaveResolutionPhase.ReevaluateDangerZones:
                    SetWaveResolutionPhase(WaveResolutionPhase.CollapseDangerZones);
                    return;
                case WaveResolutionPhase.CollapseDangerZones:
                    CompleteWaveResolution();
                    return;
            }
        }

        private void CompleteWaveResolution()
        {
            waveResolutionPhase = WaveResolutionPhase.None;
            waveResolutionPhaseElapsedSeconds = 0f;
            waveResolutionPhaseApplied = false;
            perimeterBombOrigins.Clear();
            detonatedPerimeterBombCount = 0;
        }

        private float HoldSecondsForCurrentPhase()
        {
            if (waves == null)
            {
                return 0f;
            }

            return waveResolutionPhase switch
            {
                WaveResolutionPhase.DetonatePerimeterBombs => waves.PerimeterBombPhaseHoldSeconds,
                WaveResolutionPhase.ReevaluateDangerZones => waves.DangerRefreshPhaseHoldSeconds,
                WaveResolutionPhase.CollapseDangerZones => waves.CollapsePhaseHoldSeconds,
                _ => 0f
            };
        }

        private void SetWaveResolutionPhase(WaveResolutionPhase phase)
        {
            waveResolutionPhase = phase;
            waveResolutionPhaseElapsedSeconds = 0f;
            waveResolutionPhaseApplied = false;
        }

        private void PauseActiveRobots(string reason)
        {
            if (robots == null)
            {
                return;
            }

            foreach (RobotState robot in robots)
            {
                if (robot != null && robot.IsActive)
                {
                    robot.SetActivity(RobotActivity.Idle, reason);
                }
            }
        }

        private static bool AreEqual(MiningProgressSnapshot left, MiningProgressSnapshot right)
        {
            return left.Position.Equals(right.Position)
                && left.CurrentHealth == right.CurrentHealth
                && left.MaxHealth == right.MaxHealth;
        }
    }
}
