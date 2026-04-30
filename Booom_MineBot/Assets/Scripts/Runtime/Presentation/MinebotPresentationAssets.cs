using Minebot.Progression;
using Minebot.UI;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Presentation
{
    public sealed class MinebotPresentationAssets
    {
        public Tile EmptyTile { get; private set; }
        public Tile SoilWallTile { get; private set; }
        public Tile StoneWallTile { get; private set; }
        public Tile HardRockWallTile { get; private set; }
        public Tile UltraHardWallTile { get; private set; }
        public Tile BoundaryTile { get; private set; }
        public Tile[] FloorDualGridTiles { get; private set; }
        public Tile[] SoilDualGridTiles { get; private set; }
        public Tile[] StoneDualGridTiles { get; private set; }
        public Tile[] HardRockDualGridTiles { get; private set; }
        public Tile[] UltraHardDualGridTiles { get; private set; }
        public Tile[] BoundaryDualGridTiles { get; private set; }
        public Tile[] FogNearDualGridTiles { get; private set; }
        public Tile[] FogDeepDualGridTiles { get; private set; }
        public Tile DangerTile { get; private set; }
        public Tile MarkerTile { get; private set; }
        public Tile RepairStationTile { get; private set; }
        public Tile RobotFactoryTile { get; private set; }
        public Tile ScanHintTile { get; private set; }
        public Tile BuildPreviewValidTile { get; private set; }
        public Tile BuildPreviewInvalidTile { get; private set; }
        public Tile[] WallContourTiles { get; private set; }
        public Tile[] DangerContourTiles { get; private set; }
        public Tile[] DangerOutlineTiles { get; private set; }
        public Texture2D HologramOverlayAtlas { get; private set; }
        public Texture2D BitmapGlyphAtlas { get; private set; }
        public TextAsset BitmapGlyphDescriptor { get; private set; }
        public BitmapGlyphFontDefinition BitmapGlyphFont { get; private set; }
        public Tile SoilDetailTile { get; private set; }
        public Tile StoneDetailTile { get; private set; }
        public Tile HardRockDetailTile { get; private set; }
        public Tile UltraHardDetailTile { get; private set; }
        public Sprite PlayerSprite { get; private set; }
        public Sprite RobotSprite { get; private set; }
        public GameObject PlayerActorPrefab { get; private set; }
        public GameObject HelperRobotPrefab { get; private set; }
        public ActorStateSequenceSet PlayerActorStates { get; private set; }
        public ActorStateSequenceSet HelperRobotStates { get; private set; }
        public GameObject MetalPickupPrefab { get; private set; }
        public GameObject EnergyPickupPrefab { get; private set; }
        public GameObject ExperiencePickupPrefab { get; private set; }
        public Sprite MetalPickupIcon { get; private set; }
        public Sprite EnergyPickupIcon { get; private set; }
        public Sprite ExperiencePickupIcon { get; private set; }
        public GameObject MiningCrackPrefab { get; private set; }
        public GameObject WallBreakPrefab { get; private set; }
        public GameObject ExplosionPrefab { get; private set; }
        public SpriteSequenceAsset MiningCrackSequence { get; private set; }
        public SpriteSequenceAsset WallBreakSequence { get; private set; }
        public SpriteSequenceAsset ExplosionSequence { get; private set; }
        public MinebotHudView HudPrefab { get; private set; }
        public Sprite HudPanelBackground { get; private set; }
        public Sprite HudStatusIcon { get; private set; }
        public Sprite HudInteractionIcon { get; private set; }
        public Sprite HudFeedbackIcon { get; private set; }
        public Sprite HudWarningIcon { get; private set; }
        public Sprite HudUpgradeIcon { get; private set; }
        public Sprite HudBuildIcon { get; private set; }
        public Sprite HudBuildingInteractionIcon { get; private set; }
        public Vector2 ScanLabelOffset { get; private set; }
        public Color ScanLabelColor { get; private set; }
        public float ScanLabelFontSize { get; private set; }
        public int ScanLabelSortingOrder { get; private set; }
        public float PlayerColliderRadius { get; private set; }
        public DualGridTerrainLayoutSettings TerrainLayoutSettings { get; private set; }
        public bool IsUsingConfiguredArtSet { get; private set; }

        public static MinebotPresentationAssets Create(MinebotPresentationArtSet artSet)
        {
            return Create(artSet, null);
        }

        public static MinebotPresentationAssets Create(MinebotPresentationArtSet artSet, DualGridTerrainProfile dualGridProfileOverride)
        {
            MinebotPresentationAssets fallback = CreateFallback();
            if (artSet == null && dualGridProfileOverride == null)
            {
                return fallback;
            }

            Tile[] floorTiles = ResolveDualGridTiles(artSet, dualGridProfileOverride, TerrainRenderLayerId.Floor, fallback.FloorDualGridTiles);
            Tile[] soilTiles = ResolveDualGridTiles(artSet, dualGridProfileOverride, TerrainRenderLayerId.Soil, fallback.SoilDualGridTiles);
            Tile[] stoneTiles = ResolveDualGridTiles(artSet, dualGridProfileOverride, TerrainRenderLayerId.Stone, fallback.StoneDualGridTiles);
            Tile[] hardRockTiles = ResolveDualGridTiles(artSet, dualGridProfileOverride, TerrainRenderLayerId.HardRock, fallback.HardRockDualGridTiles);
            Tile[] ultraHardTiles = ResolveDualGridTiles(artSet, dualGridProfileOverride, TerrainRenderLayerId.UltraHard, fallback.UltraHardDualGridTiles);
            Tile[] boundaryTiles = ResolveDualGridTiles(artSet, dualGridProfileOverride, TerrainRenderLayerId.Boundary, fallback.BoundaryDualGridTiles);
            Tile[] fogNearTiles = artSet != null ? artSet.FogNearDualGridTiles : fallback.FogNearDualGridTiles;
            Tile[] fogDeepTiles = artSet != null ? artSet.FogDeepDualGridTiles : fallback.FogDeepDualGridTiles;
            Tile[] wallContours = ResolveLegacyContourTiles(
                dualGridProfileOverride != null ? dualGridProfileOverride.ResolveWallContourTiles(artSet != null ? artSet.WallContourTiles : null) : artSet != null ? artSet.WallContourTiles : null,
                fallback.WallContourTiles);
            Tile[] dangerContours = ResolveLegacyContourTiles(
                dualGridProfileOverride != null ? dualGridProfileOverride.ResolveDangerContourTiles(artSet != null ? artSet.DangerContourTiles : null) : artSet != null ? artSet.DangerContourTiles : null,
                fallback.DangerContourTiles);
            DualGridTerrainLayoutSettings layoutSettings = dualGridProfileOverride != null
                ? dualGridProfileOverride.LayoutSettings
                : artSet != null ? artSet.TerrainLayoutSettings : DualGridTerrainLayoutSettings.CreateDefault();

            return new MinebotPresentationAssets
            {
                EmptyTile = artSet != null && artSet.EmptyTile != null ? artSet.EmptyTile : fallback.EmptyTile,
                SoilWallTile = artSet != null && artSet.SoilWallTile != null ? artSet.SoilWallTile : fallback.SoilWallTile,
                StoneWallTile = artSet != null && artSet.StoneWallTile != null ? artSet.StoneWallTile : fallback.StoneWallTile,
                HardRockWallTile = artSet != null && artSet.HardRockWallTile != null ? artSet.HardRockWallTile : fallback.HardRockWallTile,
                UltraHardWallTile = artSet != null && artSet.UltraHardWallTile != null ? artSet.UltraHardWallTile : fallback.UltraHardWallTile,
                BoundaryTile = artSet != null && artSet.BoundaryTile != null ? artSet.BoundaryTile : fallback.BoundaryTile,
                FloorDualGridTiles = NormalizeIndexedTiles(floorTiles, fallback.FloorDualGridTiles),
                SoilDualGridTiles = NormalizeIndexedTiles(soilTiles, fallback.SoilDualGridTiles),
                StoneDualGridTiles = NormalizeIndexedTiles(stoneTiles, fallback.StoneDualGridTiles),
                HardRockDualGridTiles = NormalizeIndexedTiles(hardRockTiles, fallback.HardRockDualGridTiles),
                UltraHardDualGridTiles = NormalizeIndexedTiles(ultraHardTiles, fallback.UltraHardDualGridTiles),
                BoundaryDualGridTiles = NormalizeIndexedTiles(boundaryTiles, fallback.BoundaryDualGridTiles),
                FogNearDualGridTiles = NormalizeIndexedTiles(fogNearTiles, fallback.FogNearDualGridTiles),
                FogDeepDualGridTiles = NormalizeIndexedTiles(fogDeepTiles, fallback.FogDeepDualGridTiles),
                DangerTile = artSet != null && artSet.DangerTile != null ? artSet.DangerTile : fallback.DangerTile,
                MarkerTile = artSet != null && artSet.MarkerTile != null ? artSet.MarkerTile : fallback.MarkerTile,
                RepairStationTile = artSet != null && artSet.RepairStationTile != null ? artSet.RepairStationTile : fallback.RepairStationTile,
                RobotFactoryTile = artSet != null && artSet.RobotFactoryTile != null ? artSet.RobotFactoryTile : fallback.RobotFactoryTile,
                ScanHintTile = artSet != null && artSet.ScanHintTile != null ? artSet.ScanHintTile : fallback.ScanHintTile,
                BuildPreviewValidTile = artSet != null && artSet.BuildPreviewValidTile != null ? artSet.BuildPreviewValidTile : fallback.BuildPreviewValidTile,
                BuildPreviewInvalidTile = artSet != null && artSet.BuildPreviewInvalidTile != null ? artSet.BuildPreviewInvalidTile : fallback.BuildPreviewInvalidTile,
                WallContourTiles = wallContours,
                DangerContourTiles = dangerContours,
                DangerOutlineTiles = NormalizeDangerOutlineTiles(artSet != null ? artSet.DangerOutlineTiles : null, fallback.DangerOutlineTiles),
                HologramOverlayAtlas = artSet != null && artSet.HologramOverlayAtlas != null ? artSet.HologramOverlayAtlas : fallback.HologramOverlayAtlas,
                BitmapGlyphAtlas = artSet != null && artSet.BitmapGlyphAtlas != null ? artSet.BitmapGlyphAtlas : fallback.BitmapGlyphAtlas,
                BitmapGlyphDescriptor = artSet != null && artSet.BitmapGlyphDescriptor != null ? artSet.BitmapGlyphDescriptor : fallback.BitmapGlyphDescriptor,
                BitmapGlyphFont = artSet != null && artSet.BitmapGlyphFont != null ? artSet.BitmapGlyphFont : fallback.BitmapGlyphFont,
                SoilDetailTile = artSet != null && artSet.SoilDetailTile != null ? artSet.SoilDetailTile : fallback.SoilDetailTile,
                StoneDetailTile = artSet != null && artSet.StoneDetailTile != null ? artSet.StoneDetailTile : fallback.StoneDetailTile,
                HardRockDetailTile = artSet != null && artSet.HardRockDetailTile != null ? artSet.HardRockDetailTile : fallback.HardRockDetailTile,
                UltraHardDetailTile = artSet != null && artSet.UltraHardDetailTile != null ? artSet.UltraHardDetailTile : fallback.UltraHardDetailTile,
                PlayerSprite = artSet != null && artSet.PlayerSprite != null ? artSet.PlayerSprite : fallback.PlayerSprite,
                RobotSprite = artSet != null && artSet.RobotSprite != null ? artSet.RobotSprite : fallback.RobotSprite,
                PlayerActorPrefab = artSet != null && artSet.ActorResources.PlayerPrefab != null ? artSet.ActorResources.PlayerPrefab : fallback.PlayerActorPrefab,
                HelperRobotPrefab = artSet != null && artSet.ActorResources.HelperRobotPrefab != null ? artSet.ActorResources.HelperRobotPrefab : fallback.HelperRobotPrefab,
                PlayerActorStates = artSet != null ? artSet.ActorResources.PlayerStates ?? fallback.PlayerActorStates : fallback.PlayerActorStates,
                HelperRobotStates = artSet != null ? artSet.ActorResources.HelperRobotStates ?? fallback.HelperRobotStates : fallback.HelperRobotStates,
                MetalPickupPrefab = artSet != null && artSet.PickupResources.MetalPickupPrefab != null ? artSet.PickupResources.MetalPickupPrefab : fallback.MetalPickupPrefab,
                EnergyPickupPrefab = artSet != null && artSet.PickupResources.EnergyPickupPrefab != null ? artSet.PickupResources.EnergyPickupPrefab : fallback.EnergyPickupPrefab,
                ExperiencePickupPrefab = artSet != null && artSet.PickupResources.ExperiencePickupPrefab != null ? artSet.PickupResources.ExperiencePickupPrefab : fallback.ExperiencePickupPrefab,
                MetalPickupIcon = artSet != null && artSet.PickupResources.MetalIcon != null ? artSet.PickupResources.MetalIcon : fallback.MetalPickupIcon,
                EnergyPickupIcon = artSet != null && artSet.PickupResources.EnergyIcon != null ? artSet.PickupResources.EnergyIcon : fallback.EnergyPickupIcon,
                ExperiencePickupIcon = artSet != null && artSet.PickupResources.ExperienceIcon != null ? artSet.PickupResources.ExperienceIcon : fallback.ExperiencePickupIcon,
                MiningCrackPrefab = artSet != null && artSet.CellFxResources.MiningCrackPrefab != null ? artSet.CellFxResources.MiningCrackPrefab : fallback.MiningCrackPrefab,
                WallBreakPrefab = artSet != null && artSet.CellFxResources.WallBreakPrefab != null ? artSet.CellFxResources.WallBreakPrefab : fallback.WallBreakPrefab,
                ExplosionPrefab = artSet != null && artSet.CellFxResources.ExplosionPrefab != null ? artSet.CellFxResources.ExplosionPrefab : fallback.ExplosionPrefab,
                MiningCrackSequence = artSet != null && artSet.CellFxResources.MiningCrackSequence != null ? artSet.CellFxResources.MiningCrackSequence : fallback.MiningCrackSequence,
                WallBreakSequence = artSet != null && artSet.CellFxResources.WallBreakSequence != null ? artSet.CellFxResources.WallBreakSequence : fallback.WallBreakSequence,
                ExplosionSequence = artSet != null && artSet.CellFxResources.ExplosionSequence != null ? artSet.CellFxResources.ExplosionSequence : fallback.ExplosionSequence,
                HudPrefab = artSet != null && artSet.HudResources.HudPrefab != null ? artSet.HudResources.HudPrefab : fallback.HudPrefab,
                HudPanelBackground = artSet != null && artSet.HudResources.PanelBackground != null ? artSet.HudResources.PanelBackground : fallback.HudPanelBackground,
                HudStatusIcon = artSet != null && artSet.HudResources.StatusIcon != null ? artSet.HudResources.StatusIcon : fallback.HudStatusIcon,
                HudInteractionIcon = artSet != null && artSet.HudResources.InteractionIcon != null ? artSet.HudResources.InteractionIcon : fallback.HudInteractionIcon,
                HudFeedbackIcon = artSet != null && artSet.HudResources.FeedbackIcon != null ? artSet.HudResources.FeedbackIcon : fallback.HudFeedbackIcon,
                HudWarningIcon = artSet != null && artSet.HudResources.WarningIcon != null ? artSet.HudResources.WarningIcon : fallback.HudWarningIcon,
                HudUpgradeIcon = artSet != null && artSet.HudResources.UpgradeIcon != null ? artSet.HudResources.UpgradeIcon : fallback.HudUpgradeIcon,
                HudBuildIcon = artSet != null && artSet.HudResources.BuildIcon != null ? artSet.HudResources.BuildIcon : fallback.HudBuildIcon,
                HudBuildingInteractionIcon = artSet != null && artSet.HudResources.BuildingInteractionIcon != null ? artSet.HudResources.BuildingInteractionIcon : fallback.HudBuildingInteractionIcon,
                ScanLabelOffset = artSet != null ? artSet.ScanLabelOffset : fallback.ScanLabelOffset,
                ScanLabelColor = artSet != null ? artSet.ScanLabelColor : fallback.ScanLabelColor,
                ScanLabelFontSize = artSet != null ? artSet.ScanLabelFontSize : fallback.ScanLabelFontSize,
                ScanLabelSortingOrder = artSet != null ? artSet.ScanLabelSortingOrder : fallback.ScanLabelSortingOrder,
                PlayerColliderRadius = artSet != null ? artSet.PlayerColliderRadius : fallback.PlayerColliderRadius,
                TerrainLayoutSettings = layoutSettings,
                IsUsingConfiguredArtSet = artSet != null || dualGridProfileOverride != null
            };
        }

        public Tile ResolveDangerOverlayTile(DangerOverlayGeometryKind geometryKind, int variant)
        {
            switch (geometryKind)
            {
                case DangerOverlayGeometryKind.Outline:
                    return DangerOutlineTileForWave(variant);
                case DangerOverlayGeometryKind.Contour:
                    return DangerContourTileForIndex(variant);
                default:
                    return DangerTile;
            }
        }

        public Tile DangerOutlineTileForWave(int currentWave)
        {
            if (DangerOutlineTiles == null || DangerOutlineTiles.Length == 0)
            {
                return DangerTile;
            }

            int index = Mathf.Clamp(Mathf.Max(0, currentWave), 0, DangerOutlineTiles.Length - 1);
            return DangerOutlineTiles[index] != null ? DangerOutlineTiles[index] : DangerTile;
        }

        public Tile WallContourTileForIndex(int index)
        {
            return TileForContourIndex(WallContourTiles, index);
        }

        public Tile DangerContourTileForIndex(int index)
        {
            return TileForContourIndex(DangerContourTiles, index);
        }

        public Tile DualGridTerrainTileFor(TerrainRenderLayerId layerId, int index)
        {
            switch (layerId)
            {
                case TerrainRenderLayerId.Soil:
                    return TileForContourIndex(SoilDualGridTiles, index);
                case TerrainRenderLayerId.Stone:
                    return TileForContourIndex(StoneDualGridTiles, index);
                case TerrainRenderLayerId.HardRock:
                    return TileForContourIndex(HardRockDualGridTiles, index);
                case TerrainRenderLayerId.UltraHard:
                    return TileForContourIndex(UltraHardDualGridTiles, index);
                case TerrainRenderLayerId.Boundary:
                    return TileForContourIndex(BoundaryDualGridTiles, index);
                default:
                    return TileForContourIndex(FloorDualGridTiles, index);
            }
        }

        public Tile FogNearDualGridTileForIndex(int index)
        {
            return TileForContourIndex(FogNearDualGridTiles, index);
        }

        public Tile FogDeepDualGridTileForIndex(int index)
        {
            return TileForContourIndex(FogDeepDualGridTiles, index);
        }

        public Tile WallBaseTileForHardness(Minebot.GridMining.HardnessTier hardness)
        {
            switch (hardness)
            {
                case Minebot.GridMining.HardnessTier.Stone:
                    return StoneDetailTile != null ? StoneDetailTile : StoneWallTile;
                case Minebot.GridMining.HardnessTier.HardRock:
                    return HardRockDetailTile != null ? HardRockDetailTile : HardRockWallTile;
                case Minebot.GridMining.HardnessTier.UltraHard:
                    return UltraHardDetailTile != null ? UltraHardDetailTile : UltraHardWallTile;
                default:
                    return SoilDetailTile != null ? SoilDetailTile : SoilWallTile;
            }
        }

        public GameObject PickupPrefabFor(WorldPickupType type)
        {
            switch (type)
            {
                case WorldPickupType.Energy:
                    return EnergyPickupPrefab;
                case WorldPickupType.Experience:
                    return ExperiencePickupPrefab;
                default:
                    return MetalPickupPrefab;
            }
        }

        public Sprite PickupIconFor(WorldPickupType type)
        {
            switch (type)
            {
                case WorldPickupType.Energy:
                    return EnergyPickupIcon;
                case WorldPickupType.Experience:
                    return ExperiencePickupIcon;
                default:
                    return MetalPickupIcon;
            }
        }

        private static MinebotPresentationAssets CreateFallback()
        {
            return new MinebotPresentationAssets
            {
                EmptyTile = CreateTile("Empty Tile", new Color(0.13f, 0.18f, 0.19f, 1f), new Color(0.07f, 0.09f, 0.1f, 1f)),
                SoilWallTile = CreateTile("Soil Wall Tile", new Color(0.43f, 0.34f, 0.24f, 1f), new Color(0.24f, 0.19f, 0.15f, 1f)),
                StoneWallTile = CreateTile("Stone Wall Tile", new Color(0.36f, 0.36f, 0.34f, 1f), new Color(0.18f, 0.18f, 0.17f, 1f)),
                HardRockWallTile = CreateTile("Hard Rock Wall Tile", new Color(0.24f, 0.26f, 0.28f, 1f), new Color(0.1f, 0.11f, 0.13f, 1f)),
                UltraHardWallTile = CreateTile("Ultra Hard Wall Tile", new Color(0.18f, 0.16f, 0.23f, 1f), new Color(0.08f, 0.07f, 0.11f, 1f)),
                BoundaryTile = CreateTile("Boundary Tile", new Color(0.05f, 0.05f, 0.06f, 1f), new Color(0.17f, 0.17f, 0.18f, 1f)),
                FloorDualGridTiles = DualGridTerrainFallbackTiles.CreateTileSet(TerrainRenderLayerId.Floor),
                SoilDualGridTiles = DualGridTerrainFallbackTiles.CreateTileSet(TerrainRenderLayerId.Soil),
                StoneDualGridTiles = DualGridTerrainFallbackTiles.CreateTileSet(TerrainRenderLayerId.Stone),
                HardRockDualGridTiles = DualGridTerrainFallbackTiles.CreateTileSet(TerrainRenderLayerId.HardRock),
                UltraHardDualGridTiles = DualGridTerrainFallbackTiles.CreateTileSet(TerrainRenderLayerId.UltraHard),
                BoundaryDualGridTiles = DualGridTerrainFallbackTiles.CreateTileSet(TerrainRenderLayerId.Boundary),
                FogNearDualGridTiles = DualGridFogFallbackTiles.CreateTileSet(DualGridFogBandKind.Near),
                FogDeepDualGridTiles = DualGridFogFallbackTiles.CreateTileSet(DualGridFogBandKind.Deep),
                DangerTile = CreateTile("Danger Tile", new Color(1f, 0.16f, 0.2f, 0.28f), new Color(1f, 0.38f, 0.24f, 0.92f)),
                MarkerTile = CreateTile("Marker Tile", new Color(0.2f, 0.84f, 1f, 0.32f), new Color(0.86f, 1f, 0.98f, 0.92f)),
                RepairStationTile = CreateTile("Repair Station Tile", new Color(0.1f, 0.38f, 0.85f, 1f), new Color(0.62f, 0.88f, 1f, 1f)),
                RobotFactoryTile = CreateTile("Robot Factory Tile", new Color(0.88f, 0.42f, 0.09f, 1f), new Color(1f, 0.78f, 0.2f, 1f)),
                ScanHintTile = CreateTile("Scan Hint Tile", new Color(0.18f, 0.82f, 1f, 0.28f), new Color(0.9f, 1f, 1f, 0.92f)),
                BuildPreviewValidTile = CreateTile("Build Preview Valid Tile", new Color(0.18f, 0.72f, 1f, 0.42f), new Color(0.82f, 0.96f, 1f, 0.92f)),
                BuildPreviewInvalidTile = CreateTile("Build Preview Invalid Tile", new Color(1f, 0.14f, 0.18f, 0.24f), new Color(1f, 0.34f, 0.24f, 0.9f)),
                WallContourTiles = CreateContourTileSet("Wall Contour", new Color(0.92f, 0.9f, 0.82f, 0.95f), 2),
                DangerContourTiles = CreateContourTileSet("Danger Contour", new Color(1f, 0.44f, 0.36f, 0.95f), 2),
                DangerOutlineTiles = new[]
                {
                    CreateOutlineTile("Danger Outline Thin Tile", new Color(1f, 0.62f, 0.54f, 0.95f), 1),
                    CreateOutlineTile("Danger Outline Medium Tile", new Color(1f, 0.46f, 0.36f, 0.95f), 2),
                    CreateOutlineTile("Danger Outline Thick Tile", new Color(1f, 0.28f, 0.22f, 0.95f), 3)
                },
                HologramOverlayAtlas = CreateTexture("Hologram Overlay Atlas Texture", 16, 16, new Color(0.18f, 0.82f, 1f, 0.4f), new Color(0.9f, 1f, 1f, 0.95f)),
                BitmapGlyphAtlas = CreateTexture("Bitmap Glyph Atlas Texture", 64, 16, new Color(0f, 0f, 0f, 0f), new Color(0.7f, 1f, 0.96f, 0.95f)),
                BitmapGlyphDescriptor = new TextAsset("fallback bitmap glyph descriptor"),
                BitmapGlyphFont = CreateFallbackBitmapGlyphFont(),
                SoilDetailTile = CreateDetailTile("Soil Detail Tile", new Color(0.43f, 0.34f, 0.24f, 1f), new Color(0.5f, 0.4f, 0.29f, 1f)),
                StoneDetailTile = CreateDetailTile("Stone Detail Tile", new Color(0.36f, 0.36f, 0.34f, 1f), new Color(0.54f, 0.54f, 0.51f, 1f)),
                HardRockDetailTile = CreateDetailTile("Hard Rock Detail Tile", new Color(0.24f, 0.26f, 0.28f, 1f), new Color(0.34f, 0.38f, 0.41f, 1f)),
                UltraHardDetailTile = CreateDetailTile("Ultra Hard Detail Tile", new Color(0.18f, 0.16f, 0.23f, 1f), new Color(0.28f, 0.24f, 0.35f, 1f)),
                PlayerSprite = CreateSprite("Player Sprite", new Color(1f, 0.86f, 0.22f, 1f), new Color(0.1f, 0.75f, 0.95f, 1f)),
                RobotSprite = CreateSprite("Robot Sprite", new Color(0.34f, 0.94f, 0.38f, 1f), new Color(0.05f, 0.28f, 0.12f, 1f)),
                PlayerActorStates = new ActorStateSequenceSet(),
                HelperRobotStates = new ActorStateSequenceSet(),
                MetalPickupIcon = CreateSprite("Metal Pickup Icon", new Color(0.9f, 0.84f, 0.62f, 1f), new Color(0.52f, 0.44f, 0.18f, 1f)),
                EnergyPickupIcon = CreateSprite("Energy Pickup Icon", new Color(0.4f, 0.95f, 1f, 1f), new Color(0.08f, 0.46f, 0.78f, 1f)),
                ExperiencePickupIcon = CreateSprite("Experience Pickup Icon", new Color(0.72f, 0.96f, 0.4f, 1f), new Color(0.2f, 0.5f, 0.12f, 1f)),
                HudPanelBackground = CreateSprite("HUD Panel Background", new Color(0.07f, 0.1f, 0.12f, 0.92f), new Color(0.32f, 0.7f, 0.78f, 0.96f)),
                HudStatusIcon = CreateSprite("HUD Status Icon", new Color(0.84f, 0.93f, 1f, 1f), new Color(0.2f, 0.64f, 0.76f, 1f)),
                HudInteractionIcon = CreateSprite("HUD Interaction Icon", new Color(0.92f, 0.86f, 0.64f, 1f), new Color(0.76f, 0.46f, 0.18f, 1f)),
                HudFeedbackIcon = CreateSprite("HUD Feedback Icon", new Color(0.78f, 0.96f, 1f, 1f), new Color(0.16f, 0.72f, 0.92f, 1f)),
                HudWarningIcon = CreateSprite("HUD Warning Icon", new Color(1f, 0.82f, 0.72f, 1f), new Color(0.98f, 0.34f, 0.24f, 1f)),
                HudUpgradeIcon = CreateSprite("HUD Upgrade Icon", new Color(0.84f, 1f, 0.62f, 1f), new Color(0.3f, 0.68f, 0.18f, 1f)),
                HudBuildIcon = CreateSprite("HUD Build Icon", new Color(0.94f, 0.9f, 0.7f, 1f), new Color(0.74f, 0.54f, 0.22f, 1f)),
                HudBuildingInteractionIcon = CreateSprite("HUD Building Interaction Icon", new Color(0.9f, 0.86f, 1f, 1f), new Color(0.46f, 0.4f, 0.82f, 1f)),
                ScanLabelOffset = new Vector2(0f, 0.62f),
                ScanLabelColor = new Color(0.62f, 1f, 0.96f, 1f),
                ScanLabelFontSize = 4f,
                ScanLabelSortingOrder = 35,
                PlayerColliderRadius = 0.42f
            };
        }

        private static Tile[] NormalizeIndexedTiles(Tile[] configuredTiles, Tile[] fallbackTiles)
        {
            var normalized = new Tile[DualGridTerrain.TileCount];
            for (int i = 0; i < normalized.Length; i++)
            {
                Tile configured = configuredTiles != null && i < configuredTiles.Length ? configuredTiles[i] : null;
                Tile fallback = fallbackTiles != null && i < fallbackTiles.Length ? fallbackTiles[i] : null;
                normalized[i] = configured != null ? configured : fallback;
            }

            return normalized;
        }

        private static Tile[] ResolveDualGridTiles(
            MinebotPresentationArtSet artSet,
            DualGridTerrainProfile overrideProfile,
            TerrainRenderLayerId layerId,
            Tile[] fallbackTiles)
        {
            if (overrideProfile != null)
            {
                return overrideProfile.ResolveFamilyTiles(layerId, artSet != null ? ArtSetTilesForLayer(artSet, layerId) : null);
            }

            if (artSet != null)
            {
                return ArtSetTilesForLayer(artSet, layerId);
            }

            return fallbackTiles;
        }

        private static Tile[] ArtSetTilesForLayer(MinebotPresentationArtSet artSet, TerrainRenderLayerId layerId)
        {
            switch (layerId)
            {
                case TerrainRenderLayerId.Soil:
                    return artSet.SoilDualGridTiles;
                case TerrainRenderLayerId.Stone:
                    return artSet.StoneDualGridTiles;
                case TerrainRenderLayerId.HardRock:
                    return artSet.HardRockDualGridTiles;
                case TerrainRenderLayerId.UltraHard:
                    return artSet.UltraHardDualGridTiles;
                case TerrainRenderLayerId.Boundary:
                    return artSet.BoundaryDualGridTiles;
                default:
                    return artSet.FloorDualGridTiles;
            }
        }

        private static Tile[] ResolveLegacyContourTiles(Tile[] configuredTiles, Tile[] fallbackTiles)
        {
            var normalized = new Tile[DualGridContour.TileCount];
            for (int i = 0; i < normalized.Length; i++)
            {
                Tile configured = configuredTiles != null && i < configuredTiles.Length ? configuredTiles[i] : null;
                Tile fallback = fallbackTiles != null && i < fallbackTiles.Length ? fallbackTiles[i] : null;
                normalized[i] = configured != null ? configured : fallback;
            }

            return normalized;
        }

        private static Tile[] NormalizeDangerOutlineTiles(Tile[] configuredTiles, Tile[] fallbackTiles)
        {
            if (configuredTiles == null || configuredTiles.Length == 0)
            {
                return fallbackTiles;
            }

            var normalized = new Tile[configuredTiles.Length];
            for (int i = 0; i < configuredTiles.Length; i++)
            {
                Tile fallback = fallbackTiles[Mathf.Min(i, fallbackTiles.Length - 1)];
                normalized[i] = configuredTiles[i] != null ? configuredTiles[i] : fallback;
            }

            return normalized;
        }

        private static Tile TileForContourIndex(Tile[] contourTiles, int index)
        {
            if (contourTiles == null || contourTiles.Length == 0)
            {
                return null;
            }

            int safeIndex = Mathf.Clamp(index, 0, contourTiles.Length - 1);
            return contourTiles[safeIndex];
        }

        private static Tile CreateTile(string name, Color fill, Color border)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            tile.sprite = CreateSprite(name + " Sprite", fill, border);
            tile.colliderType = Tile.ColliderType.None;
            return tile;
        }

        private static Tile CreateDetailTile(string name, Color fill, Color accent)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            tile.sprite = CreateDetailSprite(name + " Sprite", fill, accent);
            tile.colliderType = Tile.ColliderType.None;
            return tile;
        }

        private static Tile[] CreateContourTileSet(string namePrefix, Color outlineColor, int thickness)
        {
            var tiles = new Tile[DualGridContour.TileCount];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = CreateContourTile($"{namePrefix} {i:X1}", i, outlineColor, thickness);
            }

            return tiles;
        }

        private static Tile CreateContourTile(string name, int contourIndex, Color outlineColor, int thickness)
        {
            const int size = 16;
            const int halfSize = size / 2;
            int safeThickness = Mathf.Clamp(thickness, 1, halfSize);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name + " Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            bool topLeft = (contourIndex & (1 << 3)) != 0;
            bool topRight = (contourIndex & (1 << 2)) != 0;
            bool bottomLeft = (contourIndex & (1 << 1)) != 0;
            bool bottomRight = (contourIndex & 1) != 0;
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isTop = y >= halfSize;
                    bool isLeft = x < halfSize;
                    int xWithin = isLeft ? x : x - halfSize;
                    int yWithin = isTop ? y - halfSize : y;

                    bool isFilled = isTop
                        ? (isLeft ? topLeft : topRight)
                        : (isLeft ? bottomLeft : bottomRight);

                    if (!isFilled)
                    {
                        texture.SetPixel(x, y, clear);
                        continue;
                    }

                    bool draw = false;
                    if (isTop && isLeft)
                    {
                        draw = (topLeft != topRight && xWithin >= halfSize - safeThickness)
                            || (topLeft != bottomLeft && yWithin < safeThickness);
                    }
                    else if (isTop)
                    {
                        draw = (topRight != topLeft && xWithin < safeThickness)
                            || (topRight != bottomRight && yWithin < safeThickness);
                    }
                    else if (isLeft)
                    {
                        draw = (bottomLeft != bottomRight && xWithin >= halfSize - safeThickness)
                            || (bottomLeft != topLeft && yWithin >= halfSize - safeThickness);
                    }
                    else
                    {
                        draw = (bottomRight != bottomLeft && xWithin < safeThickness)
                            || (bottomRight != topRight && yWithin >= halfSize - safeThickness);
                    }

                    texture.SetPixel(x, y, draw ? outlineColor : clear);
                }
            }

            texture.Apply(false, true);
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            tile.sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            tile.colliderType = Tile.ColliderType.None;
            return tile;
        }

        private static Tile CreateOutlineTile(string name, Color outlineColor, int thickness)
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name + " Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color clear = new Color(0f, 0f, 0f, 0f);
            int safeThickness = Mathf.Clamp(thickness, 1, size / 2);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isOutline = x < safeThickness
                        || y < safeThickness
                        || x >= size - safeThickness
                        || y >= size - safeThickness;
                    texture.SetPixel(x, y, isOutline ? outlineColor : clear);
                }
            }

            texture.Apply(false, true);
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            tile.sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            tile.colliderType = Tile.ColliderType.None;
            return tile;
        }

        private static BitmapGlyphFontDefinition CreateFallbackBitmapGlyphFont()
        {
            var definition = ScriptableObject.CreateInstance<BitmapGlyphFontDefinition>();
            var glyphs = new BitmapGlyphFontDefinition.GlyphDefinition[10];
            for (int i = 0; i < glyphs.Length; i++)
            {
                Sprite sprite = CreateDigitSprite($"Fallback Glyph {i}", (char)('0' + i), new Color(0.7f, 1f, 0.96f, 0.95f), new Color(0.2f, 0.85f, 1f, 0.28f));
                glyphs[i] = new BitmapGlyphFontDefinition.GlyphDefinition((char)('0' + i), sprite, 10f);
            }

            definition.Configure(null, new TextAsset("fallback bitmap glyph descriptor"), 16f, 4f, glyphs);
            return definition;
        }

        private static Texture2D CreateTexture(string name, int width, int height, Color fill, Color accent)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool edge = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                    texture.SetPixel(x, y, edge ? accent : fill);
                }
            }

            texture.Apply(false, true);
            return texture;
        }

        private static Sprite CreateDigitSprite(string name, char digit, Color core, Color glow)
        {
            const int width = 10;
            const int height = 16;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name + " Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            bool[] segments = SevenSegmentMaskFor(digit);
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool lit = IsSevenSegmentPixelLit(segments, x, y, width, height);
                    if (!lit)
                    {
                        texture.SetPixel(x, y, clear);
                        continue;
                    }

                    bool border = x <= 1 || x >= width - 2 || y <= 1 || y >= height - 2;
                    texture.SetPixel(x, y, border ? glow : core);
                }
            }

            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
            sprite.name = name;
            return sprite;
        }

        private static bool[] SevenSegmentMaskFor(char digit)
        {
            switch (digit)
            {
                case '0': return new[] { true, true, true, false, true, true, true };
                case '1': return new[] { false, false, true, false, false, true, false };
                case '2': return new[] { true, false, true, true, true, false, true };
                case '3': return new[] { true, false, true, true, false, true, true };
                case '4': return new[] { false, true, true, true, false, true, false };
                case '5': return new[] { true, true, false, true, false, true, true };
                case '6': return new[] { true, true, false, true, true, true, true };
                case '7': return new[] { true, false, true, false, false, true, false };
                case '8': return new[] { true, true, true, true, true, true, true };
                case '9': return new[] { true, true, true, true, false, true, true };
                default: return new[] { false, false, false, false, false, false, false };
            }
        }

        private static bool IsSevenSegmentPixelLit(bool[] segments, int x, int y, int width, int height)
        {
            bool top = segments[0] && y >= height - 3 && x >= 2 && x <= width - 3;
            bool upperLeft = segments[1] && x <= 2 && y >= height / 2 && y <= height - 4;
            bool upperRight = segments[2] && x >= width - 3 && y >= height / 2 && y <= height - 4;
            bool middle = segments[3] && y >= height / 2 - 1 && y <= height / 2 && x >= 2 && x <= width - 3;
            bool lowerLeft = segments[4] && x <= 2 && y >= 2 && y < height / 2 - 1;
            bool lowerRight = segments[5] && x >= width - 3 && y >= 2 && y < height / 2 - 1;
            bool bottom = segments[6] && y <= 2 && x >= 2 && x <= width - 3;
            return top || upperLeft || upperRight || middle || lowerLeft || lowerRight || bottom;
        }

        private static Sprite CreateSprite(string name, Color fill, Color border)
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name + " Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    texture.SetPixel(x, y, isBorder ? border : fill);
                }
            }

            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = name;
            return sprite;
        }

        private static Sprite CreateDetailSprite(string name, Color fill, Color accent)
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name + " Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool accentPixel = x > 1
                        && y > 1
                        && x < size - 2
                        && y < size - 2
                        && ((x * 5 + y * 3) % 11 == 0 || (x * 7 + y * 2) % 13 == 0);
                    texture.SetPixel(x, y, accentPixel ? accent : fill);
                }
            }

            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = name;
            return sprite;
        }
    }
}
