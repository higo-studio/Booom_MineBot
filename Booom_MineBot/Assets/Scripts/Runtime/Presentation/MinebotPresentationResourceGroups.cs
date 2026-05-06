using System;
using Minebot.UI;
using UnityEngine;

namespace Minebot.Presentation
{
    [Serializable]
    public sealed class ActorStateSequenceSet
    {
        [SerializeField]
        [InspectorLabel("待机-朝前")]
        private SpriteSequenceAsset idleFront;

        [SerializeField]
        [InspectorLabel("待机-朝后")]
        private SpriteSequenceAsset idleBack;

        [SerializeField]
        [InspectorLabel("待机-朝侧")]
        private SpriteSequenceAsset idleSide;

        [SerializeField]
        [InspectorLabel("移动-朝前")]
        private SpriteSequenceAsset movingFront;

        [SerializeField]
        [InspectorLabel("移动-朝后")]
        private SpriteSequenceAsset movingBack;

        [SerializeField]
        [InspectorLabel("移动-朝侧")]
        private SpriteSequenceAsset movingSide;

        [SerializeField]
        [InspectorLabel("挖掘-朝前")]
        private SpriteSequenceAsset miningFront;

        [SerializeField]
        [InspectorLabel("挖掘-朝后")]
        private SpriteSequenceAsset miningBack;

        [SerializeField]
        [InspectorLabel("挖掘-朝侧")]
        private SpriteSequenceAsset miningSide;

        [SerializeField]
        [InspectorLabel("受阻-朝前")]
        private SpriteSequenceAsset blockedFront;

        [SerializeField]
        [InspectorLabel("受阻-朝后")]
        private SpriteSequenceAsset blockedBack;

        [SerializeField]
        [InspectorLabel("受阻-朝侧")]
        private SpriteSequenceAsset blockedSide;

        [SerializeField]
        [InspectorLabel("销毁")]
        private SpriteSequenceAsset destroyed;

        public SpriteSequenceAsset IdleFront => idleFront;
        public SpriteSequenceAsset IdleBack => idleBack;
        public SpriteSequenceAsset IdleSide => idleSide;
        public SpriteSequenceAsset MovingFront => movingFront;
        public SpriteSequenceAsset MovingBack => movingBack;
        public SpriteSequenceAsset MovingSide => movingSide;
        public SpriteSequenceAsset MiningFront => miningFront;
        public SpriteSequenceAsset MiningBack => miningBack;
        public SpriteSequenceAsset MiningSide => miningSide;
        public SpriteSequenceAsset BlockedFront => blockedFront;
        public SpriteSequenceAsset BlockedBack => blockedBack;
        public SpriteSequenceAsset BlockedSide => blockedSide;
        public SpriteSequenceAsset Destroyed => destroyed;

        /// <summary>
        /// 获取指定状态和方向的序列（不含销毁状态，销毁状态始终无方向）
        /// </summary>
        public SpriteSequenceAsset ForState(PresentationActorState state, ActorFacingDirection direction = ActorFacingDirection.Front)
        {
            if (state == PresentationActorState.Destroyed)
            {
                return destroyed;
            }

            return ForStateAndDirection(state, direction);
        }

        private SpriteSequenceAsset ForStateAndDirection(PresentationActorState state, ActorFacingDirection direction)
        {
            switch (state)
            {
                case PresentationActorState.Idle:
                    switch (direction)
                    {
                        case ActorFacingDirection.Front: return idleFront;
                        case ActorFacingDirection.Back: return idleBack;
                        case ActorFacingDirection.Side: return idleSide;
                    }
                    break;
                case PresentationActorState.Moving:
                    switch (direction)
                    {
                        case ActorFacingDirection.Front: return movingFront;
                        case ActorFacingDirection.Back: return movingBack;
                        case ActorFacingDirection.Side: return movingSide;
                    }
                    break;
                case PresentationActorState.Mining:
                    switch (direction)
                    {
                        case ActorFacingDirection.Front: return miningFront;
                        case ActorFacingDirection.Back: return miningBack;
                        case ActorFacingDirection.Side: return miningSide;
                    }
                    break;
                case PresentationActorState.Blocked:
                    switch (direction)
                    {
                        case ActorFacingDirection.Front: return blockedFront;
                        case ActorFacingDirection.Back: return blockedBack;
                        case ActorFacingDirection.Side: return blockedSide;
                    }
                    break;
            }
            return idleFront;
        }

#if UNITY_EDITOR
        public void Configure(
            SpriteSequenceAsset configuredIdleFront,
            SpriteSequenceAsset configuredIdleBack,
            SpriteSequenceAsset configuredIdleSide,
            SpriteSequenceAsset configuredMovingFront,
            SpriteSequenceAsset configuredMovingBack,
            SpriteSequenceAsset configuredMovingSide,
            SpriteSequenceAsset configuredMiningFront,
            SpriteSequenceAsset configuredMiningBack,
            SpriteSequenceAsset configuredMiningSide,
            SpriteSequenceAsset configuredBlockedFront,
            SpriteSequenceAsset configuredBlockedBack,
            SpriteSequenceAsset configuredBlockedSide,
            SpriteSequenceAsset configuredDestroyed)
        {
            idleFront = configuredIdleFront;
            idleBack = configuredIdleBack;
            idleSide = configuredIdleSide;
            movingFront = configuredMovingFront;
            movingBack = configuredMovingBack;
            movingSide = configuredMovingSide;
            miningFront = configuredMiningFront;
            miningBack = configuredMiningBack;
            miningSide = configuredMiningSide;
            blockedFront = configuredBlockedFront;
            blockedBack = configuredBlockedBack;
            blockedSide = configuredBlockedSide;
            destroyed = configuredDestroyed;
        }
#endif
    }

    [Serializable]
    public sealed class MinebotPresentationActorResources
    {
        [SerializeField]
        [InspectorLabel("玩家预制体")]
        private GameObject playerPrefab;

        [SerializeField]
        [InspectorLabel("从属机器人预制体")]
        private GameObject helperRobotPrefab;

        [SerializeField]
        [InspectorLabel("玩家状态序列")]
        private ActorStateSequenceSet playerStates = new ActorStateSequenceSet();

        [SerializeField]
        [InspectorLabel("从属机器人状态序列")]
        private ActorStateSequenceSet helperRobotStates = new ActorStateSequenceSet();

        public GameObject PlayerPrefab => playerPrefab;
        public GameObject HelperRobotPrefab => helperRobotPrefab;
        public ActorStateSequenceSet PlayerStates => playerStates ?? new ActorStateSequenceSet();
        public ActorStateSequenceSet HelperRobotStates => helperRobotStates ?? new ActorStateSequenceSet();

#if UNITY_EDITOR
        public void Configure(
            GameObject configuredPlayerPrefab,
            GameObject configuredHelperRobotPrefab,
            ActorStateSequenceSet configuredPlayerStates,
            ActorStateSequenceSet configuredHelperRobotStates)
        {
            playerPrefab = configuredPlayerPrefab;
            helperRobotPrefab = configuredHelperRobotPrefab;
            playerStates = configuredPlayerStates ?? new ActorStateSequenceSet();
            helperRobotStates = configuredHelperRobotStates ?? new ActorStateSequenceSet();
        }
#endif
    }

    [Serializable]
    public sealed class MinebotPresentationPickupResources
    {
        [SerializeField]
        [InspectorLabel("金属拾取物预制体")]
        private GameObject metalPickupPrefab;

        [SerializeField]
        [InspectorLabel("能量拾取物预制体")]
        private GameObject energyPickupPrefab;

        [SerializeField]
        [InspectorLabel("经验拾取物预制体")]
        private GameObject experiencePickupPrefab;

        [SerializeField]
        [InspectorLabel("金属图标")]
        private Sprite metalIcon;

        [SerializeField]
        [InspectorLabel("能量图标")]
        private Sprite energyIcon;

        [SerializeField]
        [InspectorLabel("经验图标")]
        private Sprite experienceIcon;

        public GameObject MetalPickupPrefab => metalPickupPrefab;
        public GameObject EnergyPickupPrefab => energyPickupPrefab;
        public GameObject ExperiencePickupPrefab => experiencePickupPrefab;
        public Sprite MetalIcon => metalIcon;
        public Sprite EnergyIcon => energyIcon;
        public Sprite ExperienceIcon => experienceIcon;

        public GameObject PrefabFor(Minebot.Progression.WorldPickupType type)
        {
            switch (type)
            {
                case Minebot.Progression.WorldPickupType.Energy:
                    return energyPickupPrefab;
                case Minebot.Progression.WorldPickupType.Experience:
                    return experiencePickupPrefab;
                default:
                    return metalPickupPrefab;
            }
        }

        public Sprite IconFor(Minebot.Progression.WorldPickupType type)
        {
            switch (type)
            {
                case Minebot.Progression.WorldPickupType.Energy:
                    return energyIcon;
                case Minebot.Progression.WorldPickupType.Experience:
                    return experienceIcon;
                default:
                    return metalIcon;
            }
        }

#if UNITY_EDITOR
        public void Configure(
            GameObject configuredMetalPickupPrefab,
            GameObject configuredEnergyPickupPrefab,
            GameObject configuredExperiencePickupPrefab,
            Sprite configuredMetalIcon,
            Sprite configuredEnergyIcon,
            Sprite configuredExperienceIcon)
        {
            metalPickupPrefab = configuredMetalPickupPrefab;
            energyPickupPrefab = configuredEnergyPickupPrefab;
            experiencePickupPrefab = configuredExperiencePickupPrefab;
            metalIcon = configuredMetalIcon;
            energyIcon = configuredEnergyIcon;
            experienceIcon = configuredExperienceIcon;
        }
#endif
    }

    [Serializable]
    public sealed class MinebotPresentationCellFxResources
    {
        [SerializeField]
        [InspectorLabel("挖掘裂纹预制体")]
        private GameObject miningCrackPrefab;

        [SerializeField]
        [InspectorLabel("墙体破碎预制体")]
        private GameObject wallBreakPrefab;

        [SerializeField]
        [InspectorLabel("爆炸预制体")]
        private GameObject explosionPrefab;

        [SerializeField]
        [InspectorLabel("挖掘裂纹序列")]
        private SpriteSequenceAsset miningCrackSequence;

        [SerializeField]
        [InspectorLabel("墙体破碎序列")]
        private SpriteSequenceAsset wallBreakSequence;

        [SerializeField]
        [InspectorLabel("爆炸序列")]
        private SpriteSequenceAsset explosionSequence;

        [SerializeField]
        [InspectorLabel("挖掘裂纹排序层级")]
        private int miningCrackSortingOrder = 36;

        [SerializeField]
        [InspectorLabel("挖掘裂纹偏移")]
        private Vector2 miningCrackOffset = new Vector2(0f, 0.08f);

        [SerializeField]
        [InspectorLabel("墙体破碎粒子特效预制体")]
        private GameObject wallBreakParticlePrefab;

        [SerializeField]
        [InspectorLabel("爆炸粒子特效预制体")]
        private GameObject explosionParticlePrefab;

        public GameObject MiningCrackPrefab => miningCrackPrefab;
        public GameObject WallBreakPrefab => wallBreakPrefab;
        public GameObject ExplosionPrefab => explosionPrefab;
        public GameObject WallBreakParticlePrefab => wallBreakParticlePrefab;
        public GameObject ExplosionParticlePrefab => explosionParticlePrefab;
        public SpriteSequenceAsset MiningCrackSequence => miningCrackSequence;
        public SpriteSequenceAsset WallBreakSequence => wallBreakSequence;
        public SpriteSequenceAsset ExplosionSequence => explosionSequence;
        public int MiningCrackSortingOrder => Mathf.Max(0, miningCrackSortingOrder);
        public Vector2 MiningCrackOffset => miningCrackOffset;

#if UNITY_EDITOR
        public void Configure(
            GameObject configuredMiningCrackPrefab,
            GameObject configuredWallBreakPrefab,
            GameObject configuredExplosionPrefab,
            SpriteSequenceAsset configuredMiningCrackSequence,
            SpriteSequenceAsset configuredWallBreakSequence,
            SpriteSequenceAsset configuredExplosionSequence)
        {
            miningCrackPrefab = configuredMiningCrackPrefab;
            wallBreakPrefab = configuredWallBreakPrefab;
            explosionPrefab = configuredExplosionPrefab;
            miningCrackSequence = configuredMiningCrackSequence;
            wallBreakSequence = configuredWallBreakSequence;
            explosionSequence = configuredExplosionSequence;
        }
#endif
    }

    [Serializable]
    public sealed class MinebotPresentationHudResources
    {
        [SerializeField]
        [InspectorLabel("界面预制体")]
        private MinebotHudView hudPrefab;

        [SerializeField]
        [InspectorLabel("面板背景")]
        private Sprite panelBackground;

        [SerializeField]
        [InspectorLabel("状态图标")]
        private Sprite statusIcon;

        [SerializeField]
        [InspectorLabel("交互图标")]
        private Sprite interactionIcon;

        [SerializeField]
        [InspectorLabel("反馈图标")]
        private Sprite feedbackIcon;

        [SerializeField]
        [InspectorLabel("警告图标")]
        private Sprite warningIcon;

        [SerializeField]
        [InspectorLabel("升级图标")]
        private Sprite upgradeIcon;

        [SerializeField]
        [InspectorLabel("建造图标")]
        private Sprite buildIcon;

        [SerializeField]
        [InspectorLabel("建筑交互图标")]
        private Sprite buildingInteractionIcon;

        public MinebotHudView HudPrefab => hudPrefab;
        public Sprite PanelBackground => panelBackground;
        public Sprite StatusIcon => statusIcon;
        public Sprite InteractionIcon => interactionIcon;
        public Sprite FeedbackIcon => feedbackIcon;
        public Sprite WarningIcon => warningIcon;
        public Sprite UpgradeIcon => upgradeIcon;
        public Sprite BuildIcon => buildIcon;
        public Sprite BuildingInteractionIcon => buildingInteractionIcon;

#if UNITY_EDITOR
        public void Configure(
            MinebotHudView configuredHudPrefab,
            Sprite configuredPanelBackground,
            Sprite configuredStatusIcon,
            Sprite configuredInteractionIcon,
            Sprite configuredFeedbackIcon,
            Sprite configuredWarningIcon,
            Sprite configuredUpgradeIcon,
            Sprite configuredBuildIcon,
            Sprite configuredBuildingInteractionIcon)
        {
            hudPrefab = configuredHudPrefab;
            panelBackground = configuredPanelBackground;
            statusIcon = configuredStatusIcon;
            interactionIcon = configuredInteractionIcon;
            feedbackIcon = configuredFeedbackIcon;
            warningIcon = configuredWarningIcon;
            upgradeIcon = configuredUpgradeIcon;
            buildIcon = configuredBuildIcon;
            buildingInteractionIcon = configuredBuildingInteractionIcon;
        }
#endif
    }
}
