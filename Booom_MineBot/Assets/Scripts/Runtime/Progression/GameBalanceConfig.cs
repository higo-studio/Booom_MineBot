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
        [InspectorLabel("升级阈值增量")]
        private int upgradeThresholdIncrease = 3;

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

        [Header("资源掉落范围配置")]
        [Tooltip("每种墙壁类型对应三种资源（金属/能量/经验）的掉落范围 (min, max)")]
        
        [Header("土层")]
        [SerializeField]
        [InspectorLabel("金属")]
        private Vector2Int soilMetalRange = new Vector2Int(1, 1);
        [SerializeField]
        [InspectorLabel("能量")]
        private Vector2Int soilEnergyRange = new Vector2Int(0, 1);
        [SerializeField]
        [InspectorLabel("经验")]
        private Vector2Int soilExperienceRange = new Vector2Int(1, 1);

        [Header("石层")]
        [SerializeField]
        [InspectorLabel("金属")]
        private Vector2Int stoneMetalRange = new Vector2Int(2, 2);
        [SerializeField]
        [InspectorLabel("能量")]
        private Vector2Int stoneEnergyRange = new Vector2Int(0, 1);
        [SerializeField]
        [InspectorLabel("经验")]
        private Vector2Int stoneExperienceRange = new Vector2Int(2, 2);

        [Header("硬岩")]
        [SerializeField]
        [InspectorLabel("金属")]
        private Vector2Int hardRockMetalRange = new Vector2Int(3, 3);
        [SerializeField]
        [InspectorLabel("能量")]
        private Vector2Int hardRockEnergyRange = new Vector2Int(1, 2);
        [SerializeField]
        [InspectorLabel("经验")]
        private Vector2Int hardRockExperienceRange = new Vector2Int(3, 3);

        [Header("超硬岩")]
        [SerializeField]
        [InspectorLabel("金属")]
        private Vector2Int ultraHardMetalRange = new Vector2Int(4, 4);
        [SerializeField]
        [InspectorLabel("能量")]
        private Vector2Int ultraHardEnergyRange = new Vector2Int(1, 2);
        [SerializeField]
        [InspectorLabel("经验")]
        private Vector2Int ultraHardExperienceRange = new Vector2Int(4, 4);

        public int PlayerMaxHealth => Mathf.Max(1, playerMaxHealth);
        public int FirstUpgradeThreshold => Mathf.Max(1, firstUpgradeThreshold);
        public int UpgradeThresholdIncrease => Mathf.Max(1, upgradeThresholdIncrease);
        public ResourceAmount StartingResources => startingResources;
        public ResourceAmount RepairCost => repairCost;
        public ResourceAmount RobotCost => robotCost;
        public int RobotMaxTargetDistance => Mathf.Max(1, robotMaxTargetDistance);
        public float RobotActionInterval => Mathf.Max(0f, robotActionInterval);
        public bool RobotUsesPlayerDrillTier => robotUsesPlayerDrillTier;
        public HardnessTier RobotFixedDrillTier => robotFixedDrillTier;

        public Vector2Int GetMetalRange(HardnessTier tier) => tier switch
        {
            HardnessTier.Soil => ClampRange(soilMetalRange),
            HardnessTier.Stone => ClampRange(stoneMetalRange),
            HardnessTier.HardRock => ClampRange(hardRockMetalRange),
            HardnessTier.UltraHard => ClampRange(ultraHardMetalRange),
            _ => ClampRange(soilMetalRange)
        };

        public Vector2Int GetEnergyRange(HardnessTier tier) => tier switch
        {
            HardnessTier.Soil => ClampRange(soilEnergyRange),
            HardnessTier.Stone => ClampRange(stoneEnergyRange),
            HardnessTier.HardRock => ClampRange(hardRockEnergyRange),
            HardnessTier.UltraHard => ClampRange(ultraHardEnergyRange),
            _ => ClampRange(soilEnergyRange)
        };

        public Vector2Int GetExperienceRange(HardnessTier tier) => tier switch
        {
            HardnessTier.Soil => ClampRange(soilExperienceRange),
            HardnessTier.Stone => ClampRange(stoneExperienceRange),
            HardnessTier.HardRock => ClampRange(hardRockExperienceRange),
            HardnessTier.UltraHard => ClampRange(ultraHardExperienceRange),
            _ => ClampRange(soilExperienceRange)
        };

        private static Vector2Int ClampRange(Vector2Int range)
        {
            int min = Mathf.Max(0, Mathf.Min(range.x, range.y));
            int max = Mathf.Max(0, Mathf.Max(range.x, range.y));
            return new Vector2Int(min, max);
        }
    }
}