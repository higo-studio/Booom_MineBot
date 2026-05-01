using System;
using UnityEngine;

namespace Minebot.Progression
{
    [Serializable]
    public sealed class UpgradeDefinition
    {
        [InspectorLabel("内部标识")]
        public string id = "upgrade";

        [InspectorLabel("显示名称")]
        public string displayName = "升级";

        [InspectorLabel("钻头等级增量")]
        public int drillTierDelta;

        [InspectorLabel("最大生命增量")]
        public int maxHealthDelta;

        [InspectorLabel("权重")]
        public int weight = 1;
    }

    [CreateAssetMenu(menuName = "Minebot/成长/升级池")]
    public sealed class UpgradePoolConfig : ScriptableObject
    {
        [SerializeField]
        [InspectorLabel("升级列表")]
        private UpgradeDefinition[] upgrades = Array.Empty<UpgradeDefinition>();

        public UpgradeDefinition[] Upgrades => upgrades;
    }
}
