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
    public static class RuntimeServiceFactory
    {
        public static MinebotContainer CreateContainer(BootstrapConfig config)
        {
            Debug.Log($"[RuntimeServiceFactory.CreateContainer] 调用 - config: {(config != null ? config.name : "null")}");

            bool usingGeneratedMap = config == null || config.DefaultMap == null;
            GameBalanceConfig balance = config != null ? config.BalanceConfig : null;
            MapGenerationSettings generatedMapSettings = config != null
                ? config.GeneratedMapConfig.ToSettings()
                : MapGenerationSettings.CreateDefault();
            RewardConfig? rewardConfig = balance != null ? CreateRewardConfig(balance) : null;
            LogicalGridState grid = config != null && config.DefaultMap != null
                ? config.DefaultMap.CreateGridState()
                : MapGenerator.Generate(generatedMapSettings, rewardConfig);
            HazardRules hazardRules = config != null ? config.HazardRules : null;
            MiningRules miningRules = config != null ? config.MiningRules : null;
            WaveConfig waveConfig = config != null ? config.WaveConfig : null;
            UpgradePoolConfig upgradePool = config != null ? config.UpgradePool : null;
            ScoreConfig scoreConfig = config != null ? config.ScoreConfig : null;
            IReadOnlyList<BuildingDefinition> buildingDefinitions = config != null
                ? config.BuildingDefinitions
                : null;

            int maxHealth = balance != null ? balance.PlayerMaxHealth : 3;
            int firstThreshold = balance != null ? balance.FirstUpgradeThreshold : 4;
            ResourceAmount startingResources = balance != null ? balance.StartingResources : new ResourceAmount(1, 4, 0);
            int playerMarkerCapacity = balance != null ? balance.PlayerMarkerCapacity : 0;
            int robotMaxTargetDistance = balance != null ? balance.RobotMaxTargetDistance : RobotAutomationService.DefaultMaxTargetDistance;
            float robotActionInterval = balance != null ? balance.RobotActionInterval : 0.35f;
            ResourceAmount robotCost = balance != null ? balance.RobotCost : new ResourceAmount(4, 0, 0);
            ResourceAmount robotRecycleDrop = waveConfig != null ? waveConfig.RobotRecycleDrop : ResourceAmount.Zero;
            bool robotUsesPlayerDrillTier = balance == null || balance.RobotUsesPlayerDrillTier;
            HardnessTier robotFixedDrillTier = balance != null ? balance.RobotFixedDrillTier : HardnessTier.Soil;

            var container = new MinebotContainer();
            container.RegisterInstance(config);
            container.RegisterInstance(balance);
            container.RegisterInstance(hazardRules);
            container.RegisterInstance(miningRules);
            container.RegisterInstance(waveConfig);
            container.RegisterInstance(upgradePool);
            container.RegisterInstance(scoreConfig);
            container.RegisterInstance(buildingDefinitions);
            container.RegisterInstance(grid);

            var robots = new List<RobotState>();
            container.RegisterInstance(robots);
            container.RegisterInstance<IList<RobotState>>(robots);
            container.RegisterInstance<IReadOnlyList<RobotState>>(robots);

            container.RegisterSingleton(c => new PlayerEconomy(startingResources));
            container.RegisterSingleton(c => new PlayerVitals(maxHealth));
            container.RegisterSingleton(c => new ExperienceService(firstThreshold));
            container.RegisterSingleton<WorldPickupService>();
            container.RegisterSingleton<ScoreService>();
            container.RegisterSingleton(c => new WaveSurvivalService(c.Resolve<LogicalGridState>(), c.Resolve<WaveConfig>()));
            container.RegisterSingleton(c => new PlayerMiningState(
                c.Resolve<LogicalGridState>().PlayerSpawn,
                HardnessTier.Soil,
                playerMarkerCapacity));
            container.RegisterSingleton<MiningService>();
            container.RegisterSingleton<HazardService>();
            container.RegisterSingleton(c => new RobotAutomationService(
                c.Resolve<LogicalGridState>(),
                c.Resolve<MiningRules>(),
                robotMaxTargetDistance,
                robotActionInterval));
            container.RegisterSingleton(c => new GameSessionService(
                c.Resolve<LogicalGridState>(),
                c.Resolve<PlayerMiningState>(),
                c.Resolve<MiningService>(),
                c.Resolve<HazardService>(),
                c.Resolve<HazardRules>(),
                c.Resolve<PlayerEconomy>(),
                c.Resolve<ExperienceService>(),
                c.Resolve<WorldPickupService>(),
                c.Resolve<PlayerVitals>(),
                c.Resolve<RobotAutomationService>(),
                c.Resolve<IList<RobotState>>(),
                c.Resolve<WaveSurvivalService>(),
                robotRecycleDrop,
                robotUsesPlayerDrillTier,
                robotFixedDrillTier,
                c.Resolve<ScoreService>()));
            container.RegisterSingleton<UpgradeSelectionService>();
            container.RegisterSingleton(c => new RobotFactoryService(c.Resolve<PlayerEconomy>(), robotCost, c.Resolve<List<RobotState>>()));
            container.RegisterSingleton<BaseOpsService>();
            container.RegisterSingleton<BuildingPlacementService>();
            container.RegisterSingleton<RuntimeServiceRegistry>();

            RuntimeServiceRegistry services = container.Resolve<RuntimeServiceRegistry>();
            HazardService hazards = container.Resolve<HazardService>();

            Debug.Log(
                $"[RuntimeServiceFactory] 配置检查 - HazardRules: {(hazardRules != null ? hazardRules.name : "null")}, " +
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
                Debug.Log($"[RuntimeServiceFactory] 正在生成炸弹 - Seed: {seed}, Chance: {chance:F4}, SafeRadius: {safeRadius}");
                hazards.SeedBombs(seed, chance, services.Grid.PlayerSpawn, safeRadius);
            }

            return container;
        }

        public static RuntimeServiceRegistry Create(BootstrapConfig config)
        {
            return CreateContainer(config).Resolve<RuntimeServiceRegistry>();
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
