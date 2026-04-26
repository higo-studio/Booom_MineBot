using UnityEngine;

namespace Minebot.HazardInference
{
    [CreateAssetMenu(menuName = "Minebot/Hazard Inference/Hazard Rules")]
    public sealed class HazardRules : ScriptableObject
    {
        public const int DefaultBombSeed = 20260425;
        public const float DefaultBombSpawnChance = 0.09f;
        public const int DefaultBombSafeRadius = 2;
        public const int DefaultScanEnergyCost = 1;
        public const int DefaultScanFrontierRange = 1;
        public const int DefaultDirectBombDamage = 1;
        public const int DefaultExplosionRadius = 1;

        [SerializeField]
        [Range(0f, 0.35f)]
        private float bombSpawnChance = DefaultBombSpawnChance;

        [SerializeField]
        private int bombSeed = DefaultBombSeed;

        [SerializeField]
        private int bombSafeRadius = DefaultBombSafeRadius;

        [SerializeField]
        private int scanEnergyCost = DefaultScanEnergyCost;

        [SerializeField]
        private int scanFrontierRange = DefaultScanFrontierRange;

        [SerializeField]
        private int directBombDamage = DefaultDirectBombDamage;

        [SerializeField]
        private int explosionRadius = DefaultExplosionRadius;

        [SerializeField]
        private bool scanUsesEightWayNeighbors = true;

        public float BombSpawnChance => Mathf.Clamp01(bombSpawnChance);
        public int BombSeed => bombSeed;
        public int BombSafeRadius => Mathf.Max(0, bombSafeRadius);
        public int ScanEnergyCost => Mathf.Max(0, scanEnergyCost);
        public int ScanFrontierRange => Mathf.Max(0, scanFrontierRange);
        public int DirectBombDamage => Mathf.Max(0, directBombDamage);
        public int ExplosionRadius => Mathf.Max(0, explosionRadius);
        public bool ScanUsesEightWayNeighbors => scanUsesEightWayNeighbors;
    }
}
