using Minebot.Common;
using UnityEngine;

namespace Minebot.WaveSurvival
{
    [CreateAssetMenu(menuName = "Minebot/Wave Survival/Wave Config")]
    public sealed class WaveConfig : ScriptableObject
    {
        [SerializeField]
        private float firstWaveDelay = 30f;

        [SerializeField]
        private int baseDangerRadius = 1;

        [SerializeField]
        private int radiusGrowthEveryWaves = 2;

        [SerializeField]
        private ResourceAmount robotRecycleDrop = new ResourceAmount(2, 0, 0);

        public float FirstWaveDelay => Mathf.Max(1f, firstWaveDelay);
        public int BaseDangerRadius => Mathf.Max(0, baseDangerRadius);
        public int RadiusGrowthEveryWaves => Mathf.Max(1, radiusGrowthEveryWaves);
        public ResourceAmount RobotRecycleDrop => robotRecycleDrop;

        public int DangerRadiusForWave(int wave)
        {
            return BaseDangerRadius + Mathf.Max(0, wave - 1) / RadiusGrowthEveryWaves;
        }
    }
}
