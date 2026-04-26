using System;
using Minebot.UI;
using UnityEngine;

namespace Minebot.Presentation
{
    [Serializable]
    public sealed class ActorStateSequenceSet
    {
        [SerializeField]
        private SpriteSequenceAsset idle;

        [SerializeField]
        private SpriteSequenceAsset moving;

        [SerializeField]
        private SpriteSequenceAsset mining;

        [SerializeField]
        private SpriteSequenceAsset blocked;

        [SerializeField]
        private SpriteSequenceAsset destroyed;

        public SpriteSequenceAsset Idle => idle;
        public SpriteSequenceAsset Moving => moving;
        public SpriteSequenceAsset Mining => mining;
        public SpriteSequenceAsset Blocked => blocked;
        public SpriteSequenceAsset Destroyed => destroyed;

        public SpriteSequenceAsset ForState(PresentationActorState state)
        {
            switch (state)
            {
                case PresentationActorState.Moving:
                    return moving;
                case PresentationActorState.Mining:
                    return mining;
                case PresentationActorState.Blocked:
                    return blocked;
                case PresentationActorState.Destroyed:
                    return destroyed;
                default:
                    return idle;
            }
        }

#if UNITY_EDITOR
        public void Configure(
            SpriteSequenceAsset configuredIdle,
            SpriteSequenceAsset configuredMoving,
            SpriteSequenceAsset configuredMining,
            SpriteSequenceAsset configuredBlocked,
            SpriteSequenceAsset configuredDestroyed)
        {
            idle = configuredIdle;
            moving = configuredMoving;
            mining = configuredMining;
            blocked = configuredBlocked;
            destroyed = configuredDestroyed;
        }
#endif
    }

    [Serializable]
    public sealed class MinebotPresentationActorResources
    {
        [SerializeField]
        private GameObject playerPrefab;

        [SerializeField]
        private GameObject helperRobotPrefab;

        [SerializeField]
        private ActorStateSequenceSet playerStates = new ActorStateSequenceSet();

        [SerializeField]
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
        private GameObject metalPickupPrefab;

        [SerializeField]
        private GameObject energyPickupPrefab;

        [SerializeField]
        private GameObject experiencePickupPrefab;

        [SerializeField]
        private Sprite metalIcon;

        [SerializeField]
        private Sprite energyIcon;

        [SerializeField]
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
        private GameObject miningCrackPrefab;

        [SerializeField]
        private GameObject wallBreakPrefab;

        [SerializeField]
        private GameObject explosionPrefab;

        [SerializeField]
        private SpriteSequenceAsset miningCrackSequence;

        [SerializeField]
        private SpriteSequenceAsset wallBreakSequence;

        [SerializeField]
        private SpriteSequenceAsset explosionSequence;

        public GameObject MiningCrackPrefab => miningCrackPrefab;
        public GameObject WallBreakPrefab => wallBreakPrefab;
        public GameObject ExplosionPrefab => explosionPrefab;
        public SpriteSequenceAsset MiningCrackSequence => miningCrackSequence;
        public SpriteSequenceAsset WallBreakSequence => wallBreakSequence;
        public SpriteSequenceAsset ExplosionSequence => explosionSequence;

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
        private MinebotHudView hudPrefab;

        [SerializeField]
        private Sprite panelBackground;

        [SerializeField]
        private Sprite statusIcon;

        [SerializeField]
        private Sprite interactionIcon;

        [SerializeField]
        private Sprite feedbackIcon;

        [SerializeField]
        private Sprite warningIcon;

        [SerializeField]
        private Sprite upgradeIcon;

        [SerializeField]
        private Sprite buildIcon;

        [SerializeField]
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
