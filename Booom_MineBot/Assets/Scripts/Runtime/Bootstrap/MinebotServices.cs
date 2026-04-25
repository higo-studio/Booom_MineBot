using Minebot.Automation;
using System.Collections.Generic;
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

            LogicalGridState grid = config != null && config.DefaultMap != null
                ? config.DefaultMap.CreateGridState()
                : MapGenerator.Generate(new MapGenerationSettings(new Vector2Int(12, 12), new Minebot.Common.GridPosition(6, 6), 1));

            GameBalanceConfig balance = config != null ? config.BalanceConfig : null;
            int maxHealth = balance != null ? balance.PlayerMaxHealth : 3;
            int firstThreshold = balance != null ? balance.FirstUpgradeThreshold : 5;
            var economy = new PlayerEconomy(balance != null ? balance.StartingResources : new Minebot.Common.ResourceAmount(0, 3, 0));
            var vitals = new PlayerVitals(maxHealth);
            var experience = new ExperienceService(firstThreshold);
            var robots = new List<RobotState>();

            var miningState = new PlayerMiningState(grid.PlayerSpawn, HardnessTier.Soil);
            var mining = new MiningService(grid);
            var hazards = new HazardService(grid);
            var session = new GameSessionService(
                miningState,
                mining,
                hazards,
                config != null ? config.HazardRules : null,
                economy,
                experience,
                vitals);
            var upgrades = new UpgradeSelectionService(
                experience,
                miningState,
                vitals,
                config != null ? config.UpgradePool : null);
            var factory = new RobotFactoryService(
                economy,
                balance != null ? balance.RobotCost : new Minebot.Common.ResourceAmount(5, 0, 0),
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
                new RobotAutomationService(grid),
                factory,
                robots,
                new WaveSurvivalService(grid, config != null ? config.WaveConfig : null));

            return Current;
        }

        public static void ResetForTests()
        {
            Current = null;
        }
    }
}
