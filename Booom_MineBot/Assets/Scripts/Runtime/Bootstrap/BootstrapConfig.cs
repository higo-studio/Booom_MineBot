using Minebot.GridMining;
using Minebot.HazardInference;
using Minebot.Progression;
using Minebot.WaveSurvival;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Minebot.Bootstrap
{
    [CreateAssetMenu(menuName = "Minebot/Bootstrap/Bootstrap Config")]
    public sealed class BootstrapConfig : ScriptableObject
    {
        [SerializeField]
        private string gameplaySceneName = "Gameplay";

        [SerializeField]
        private InputActionAsset inputActions;

        [SerializeField]
        private MapDefinition defaultMap;

        [SerializeField]
        private GameBalanceConfig balanceConfig;

        [SerializeField]
        private UpgradePoolConfig upgradePool;

        [SerializeField]
        private HazardRules hazardRules;

        [SerializeField]
        private WaveConfig waveConfig;

        [SerializeField]
        private BuildingDefinition[] buildingDefinitions;

        public string GameplaySceneName => string.IsNullOrWhiteSpace(gameplaySceneName) ? "Gameplay" : gameplaySceneName;
        public InputActionAsset InputActions => inputActions;
        public MapDefinition DefaultMap => defaultMap;
        public GameBalanceConfig BalanceConfig => balanceConfig;
        public UpgradePoolConfig UpgradePool => upgradePool;
        public HazardRules HazardRules => hazardRules;
        public WaveConfig WaveConfig => waveConfig;
        public IReadOnlyList<BuildingDefinition> BuildingDefinitions => buildingDefinitions;
    }
}
