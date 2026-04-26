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

        public static RuntimeServiceRegistry Initialize(BootstrapConfig config)
        {
            if (Current != null)
            {
                return Current;
            }

            bool usingGeneratedMap = config == null || config.DefaultMap == null;
            LogicalGridState grid = config != null && config.DefaultMap != null
                ? config.DefaultMap.CreateGridState()
                : MapGenerator.Generate(new MapGenerationSettings(new Vector2Int(12, 12), new Minebot.Common.GridPosition(6, 6), 1));

            GameBalanceConfig balance = config != null ? config.BalanceConfig : null;
            int maxHealth = balance != null ? balance.PlayerMaxHealth : 3;
            int firstThreshold = balance != null ? balance.FirstUpgradeThreshold : 4;
            var economy = new PlayerEconomy(balance != null ? balance.StartingResources : new Minebot.Common.ResourceAmount(1, 4, 0));
            var vitals = new PlayerVitals(maxHealth);
            var experience = new ExperienceService(firstThreshold);
            var robots = new List<RobotState>();
            WaveConfig waveConfig = config != null ? config.WaveConfig : null;

            var miningState = new PlayerMiningState(grid.PlayerSpawn, HardnessTier.Soil);
            var mining = new MiningService(grid);
            var hazards = new HazardService(grid);
            HazardRules hazardRules = config != null ? config.HazardRules : null;
            if (usingGeneratedMap)
            {
                hazards.SeedBombs(
                    hazardRules != null ? hazardRules.BombSeed : HazardRules.DefaultBombSeed,
                    hazardRules != null ? hazardRules.BombSpawnChance : HazardRules.DefaultBombSpawnChance,
                    grid.PlayerSpawn,
                    hazardRules != null ? hazardRules.BombSafeRadius : HazardRules.DefaultBombSafeRadius);
            }

            var robotAutomation = new RobotAutomationService(
                grid,
                balance != null ? balance.RobotMaxTargetDistance : RobotAutomationService.DefaultMaxTargetDistance,
                balance != null ? balance.RobotActionInterval : 0.35f);
            var session = new GameSessionService(
                miningState,
                mining,
                hazards,
                hazardRules,
                economy,
                experience,
                vitals,
                robotAutomation,
                robots,
                waveConfig != null ? waveConfig.RobotRecycleDrop : ResourceAmount.Zero,
                balance == null || balance.RobotUsesPlayerDrillTier,
                balance != null ? balance.RobotFixedDrillTier : HardnessTier.Soil);
            var upgrades = new UpgradeSelectionService(
                experience,
                miningState,
                vitals,
                config != null ? config.UpgradePool : null);
            var factory = new RobotFactoryService(
                economy,
                balance != null ? balance.RobotCost : new Minebot.Common.ResourceAmount(4, 0, 0),
                robots);

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
                new BaseOpsService(economy, vitals),
                robotAutomation,
                factory,
                robots,
                new WaveSurvivalService(grid, waveConfig));

            return Current;
        }

        public static void ResetForTests()
        {
            Current = null;
        }
    }
}
