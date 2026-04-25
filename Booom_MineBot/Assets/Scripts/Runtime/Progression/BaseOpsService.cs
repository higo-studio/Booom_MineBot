using Minebot.Common;

namespace Minebot.Progression
{
    public sealed class BaseOpsService
    {
        private readonly PlayerEconomy economy;
        private readonly PlayerVitals vitals;

        public BaseOpsService(PlayerEconomy economy, PlayerVitals vitals)
        {
            this.economy = economy;
            this.vitals = vitals;
        }

        public bool TryRepair(ResourceAmount repairCost)
        {
            if (!economy.TrySpend(repairCost))
            {
                return false;
            }

            vitals.RepairToFull();
            return true;
        }

        public bool TryBuildRobot(ResourceAmount robotCost)
        {
            return economy.TrySpend(robotCost);
        }
    }
}
