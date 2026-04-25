using UnityEngine;

namespace Minebot.HazardInference
{
    [CreateAssetMenu(menuName = "Minebot/Hazard Inference/Hazard Rules")]
    public sealed class HazardRules : ScriptableObject
    {
        [SerializeField]
        private int scanEnergyCost = 1;

        [SerializeField]
        private int directBombDamage = 1;

        [SerializeField]
        private int explosionRadius = 1;

        [SerializeField]
        private bool scanUsesEightWayNeighbors = true;

        public int ScanEnergyCost => Mathf.Max(0, scanEnergyCost);
        public int DirectBombDamage => Mathf.Max(0, directBombDamage);
        public int ExplosionRadius => Mathf.Max(0, explosionRadius);
        public bool ScanUsesEightWayNeighbors => scanUsesEightWayNeighbors;
    }
}
