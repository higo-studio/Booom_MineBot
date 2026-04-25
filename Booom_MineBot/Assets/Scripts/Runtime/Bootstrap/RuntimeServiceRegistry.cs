using System.Collections.Generic;
using Minebot.Automation;
using Minebot.GridMining;
using Minebot.HazardInference;
using Minebot.Progression;
using Minebot.WaveSurvival;

namespace Minebot.Bootstrap
{
    public sealed class RuntimeServiceRegistry
    {
        public RuntimeServiceRegistry(
            LogicalGridState grid,
            PlayerMiningState playerMiningState,
            MiningService mining,
            HazardService hazards,
            GameSessionService session,
            UpgradeSelectionService upgrades,
            PlayerEconomy economy,
            PlayerVitals vitals,
            ExperienceService experience,
            BaseOpsService baseOps,
            RobotAutomationService robotAutomation,
            RobotFactoryService robotFactory,
            IReadOnlyList<RobotState> robots,
            WaveSurvivalService waves)
        {
            Grid = grid;
            PlayerMiningState = playerMiningState;
            Mining = mining;
            Hazards = hazards;
            Session = session;
            Upgrades = upgrades;
            Economy = economy;
            Vitals = vitals;
            Experience = experience;
            BaseOps = baseOps;
            RobotAutomation = robotAutomation;
            RobotFactory = robotFactory;
            Robots = robots;
            Waves = waves;
        }

        public LogicalGridState Grid { get; }
        public PlayerMiningState PlayerMiningState { get; }
        public MiningService Mining { get; }
        public HazardService Hazards { get; }
        public GameSessionService Session { get; }
        public UpgradeSelectionService Upgrades { get; }
        public PlayerEconomy Economy { get; }
        public PlayerVitals Vitals { get; }
        public ExperienceService Experience { get; }
        public BaseOpsService BaseOps { get; }
        public RobotAutomationService RobotAutomation { get; }
        public RobotFactoryService RobotFactory { get; }
        public IReadOnlyList<RobotState> Robots { get; }
        public WaveSurvivalService Waves { get; }
    }
}
