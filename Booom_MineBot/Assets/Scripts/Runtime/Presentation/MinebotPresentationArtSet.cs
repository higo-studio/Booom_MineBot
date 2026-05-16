using System;
using System.Collections.Generic;
using Minebot.GridMining;
using Minebot.UI;
using UnityEngine;
using UnityEngine.Tilemaps;
using TileBase = UnityEngine.Tilemaps.TileBase;

namespace Minebot.Presentation
{
    [CreateAssetMenu(menuName = "Minebot/表现/表现美术集")]
    public sealed class MinebotPresentationArtSet : ScriptableObject
    {
        [Header("地形")]
        [SerializeField]
        [InspectorLabel("双网格地形配置")]
        [HideInInspector]
        private DualGridTerrainProfile dualGridTerrainProfile;

        [SerializeField]
        [InspectorLabel("布局设置")]
        private DualGridTerrainLayoutSettings layoutSettings = default;

        [SerializeField]
        [InspectorLabel("地形族配置")]
        private DualGridTerrainFamilyProfile[] families = Array.Empty<DualGridTerrainFamilyProfile>();

        [SerializeField]
        [InspectorLabel("旧拓扑资源")]
        private DualGridLegacyTopologyAssets legacyTopology = new DualGridLegacyTopologyAssets();

        [SerializeField]
        [InspectorLabel("允许回退到旧双网格数组")]
        [HideInInspector]
        private bool allowLegacyArtSetFallback = false;

        [SerializeField]
        [HideInInspector]
        private TileBase[] floorDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        [HideInInspector]
        private TileBase[] soilDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        [HideInInspector]
        private TileBase[] stoneDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        [HideInInspector]
        private TileBase[] hardRockDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        [HideInInspector]
        private TileBase[] ultraHardDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        [HideInInspector]
        private TileBase[] boundaryDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        [InspectorLabel("近雾双网格瓦片")]
        private TileBase[] fogNearDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        [InspectorLabel("深雾双网格瓦片")]
        private TileBase[] fogDeepDualGridTiles = Array.Empty<TileBase>();

        [Header("覆盖层")]
        [SerializeField]
        [InspectorLabel("危险覆盖瓦片")]
        private TileBase dangerTile;

        [SerializeField]
        [InspectorLabel("标记瓦片")]
        private TileBase markerTile;

        [SerializeField]
        [InspectorLabel("标记悬停预览瓦片")]
        private TileBase markerHoverTile;

        [SerializeField]
        [InspectorLabel("探测提示瓦片")]
        private TileBase scanHintTile;

        [SerializeField]
        [InspectorLabel("危险描边瓦片")]
        private TileBase[] dangerOutlineTiles = Array.Empty<TileBase>();

        [SerializeField]
        [HideInInspector]
        private TileBase[] wallContourTiles = Array.Empty<TileBase>();

        [SerializeField]
        [HideInInspector]
        private TileBase[] dangerContourTiles = Array.Empty<TileBase>();

        [SerializeField]
        [InspectorLabel("土层细节瓦片")]
        private TileBase soilDetailTile;

        [SerializeField]
        [InspectorLabel("石层细节瓦片")]
        private TileBase stoneDetailTile;

        [SerializeField]
        [InspectorLabel("硬岩细节瓦片")]
        private TileBase hardRockDetailTile;

        [SerializeField]
        [InspectorLabel("超硬岩细节瓦片")]
        private TileBase ultraHardDetailTile;

        [SerializeField]
        [InspectorLabel("建造有效预览瓦片")]
        private TileBase buildPreviewValidTile;

        [SerializeField]
        [InspectorLabel("建造无效预览瓦片")]
        private TileBase buildPreviewInvalidTile;

        [SerializeField]
        [InspectorLabel("Ground Tile")]
        private TileBase groundTile;

        [Header("全息覆盖层")]
        [SerializeField]
        [InspectorLabel("全息覆盖图集")]
        private Texture2D hologramOverlayAtlas;

        [SerializeField]
        [InspectorLabel("位图字形图集")]
        private Texture2D bitmapGlyphAtlas;

        [SerializeField]
        [InspectorLabel("位图字形描述文件")]
        private TextAsset bitmapGlyphDescriptor;

        [SerializeField]
        [InspectorLabel("位图字形字体")]
        private BitmapGlyphFontDefinition bitmapGlyphFont;

        [SerializeField]
        [InspectorLabel("探测标签偏移")]
        private Vector2 scanLabelOffset = new Vector2(0f, 0.62f);

        [SerializeField]
        [InspectorLabel("探测标签颜色")]
        private Color scanLabelColor = new Color(0.62f, 1f, 0.96f, 1f);

        [SerializeField]
        [InspectorLabel("探测标签字号")]
        private float scanLabelFontSize = 4f;

        [SerializeField]
        [InspectorLabel("探测标签排序层级")]
        private int scanLabelSortingOrder = 35;

        [Header("设施")]
        [SerializeField]
        [InspectorLabel("维修站瓦片")]
        private TileBase repairStationTile;

        [SerializeField]
        [InspectorLabel("机器人工厂瓦片")]
        private TileBase robotFactoryTile;

        [Header("角色")]
        [SerializeField]
        [InspectorLabel("玩家精灵")]
        private Sprite playerSprite;

        [SerializeField]
        [InspectorLabel("机器人精灵")]
        private Sprite robotSprite;

        [SerializeField]
        [InspectorLabel("玩家碰撞半径")]
        private float playerColliderRadius = 0.42f;

        [Header("预制体玩法美术")]
        [SerializeField]
        [InspectorLabel("角色资源")]
        private MinebotPresentationActorResources actorResources = new MinebotPresentationActorResources();

        [SerializeField]
        [InspectorLabel("拾取物资源")]
        private MinebotPresentationPickupResources pickupResources = new MinebotPresentationPickupResources();

        [SerializeField]
        [InspectorLabel("格子特效资源")]
        private MinebotPresentationCellFxResources cellFxResources = new MinebotPresentationCellFxResources();

        [SerializeField]
        [InspectorLabel("界面资源")]
        private MinebotPresentationHudResources hudResources = new MinebotPresentationHudResources();

        [Header("Debug")]
        [SerializeField]
        [InspectorLabel("显示雾层")]
        private bool debugShowFog = true;

        [Header("地形层排序")]
        [SerializeField]
        [InspectorLabel("DG Floor 排序层级")]
        private int floorSortingOrder = 0;

        [SerializeField]
        [InspectorLabel("DG Wall 排序层级")]
        private int wallSortingOrder = 10;

        [SerializeField]
        [InspectorLabel("DG Boundary 排序层级")]
        private int boundarySortingOrder = 20;

        [Header("地形层偏移")]
        [SerializeField]
        [InspectorLabel("地板偏移（与全局偏移叠加）")]
        private Vector3 floorDisplayOffset = default;

        [Header("覆盖层排序")]
        [SerializeField]
        [InspectorLabel("Fog Deep 排序层级")]
        private int fogDeepSortingOrder = 8;

        [SerializeField]
        [InspectorLabel("Fog Near 排序层级")]
        private int fogNearSortingOrder = 9;

        [SerializeField]
        [InspectorLabel("Danger 排序层级")]
        private int dangerSortingOrder = 10;

        [SerializeField]
        [InspectorLabel("Facility 排序层级")]
        private int facilitySortingOrder = 15;

        [SerializeField]
        [InspectorLabel("Marker 排序层级")]
        private int markerSortingOrder = 20;

        [SerializeField]
        [InspectorLabel("BuildPreview 排序层级")]
        private int buildPreviewSortingOrder = 25;

        [Header("角色排序")]
        [SerializeField]
        [InspectorLabel("玩家排序层级")]
        private int playerSortingOrder = 40;

        [SerializeField]
        [InspectorLabel("从属机器人排序层级")]
        private int robotSortingOrder = 40;

        public bool HasInlineDualGridConfiguration => families != null && families.Length > 0;
        public DualGridTerrainLayoutSettings TerrainLayoutSettings => HasInlineDualGridConfiguration
            ? ResolveConfiguredLayoutSettings(layoutSettings)
            : dualGridTerrainProfile != null ? dualGridTerrainProfile.LayoutSettings : DualGridTerrainLayoutSettings.CreateDefault();
        public TileBase[] FloorDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.Floor, floorDualGridTiles);
        public TileBase[] SoilDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.Soil, soilDualGridTiles);
        public TileBase[] StoneDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.Stone, stoneDualGridTiles);
        public TileBase[] HardRockDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.HardRock, hardRockDualGridTiles);
        public TileBase[] UltraHardDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.UltraHard, ultraHardDualGridTiles);
        public TileBase[] BoundaryDualGridTiles => ResolveDualGridTiles(TerrainRenderLayerId.Boundary, boundaryDualGridTiles);
        public TileBase[] FogNearDualGridTiles => ResolveFogDualGridTiles(fogNearDualGridTiles);
        public TileBase[] FogDeepDualGridTiles => ResolveFogDualGridTiles(fogDeepDualGridTiles);
        public TileBase DangerTile => dangerTile;
        public TileBase MarkerTile => markerTile;
        public TileBase MarkerHoverTile => markerHoverTile;
        public TileBase ScanHintTile => scanHintTile;
        public TileBase[] DangerOutlineTiles => dangerOutlineTiles ?? Array.Empty<TileBase>();
        public TileBase[] WallContourTiles => ResolveWallContourTiles();
        public TileBase[] DangerContourTiles => ResolveDangerContourTiles();
        public TileBase SoilDetailTile => soilDetailTile;
        public TileBase StoneDetailTile => stoneDetailTile;
        public TileBase HardRockDetailTile => hardRockDetailTile;
        public TileBase UltraHardDetailTile => ultraHardDetailTile;
        public TileBase BuildPreviewValidTile => buildPreviewValidTile;
        public TileBase BuildPreviewInvalidTile => buildPreviewInvalidTile;
        public TileBase GroundTile => groundTile;
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
        public bool DebugShowFog => debugShowFog;
        public int FloorSortingOrder => floorSortingOrder;
        public int WallSortingOrder => wallSortingOrder;
        public int BoundarySortingOrder => boundarySortingOrder;
        public int FogDeepSortingOrder => fogDeepSortingOrder;
        public int FogNearSortingOrder => fogNearSortingOrder;
        public int DangerSortingOrder => dangerSortingOrder;
        public int FacilitySortingOrder => facilitySortingOrder;
        public int MarkerSortingOrder => markerSortingOrder;
        public int BuildPreviewSortingOrder => buildPreviewSortingOrder;
        public int PlayerSortingOrder => playerSortingOrder;
        public int RobotSortingOrder => robotSortingOrder;
        public Vector3 FloorDisplayOffset => floorDisplayOffset;

        private TileBase[] ResolveDualGridTiles(TerrainRenderLayerId layerId, TileBase[] configuredTiles)
        {
            if (HasInlineDualGridConfiguration)
            {
                return ResolveInlineFamilyTiles(layerId, configuredTiles);
            }

            if (dualGridTerrainProfile != null)
            {
                return dualGridTerrainProfile.ResolveFamilyTiles(layerId, configuredTiles);
            }

            if (configuredTiles != null && configuredTiles.Length > 0)
            {
                return configuredTiles;
            }

            return Array.Empty<TileBase>();
        }

        private TileBase[] ResolveFogDualGridTiles(TileBase[] configuredTiles)
        {
            if (configuredTiles != null && configuredTiles.Length > 0)
            {
                return configuredTiles;
            }

            return Array.Empty<TileBase>();
        }

        private TileBase[] ResolveWallContourTiles()
        {
            if (HasInlineDualGridConfiguration)
            {
                return ResolveInlineLegacyTiles(legacyTopology != null ? legacyTopology.WallContourTiles : null, wallContourTiles);
            }

            if (dualGridTerrainProfile != null)
            {
                return dualGridTerrainProfile.ResolveWallContourTiles(wallContourTiles);
            }

            return wallContourTiles ?? Array.Empty<TileBase>();
        }

        private TileBase[] ResolveDangerContourTiles()
        {
            if (HasInlineDualGridConfiguration)
            {
                return ResolveInlineLegacyTiles(legacyTopology != null ? legacyTopology.DangerContourTiles : null, dangerContourTiles);
            }

            if (dualGridTerrainProfile != null)
            {
                return dualGridTerrainProfile.ResolveDangerContourTiles(dangerContourTiles);
            }

            return dangerContourTiles ?? Array.Empty<TileBase>();
        }

        public IEnumerable<string> GetDualGridValidationIssues()
        {
            if (HasInlineDualGridConfiguration)
            {
                foreach (DualGridTerrainFamilyProfile family in EnsureInlineFamilies())
                {
                    if (family == null)
                    {
                        yield return "双网格配置中存在缺失的地形族条目。";
                        continue;
                    }

                    foreach (string issue in family.GetValidationIssues())
                    {
                        yield return issue;
                    }
                }

                if (dualGridTerrainProfile != null)
                {
                    yield return "仍引用旧的默认双网格 profile，请执行双网格迁移清理。";
                }

                if (allowLegacyArtSetFallback)
                {
                    yield return "仍允许回退到旧双网格数组，请执行双网格迁移清理。";
                }

                if (HasAnyLegacyTileReference(floorDualGridTiles)
                    || HasAnyLegacyTileReference(soilDualGridTiles)
                    || HasAnyLegacyTileReference(stoneDualGridTiles)
                    || HasAnyLegacyTileReference(hardRockDualGridTiles)
                    || HasAnyLegacyTileReference(ultraHardDualGridTiles)
                    || HasAnyLegacyTileReference(boundaryDualGridTiles)
                    || HasAnyLegacyTileReference(wallContourTiles)
                    || HasAnyLegacyTileReference(dangerContourTiles))
                {
                    yield return "仍保留旧双网格数组引用，请执行双网格迁移清理。";
                }

                yield break;
            }

            if (dualGridTerrainProfile != null)
            {
                foreach (string issue in dualGridTerrainProfile.GetValidationIssues())
                {
                    yield return issue;
                }
            }
        }

        private TileBase[] ResolveInlineFamilyTiles(TerrainRenderLayerId layerId, TileBase[] legacyTiles)
        {
            DualGridTerrainFamilyProfile family = FindInlineFamily(layerId);
            if (family == null)
            {
                return ResolveInlineFallbackTiles(layerId, legacyTiles);
            }

            if (!family.Enabled)
            {
                return new TileBase[DualGridTerrain.TileCount];
            }

            Tile[] resolved = family.ResolveTiles(allowLegacyArtSetFallback ? legacyTiles : null);
            return HasAnyResolvedTile(resolved) ? resolved : ResolveInlineFallbackTiles(layerId, legacyTiles);
        }

        private TileBase[] ResolveInlineFallbackTiles(TerrainRenderLayerId layerId, TileBase[] legacyTiles)
        {
            if (allowLegacyArtSetFallback && legacyTiles != null && legacyTiles.Length > 0)
            {
                var normalized = new TileBase[DualGridTerrain.TileCount];
                for (int i = 0; i < normalized.Length; i++)
                {
                    normalized[i] = i < legacyTiles.Length ? legacyTiles[i] : null;
                }

                return normalized;
            }

            Debug.LogWarning($"Dual Grid 地形族 {layerId} 缺少离线瓦片资源。运行时不会再自动生成 fallback。");
            return new TileBase[DualGridTerrain.TileCount];
        }

        private DualGridTerrainFamilyProfile[] EnsureInlineFamilies()
        {
            if (families != null && families.Length == DualGridTerrain.MaterialFamilies.Length)
            {
                return families;
            }

            DualGridTerrainFamilyProfile[] legacyFamilies = families;
            families = DualGridTerrainFamilyProfile.CreateDefaults();
            MigrateLegacyFamilies(legacyFamilies, families);
            return families;
        }

        private DualGridTerrainFamilyProfile FindInlineFamily(TerrainRenderLayerId layerId)
        {
            DualGridTerrainFamilyProfile[] configuredFamilies = EnsureInlineFamilies();
            for (int i = 0; i < configuredFamilies.Length; i++)
            {
                if (configuredFamilies[i] != null && configuredFamilies[i].LayerId == layerId)
                {
                    return configuredFamilies[i];
                }
            }

            return null;
        }

        private static TileBase[] ResolveInlineLegacyTiles(Tile[] configuredTiles, TileBase[] fallbackTiles)
        {
            TileBase[] preferred = configuredTiles != null && configuredTiles.Length > 0 ? configuredTiles : fallbackTiles;
            if (preferred == null || preferred.Length == 0)
            {
                return Array.Empty<TileBase>();
            }

            var normalized = new TileBase[DualGridContour.TileCount];
            for (int i = 0; i < normalized.Length; i++)
            {
                normalized[i] = i < preferred.Length ? preferred[i] : null;
            }

            return normalized;
        }

        private static bool HasAnyResolvedTile(Tile[] tiles)
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

        private static bool HasAnyLegacyTileReference(TileBase[] tiles)
        {
            return tiles != null && tiles.Length > 0;
        }

        private static DualGridTerrainLayoutSettings ResolveConfiguredLayoutSettings(DualGridTerrainLayoutSettings settings)
        {
            return settings.DisplayOffset == default && settings.SortingOrderStep == 0
                ? DualGridTerrainLayoutSettings.CreateDefault()
                : settings;
        }

        private static void MigrateLegacyFamilies(DualGridTerrainFamilyProfile[] source, DualGridTerrainFamilyProfile[] destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            CopyFamilyIfPresent(source, destination, TerrainRenderLayerId.Floor, TerrainRenderLayerId.Floor);
            CopyFamilyIfPresent(source, destination, TerrainRenderLayerId.Boundary, TerrainRenderLayerId.Boundary);

            DualGridTerrainFamilyProfile wallSource = FindFamilyIn(source, TerrainRenderLayerId.Soil)
                ?? FindFamilyIn(source, TerrainRenderLayerId.Stone)
                ?? FindFamilyIn(source, TerrainRenderLayerId.HardRock)
                ?? FindFamilyIn(source, TerrainRenderLayerId.UltraHard);
            if (wallSource != null)
            {
                CopyFromWallSourceIfMissing(destination, TerrainRenderLayerId.Soil, wallSource);
                CopyFromWallSourceIfMissing(destination, TerrainRenderLayerId.Stone, wallSource);
                CopyFromWallSourceIfMissing(destination, TerrainRenderLayerId.HardRock, wallSource);
                CopyFromWallSourceIfMissing(destination, TerrainRenderLayerId.UltraHard, wallSource);
            }
        }

        private static void CopyFamilyIfPresent(
            DualGridTerrainFamilyProfile[] source,
            DualGridTerrainFamilyProfile[] destination,
            TerrainRenderLayerId sourceLayerId,
            TerrainRenderLayerId destinationLayerId)
        {
            DualGridTerrainFamilyProfile sourceFamily = FindFamilyIn(source, sourceLayerId);
            DualGridTerrainFamilyProfile destinationFamily = FindFamilyIn(destination, destinationLayerId);
            if (sourceFamily == null || destinationFamily == null)
            {
                return;
            }

            destinationFamily.CopyFrom(sourceFamily);
        }

        private static DualGridTerrainFamilyProfile FindFamilyIn(DualGridTerrainFamilyProfile[] source, TerrainRenderLayerId layerId)
        {
            if (source == null)
            {
                return null;
            }

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != null && source[i].LayerId == layerId)
                {
                    return source[i];
                }
            }

            return null;
        }

        private static void CopyFromWallSourceIfMissing(
            DualGridTerrainFamilyProfile[] destination,
            TerrainRenderLayerId destinationLayerId,
            DualGridTerrainFamilyProfile wallSource)
        {
            DualGridTerrainFamilyProfile destinationFamily = FindFamilyIn(destination, destinationLayerId);
            if (destinationFamily == null)
            {
                return;
            }

            destinationFamily.CopyFrom(wallSource);
            destinationFamily.SetLayerId(destinationLayerId);
        }

#if UNITY_EDITOR
        public bool NormalizeDualGridConfiguration()
        {
            bool changed = false;
            if (HasInlineDualGridConfiguration)
            {
                families = EnsureInlineFamilies();
            }
            else
            {
                ConfigureInlineDualGridConfiguration(
                    dualGridTerrainProfile,
                    floorDualGridTiles,
                    soilDualGridTiles,
                    stoneDualGridTiles,
                    hardRockDualGridTiles,
                    ultraHardDualGridTiles,
                    boundaryDualGridTiles,
                    wallContourTiles,
                    dangerContourTiles,
                    clearLegacyProfileReference: true);
                changed = true;
            }

            if (NormalizeInlineFamilies())
            {
                changed = true;
            }

            if (NormalizeInlineLegacyTopology())
            {
                changed = true;
            }

            if (allowLegacyArtSetFallback)
            {
                allowLegacyArtSetFallback = false;
                changed = true;
            }

            if (ClearLegacyDualGridReferences())
            {
                changed = true;
            }

            return changed;
        }
#endif

        public void Configure(
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
            int configuredScanLabelSortingOrder = 35,
            TileBase configuredGroundTile = null)
        {
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
            groundTile = configuredGroundTile;
            dangerOutlineTiles = dangerOutline ?? Array.Empty<TileBase>();
            fogNearDualGridTiles = fogNearDualGrid ?? Array.Empty<TileBase>();
            fogDeepDualGridTiles = fogDeepDualGrid ?? Array.Empty<TileBase>();
            bitmapGlyphFont = configuredBitmapGlyphFont;
            bitmapGlyphAtlas = configuredBitmapGlyphAtlas;
            bitmapGlyphDescriptor = configuredBitmapGlyphDescriptor;
            hologramOverlayAtlas = configuredHologramOverlayAtlas;
            scanLabelOffset = configuredScanLabelOffset ?? new Vector2(0f, 0.62f);
            scanLabelColor = configuredScanLabelColor ?? new Color(0.62f, 1f, 0.96f, 1f);
            scanLabelFontSize = Mathf.Max(0.5f, configuredScanLabelFontSize);
            scanLabelSortingOrder = Mathf.Clamp(configuredScanLabelSortingOrder, 1, 100);

            ConfigureInlineDualGridConfiguration(
                configuredDualGridTerrainProfile,
                floorDualGrid,
                soilDualGrid,
                stoneDualGrid,
                hardRockDualGrid,
                ultraHardDualGrid,
                boundaryDualGrid,
                wallContour,
                dangerContour,
                clearLegacyProfileReference: true);
#if UNITY_EDITOR
            NormalizeInlineFamilies();
#endif
            allowLegacyArtSetFallback = false;
            ClearLegacyDualGridReferences();
        }

        private void ConfigureInlineDualGridConfiguration(
            DualGridTerrainProfile sourceProfile,
            TileBase[] floorDualGrid,
            TileBase[] soilDualGrid,
            TileBase[] stoneDualGrid,
            TileBase[] hardRockDualGrid,
            TileBase[] ultraHardDualGrid,
            TileBase[] boundaryDualGrid,
            TileBase[] wallContours,
            TileBase[] dangerContours,
            bool clearLegacyProfileReference)
        {
            if (sourceProfile != null)
            {
                layoutSettings = sourceProfile.LayoutSettings;
                allowLegacyArtSetFallback = false;
                CopyInlineFamiliesFrom(sourceProfile.Families);
                CopyInlineLegacyTopologyFrom(sourceProfile.LegacyTopology);
            }
            else
            {
                layoutSettings = DualGridTerrainLayoutSettings.CreateDefault();
                allowLegacyArtSetFallback = false;
                families = DualGridTerrainFamilyProfile.CreateDefaults();
                ConfigureInlineFamilyTiles(TerrainRenderLayerId.Floor, floorDualGrid);
                ConfigureInlineFamilyTiles(TerrainRenderLayerId.Soil, soilDualGrid);
                ConfigureInlineFamilyTiles(TerrainRenderLayerId.Stone, stoneDualGrid);
                ConfigureInlineFamilyTiles(TerrainRenderLayerId.HardRock, hardRockDualGrid);
                ConfigureInlineFamilyTiles(TerrainRenderLayerId.UltraHard, ultraHardDualGrid);
                ConfigureInlineFamilyTiles(TerrainRenderLayerId.Boundary, boundaryDualGrid);
                ConfigureInlineLegacyTopology(wallContours, dangerContours);
            }

            if (clearLegacyProfileReference)
            {
                dualGridTerrainProfile = null;
            }
        }

        private void CopyInlineFamiliesFrom(DualGridTerrainFamilyProfile[] sourceFamilies)
        {
            DualGridTerrainFamilyProfile[] configuredFamilies = DualGridTerrainFamilyProfile.CreateDefaults();
            for (int i = 0; i < configuredFamilies.Length; i++)
            {
                TerrainRenderLayerId targetLayerId = configuredFamilies[i].LayerId;
                DualGridTerrainFamilyProfile source = FindFamilyIn(sourceFamilies, targetLayerId);
                if (source != null)
                {
                    configuredFamilies[i].CopyFrom(source);
                    configuredFamilies[i].SetLayerId(targetLayerId);
                }
            }

            families = configuredFamilies;
        }

        private void ConfigureInlineFamilyTiles(TerrainRenderLayerId layerId, TileBase[] tiles)
        {
            DualGridTerrainFamilyProfile family = FindInlineFamily(layerId);
            if (family == null)
            {
                return;
            }

            var tileArray = new Tile[tiles != null ? tiles.Length : 0];
            for (int i = 0; i < tileArray.Length; i++)
            {
                tileArray[i] = tiles[i] as Tile;
            }

            family.ConfigureLegacyMigration(tileArray, tileArray);
        }

        private void CopyInlineLegacyTopologyFrom(DualGridLegacyTopologyAssets sourceLegacyTopology)
        {
            legacyTopology = legacyTopology ?? new DualGridLegacyTopologyAssets();
            legacyTopology.Configure(
                sourceLegacyTopology != null ? sourceLegacyTopology.WallContourTiles : Array.Empty<Tile>(),
                sourceLegacyTopology != null ? sourceLegacyTopology.DangerContourTiles : Array.Empty<Tile>());
        }

        private void ConfigureInlineLegacyTopology(TileBase[] wallContours, TileBase[] dangerContours)
        {
            legacyTopology = legacyTopology ?? new DualGridLegacyTopologyAssets();
            legacyTopology.Configure(ToTileArray(wallContours), ToTileArray(dangerContours));
        }

#if UNITY_EDITOR
        private bool NormalizeInlineFamilies()
        {
            bool changed = false;
            foreach (TerrainRenderLayerId layerId in DualGridTerrain.MaterialFamilies)
            {
                DualGridTerrainFamilyProfile family = FindInlineFamily(layerId);
                if (family != null && family.NormalizeLegacyAuthoring(LegacyConfiguredTilesForLayer(layerId)))
                {
                    family.SetLayerId(layerId);
                    changed = true;
                }
            }

            return changed;
        }

        private bool NormalizeInlineLegacyTopology()
        {
            if ((legacyTopology == null
                    || (!HasAnyResolvedTile(legacyTopology.WallContourTiles) && !HasAnyResolvedTile(legacyTopology.DangerContourTiles)))
                && (HasAnyLegacyTileReference(wallContourTiles) || HasAnyLegacyTileReference(dangerContourTiles)))
            {
                ConfigureInlineLegacyTopology(wallContourTiles, dangerContourTiles);
                return true;
            }

            return false;
        }

        private TileBase[] LegacyConfiguredTilesForLayer(TerrainRenderLayerId layerId)
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
#endif

        private bool ClearLegacyDualGridReferences()
        {
            bool changed = false;

            if (dualGridTerrainProfile != null)
            {
                dualGridTerrainProfile = null;
                changed = true;
            }

            if (HasAnyLegacyTileReference(floorDualGridTiles))
            {
                floorDualGridTiles = Array.Empty<TileBase>();
                changed = true;
            }

            if (HasAnyLegacyTileReference(soilDualGridTiles))
            {
                soilDualGridTiles = Array.Empty<TileBase>();
                changed = true;
            }

            if (HasAnyLegacyTileReference(stoneDualGridTiles))
            {
                stoneDualGridTiles = Array.Empty<TileBase>();
                changed = true;
            }

            if (HasAnyLegacyTileReference(hardRockDualGridTiles))
            {
                hardRockDualGridTiles = Array.Empty<TileBase>();
                changed = true;
            }

            if (HasAnyLegacyTileReference(ultraHardDualGridTiles))
            {
                ultraHardDualGridTiles = Array.Empty<TileBase>();
                changed = true;
            }

            if (HasAnyLegacyTileReference(boundaryDualGridTiles))
            {
                boundaryDualGridTiles = Array.Empty<TileBase>();
                changed = true;
            }

            if (HasAnyLegacyTileReference(wallContourTiles))
            {
                wallContourTiles = Array.Empty<TileBase>();
                changed = true;
            }

            if (HasAnyLegacyTileReference(dangerContourTiles))
            {
                dangerContourTiles = Array.Empty<TileBase>();
                changed = true;
            }

            return changed;
        }

        private static Tile[] ToTileArray(TileBase[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<Tile>();
            }

            var result = new Tile[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = source[i] as Tile;
            }

            return result;
        }
#if UNITY_EDITOR

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
