using UnityEngine;

namespace Minebot.GridMining
{
    /// <summary>
    /// 资源掉落配置结构，用于在程序集中传递奖励范围配置
    /// </summary>
    [System.Serializable]
    public readonly struct RewardConfig
    {
        // 土层掉落范围 (金属/能量/经验)
        public readonly Vector2Int soilMetal;
        public readonly Vector2Int soilEnergy;
        public readonly Vector2Int soilExperience;

        // 石层掉落范围
        public readonly Vector2Int stoneMetal;
        public readonly Vector2Int stoneEnergy;
        public readonly Vector2Int stoneExperience;

        // 硬岩掉落范围
        public readonly Vector2Int hardRockMetal;
        public readonly Vector2Int hardRockEnergy;
        public readonly Vector2Int hardRockExperience;

        // 超硬岩掉落范围
        public readonly Vector2Int ultraHardMetal;
        public readonly Vector2Int ultraHardEnergy;
        public readonly Vector2Int ultraHardExperience;

        public static RewardConfig Default => new RewardConfig(
            new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1),   // 土层
            new Vector2Int(2, 2), new Vector2Int(0, 1), new Vector2Int(2, 2),   // 石层
            new Vector2Int(3, 3), new Vector2Int(1, 2), new Vector2Int(3, 3),   // 硬岩
            new Vector2Int(4, 4), new Vector2Int(1, 2), new Vector2Int(4, 4));  // 超硬岩

        public RewardConfig(
            Vector2Int soilMetal, Vector2Int soilEnergy, Vector2Int soilExperience,
            Vector2Int stoneMetal, Vector2Int stoneEnergy, Vector2Int stoneExperience,
            Vector2Int hardRockMetal, Vector2Int hardRockEnergy, Vector2Int hardRockExperience,
            Vector2Int ultraHardMetal, Vector2Int ultraHardEnergy, Vector2Int ultraHardExperience)
        {
            this.soilMetal = soilMetal;
            this.soilEnergy = soilEnergy;
            this.soilExperience = soilExperience;
            this.stoneMetal = stoneMetal;
            this.stoneEnergy = stoneEnergy;
            this.stoneExperience = stoneExperience;
            this.hardRockMetal = hardRockMetal;
            this.hardRockEnergy = hardRockEnergy;
            this.hardRockExperience = hardRockExperience;
            this.ultraHardMetal = ultraHardMetal;
            this.ultraHardEnergy = ultraHardEnergy;
            this.ultraHardExperience = ultraHardExperience;
        }

        public Vector2Int GetMetalRange(HardnessTier tier) => tier switch
        {
            HardnessTier.Soil => soilMetal,
            HardnessTier.Stone => stoneMetal,
            HardnessTier.HardRock => hardRockMetal,
            HardnessTier.UltraHard => ultraHardMetal,
            _ => soilMetal
        };

        public Vector2Int GetEnergyRange(HardnessTier tier) => tier switch
        {
            HardnessTier.Soil => soilEnergy,
            HardnessTier.Stone => stoneEnergy,
            HardnessTier.HardRock => hardRockEnergy,
            HardnessTier.UltraHard => ultraHardEnergy,
            _ => soilEnergy
        };

        public Vector2Int GetExperienceRange(HardnessTier tier) => tier switch
        {
            HardnessTier.Soil => soilExperience,
            HardnessTier.Stone => stoneExperience,
            HardnessTier.HardRock => hardRockExperience,
            HardnessTier.UltraHard => ultraHardExperience,
            _ => soilExperience
        };
    }
}