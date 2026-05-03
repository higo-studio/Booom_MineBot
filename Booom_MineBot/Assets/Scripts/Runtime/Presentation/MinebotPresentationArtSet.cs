using System;
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
        [InspectorLabel("空地瓦片")]
        private TileBase emptyTile;

        [SerializeField]
        [InspectorLabel("土层墙瓦片")]
        private TileBase soilWallTile;

        [SerializeField]
        [InspectorLabel("石层墙瓦片")]
        private TileBase stoneWallTile;

        [SerializeField]
        [InspectorLabel("硬岩墙瓦片")]
        private TileBase hardRockWallTile;

        [SerializeField]
        [InspectorLabel("超硬岩墙瓦片")]
        private TileBase ultraHardWallTile;

        [SerializeField]
        [InspectorLabel("边界瓦片")]
        private TileBase boundaryTile;

        [SerializeField]
        [InspectorLabel("双网格地形配置")]
        private DualGridTerrainProfile dualGridTerrainProfile;

        [SerializeField]
        [InspectorLabel("双网格地板瓦片")]
        private TileBase[] floorDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        [InspectorLabel("双网格土层瓦片")]
        private TileBase[] soilDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        [InspectorLabel("双网格石层瓦片")]
        private TileBase[] stoneDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        [InspectorLabel("双网格硬岩瓦片")]
        private TileBase[] hardRockDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        [InspectorLabel("双网格超硬岩瓦片")]
        private TileBase[] ultraHardDualGridTiles = Array.Empty<TileBase>();

        [SerializeField]
        [InspectorLabel("双网格边界瓦片")]
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
        [InspectorLabel("探测提示瓦片")]
        private TileBase scanHintTile;

        [SerializeField]
        [InspectorLabel("危险描边瓦片")]
        private TileBase[] dangerOutlineTiles = Array.Empty<TileBase>();

        [SerializeField]
        [InspectorLabel("墙体轮廓瓦片")]
        private TileBase[] wallContourTiles = Array.Empty<TileBase>();

        [SerializeField]
        [InspectorLabel("危险轮廓瓦片")]
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

        public TileBase EmptyTile => emptyTile;
        public TileBase SoilWallTile => soilWallTile;
        public TileBase StoneWallTile => stoneWallTile;
        public TileBase HardRockWallTile => hardRockWallTile;
        public TileBase UltraHardWallTile => ultraHardWallTile;
        public TileBase BoundaryTile => boundaryTile;
        public DualGridTerrainProfile DualGridTerrainProfile => dualGridTerrainProfile;
        public DualGridTerrainLayoutSettings TerrainLayoutSettings => dualGridTerrainProfile != null ? dualGridTerrainProfile.LayoutSettings : DualGridTerrainLayoutSettings.CreateDefault();
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