using System;
using Minebot.GridMining;
using Minebot.UI;
using UnityEngine;
using UnityEngine.Tilemaps;
using TileBase = UnityEngine.Tilemaps.TileBase;

namespace Minebot.Presentation
{
    [CreateAssetMenu(menuName = "Minebot/Presentation/Presentation Art Set")]
    public sealed class MinebotPresentationArtSet : ScriptableObject
    {
        [Header("Terrain")]
        [SerializeField]
        private TileBase emptyTile;

        [SerializeField]
        private TileBase soilWallTile;

        [SerializeField]
        private TileBase stoneWallTile;

        [SerializeField]
        private TileBase hardRockWallTile;

        [SerializeField]
        private TileBase ultraHardWallTile;

        [SerializeField]
        private TileBase boundaryTile;

        [SerializeField]
        private DualGridTerrainProfile dualGridTerrainProfile;

        [SerializeField]
        private TileBase[] floorDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        private TileBase[] soilDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        private TileBase[] stoneDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        private TileBase[] hardRockDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        private TileBase[] ultraHardDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        private TileBase[] boundaryDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        private TileBase[] fogNearDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        private TileBase[] fogDeepDualGridTiles = Array.Empty<TileBase>();

        [Header("Overlay")]
        [SerializeField]
        private TileBase dangerTile;

        [SerializeField]
        private TileBase markerTile;

        [SerializeField]
        private TileBase scanHintTile;

        [SerializeField]
        private TileBase[] dangerOutlineTiles = Array.Empty<TileBase>();

        [SerializeField]
        private TileBase[] wallContourTiles = Array.Empty<TileBase>();

        [SerializeField]
        private TileBase[] dangerContourTiles = Array.Empty<TileBase>();

        [SerializeField]
        private TileBase soilDetailTile;

        [SerializeField]
        private TileBase stoneDetailTile;

        [SerializeField]
        private TileBase hardRockDetailTile;

        [SerializeField]
        private TileBase ultraHardDetailTile;

        [SerializeField]
        private TileBase buildPreviewValidTile;

        [SerializeField]
        private TileBase buildPreviewInvalidTile;

        [Header("Hologram Overlay")]
        [SerializeField]
        private Texture2D hologramOverlayAtlas;

        [SerializeField]
        private Texture2D bitmapGlyphAtlas;

        [SerializeField]
        private TextAsset bitmapGlyphDescriptor;

        [SerializeField]
        private BitmapGlyphFontDefinition bitmapGlyphFont;

        [SerializeField]
        private Vector2 scanLabelOffset = new Vector2(0f, 0.62f);

        [SerializeField]
        private Color scanLabelColor = new Color(0.62f, 1f, 0.96f, 1f);

        [SerializeField]
        private float scanLabelFontSize = 4f;

        [SerializeField]
        private int scanLabelSortingOrder = 35;

        [Header("Facilities")]
        [SerializeField]
        private TileBase repairStationTile;

        [SerializeField]
        private TileBase robotFactoryTile;

        [Header("Actors")]
        [SerializeField]
        private Sprite playerSprite;

        [SerializeField]
        private Sprite robotSprite;

        [SerializeField]
        private float playerColliderRadius = 0.42f;

        [Header("Prefab Gameplay Art")]
        [SerializeField]
        private MinebotPresentationActorResources actorResources = new MinebotPresentationActorResources();

        [SerializeField]
        private MinebotPresentationPickupResources pickupResources = new MinebotPresentationPickupResources();

        [SerializeField]
        private MinebotPresentationCellFxResources cellFxResources = new MinebotPresentationCellFxResources();

        [SerializeField]
        private MinebotPresentationHudResources hudResources = new MinebotPresentationHudResources();

        [NonSerialized]
        private TileBase[] generatedFloorDualGridTiles;

        [NonSerialized]
        private TileBase[] generatedSoilDualGridTiles;

        [NonSerialized]
        private TileBase[] generatedStoneDualGridTiles;

        [NonSerialized]
        private TileBase[] generatedHardRockDualGridTiles;

        [NonSerialized]
        private TileBase[] generatedUltraHardDualGridTiles;

        [NonSerialized]
        private TileBase[] generatedBoundaryDualGridTiles;

        [NonSerialized]
        private TileBase[] generatedFogNearDualGridTiles;

        [NonSerialized]
        private TileBase[] generatedFogDeepDualGridTiles;

        public TileBase EmptyTile => emptyTile;
        public TileBase SoilWallTile => soilWallTile;
        public TileBase StoneWallTile => stoneWallTile;
        public TileBase HardRockWallTile => hardRockWallTile;
        public TileBase UltraHardWallTile => ultraHardWallTile;
        public TileBase BoundaryTile => boundaryTile;
        public DualGridTerrainProfile DualGridTerrainProfile => dualGridTerrainProfile;
        public DualGridTerrainLayoutSettings TerrainLayoutSettings => dualGridTerrainProfile != null ? dualGridTerrainProfile.LayoutSettings : DualGridTerrainLayoutSettings.CreateDefault();
        public TileBase[] FloorDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.Floor, floorDualGridTiles, ref generatedFloorDualGridTiles);
        public TileBase[] SoilDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.Soil, soilDualGridTiles, ref generatedSoilDualGridTiles);
        public TileBase[] StoneDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.Stone, stoneDualGridTiles, ref generatedStoneDualGridTiles);
        public TileBase[] HardRockDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.HardRock, hardRockDualGridTiles, ref generatedHardRockDualGridTiles);
        public TileBase[] UltraHardDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.UltraHard, ultraHardDualGridTiles, ref generatedUltraHardDualGridTiles);
        public TileBase[] BoundaryDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.Boundary, boundaryDualGridTiles, ref generatedBoundaryDualGridTiles);
        public TileBase[] FogNearDualGridTiles => ResolveFogDualGridTiles(fogNearDualGridTiles, ref generatedFogNearDualGridTiles, DualGridFogBandKind.Near);
        public TileBase[] FogDeepDualGridTiles => ResolveFogDualGridTiles(fogDeepDualGridTiles, ref generatedFogDeepDualGridTiles, DualGridFogBandKind.Deep);
        public TileBase DangerTile => dangerTile;
        public TileBase MarkerTile => markerTile;
        public TileBase ScanHintTile => scanHintTile;
        public TileBase[] DangerOutlineTiles => dangerOutlineTiles ?? Array.Empty<TileBase>();
        public TileBase[] WallContourTiles => dualGridTerrainProfile != null
            ? dualGridTerrainProfile.ResolveWallContourTiles(wallContourTiles)
            : wallContourTiles ?? Array.Empty<TileBase>();
        public TileBase[] DangerContourTiles => dualGridTerrainProfile != null
            ? dualGridTerrainProfile.ResolveDangerContourTiles(dangerContourTiles)
            : dangerContourTiles ?? Array.Empty<TileBase>();
        public TileBase SoilDetailTile => soilDetailTile;
        public TileBase StoneDetailTile => stoneDetailTile;
        public TileBase HardRockDetailTile => hardRockDetailTile;
        public TileBase UltraHardDetailTile => ultraHardDetailTile;
        public TileBase BuildPreviewValidTile => buildPreviewValidTile;
        public TileBase BuildPreviewInvalidTile => buildPreviewInvalidTile;
        public Texture2D HologramOverlayAtlas => hologramOverlayAtlas;
        public Texture2D BitmapGlyphAtlas => bitmapGlyphAtlas;
        public TextAsset BitmapGlyphDescriptor => bitmapGlyphDescriptor;
        public BitmapGlyphFontDefinition BitmapGlyphFont => bitmapGlyphFont;
        public Vector2 ScanLabelOffset => scanLabelOffset;
        public Color ScanLabelColor => scanLabelColor.a > 0f ? scanLabelColor : new Color(0.62f, 1f, 0.96f, 1f);
        public float ScanLabelFontSize => Mathf.Max(0.5f, scanLabelFontSize);
        public int ScanLabelSortingOrder => Mathf.Clamp(scanLabelSortingOrder, 1, 100);
        public TileBase RepairStationTile => repairStationTile;
        public TileBase RobotFactoryTile => robotFactoryTile;
        public Sprite PlayerSprite => playerSprite;
        public Sprite RobotSprite => robotSprite;
        public float PlayerColliderRadius => Mathf.Clamp(playerColliderRadius, 0.1f, 0.49f);
        public MinebotPresentationActorResources ActorResources => actorResources ?? new MinebotPresentationActorResources();
        public MinebotPresentationPickupResources PickupResources => pickupResources ?? new MinebotPresentationPickupResources();
        public MinebotPresentationCellFxResources CellFxResources => cellFxResources ?? new MinebotPresentationCellFxResources();
        public MinebotPresentationHudResources HudResources => hudResources ?? new MinebotPresentationHudResources();

        private TileBase[] ResolveDualGridTiles(TerrainRenderLayerId layerId, TileBase[] configuredTiles, ref TileBase[] generatedTiles)
        {
            if (dualGridTerrainProfile != null)
            {
                return dualGridTerrainProfile.ResolveFamilyTiles(layerId, configuredTiles);
            }

            if (configuredTiles != null && configuredTiles.Length > 0)
            {
                return configuredTiles;
            }

            return generatedTiles ??= DualGridTerrainFallbackTiles.CreateTileSet(layerId);
        }

        private TileBase[] ResolveFogDualGridTiles(TileBase[] configuredTiles, ref TileBase[] generatedTiles, DualGridFogBandKind bandKind)
        {
            if (configuredTiles != null && configuredTiles.Length > 0)
            {
                return configuredTiles;
            }

            return generatedTiles ??= DualGridFogFallbackTiles.CreateTileSet(bandKind);
        }

#if UNITY_EDITOR
        public TileBase[] GetLegacyConfiguredDualGridTiles(TerrainRenderLayerId layerId)
        {
            switch (layerId)
            {
                case TerrainRenderLayerId.Soil:
                    return soilDualGridTiles ?? Array.Empty<TileBase>();
                case TerrainRenderLayerId.Stone:
                    return stoneDualGridTiles ?? Array.Empty<TileBase>();
                case TerrainRenderLayerId.HardRock:
                    return hardRockDualGridTiles ?? Array.Empty<TileBase>();
                case TerrainRenderLayerId.UltraHard:
                    return ultraHardDualGridTiles ?? Array.Empty<TileBase>();
                case TerrainRenderLayerId.Boundary:
                    return boundaryDualGridTiles ?? Array.Empty<TileBase>();
                default:
                    return floorDualGridTiles ?? Array.Empty<TileBase>();
            }
        }

        public TileBase[] GetLegacyConfiguredWallContourTiles()
        {
            return wallContourTiles ?? Array.Empty<TileBase>();
        }

        public TileBase[] GetLegacyConfiguredDangerContourTiles()
        {
            return dangerContourTiles ?? Array.Empty<TileBase>();
        }

        public void AssignDualGridTerrainProfile(DualGridTerrainProfile profile)
        {
            dualGridTerrainProfile = profile;
        }
#endif

        public TileBase TileForHardness(HardnessTier hardness)
        {
            switch (hardness)
            {
                case HardnessTier.Stone:
                    return stoneDetailTile != null ? stoneDetailTile : stoneWallTile != null ? stoneWallTile : soilWallTile;
                case HardnessTier.HardRock:
                    return hardRockDetailTile != null ? hardRockDetailTile : hardRockWallTile != null ? hardRockWallTile : soilWallTile;
                case HardnessTier.UltraHard:
                    return ultraHardDetailTile != null ? ultraHardDetailTile : ultraHardWallTile != null ? ultraHardWallTile : hardRockWallTile != null ? hardRockWallTile : soilWallTile;
                default:
                    return soilDetailTile != null ? soilDetailTile : soilWallTile;
            }
        }

#if UNITY_EDITOR
        public void Configure(
            TileBase empty,
            TileBase soilWall,
            TileBase stoneWall,
            TileBase hardRockWall,
            TileBase ultraHardWall,
            TileBase boundary,
            TileBase danger,
            TileBase marker,
            TileBase scanHint,
            TileBase repairStation,
            TileBase robotFactory,
            Sprite player,
            Sprite robot,
            TileBase soilDetail = null,
            TileBase stoneDetail = null,
            TileBase hardRockDetail = null,
            TileBase ultraHardDetail = null,
            TileBase buildPreviewValid = null,
            TileBase buildPreviewInvalid = null,
            TileBase[] wallContour = null,
            TileBase[] dangerContour = null,
            TileBase[] dangerOutline = null,
            TileBase[] floorDualGrid = null,
            TileBase[] soilDualGrid = null,
            TileBase[] stoneDualGrid = null,
            TileBase[] hardRockDualGrid = null,
            TileBase[] ultraHardDualGrid = null,
            TileBase[] boundaryDualGrid = null,
            TileBase[] fogNearDualGrid = null,
            TileBase[] fogDeepDualGrid = null,
            DualGridTerrainProfile configuredDualGridTerrainProfile = null,
            BitmapGlyphFontDefinition configuredBitmapGlyphFont = null,
            Texture2D configuredBitmapGlyphAtlas = null,
            TextAsset configuredBitmapGlyphDescriptor = null,
            Texture2D configuredHologramOverlayAtlas = null,
            Vector2? configuredScanLabelOffset = null,
            Color? configuredScanLabelColor = null,
            float configuredScanLabelFontSize = 4f,
            int configuredScanLabelSortingOrder = 35)
        {
            emptyTile = empty;
            soilWallTile = soilWall;
            stoneWallTile = stoneWall;
            hardRockWallTile = hardRockWall;
            ultraHardWallTile = ultraHardWall;
            boundaryTile = boundary;
            dangerTile = danger;
            markerTile = marker;
            scanHintTile = scanHint;
            repairStationTile = repairStation;
            robotFactoryTile = robotFactory;
            playerSprite = player;
            robotSprite = robot;
            soilDetailTile = soilDetail;
            stoneDetailTile = stoneDetail;
            hardRockDetailTile = hardRockDetail;
            ultraHardDetailTile = ultraHardDetail;
            buildPreviewValidTile = buildPreviewValid;
            buildPreviewInvalidTile = buildPreviewInvalid;
            wallContourTiles = wallContour ?? Array.Empty<TileBase>();
            dangerContourTiles = dangerContour ?? Array.Empty<TileBase>();
            dangerOutlineTiles = dangerOutline ?? Array.Empty<TileBase>();
            floorDualGridTiles = floorDualGrid ?? Array.Empty<TileBase>();
            soilDualGridTiles = soilDualGrid ?? Array.Empty<TileBase>();
            stoneDualGridTiles = stoneDualGrid ?? Array.Empty<TileBase>();
            hardRockDualGridTiles = hardRockDualGrid ?? Array.Empty<TileBase>();
            ultraHardDualGridTiles = ultraHardDualGrid ?? Array.Empty<TileBase>();
            boundaryDualGridTiles = boundaryDualGrid ?? Array.Empty<TileBase>();
            fogNearDualGridTiles = fogNearDualGrid ?? Array.Empty<TileBase>();
            fogDeepDualGridTiles = fogDeepDualGrid ?? Array.Empty<TileBase>();
            dualGridTerrainProfile = configuredDualGridTerrainProfile;
            bitmapGlyphFont = configuredBitmapGlyphFont;
            bitmapGlyphAtlas = configuredBitmapGlyphAtlas;
            bitmapGlyphDescriptor = configuredBitmapGlyphDescriptor;
            hologramOverlayAtlas = configuredHologramOverlayAtlas;
            scanLabelOffset = configuredScanLabelOffset ?? new Vector2(0f, 0.62f);
            scanLabelColor = configuredScanLabelColor ?? new Color(0.62f, 1f, 0.96f, 1f);
            scanLabelFontSize = Mathf.Max(0.5f, configuredScanLabelFontSize);
            scanLabelSortingOrder = Mathf.Clamp(configuredScanLabelSortingOrder, 1, 100);
        }

        public void ConfigureActorPresentation(
            GameObject playerPrefab,
            GameObject helperRobotPrefab,
            ActorStateSequenceSet playerStates,
            ActorStateSequenceSet helperRobotStates)
        {
            actorResources = actorResources ?? new MinebotPresentationActorResources();
            actorResources.Configure(playerPrefab, helperRobotPrefab, playerStates, helperRobotStates);
        }

        public void ConfigurePickupPresentation(
            GameObject metalPickupPrefab,
            GameObject energyPickupPrefab,
            GameObject experiencePickupPrefab,
            Sprite metalIcon,
            Sprite energyIcon,
            Sprite experienceIcon)
        {
            pickupResources = pickupResources ?? new MinebotPresentationPickupResources();
            pickupResources.Configure(metalPickupPrefab, energyPickupPrefab, experiencePickupPrefab, metalIcon, energyIcon, experienceIcon);
        }

        public void ConfigureCellFxPresentation(
            GameObject miningCrackPrefab,
            GameObject wallBreakPrefab,
            GameObject explosionPrefab,
            SpriteSequenceAsset miningCrackSequence,
            SpriteSequenceAsset wallBreakSequence,
            SpriteSequenceAsset explosionSequence)
        {
            cellFxResources = cellFxResources ?? new MinebotPresentationCellFxResources();
            cellFxResources.Configure(
                miningCrackPrefab,
                wallBreakPrefab,
                explosionPrefab,
                miningCrackSequence,
                wallBreakSequence,
                explosionSequence);
        }

        public void ConfigureHudPresentation(
            MinebotHudView configuredHudPrefab,
            Sprite panelBackground,
            Sprite statusIcon,
            Sprite interactionIcon,
            Sprite feedbackIcon,
            Sprite warningIcon,
            Sprite upgradeIcon,
            Sprite buildIcon,
            Sprite buildingInteractionIcon)
        {
            hudResources = hudResources ?? new MinebotPresentationHudResources();
            hudResources.Configure(
                configuredHudPrefab,
                panelBackground,
                statusIcon,
                interactionIcon,
                feedbackIcon,
                warningIcon,
                upgradeIcon,
                buildIcon,
                buildingInteractionIcon);
        }
#endif
    }
}
