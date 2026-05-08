using System;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Progression
{
    [Serializable]
    public sealed class BuildingScoreEntry
    {
        [InspectorLabel("建筑标识")]
        public string buildingId = "building";

        [InspectorLabel("建造得分")]
        public int score = 25;
    }

    [CreateAssetMenu(menuName = "Minebot/成长/计分配置")]
    public sealed class ScoreConfig : ScriptableObject
    {
        public const int DefaultSoilWallScore = 5;
        public const int DefaultStoneWallScore = 10;
        public const int DefaultHardRockWallScore = 15;
        public const int DefaultUltraHardWallScore = 25;
        public const int DefaultEarthquakeBombScore = 20;
        public const int DefaultWaveSurvivedScore = 50;
        public const int DefaultBuildingScore = 30;

        [SerializeField]
        [InspectorLabel("土层破坏得分")]
        private int soilWallScore = DefaultSoilWallScore;

        [SerializeField]
        [InspectorLabel("石层破坏得分")]
        private int stoneWallScore = DefaultStoneWallScore;

        [SerializeField]
        [InspectorLabel("硬岩破坏得分")]
        private int hardRockWallScore = DefaultHardRockWallScore;

        [SerializeField]
        [InspectorLabel("超硬岩破坏得分")]
        private int ultraHardWallScore = DefaultUltraHardWallScore;

        [SerializeField]
        [InspectorLabel("地震炸毁炸弹得分")]
        private int earthquakeBombScore = DefaultEarthquakeBombScore;

        [SerializeField]
        [InspectorLabel("成功过波得分")]
        private int waveSurvivedScore = DefaultWaveSurvivedScore;

        [SerializeField]
        [InspectorLabel("默认建筑得分")]
        private int defaultBuildingScore = DefaultBuildingScore;

        [SerializeField]
        [InspectorLabel("建筑得分覆盖表")]
        private BuildingScoreEntry[] buildingScores = Array.Empty<BuildingScoreEntry>();

        public int EarthquakeBombScore => Mathf.Max(0, earthquakeBombScore);
        public int WaveSurvivedScore => Mathf.Max(0, waveSurvivedScore);
        public int DefaultBuildingScoreValue => Mathf.Max(0, defaultBuildingScore);

        public int ScoreForWall(HardnessTier hardnessTier)
        {
            return hardnessTier switch
            {
                HardnessTier.Soil => Mathf.Max(0, soilWallScore),
                HardnessTier.Stone => Mathf.Max(0, stoneWallScore),
                HardnessTier.HardRock => Mathf.Max(0, hardRockWallScore),
                HardnessTier.UltraHard => Mathf.Max(0, ultraHardWallScore),
                _ => Mathf.Max(0, soilWallScore)
            };
        }

        public int ScoreForBuilding(string buildingId)
        {
            if (!string.IsNullOrWhiteSpace(buildingId))
            {
                for (int i = 0; i < buildingScores.Length; i++)
                {
                    BuildingScoreEntry entry = buildingScores[i];
                    if (entry != null && string.Equals(entry.buildingId, buildingId, StringComparison.OrdinalIgnoreCase))
                    {
                        return Mathf.Max(0, entry.score);
                    }
                }
            }

            return DefaultBuildingScoreValue;
        }
    }
}
