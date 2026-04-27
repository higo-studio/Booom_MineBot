using System;
using Minebot.GridMining;
using Minebot.UI;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Presentation
{
    [CreateAssetMenu(menuName = "Minebot/Presentation/Presentation Art Set")]
    public sealed class MinebotPresentationArtSet : ScriptableObject
    {
        [Header("Terrain")]
        [SerializeField]
        private Tile emptyTile;

        [SerializeField]
        private Tile soilWallTile;

        [SerializeField]
        private Tile stoneWallTile;

        [SerializeField]
        private Tile hardRockWallTile;

        [SerializeField]
        private Tile ultraHardWallTile;

        [SerializeField]
        private Tile boundaryTile;

        [SerializeField]
        private DualGridTerrainProfile dualGridTerrainProfile;

        [SerializeField]
        private Tile[] floorDualGridTiles = Array.Empty<Tile>();

        [SerializeField]
        private Tile[] soilDualGridTiles = Array.Empty<Tile>();

        [SerializeField]
        private Tile[] stoneDualGridTiles = Array.Empty<Tile>();

        [SerializeField]
        private Tile[] hardRockDualGridTiles = Array.Empty<Tile>();

        [SerializeField]
        private Tile[] ultraHardDualGridTiles = Array.Empty<Tile>();

        [SerializeField]
        private Tile[] boundaryDualGridTiles = Array.Empty<Tile>();

        [Header("Overlay")]
        [SerializeField]
        private Tile dangerTile;

        [SerializeField]
        private Tile markerTile;

        [SerializeField]
        private Tile scanHintTile;

        [SerializeField]
        private Tile[] dangerOutlineTiles = Array.Empty<Tile>();

        [SerializeField]
        private Tile[] wallContourTiles = Array.Empty<Tile>();

        [SerializeField]
        private Tile[] dangerContourTiles = Array.Empty<Tile>();

        [SerializeField]
        private Tile soilDetailTile;

        [SerializeField]
        private Tile stoneDetailTile;

        [SerializeField]
        private Tile hardRockDetailTile;

        [SerializeField]
        private Tile ultraHardDetailTile;

        [SerializeField]
        private Tile buildPreviewValidTile;

        [SerializeField]
        private Tile buildPreviewInvalidTile;

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
        private Tile repairStationTile;

        [SerializeField]
        private Tile robotFactoryTile;

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
        private Tile[] generatedFloorDualGridTiles;

        [NonSerialized]
        private Tile[] generatedSoilDualGridTiles;

        [NonSerialized]
        private Tile[] generatedStoneDualGridTiles;

        [NonSerialized]
        private Tile[] generatedHardRockDualGridTiles;

        [NonSerialized]
        private Tile[] generatedUltraHardDualGridTiles;

        [NonSerialized]
        private Tile[] generatedBoundaryDualGridTiles;

        public Tile EmptyTile => emptyTile;
        public Tile SoilWallTile => soilWallTile;
        public Tile StoneWallTile => stoneWallTile;
        public Tile HardRockWallTile => hardRockWallTile;
        public Tile UltraHardWallTile => ultraHardWallTile;
        public Tile BoundaryTile => boundaryTile;
        public DualGridTerrainProfile DualGridTerrainProfile => dualGridTerrainProfile;
        public DualGridTerrainLayoutSettings TerrainLayoutSettings => dualGridTerrainProfile != null ? dualGridTerrainProfile.LayoutSettings : DualGridTerrainLayoutSettings.CreateDefault();
        public Tile[] FloorDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.Floor, floorDualGridTiles, ref generatedFloorDualGridTiles);
        public Tile[] SoilDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.Soil, soilDualGridTiles, ref generatedSoilDualGridTiles);
        public Tile[] StoneDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.Stone, stoneDualGridTiles, ref generatedStoneDualGridTiles);
        public Tile[] HardRockDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.HardRock, hardRockDualGridTiles, ref generatedHardRockDualGridTiles);
        public Tile[] UltraHardDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.UltraHard, ultraHardDualGridTiles, ref generatedUltraHardDualGridTiles);
        public Tile[] BoundaryDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.Boundary, boundaryDualGridTiles, ref generatedBoundaryDualGridTiles);
        public Tile DangerTile => dangerTile;
        public Tile MarkerTile => markerTile;
        public Tile ScanHintTile => scanHintTile;
        public Tile[] DangerOutlineTiles => dangerOutlineTiles ?? Array.Empty<Tile>();
        public Tile[] WallContourTiles => dualGridTerrainProfile != null
            ? dualGridTerrainProfile.ResolveWallContourTiles(wallContourTiles)
            : wallContourTiles ?? Array.Empty<Tile>();
        public Tile[] DangerContourTiles => dualGridTerrainProfile != null
            ? dualGridTerrainProfile.ResolveDangerContourTiles(dangerContourTiles)
            : dangerContourTiles ?? Array.Empty<Tile>();
        public Tile SoilDetailTile => soilDetailTile;
        public Tile StoneDetailTile => stoneDetailTile;
        public Tile HardRockDetailTile => hardRockDetailTile;
        public Tile UltraHardDetailTile => ultraHardDetailTile;
        public Tile BuildPreviewValidTile => buildPreviewValidTile;
        public Tile BuildPreviewInvalidTile => buildPreviewInvalidTile;
        public Texture2D HologramOverlayAtlas => hologramOverlayAtlas;
        public Texture2D BitmapGlyphAtlas => bitmapGlyphAtlas;
        public TextAsset BitmapGlyphDescriptor => bitmapGlyphDescriptor;
        public BitmapGlyphFontDefinition BitmapGlyphFont => bitmapGlyphFont;
        public Vector2 ScanLabelOffset => scanLabelOffset;
        public Color ScanLabelColor => scanLabelColor.a > 0f ? scanLabelColor : new Color(0.62f, 1f, 0.96f, 1f);
        public float ScanLabelFontSize => Mathf.Max(0.5f, scanLabelFontSize);
        public int ScanLabelSortingOrder => Mathf.Clamp(scanLabelSortingOrder, 1, 100);
        public Tile RepairStationTile => repairStationTile;
        public Tile RobotFactoryTile => robotFactoryTile;
        public Sprite PlayerSprite => playerSprite;
        public Sprite RobotSprite => robotSprite;
        public float PlayerColliderRadius => Mathf.Clamp(playerColliderRadius, 0.1f, 0.49f);
        public MinebotPresentationActorResources ActorResources => actorResources ?? new MinebotPresentationActorResources();
        public MinebotPresentationPickupResources PickupResources => pickupResources ?? new MinebotPresentationPickupResources();
        public MinebotPresentationCellFxResources CellFxResources => cellFxResources ?? new MinebotPresentationCellFxResources();
        public MinebotPresentationHudResources HudResources => hudResources ?? new MinebotPresentationHudResources();

        private Tile[] ResolveDualGridTiles(TerrainRenderLayerId layerId, Tile[] configuredTiles, ref Tile[] generatedTiles)
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

#if UNITY_EDITOR
        public Tile[] GetLegacyConfiguredDualGridTiles(TerrainRenderLayerId layerId)
        {
            switch (layerId)
            {
                case TerrainRenderLayerId.Soil:
                    return soilDualGridTiles ?? Array.Empty<Tile>();
                case TerrainRenderLayerId.Stone:
                    return stoneDualGridTiles ?? Array.Empty<Tile>();
                case TerrainRenderLayerId.HardRock:
                    return hardRockDualGridTiles ?? Array.Empty<Tile>();
                case TerrainRenderLayerId.UltraHard:
                    return ultraHardDualGridTiles ?? Array.Empty<Tile>();
                case TerrainRenderLayerId.Boundary:
                    return boundaryDualGridTiles ?? Array.Empty<Tile>();
                default:
                    return floorDualGridTiles ?? Array.Empty<Tile>();
            }
        }

        public Tile[] GetLegacyConfiguredWallContourTiles()
        {
            return wallContourTiles ?? Array.Empty<Tile>();
        }

        public Tile[] GetLegacyConfiguredDangerContourTiles()
        {
            return dangerContourTiles ?? Array.Empty<Tile>();
        }

        public void AssignDualGridTerrainProfile(DualGridTerrainProfile profile)
        {
            dualGridTerrainProfile = profile;
        }
#endif

        public Tile TileForHardness(HardnessTier hardness)
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
            Tile empty,
            Tile soilWall,
            Tile stoneWall,
            Tile hardRockWall,
            Tile ultraHardWall,
            Tile boundary,
            Tile danger,
            Tile marker,
            Tile scanHint,
            Tile repairStation,
            Tile robotFactory,
            Sprite player,
            Sprite robot,
            Tile soilDetail = null,
            Tile stoneDetail = null,
            Tile hardRockDetail = null,
            Tile ultraHardDetail = null,
            Tile buildPreviewValid = null,
            Tile buildPreviewInvalid = null,
            Tile[] wallContour = null,
            Tile[] dangerContour = null,
            Tile[] dangerOutline = null,
            Tile[] floorDualGrid = null,
            Tile[] soilDualGrid = null,
            Tile[] stoneDualGrid = null,
            Tile[] hardRockDualGrid = null,
            Tile[] ultraHardDualGrid = null,
            Tile[] boundaryDualGrid = null,
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
            wallContourTiles = wallContour ?? Array.Empty<Tile>();
            dangerContourTiles = dangerContour ?? Array.Empty<Tile>();
            dangerOutlineTiles = dangerOutline ?? Array.Empty<Tile>();
            floorDualGridTiles = floorDualGrid ?? Array.Empty<Tile>();
            soilDualGridTiles = soilDualGrid ?? Array.Empty<Tile>();
            stoneDualGridTiles = stoneDualGrid ?? Array.Empty<Tile>();
            hardRockDualGridTiles = hardRockDualGrid ?? Array.Empty<Tile>();
            ultraHardDualGridTiles = ultraHardDualGrid ?? Array.Empty<Tile>();
            boundaryDualGridTiles = boundaryDualGrid ?? Array.Empty<Tile>();
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
