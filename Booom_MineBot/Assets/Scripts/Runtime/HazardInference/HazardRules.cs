using UnityEngine;

namespace Minebot.HazardInference
{
    [CreateAssetMenu(menuName = "Minebot/风险判断/炸药规则")]
    public sealed class HazardRules : ScriptableObject
    {
        public const int DefaultBombSeed = 20260425;
        public const float DefaultBombSpawnChance = 0.09f;
        public const int DefaultBombSafeRadius = 2;
        public const int DefaultScanEnergyCost = 1;
        public const int DefaultScanFrontierRange = 1;
        public const bool DefaultScanUsesEightWayNeighbors = true;
        public const float DefaultPassiveHazardSenseIntervalSeconds = 1f;
        public const int DefaultDirectBombDamage = 1;
        public const int DefaultExplosionRadius = 1;

        [SerializeField]
        [Range(0f, 0.35f)]
        [InspectorLabel("炸药生成概率")]
        private float bombSpawnChance = DefaultBombSpawnChance;

        [SerializeField]
        [InspectorLabel("炸药随机种子")]
        private int bombSeed = DefaultBombSeed;

        [SerializeField]
        [InspectorLabel("出生安全半径")]
        private int bombSafeRadius = DefaultBombSafeRadius;

        [SerializeField]
        [InspectorLabel("探测能量消耗（兼容保留，当前不生效）")]
        private int scanEnergyCost = DefaultScanEnergyCost;

        [SerializeField]
        [InspectorLabel("探测前沿范围")]
        private int scanFrontierRange = DefaultScanFrontierRange;

        [SerializeField]
        [InspectorLabel("被动风险感知间隔（秒）")]
        private float passiveHazardSenseIntervalSeconds = DefaultPassiveHazardSenseIntervalSeconds;

        [SerializeField]
        [InspectorLabel("炸药直接伤害")]
        private int directBombDamage = DefaultDirectBombDamage;

        [SerializeField]
        [InspectorLabel("爆炸半径（兼容保留，当前不生效）")]
        private int explosionRadius = DefaultExplosionRadius;

        [SerializeField]
        [InspectorLabel("探测使用八邻域")]
        private bool scanUsesEightWayNeighbors = DefaultScanUsesEightWayNeighbors;

        public float BombSpawnChance => Mathf.Clamp01(bombSpawnChance);
        public int BombSeed => bombSeed;
        public int BombSafeRadius => Mathf.Max(0, bombSafeRadius);
        public int ScanEnergyCost => Mathf.Max(0, scanEnergyCost);
        public int ScanFrontierRange => Mathf.Max(0, scanFrontierRange);
        public float PassiveHazardSenseIntervalSeconds => Mathf.Max(0.1f, passiveHazardSenseIntervalSeconds);
        public int DirectBombDamage => Mathf.Max(0, directBombDamage);
        public int ExplosionRadius => Mathf.Max(0, explosionRadius);
        public bool ScanUsesEightWayNeighbors => scanUsesEightWayNeighbors;
    }
}
