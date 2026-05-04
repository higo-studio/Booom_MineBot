using Minebot.Automation;
using System.Collections.Generic;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.HazardInference;
using Minebot.Progression;
using Minebot.WaveSurvival;
using UnityEngine;

namespace Minebot.Bootstrap
{
    public static class MinebotServices
    {
        public static RuntimeServiceRegistry Current { get; private set; }
        public static bool IsInitialized => Current != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsOnPlayStart()
        {
            Current = null;
        }

        public static RuntimeServiceRegistry Initialize(BootstrapConfig config)
        {
            Debug.Log($"[MinebotServices.Initialize] 调用 - config: {(config != null ? config.name : "null")}");
            if (Current != null)
            {
                Debug.Log($"[MinebotServices.Initialize] 服务已存在，直接返回");
                return Current;
            }

            bool usingGeneratedMap = config == null || config.DefaultMap == null;
            GameBalanceConfig balance = config != null ? config.BalanceConfig : null;
            MapGenerationSettings generatedMapSettings = config != null
                ? config.GeneratedMapConfig.ToSettings()
                : MapGenerationSettings.CreateDefault();
            RewardConfig? rewardConfig = balance != null ? CreateRewardConfig(balance) : null;
            LogicalGridState grid = config != null && config.DefaultMap != null
                ? config.DefaultMap.CreateGridState()
                : MapGenerator.Generate(generatedMapSettings, rewardConfig);
            int maxHealth = balance != null ? balance.PlayerMaxHealth : 3;
            int firstThreshold = balance != null ? balance.FirstUpgradeThreshold : 4;
            var economy = new PlayerEconomy(balance != null ? balance.StartingResources : new Minebot.Common.ResourceAmount(1, 4, 0));
            var vitals = new PlayerVitals(maxHealth);
            var experience = new ExperienceService(firstThreshold);
            var worldPickups = new WorldPickupService();
            var robots = new List<RobotState>();
            WaveConfig waveConfig = config != null ? config.WaveConfig : null;
            var waves = new WaveSurvivalService(grid, waveConfig);
            MiningRules miningRules = config != null ? config.MiningRules : null;

            var miningState = new PlayerMiningState(grid.PlayerSpawn, HardnessTier.Soil);
            var mining = new MiningService(grid, miningRules);
            var hazards = new HazardService(grid);
            HazardRules hazardRules = config != null ? config.HazardRules : null;
            Debug.Log(
                $"[MinebotServices] 配置检查 - HazardRules: {(hazardRules != null ? hazardRules.name : "null")}, " +
                $"BombSpawnChance: {(hazardRules?.BombSpawnChance ?? HazardRules.DefaultBombSpawnChance):F4}, " +
                $"BombSeed: {(hazardRules?.BombSeed ?? HazardRules.DefaultBombSeed)}, " +
                $"BombSafeRadius: {(hazardRules?.BombSafeRadius ?? HazardRules.DefaultBombSafeRadius)}, " +
                $"ScanFrontierRange: {(hazardRules?.ScanFrontierRange ?? HazardRules.DefaultScanFrontierRange)}, " +
                $"ScanUsesEightWayNeighbors: {(hazardRules?.ScanUsesEightWayNeighbors ?? HazardRules.DefaultScanUsesEightWayNeighbors)}, " +
                $"PassiveHazardSenseInterval: {(hazardRules?.PassiveHazardSenseIntervalSeconds ?? HazardRules.DefaultPassiveHazardSenseIntervalSeconds):F2}, " +
                $"DirectBombDamage: {(hazardRules?.DirectBombDamage ?? HazardRules.DefaultDirectBombDamage)}, " +
                $"MiningRules: {(miningRules != null ? miningRules.name : "null")}, " +
                $"PlayerMiningTick: {(miningRules != null ? miningRules.PlayerMiningTickIntervalSeconds : MiningRules.DefaultPlayerMiningTickIntervalSeconds):F2}, " +
                $"MiningGrace: {(miningRules != null ? miningRules.MiningDisengageGraceSeconds : MiningRules.DefaultMiningDisengageGraceSeconds):F2}, " +
                $"PlayerBaseAttack: {(miningRules != null ? miningRules.PlayerBaseAttack : MiningRules.DefaultPlayerBaseAttack)}");
            if (usingGeneratedMap)
            {
                int seed = hazardRules != null ? hazardRules.BombSeed : HazardRules.DefaultBombSeed;
                float chance = hazardRules != null ? hazardRules.BombSpawnChance : HazardRules.DefaultBombSpawnChance;
                int safeRadius = hazardRules != null ? hazardRules.BombSafeRadius : HazardRules.DefaultBombSafeRadius;
                Debug.Log($"[MinebotServices] 正在生成炸弹 - Seed: {seed}, Chance: {chance:F4}, SafeRadius: {safeRadius}");
                hazards.SeedBombs(seed, chance, grid.PlayerSpawn, safeRadius);
            }

            var robotAutomation = new RobotAutomationService(
                grid,
                miningRules,
                balance != null ? balance.RobotMaxTargetDistance : RobotAutomationService.DefaultMaxTargetDistance,
                balance != null ? balance.RobotActionInterval : 0.35f);
            var session = new GameSessionService(
                grid,
                miningState,
                mining,
                hazards,
                hazardRules,
                economy,
                experience,
                worldPickups,
                vitals,
                robotAutomation,
                robots,
                waves,
                waveConfig != null ? waveConfig.RobotRecycleDrop : ResourceAmount.Zero,
                balance == null || balance.RobotUsesPlayerDrillTier,
                balance != null ? balance.RobotFixedDrillTier : HardnessTier.Soil);
            var upgrades = new UpgradeSelectionService(
                experience,
                miningState,
                vitals,
                config != null ? config.UpgradePool : null,
                balance);
            var factory = new RobotFactoryService(
                economy,
                balance != null ? balance.RobotCost : new Minebot.Common.ResourceAmount(4, 0, 0),
                robots);
            var buildings = new BuildingPlacementService(grid, economy);
            IReadOnlyList<BuildingDefinition> buildingDefinitions = config != null
                ? config.BuildingDefinitions
                : null;

            Current = new RuntimeServiceRegistry(
                grid,
                miningState,
                mining,
                hazards,
                session,
                upgrades,
                economy,
                vitals,
                experience,
                worldPickups,
                new BaseOpsService(economy, vitals),
                buildings,
                buildingDefinitions,
                robotAutomation,
                factory,
                robots,
                waves);

            return Current;
        }

        public static void ResetForTests()
        {
            Current = null;
        }

        private static RewardConfig CreateRewardConfig(GameBalanceConfig balance)
        {
            return new RewardConfig(
                balance.GetMetalRange(HardnessTier.Soil),
                balance.GetEnergyRange(HardnessTier.Soil),
                balance.GetExperienceRange(HardnessTier.Soil),
                balance.GetMetalRange(HardnessTier.Stone),
                balance.GetEnergyRange(HardnessTier.Stone),
                balance.GetExperienceRange(HardnessTier.Stone),
                balance.GetMetalRange(HardnessTier.HardRock),
                balance.GetEnergyRange(HardnessTier.HardRock),
                balance.GetExperienceRange(HardnessTier.HardRock),
                balance.GetMetalRange(HardnessTier.UltraHard),
                balance.GetEnergyRange(HardnessTier.UltraHard),
                balance.GetExperienceRange(HardnessTier.UltraHard));
        }
    }
}