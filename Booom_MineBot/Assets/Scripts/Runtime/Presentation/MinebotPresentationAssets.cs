using System;
using System.Collections.Generic;
using Minebot.Progression;
using Minebot.UI;
using UnityEngine;
using UnityEngine.Tilemaps;
using TileBase = UnityEngine.Tilemaps.TileBase;

namespace Minebot.Presentation
{
    public sealed class MinebotPresentationAssets
    {
        public const string DefaultArtSetResourcePath = "Minebot/MinebotPresentationArtSet_Default";

        public TileBase EmptyTile { get; private set; }
        public TileBase SoilWallTile { get; private set; }
        public TileBase StoneWallTile { get; private set; }
        public TileBase HardRockWallTile { get; private set; }
        public TileBase UltraHardWallTile { get; private set; }
        public TileBase BoundaryTile { get; private set; }
        public TileBase[] FloorDualGridTiles { get; private set; }
        public TileBase[] SoilDualGridTiles { get; private set; }
        public TileBase[] StoneDualGridTiles { get; private set; }
        public TileBase[] HardRockDualGridTiles { get; private set; }
        public TileBase[] UltraHardDualGridTiles { get; private set; }
        public TileBase[] BoundaryDualGridTiles { get; private set; }
        public TileBase[] FogNearDualGridTiles { get; private set; }
        public TileBase[] FogDeepDualGridTiles { get; private set; }
        public TileBase DangerTile { get; private set; }
        public TileBase MarkerTile { get; private set; }
        public TileBase RepairStationTile { get; private set; }
        public TileBase RobotFactoryTile { get; private set; }
        public TileBase ScanHintTile { get; private set; }
        public TileBase BuildPreviewValidTile { get; private set; }
        public TileBase BuildPreviewInvalidTile { get; private set; }
        public TileBase[] WallContourTiles { get; private set; }
        public TileBase[] DangerContourTiles { get; private set; }
        public TileBase[] DangerOutlineTiles { get; private set; }
        public Texture2D HologramOverlayAtlas { get; private set; }
        public Texture2D BitmapGlyphAtlas { get; private set; }
        public TextAsset BitmapGlyphDescriptor { get; private set; }
        public BitmapGlyphFontDefinition BitmapGlyphFont { get; private set; }
        public TileBase SoilDetailTile { get; private set; }
        public TileBase StoneDetailTile { get; private set; }
        public TileBase HardRockDetailTile { get; private set; }
        public TileBase UltraHardDetailTile { get; private set; }
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
        public int MiningCrackSortingOrder { get; private set; }
        public Vector2 MiningCrackOffset { get; private set; }
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
        public bool DebugShowFog { get; private set; }
        public int FloorSortingOrder { get; private set; }
        public int WallSortingOrder { get; private set; }
        public int BoundarySortingOrder { get; private set; }
        public int FogDeepSortingOrder { get; private set; }
        public int FogNearSortingOrder { get; private set; }
        public int DangerSortingOrder { get; private set; }
        public int FacilitySortingOrder { get; private set; }
        public int MarkerSortingOrder { get; private set; }
        public int BuildPreviewSortingOrder { get; private set; }
        public int PlayerSortingOrder { get; private set; }
        public int RobotSortingOrder { get; private set; }
        public Vector3 FloorDisplayOffset { get; private set; }

        public static MinebotPresentationAssets Create(MinebotPresentationArtSet artSet)
        {
            return Create(artSet, null);
        }

        public static MinebotPresentationArtSet LoadDefaultArtSet()
        {
            return Resources.Load<MinebotPresentationArtSet>(DefaultArtSetResourcePath);
        }

        public static MinebotPresentationAssets Create(MinebotPresentationArtSet artSet, DualGridTerrainProfile dualGridProfileOverride)
        {
            MinebotPresentationArtSet resolvedArtSet = artSet ?? LoadDefaultArtSet();
            MinebotPresentationAssets missingDefaults = CreateMissingDefaults();
            if (resolvedArtSet == null && dualGridProfileOverride == null)
            {
                Debug.LogError("缺少默认 MinebotPresentationArtSet 资源，且未提供运行时 art set 覆盖。表现层不会再自动生成临时纹理。");
            }

            TileBase[] floorTiles = ResolveDualGridTiles(resolvedArtSet, dualGridProfileOverride, TerrainRenderLayerId.Floor, missingDefaults.FloorDualGridTiles);
            TileBase[] soilTiles = ResolveDualGridTiles(resolvedArtSet, dualGridProfileOverride, TerrainRenderLayerId.Soil, missingDefaults.SoilDualGridTiles);
            TileBase[] stoneTiles = ResolveDualGridTiles(resolvedArtSet, dualGridProfileOverride, TerrainRenderLayerId.Stone, missingDefaults.StoneDualGridTiles);
            TileBase[] hardRockTiles = ResolveDualGridTiles(resolvedArtSet, dualGridProfileOverride, TerrainRenderLayerId.HardRock, missingDefaults.HardRockDualGridTiles);
            TileBase[] ultraHardTiles = ResolveDualGridTiles(resolvedArtSet, dualGridProfileOverride, TerrainRenderLayerId.UltraHard, missingDefaults.UltraHardDualGridTiles);
            TileBase[] boundaryTiles = ResolveDualGridTiles(resolvedArtSet, dualGridProfileOverride, TerrainRenderLayerId.Boundary, missingDefaults.BoundaryDualGridTiles);
            TileBase[] fogNearTiles = resolvedArtSet != null ? resolvedArtSet.FogNearDualGridTiles : missingDefaults.FogNearDualGridTiles;
            TileBase[] fogDeepTiles = resolvedArtSet != null ? resolvedArtSet.FogDeepDualGridTiles : missingDefaults.FogDeepDualGridTiles;
            TileBase[] wallContours = ResolveLegacyContourTiles(
                dualGridProfileOverride != null ? dualGridProfileOverride.ResolveWallContourTiles(resolvedArtSet != null ? resolvedArtSet.WallContourTiles : null) : resolvedArtSet != null ? resolvedArtSet.WallContourTiles : null,
                missingDefaults.WallContourTiles);
            TileBase[] dangerContours = ResolveLegacyContourTiles(
                dualGridProfileOverride != null ? dualGridProfileOverride.ResolveDangerContourTiles(resolvedArtSet != null ? resolvedArtSet.DangerContourTiles : null) : resolvedArtSet != null ? resolvedArtSet.DangerContourTiles : null,
                missingDefaults.DangerContourTiles);
            DualGridTerrainLayoutSettings layoutSettings = dualGridProfileOverride != null
                ? dualGridProfileOverride.LayoutSettings
                : resolvedArtSet != null ? resolvedArtSet.TerrainLayoutSettings : DualGridTerrainLayoutSettings.CreateDefault();

            var assets = new MinebotPresentationAssets
            {
                EmptyTile = resolvedArtSet != null ? resolvedArtSet.EmptyTile : missingDefaults.EmptyTile,
                SoilWallTile = resolvedArtSet != null ? resolvedArtSet.SoilWallTile : missingDefaults.SoilWallTile,
                StoneWallTile = resolvedArtSet != null ? resolvedArtSet.StoneWallTile : missingDefaults.StoneWallTile,
                HardRockWallTile = resolvedArtSet != null ? resolvedArtSet.HardRockWallTile : missingDefaults.HardRockWallTile,
                UltraHardWallTile = resolvedArtSet != null ? resolvedArtSet.UltraHardWallTile : missingDefaults.UltraHardWallTile,
                BoundaryTile = resolvedArtSet != null ? resolvedArtSet.BoundaryTile : missingDefaults.BoundaryTile,
                FloorDualGridTiles = NormalizeIndexedTiles(floorTiles, missingDefaults.FloorDualGridTiles),
                SoilDualGridTiles = NormalizeIndexedTiles(soilTiles, missingDefaults.SoilDualGridTiles),
                StoneDualGridTiles = NormalizeIndexedTiles(stoneTiles, missingDefaults.StoneDualGridTiles),
                HardRockDualGridTiles = NormalizeIndexedTiles(hardRockTiles, missingDefaults.HardRockDualGridTiles),
                UltraHardDualGridTiles = NormalizeIndexedTiles(ultraHardTiles, missingDefaults.UltraHardDualGridTiles),
                BoundaryDualGridTiles = NormalizeIndexedTiles(boundaryTiles, missingDefaults.BoundaryDualGridTiles),
                FogNearDualGridTiles = NormalizeIndexedTiles(fogNearTiles, missingDefaults.FogNearDualGridTiles),
                FogDeepDualGridTiles = NormalizeIndexedTiles(fogDeepTiles, missingDefaults.FogDeepDualGridTiles),
                DangerTile = resolvedArtSet != null ? resolvedArtSet.DangerTile : missingDefaults.DangerTile,
                MarkerTile = resolvedArtSet != null ? resolvedArtSet.MarkerTile : missingDefaults.MarkerTile,
                RepairStationTile = resolvedArtSet != null ? resolvedArtSet.RepairStationTile : missingDefaults.RepairStationTile,
                RobotFactoryTile = resolvedArtSet != null ? resolvedArtSet.RobotFactoryTile : missingDefaults.RobotFactoryTile,
                ScanHintTile = resolvedArtSet != null ? resolvedArtSet.ScanHintTile : missingDefaults.ScanHintTile,
                BuildPreviewValidTile = resolvedArtSet != null ? resolvedArtSet.BuildPreviewValidTile : missingDefaults.BuildPreviewValidTile,
                BuildPreviewInvalidTile = resolvedArtSet != null ? resolvedArtSet.BuildPreviewInvalidTile : missingDefaults.BuildPreviewInvalidTile,
                WallContourTiles = wallContours,
                DangerContourTiles = dangerContours,
                DangerOutlineTiles = NormalizeDangerOutlineTiles(resolvedArtSet != null ? resolvedArtSet.DangerOutlineTiles : null, missingDefaults.DangerOutlineTiles),
                HologramOverlayAtlas = resolvedArtSet != null ? resolvedArtSet.HologramOverlayAtlas : missingDefaults.HologramOverlayAtlas,
                BitmapGlyphAtlas = resolvedArtSet != null ? resolvedArtSet.BitmapGlyphAtlas : missingDefaults.BitmapGlyphAtlas,
                BitmapGlyphDescriptor = resolvedArtSet != null ? resolvedArtSet.BitmapGlyphDescriptor : missingDefaults.BitmapGlyphDescriptor,
                BitmapGlyphFont = resolvedArtSet != null ? resolvedArtSet.BitmapGlyphFont : missingDefaults.BitmapGlyphFont,
                SoilDetailTile = resolvedArtSet != null ? resolvedArtSet.SoilDetailTile : missingDefaults.SoilDetailTile,
                StoneDetailTile = resolvedArtSet != null ? resolvedArtSet.StoneDetailTile : missingDefaults.StoneDetailTile,
                HardRockDetailTile = resolvedArtSet != null ? resolvedArtSet.HardRockDetailTile : missingDefaults.HardRockDetailTile,
                UltraHardDetailTile = resolvedArtSet != null ? resolvedArtSet.UltraHardDetailTile : missingDefaults.UltraHardDetailTile,
                PlayerSprite = resolvedArtSet != null ? resolvedArtSet.PlayerSprite : missingDefaults.PlayerSprite,
                RobotSprite = resolvedArtSet != null ? resolvedArtSet.RobotSprite : missingDefaults.RobotSprite,
                PlayerActorPrefab = resolvedArtSet != null ? resolvedArtSet.ActorResources.PlayerPrefab : missingDefaults.PlayerActorPrefab,
                HelperRobotPrefab = resolvedArtSet != null ? resolvedArtSet.ActorResources.HelperRobotPrefab : missingDefaults.HelperRobotPrefab,
                PlayerActorStates = resolvedArtSet != null ? resolvedArtSet.ActorResources.PlayerStates ?? missingDefaults.PlayerActorStates : missingDefaults.PlayerActorStates,
                HelperRobotStates = resolvedArtSet != null ? resolvedArtSet.ActorResources.HelperRobotStates ?? missingDefaults.HelperRobotStates : missingDefaults.HelperRobotStates,
                MetalPickupPrefab = resolvedArtSet != null ? resolvedArtSet.PickupResources.MetalPickupPrefab : missingDefaults.MetalPickupPrefab,
                EnergyPickupPrefab = resolvedArtSet != null ? resolvedArtSet.PickupResources.EnergyPickupPrefab : missingDefaults.EnergyPickupPrefab,
                ExperiencePickupPrefab = resolvedArtSet != null ? resolvedArtSet.PickupResources.ExperiencePickupPrefab : missingDefaults.ExperiencePickupPrefab,
                MetalPickupIcon = resolvedArtSet != null ? resolvedArtSet.PickupResources.MetalIcon : missingDefaults.MetalPickupIcon,
                EnergyPickupIcon = resolvedArtSet != null ? resolvedArtSet.PickupResources.EnergyIcon : missingDefaults.EnergyPickupIcon,
                ExperiencePickupIcon = resolvedArtSet != null ? resolvedArtSet.PickupResources.ExperienceIcon : missingDefaults.ExperiencePickupIcon,
                MiningCrackPrefab = resolvedArtSet != null ? resolvedArtSet.CellFxResources.MiningCrackPrefab : missingDefaults.MiningCrackPrefab,
                WallBreakPrefab = resolvedArtSet != null ? resolvedArtSet.CellFxResources.WallBreakPrefab : missingDefaults.WallBreakPrefab,
                ExplosionPrefab = resolvedArtSet != null ? resolvedArtSet.CellFxResources.ExplosionPrefab : missingDefaults.ExplosionPrefab,
                MiningCrackSequence = resolvedArtSet != null ? resolvedArtSet.CellFxResources.MiningCrackSequence : missingDefaults.MiningCrackSequence,
                WallBreakSequence = resolvedArtSet != null ? resolvedArtSet.CellFxResources.WallBreakSequence : missingDefaults.WallBreakSequence,
                ExplosionSequence = resolvedArtSet != null ? resolvedArtSet.CellFxResources.ExplosionSequence : missingDefaults.ExplosionSequence,
                MiningCrackSortingOrder = resolvedArtSet != null ? resolvedArtSet.CellFxResources.MiningCrackSortingOrder : 36,
                MiningCrackOffset = resolvedArtSet != null ? resolvedArtSet.CellFxResources.MiningCrackOffset : new Vector2(0f, 0.08f),
                HudPrefab = resolvedArtSet != null ? resolvedArtSet.HudResources.HudPrefab : missingDefaults.HudPrefab,
                HudPanelBackground = resolvedArtSet != null ? resolvedArtSet.HudResources.PanelBackground : missingDefaults.HudPanelBackground,
                HudStatusIcon = resolvedArtSet != null ? resolvedArtSet.HudResources.StatusIcon : missingDefaults.HudStatusIcon,
                HudInteractionIcon = resolvedArtSet != null ? resolvedArtSet.HudResources.InteractionIcon : missingDefaults.HudInteractionIcon,
                HudFeedbackIcon = resolvedArtSet != null ? resolvedArtSet.HudResources.FeedbackIcon : missingDefaults.HudFeedbackIcon,
                HudWarningIcon = resolvedArtSet != null ? resolvedArtSet.HudResources.WarningIcon : missingDefaults.HudWarningIcon,
                HudUpgradeIcon = resolvedArtSet != null ? resolvedArtSet.HudResources.UpgradeIcon : missingDefaults.HudUpgradeIcon,
                HudBuildIcon = resolvedArtSet != null ? resolvedArtSet.HudResources.BuildIcon : missingDefaults.HudBuildIcon,
                HudBuildingInteractionIcon = resolvedArtSet != null ? resolvedArtSet.HudResources.BuildingInteractionIcon : missingDefaults.HudBuildingInteractionIcon,
                ScanLabelOffset = resolvedArtSet != null ? resolvedArtSet.ScanLabelOffset : missingDefaults.ScanLabelOffset,
                ScanLabelColor = resolvedArtSet != null ? resolvedArtSet.ScanLabelColor : missingDefaults.ScanLabelColor,
                ScanLabelFontSize = resolvedArtSet != null ? resolvedArtSet.ScanLabelFontSize : missingDefaults.ScanLabelFontSize,
                ScanLabelSortingOrder = resolvedArtSet != null ? resolvedArtSet.ScanLabelSortingOrder : missingDefaults.ScanLabelSortingOrder,
                PlayerColliderRadius = resolvedArtSet != null ? resolvedArtSet.PlayerColliderRadius : missingDefaults.PlayerColliderRadius,
                TerrainLayoutSettings = layoutSettings,
                IsUsingConfiguredArtSet = resolvedArtSet != null || dualGridProfileOverride != null,
                DebugShowFog = resolvedArtSet?.DebugShowFog ?? true,
                FloorSortingOrder = resolvedArtSet?.FloorSortingOrder ?? 0,
                WallSortingOrder = resolvedArtSet?.WallSortingOrder ?? 10,
                BoundarySortingOrder = resolvedArtSet?.BoundarySortingOrder ?? 20,
                FogDeepSortingOrder = resolvedArtSet?.FogDeepSortingOrder ?? 8,
                FogNearSortingOrder = resolvedArtSet?.FogNearSortingOrder ?? 9,
                DangerSortingOrder = resolvedArtSet?.DangerSortingOrder ?? 10,
                FacilitySortingOrder = resolvedArtSet?.FacilitySortingOrder ?? 15,
                MarkerSortingOrder = resolvedArtSet?.MarkerSortingOrder ?? 20,
                BuildPreviewSortingOrder = resolvedArtSet?.BuildPreviewSortingOrder ?? 25,
                PlayerSortingOrder = resolvedArtSet?.PlayerSortingOrder ?? 40,
                RobotSortingOrder = resolvedArtSet?.RobotSortingOrder ?? 40,
                FloorDisplayOffset = resolvedArtSet?.FloorDisplayOffset ?? default
            };

            ReportMissingResources(assets, resolvedArtSet, dualGridProfileOverride);
            return assets;
        }

        public TileBase ResolveDangerOverlayTile(DangerOverlayGeometryKind geometryKind, int variant)
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

        public TileBase DangerOutlineTileForWave(int currentWave)
        {
            if (DangerOutlineTiles == null || DangerOutlineTiles.Length == 0)
            {
                return DangerTile;
            }

            int index = Mathf.Clamp(Mathf.Max(0, currentWave), 0, DangerOutlineTiles.Length - 1);
            return DangerOutlineTiles[index] ?? DangerTile;
        }

        public TileBase WallContourTileForIndex(int index)
        {
            return TileForContourIndex(WallContourTiles, index);
        }

        public TileBase DangerContourTileForIndex(int index)
        {
            return TileForContourIndex(DangerContourTiles, index);
        }

        public TileBase DualGridTerrainTileFor(TerrainRenderLayerId layerId, int index)
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

        public TileBase FogNearDualGridTileForIndex(int index)
        {
            return TileForContourIndex(FogNearDualGridTiles, index);
        }

        public TileBase FogDeepDualGridTileForIndex(int index)
        {
            return TileForContourIndex(FogDeepDualGridTiles, index);
        }

        public TileBase WallBaseTileForHardness(Minebot.GridMining.HardnessTier hardness)
        {
            switch (hardness)
            {
                case Minebot.GridMining.HardnessTier.Stone:
                    return StoneDetailTile ?? StoneWallTile;
                case Minebot.GridMining.HardnessTier.HardRock:
                    return HardRockDetailTile ?? HardRockWallTile;
                case Minebot.GridMining.HardnessTier.UltraHard:
                    return UltraHardDetailTile ?? UltraHardWallTile;
                default:
                    return SoilDetailTile ?? SoilWallTile;
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

        private static MinebotPresentationAssets CreateMissingDefaults()
        {
            return new MinebotPresentationAssets
            {
                FloorDualGridTiles = CreateEmptyIndexedTiles(DualGridTerrain.TileCount),
                SoilDualGridTiles = CreateEmptyIndexedTiles(DualGridTerrain.TileCount),
                StoneDualGridTiles = CreateEmptyIndexedTiles(DualGridTerrain.TileCount),
                HardRockDualGridTiles = CreateEmptyIndexedTiles(DualGridTerrain.TileCount),
                UltraHardDualGridTiles = CreateEmptyIndexedTiles(DualGridTerrain.TileCount),
                BoundaryDualGridTiles = CreateEmptyIndexedTiles(DualGridTerrain.TileCount),
                FogNearDualGridTiles = CreateEmptyIndexedTiles(DualGridFog.TileCount),
                FogDeepDualGridTiles = CreateEmptyIndexedTiles(DualGridFog.TileCount),
                WallContourTiles = CreateEmptyIndexedTiles(DualGridContour.TileCount),
                DangerContourTiles = CreateEmptyIndexedTiles(DualGridContour.TileCount),
                DangerOutlineTiles = Array.Empty<TileBase>(),
                PlayerActorStates = new ActorStateSequenceSet(),
                HelperRobotStates = new ActorStateSequenceSet(),
                ScanLabelOffset = new Vector2(0f, 0.62f),
                ScanLabelColor = new Color(0.62f, 1f, 0.96f, 1f),
                ScanLabelFontSize = 4f,
                ScanLabelSortingOrder = 35,
                PlayerColliderRadius = 0.42f
            };
        }

        private static TileBase[] NormalizeIndexedTiles(TileBase[] configuredTiles, TileBase[] fallbackTiles)
        {
            var normalized = new TileBase[DualGridTerrain.TileCount];
            for (int i = 0; i < normalized.Length; i++)
            {
                TileBase configured = configuredTiles != null && i < configuredTiles.Length ? configuredTiles[i] : null;
                TileBase fallback = fallbackTiles != null && i < fallbackTiles.Length ? fallbackTiles[i] : null;
                normalized[i] = configured ?? fallback;
            }

            return normalized;
        }

        private static TileBase[] ResolveDualGridTiles(
            MinebotPresentationArtSet artSet,
            DualGridTerrainProfile overrideProfile,
            TerrainRenderLayerId layerId,
            TileBase[] fallbackTiles)
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

        private static TileBase[] ArtSetTilesForLayer(MinebotPresentationArtSet artSet, TerrainRenderLayerId layerId)
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

        private static TileBase[] ResolveLegacyContourTiles(TileBase[] configuredTiles, TileBase[] fallbackTiles)
        {
            var normalized = new TileBase[DualGridContour.TileCount];
            for (int i = 0; i < normalized.Length; i++)
            {
                TileBase configured = configuredTiles != null && i < configuredTiles.Length ? configuredTiles[i] : null;
                TileBase fallback = fallbackTiles != null && i < fallbackTiles.Length ? fallbackTiles[i] : null;
                normalized[i] = configured ?? fallback;
            }

            return normalized;
        }

        private static TileBase[] NormalizeDangerOutlineTiles(TileBase[] configuredTiles, TileBase[] fallbackTiles)
        {
            if (configuredTiles == null || configuredTiles.Length == 0)
            {
                return fallbackTiles;
            }

            var normalized = new TileBase[configuredTiles.Length];
            for (int i = 0; i < configuredTiles.Length; i++)
            {
                TileBase fallback = fallbackTiles != null && fallbackTiles.Length > 0
                    ? fallbackTiles[Mathf.Min(i, fallbackTiles.Length - 1)]
                    : null;
                normalized[i] = configuredTiles[i] ?? fallback;
            }

            return normalized;
        }

        private static TileBase TileForContourIndex(TileBase[] contourTiles, int index)
        {
            if (contourTiles == null || contourTiles.Length == 0)
            {
                return null;
            }

            int safeIndex = Mathf.Clamp(index, 0, contourTiles.Length - 1);
            return contourTiles[safeIndex];
        }

        private static TileBase[] CreateEmptyIndexedTiles(int count)
        {
            return count <= 0 ? Array.Empty<TileBase>() : new TileBase[count];
        }

        private static void ReportMissingResources(
            MinebotPresentationAssets assets,
            MinebotPresentationArtSet artSet,
            DualGridTerrainProfile profileOverride)
        {
            var missing = new List<string>();
            bool terrainOnlyOverride = artSet == null && profileOverride != null;
            if (artSet == null && profileOverride == null)
            {
                missing.Add("默认 MinebotPresentationArtSet");
            }

            AppendMissingIndexedTiles(missing, "DG Floor", assets.FloorDualGridTiles);
            AppendMissingIndexedTiles(missing, "DG Soil", assets.SoilDualGridTiles);
            AppendMissingIndexedTiles(missing, "DG Stone", assets.StoneDualGridTiles);
            AppendMissingIndexedTiles(missing, "DG HardRock", assets.HardRockDualGridTiles);
            AppendMissingIndexedTiles(missing, "DG UltraHard", assets.UltraHardDualGridTiles);
            AppendMissingIndexedTiles(missing, "DG Boundary", assets.BoundaryDualGridTiles);
            if (!terrainOnlyOverride)
            {
                AppendMissingIndexedTiles(missing, "DG Fog Near", assets.FogNearDualGridTiles);
                AppendMissingIndexedTiles(missing, "DG Fog Deep", assets.FogDeepDualGridTiles);
                if (assets.BitmapGlyphFont == null)
                {
                    missing.Add("BitmapGlyphFont");
                }
            }

            if (missing.Count == 0)
            {
                return;
            }

            Debug.LogWarning($"Minebot 表现资源存在缺失：{string.Join("、", missing)}。运行时不会再自动生成临时纹理，请先补齐离线资源。");
        }

        private static void AppendMissingIndexedTiles(ICollection<string> missing, string label, TileBase[] tiles)
        {
            if (!HasAnyTile(tiles))
            {
                missing.Add(label);
            }
        }

        private static bool HasAnyTile(TileBase[] tiles)
        {
            if (tiles == null)
            {
                return false;
            }

            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
