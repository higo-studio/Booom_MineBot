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

        public float FirstWaveDelay => Mathf.Max(1f, firstWaveDelay);
        public int BaseDangerRadius => Mathf.Max(0, baseDangerRadius);
        public int RadiusGrowthEveryWaves => Mathf.Max(1, radiusGrowthEveryWaves);
        public ResourceAmount RobotRecycleDrop => robotRecycleDrop;
        public float PerimeterBombPhaseHoldSeconds => Mathf.Max(0f, perimeterBombPhaseHoldSeconds);
        public float DangerRefreshPhaseHoldSeconds => Mathf.Max(0f, dangerRefreshPhaseHoldSeconds);
        public float CollapsePhaseHoldSeconds => Mathf.Max(0f, collapsePhaseHoldSeconds);

        public int DangerRadiusForWave(int wave)
        {
            return BaseDangerRadius + Mathf.Max(0, wave - 1) / RadiusGrowthEveryWaves;
        }
    }
}
