using JSAM;
using Minebot.Common;
using UnityEngine;

namespace Minebot.Bootstrap
{
    [CreateAssetMenu(menuName = "Minebot/音频/音频配置")]
    public sealed class MinebotAudioConfig : ScriptableObject
    {
        [SerializeField]
        [InspectorLabel("音乐")]
        private MinebotMusicCueGroup music = new MinebotMusicCueGroup();

        [SerializeField]
        [InspectorLabel("模式与界面")]
        private MinebotModeAndUiCueGroup modeAndUi = new MinebotModeAndUiCueGroup();

        [SerializeField]
        [InspectorLabel("玩家与地形")]
        private MinebotPlayerAndTerrainCueGroup playerAndTerrain = new MinebotPlayerAndTerrainCueGroup();

        [SerializeField]
        [InspectorLabel("掉落与成长")]
        private MinebotPickupAndGrowthCueGroup pickupAndGrowth = new MinebotPickupAndGrowthCueGroup();

        [SerializeField]
        [InspectorLabel("建筑与据点")]
        private MinebotBaseOpsCueGroup baseOps = new MinebotBaseOpsCueGroup();

        [SerializeField]
        [InspectorLabel("从属机器人")]
        private MinebotRobotCueGroup robots = new MinebotRobotCueGroup();

        [SerializeField]
        [InspectorLabel("波次与失败")]
        private MinebotWaveAndFailureCueGroup waveAndFailure = new MinebotWaveAndFailureCueGroup();

        public MinebotMusicCueGroup Music => music ??= new MinebotMusicCueGroup();
        public MinebotModeAndUiCueGroup ModeAndUi => modeAndUi ??= new MinebotModeAndUiCueGroup();
        public MinebotPlayerAndTerrainCueGroup PlayerAndTerrain => playerAndTerrain ??= new MinebotPlayerAndTerrainCueGroup();
        public MinebotPickupAndGrowthCueGroup PickupAndGrowth => pickupAndGrowth ??= new MinebotPickupAndGrowthCueGroup();
        public MinebotBaseOpsCueGroup BaseOps => baseOps ??= new MinebotBaseOpsCueGroup();
        public MinebotRobotCueGroup Robots => robots ??= new MinebotRobotCueGroup();
        public MinebotWaveAndFailureCueGroup WaveAndFailure => waveAndFailure ??= new MinebotWaveAndFailureCueGroup();
    }

    [System.Serializable]
    public sealed class MinebotMusicCueGroup
    {
        [SerializeField]
        [InspectorLabel("主循环音乐")]
        private MusicFileObject gameplayLoop;

        [SerializeField]
        [InspectorLabel("预警音乐")]
        private MusicFileObject waveWarning;

        [SerializeField]
        [InspectorLabel("地震结算音乐")]
        private MusicFileObject waveResolution;

        public MusicFileObject GameplayLoop => gameplayLoop;
        public MusicFileObject WaveWarning => waveWarning;
        public MusicFileObject WaveResolution => waveResolution;
    }

    [System.Serializable]
    public sealed class MinebotModeAndUiCueGroup
    {
        [SerializeField]
        [InspectorLabel("标记模式切换")]
        private SoundFileObject modeMarkerToggle;

        [SerializeField]
        [InspectorLabel("建造模式切换")]
        private SoundFileObject modeBuildToggle;

        [SerializeField]
        [InspectorLabel("建筑按钮选择")]
        private SoundFileObject buildingSelect;

        [SerializeField]
        [InspectorLabel("成功标记")]
        private SoundFileObject markerSet;

        [SerializeField]
        [InspectorLabel("取消标记")]
        private SoundFileObject markerClear;

        [SerializeField]
        [InspectorLabel("动作无效")]
        private SoundFileObject actionDenied;

        public SoundFileObject ModeMarkerToggle => modeMarkerToggle;
        public SoundFileObject ModeBuildToggle => modeBuildToggle;
        public SoundFileObject BuildingSelect => buildingSelect;
        public SoundFileObject MarkerSet => markerSet;
        public SoundFileObject MarkerClear => markerClear;
        public SoundFileObject ActionDenied => actionDenied;
    }

    [System.Serializable]
    public sealed class MinebotPlayerAndTerrainCueGroup
    {
        [SerializeField]
        [InspectorLabel("玩家移动")]
        private SoundFileObject playerMove;

        [SerializeField]
        [InspectorLabel("玩家受阻")]
        private SoundFileObject playerBlock;

        [SerializeField]
        [InspectorLabel("玩家持续挖掘")]
        private SoundFileObject playerMiningLoop;

        [SerializeField]
        [InspectorLabel("攻击不足")]
        private SoundFileObject playerMiningWeak;

        [SerializeField]
        [InspectorLabel("岩壁破坏")]
        private SoundFileObject terrainWallBreak;

        [SerializeField]
        [InspectorLabel("炸药爆炸")]
        private SoundFileObject hazardBombExplosion;

        [SerializeField]
        [InspectorLabel("玩家受伤")]
        private SoundFileObject playerDamage;

        public SoundFileObject PlayerMove => playerMove;
        public SoundFileObject PlayerBlock => playerBlock;
        public SoundFileObject PlayerMiningLoop => playerMiningLoop;
        public SoundFileObject PlayerMiningWeak => playerMiningWeak;
        public SoundFileObject TerrainWallBreak => terrainWallBreak;
        public SoundFileObject HazardBombExplosion => hazardBombExplosion;
        public SoundFileObject PlayerDamage => playerDamage;
    }

    [System.Serializable]
    public sealed class MinebotPickupAndGrowthCueGroup
    {
        [SerializeField]
        [InspectorLabel("金属吸收")]
        private SoundFileObject pickupMetalAbsorb;

        [SerializeField]
        [InspectorLabel("能量吸收")]
        private SoundFileObject pickupEnergyAbsorb;

        [SerializeField]
        [InspectorLabel("经验吸收")]
        private SoundFileObject pickupExpAbsorb;

        [SerializeField]
        [InspectorLabel("升级可选提示")]
        private SoundFileObject upgradeAvailable;

        [SerializeField]
        [InspectorLabel("升级应用")]
        private SoundFileObject upgradeApply;

        public SoundFileObject PickupMetalAbsorb => pickupMetalAbsorb;
        public SoundFileObject PickupEnergyAbsorb => pickupEnergyAbsorb;
        public SoundFileObject PickupExpAbsorb => pickupExpAbsorb;
        public SoundFileObject UpgradeAvailable => upgradeAvailable;
        public SoundFileObject UpgradeApply => upgradeApply;
    }

    [System.Serializable]
    public sealed class MinebotBaseOpsCueGroup
    {
        [SerializeField]
        [InspectorLabel("维修成功")]
        private SoundFileObject repairSuccess;

        [SerializeField]
        [InspectorLabel("生产机器人成功")]
        private SoundFileObject robotBuildSuccess;

        [SerializeField]
        [InspectorLabel("建造成功")]
        private SoundFileObject buildPlaceSuccess;

        public SoundFileObject RepairSuccess => repairSuccess;
        public SoundFileObject RobotBuildSuccess => robotBuildSuccess;
        public SoundFileObject BuildPlaceSuccess => buildPlaceSuccess;
    }

    [System.Serializable]
    public sealed class MinebotRobotCueGroup
    {
        [SerializeField]
        [InspectorLabel("机器人持续挖掘")]
        private SoundFileObject robotMiningLoop;

        [SerializeField]
        [InspectorLabel("机器人破墙")]
        private SoundFileObject robotWallBreak;

        [SerializeField]
        [InspectorLabel("机器人损毁")]
        private SoundFileObject robotDestroyed;

        public SoundFileObject RobotMiningLoop => robotMiningLoop;
        public SoundFileObject RobotWallBreak => robotWallBreak;
        public SoundFileObject RobotDestroyed => robotDestroyed;
    }

    [System.Serializable]
    public sealed class MinebotWaveAndFailureCueGroup
    {
        [SerializeField]
        [InspectorLabel("失败提示")]
        private SoundFileObject gameOver;

        [SerializeField]
        [InspectorLabel("预警开始")]
        private SoundFileObject waveWarningStart;

        [SerializeField]
        [InspectorLabel("危险区重算")]
        private SoundFileObject waveDangerRefresh;

        [SerializeField]
        [InspectorLabel("塌方回填")]
        private SoundFileObject waveCollapse;

        [SerializeField]
        [InspectorLabel("成功撑过一波")]
        private SoundFileObject waveSurvived;

        public SoundFileObject GameOver => gameOver;
        public SoundFileObject WaveWarningStart => waveWarningStart;
        public SoundFileObject WaveDangerRefresh => waveDangerRefresh;
        public SoundFileObject WaveCollapse => waveCollapse;
        public SoundFileObject WaveSurvived => waveSurvived;
    }
}
