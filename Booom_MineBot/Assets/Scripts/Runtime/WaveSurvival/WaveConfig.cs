using Minebot.Common;
using UnityEngine;

namespace Minebot.WaveSurvival
{
    [CreateAssetMenu(menuName = "Minebot/地震波/地震波配置")]
    public sealed class WaveConfig : ScriptableObject
    {
        public const float DefaultFirstWaveDelay = 30f;
        public const int DefaultBaseDangerRadius = 1;
        public const int DefaultRadiusGrowthEveryWaves = 2;
        public const float DefaultPerimeterBombPhaseHoldSeconds = 0.2f;
        public const float DefaultDangerRefreshPhaseHoldSeconds = 0.2f;
        public const float DefaultCollapsePhaseHoldSeconds = 0.2f;
        public const float DefaultCollapseBombMixRatio = 0.12f;
        public const int DefaultCollapseBombSeed = 20260508;

        [SerializeField]
        [InspectorLabel("首波延迟（秒）")]
        private float firstWaveDelay = DefaultFirstWaveDelay;

        [SerializeField]
        [InspectorLabel("基础危险区半径")]
        private int baseDangerRadius = DefaultBaseDangerRadius;

        [SerializeField]
        [InspectorLabel("每几波扩大半径")]
        private int radiusGrowthEveryWaves = DefaultRadiusGrowthEveryWaves;

        [SerializeField]
        [InspectorLabel("危险区厚度表（1~50 波）")]
        private int[] dangerRadiusByWave = CreateDefaultDangerRadiusByWave();

        [SerializeField]
        [InspectorLabel("机器人回收掉落")]
        private ResourceAmount robotRecycleDrop = new ResourceAmount(2, 0, 0);

        [SerializeField]
        [InspectorLabel("外围炸弹阶段停顿（秒）")]
        private float perimeterBombPhaseHoldSeconds = DefaultPerimeterBombPhaseHoldSeconds;

        [SerializeField]
        [InspectorLabel("危险区重算阶段停顿（秒）")]
        private float dangerRefreshPhaseHoldSeconds = DefaultDangerRefreshPhaseHoldSeconds;

        [SerializeField]
        [InspectorLabel("塌方回填阶段停顿（秒）")]
        private float collapsePhaseHoldSeconds = DefaultCollapsePhaseHoldSeconds;

        [SerializeField]
        [Range(0f, 1f)]
        [InspectorLabel("塌方混雷比例")]
        private float collapseBombMixRatio = DefaultCollapseBombMixRatio;

        [SerializeField]
        [InspectorLabel("塌方混雷随机种子")]
        private int collapseBombSeed = DefaultCollapseBombSeed;

        public float FirstWaveDelay => Mathf.Max(1f, firstWaveDelay);
        public int BaseDangerRadius => Mathf.Max(0, baseDangerRadius);
        public int RadiusGrowthEveryWaves => Mathf.Max(1, radiusGrowthEveryWaves);
        public int[] DangerRadiusByWave => dangerRadiusByWave != null && dangerRadiusByWave.Length > 0
            ? dangerRadiusByWave
            : CreateDefaultDangerRadiusByWave();
        public ResourceAmount RobotRecycleDrop => robotRecycleDrop;
        public float PerimeterBombPhaseHoldSeconds => Mathf.Max(0f, perimeterBombPhaseHoldSeconds);
        public float DangerRefreshPhaseHoldSeconds => Mathf.Max(0f, dangerRefreshPhaseHoldSeconds);
        public float CollapsePhaseHoldSeconds => Mathf.Max(0f, collapsePhaseHoldSeconds);
        public float CollapseBombMixRatio => Mathf.Clamp01(collapseBombMixRatio);
        public int CollapseBombSeed => collapseBombSeed;

        public int DangerRadiusForWave(int wave)
        {
            int[] table = DangerRadiusByWave;
            if (table.Length == 0)
            {
                return BaseDangerRadius + Mathf.Max(0, wave - 1) / RadiusGrowthEveryWaves;
            }

            int index = Mathf.Clamp(Mathf.Max(1, wave) - 1, 0, table.Length - 1);
            return Mathf.Max(0, table[index]);
        }

        private static int[] CreateDefaultDangerRadiusByWave()
        {
            var table = new int[50];
            for (int i = 0; i < table.Length; i++)
            {
                table[i] = DefaultBaseDangerRadius + i / DefaultRadiusGrowthEveryWaves;
            }

            return table;
        }
    }
}
