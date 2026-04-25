using System.Collections.Generic;
using Minebot.Common;
using Minebot.Progression;

namespace Minebot.Automation
{
    public sealed class RobotFactoryService
    {
        private readonly PlayerEconomy economy;
        private readonly ResourceAmount robotCost;
        private readonly List<RobotState> robots;

        public RobotFactoryService(PlayerEconomy economy, ResourceAmount robotCost, List<RobotState> robots)
        {
            this.economy = economy;
            this.robotCost = robotCost;
            this.robots = robots;
        }

        public IReadOnlyList<RobotState> Robots => robots;

        public bool TryProduce(GridPosition spawnPosition, out RobotState robot)
        {
            robot = null;
            if (!economy.TrySpend(robotCost))
            {
                return false;
            }

            robot = new RobotState(spawnPosition);
            robots.Add(robot);
            return true;
        }
    }
}
