using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Progression
{
    [CreateAssetMenu(menuName = "Minebot/成长/数值配置")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        [SerializeField]
        [InspectorLabel("玩家最大生命")]
        private int playerMaxHealth = 3;

        [SerializeField]
        [InspectorLabel("首次升级阈值")]
        private int firstUpgradeThreshold = 4;

        [SerializeField]
        [InspectorLabel("初始资源")]
        private ResourceAmount startingResources = new ResourceAmount(1, 4, 0);

        [SerializeField]
        [InspectorLabel("维修消耗")]
        private ResourceAmount repairCost = new ResourceAmount(2, 0, 0);

        [SerializeField]
        [InspectorLabel("机器人造价")]
        private ResourceAmount robotCost = new ResourceAmount(4, 0, 0);

        [SerializeField]
        [InspectorLabel("机器人最大目标距离")]
        private int robotMaxTargetDistance = 7;

        [SerializeField]
        [InspectorLabel("机器人行动间隔")]
        private float robotActionInterval = 0.35f;

        [SerializeField]
        [InspectorLabel("机器人沿用玩家钻头等级")]
        private bool robotUsesPlayerDrillTier = true;

        [SerializeField]
        [InspectorLabel("机器人固定钻头等级")]
        private HardnessTier robotFixedDrillTier = HardnessTier.Soil;

        public int PlayerMaxHealth => Mathf.Max(1, playerMaxHealth);
        public int FirstUpgradeThreshold => Mathf.Max(1, firstUpgradeThreshold);
        public ResourceAmount StartingResources => startingResources;
        public ResourceAmount RepairCost => repairCost;
        public ResourceAmount RobotCost => robotCost;
        public int RobotMaxTargetDistance => Mathf.Max(1, robotMaxTargetDistance);
        public float RobotActionInterval => Mathf.Max(0f, robotActionInterval);
        public bool RobotUsesPlayerDrillTier => robotUsesPlayerDrillTier;
        public HardnessTier RobotFixedDrillTier => robotFixedDrillTier;
    }
}
