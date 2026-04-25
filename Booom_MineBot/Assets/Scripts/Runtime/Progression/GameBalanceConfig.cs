using Minebot.Common;
using UnityEngine;

namespace Minebot.Progression
{
    [CreateAssetMenu(menuName = "Minebot/Progression/Game Balance Config")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        [SerializeField]
        private int playerMaxHealth = 3;

        [SerializeField]
        private int firstUpgradeThreshold = 4;

        [SerializeField]
        private ResourceAmount startingResources = new ResourceAmount(1, 4, 0);

        [SerializeField]
        private ResourceAmount repairCost = new ResourceAmount(2, 0, 0);

        [SerializeField]
        private ResourceAmount robotCost = new ResourceAmount(4, 0, 0);

        public int PlayerMaxHealth => Mathf.Max(1, playerMaxHealth);
        public int FirstUpgradeThreshold => Mathf.Max(1, firstUpgradeThreshold);
        public ResourceAmount StartingResources => startingResources;
        public ResourceAmount RepairCost => repairCost;
        public ResourceAmount RobotCost => robotCost;
    }
}
