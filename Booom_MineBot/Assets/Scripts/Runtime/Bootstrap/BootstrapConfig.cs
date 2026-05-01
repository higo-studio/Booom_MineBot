using Minebot.GridMining;
using Minebot.HazardInference;
using Minebot.Progression;
using Minebot.WaveSurvival;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Minebot.Bootstrap
{
    [CreateAssetMenu(menuName = "Minebot/启动/启动配置")]
    public sealed class BootstrapConfig : ScriptableObject
    {
        [SerializeField]
        [InspectorLabel("玩法场景名")]
        private string gameplaySceneName = "Gameplay";

        [SerializeField]
        [InspectorLabel("输入动作资源")]
        private InputActionAsset inputActions;

        [SerializeField]
        [InspectorLabel("默认地图")]
        private MapDefinition defaultMap;

        [SerializeField]
        [InspectorLabel("程序生成地图配置")]
        private GeneratedMapConfig generatedMapConfig = new GeneratedMapConfig();

        [SerializeField]
        [InspectorLabel("数值配置")]
        private GameBalanceConfig balanceConfig;

        [SerializeField]
        [InspectorLabel("升级池配置")]
        private UpgradePoolConfig upgradePool;

        [SerializeField]
        [InspectorLabel("炸药规则配置")]
        private HazardRules hazardRules;

        [SerializeField]
        [InspectorLabel("地震波配置")]
        private WaveConfig waveConfig;

        [SerializeField]
        [InspectorLabel("建筑定义列表")]
        private BuildingDefinition[] buildingDefinitions;

        public string GameplaySceneName => string.IsNullOrWhiteSpace(gameplaySceneName) ? "Gameplay" : gameplaySceneName;
        public InputActionAsset InputActions => inputActions;
        public MapDefinition DefaultMap => defaultMap;
        public GeneratedMapConfig GeneratedMapConfig => generatedMapConfig ?? (generatedMapConfig = new GeneratedMapConfig());
        public GameBalanceConfig BalanceConfig => balanceConfig;
        public UpgradePoolConfig UpgradePool => upgradePool;
        public HazardRules HazardRules => hazardRules;
        public WaveConfig WaveConfig => waveConfig;
        public IReadOnlyList<BuildingDefinition> BuildingDefinitions => buildingDefinitions;
    }
}
