using Minebot.Common;
using UnityEngine;

namespace Minebot.GridMining
{
    [System.Serializable]
    public sealed class GeneratedMapConfig
    {
        public static readonly Vector2Int DefaultBaseSize = new Vector2Int(12, 12);
        public static readonly Vector2 DefaultNoiseScale = new Vector2(0.045f, 0.045f);
        public static readonly Vector2 DefaultNoiseOffset = new Vector2(17.31f, 91.73f);
        public const int DefaultSizeMultiplier = 20;
        public const int DefaultSafeRadius = 1;
        public const float DefaultRadialWeight = 0.75f;
        public const float DefaultNoiseWeight = 0.25f;
        public const float DefaultRadialExponent = 1.15f;
        public const float DefaultForcedUltraHardDistanceNormalized = 0.82f;
        public const float DefaultStoneThreshold = 0.24f;
        public const float DefaultHardRockThreshold = 0.49f;
        public const float DefaultUltraHardThreshold = 0.71f;

        [SerializeField]
        [InspectorLabel("基础尺寸")]
        private Vector2Int baseSize = DefaultBaseSize;

        [SerializeField]
        [InspectorLabel("尺寸倍率")]
        private int sizeMultiplier = DefaultSizeMultiplier;

        [SerializeField]
        [InspectorLabel("出生安全半径")]
        private int safeRadius = DefaultSafeRadius;

        [SerializeField]
        [Range(0f, 1f)]
        [InspectorLabel("径向权重")]
        private float radialWeight = DefaultRadialWeight;

        [SerializeField]
        [Range(0f, 1f)]
        [InspectorLabel("噪声权重")]
        private float noiseWeight = DefaultNoiseWeight;

        [SerializeField]
        [InspectorLabel("噪声缩放")]
        private Vector2 noiseScale = DefaultNoiseScale;

        [SerializeField]
        [InspectorLabel("噪声偏移")]
        private Vector2 noiseOffset = DefaultNoiseOffset;

        [SerializeField]
        [InspectorLabel("径向指数")]
        private float radialExponent = DefaultRadialExponent;

        [SerializeField]
        [Range(0f, 1f)]
        [InspectorLabel("强制超硬岩距离阈值")]
        private float forcedUltraHardDistanceNormalized = DefaultForcedUltraHardDistanceNormalized;

        [SerializeField]
        [Range(0f, 1f)]
        [InspectorLabel("石层阈值")]
        private float stoneThreshold = DefaultStoneThreshold;

        [SerializeField]
        [Range(0f, 1f)]
        [InspectorLabel("硬岩阈值")]
        private float hardRockThreshold = DefaultHardRockThreshold;

        [SerializeField]
        [Range(0f, 1f)]
        [InspectorLabel("超硬岩阈值")]
        private float ultraHardThreshold = DefaultUltraHardThreshold;

        public Vector2Int BaseSize => new Vector2Int(Mathf.Max(5, baseSize.x), Mathf.Max(5, baseSize.y));
        public int SizeMultiplier => Mathf.Max(1, sizeMultiplier);
        public int SafeRadius => Mathf.Max(0, safeRadius);
        public float RadialWeight => Mathf.Max(0f, radialWeight);
        public float NoiseWeight => Mathf.Max(0f, noiseWeight);
        public Vector2 NoiseScale => new Vector2(Mathf.Max(0.0001f, Mathf.Abs(noiseScale.x)), Mathf.Max(0.0001f, Mathf.Abs(noiseScale.y)));
        public Vector2 NoiseOffset => noiseOffset;
        public float RadialExponent => Mathf.Max(0.1f, radialExponent);
        public float ForcedUltraHardDistanceNormalized => Mathf.Clamp01(forcedUltraHardDistanceNormalized);
        public float StoneThreshold => Mathf.Clamp01(stoneThreshold);
        public float HardRockThreshold => Mathf.Clamp(hardRockThreshold, StoneThreshold, 1f);
        public float UltraHardThreshold => Mathf.Clamp(ultraHardThreshold, HardRockThreshold, 1f);

        public Vector2Int ResolveSize()
        {
            Vector2Int resolvedBaseSize = BaseSize;
            int multiplier = SizeMultiplier;
            return new Vector2Int(resolvedBaseSize.x * multiplier, resolvedBaseSize.y * multiplier);
        }

        public MapGenerationSettings ToSettings()
        {
            Vector2Int resolvedSize = ResolveSize();
            GridPosition spawn = new GridPosition(resolvedSize.x / 2, resolvedSize.y / 2);
            return new MapGenerationSettings(
                resolvedSize,
                spawn,
                SafeRadius,
                RadialWeight,
                NoiseWeight,
                NoiseScale,
                NoiseOffset,
                RadialExponent,
                ForcedUltraHardDistanceNormalized,
                StoneThreshold,
                HardRockThreshold,
                UltraHardThreshold);
        }

        public static MapGenerationSettings CreateDefaultSettings()
        {
            return new GeneratedMapConfig().ToSettings();
        }
    }
}
