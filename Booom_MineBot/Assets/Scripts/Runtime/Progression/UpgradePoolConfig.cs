using System;
using UnityEngine;

namespace Minebot.Progression
{
    [Serializable]
    public sealed class UpgradeDefinition
    {
        public string id = "upgrade";
        public string displayName = "升级";
        public int drillTierDelta;
        public int maxHealthDelta;
        public int weight = 1;
    }

    [CreateAssetMenu(menuName = "Minebot/Progression/Upgrade Pool")]
    public sealed class UpgradePoolConfig : ScriptableObject
    {
        [SerializeField]
        private UpgradeDefinition[] upgrades = Array.Empty<UpgradeDefinition>();

        public UpgradeDefinition[] Upgrades => upgrades;
    }
}
