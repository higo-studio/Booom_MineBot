using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Presentation
{
    public enum DualGridAuthoringMode : byte
    {
        [InspectorName("16 格图集")]
        Atlas16 = 0,
        [InspectorName("显式 16 格")]
        Explicit16 = 1,
        [InspectorName("规范 6 格")]
        Canonical6 = 2
    }

    [Serializable]
    public struct DualGridAtlas16Source
    {
        [SerializeField]
        [InspectorLabel("图集纹理")]
        private Texture2D atlasTexture;

        [SerializeField]
        [InspectorLabel("列数")]
        private int columns;

        [SerializeField]
        [InspectorLabel("行数")]
        private int rows;

        [SerializeField]
        [InspectorLabel("单格尺寸")]
        private Vector2Int tileSize;

        [SerializeField]
        [InspectorLabel("内边距")]
        private Vector2Int padding;

        [SerializeField]
        [InspectorLabel("按行排列")]
        private bool rowMajor;

        public Texture2D AtlasTexture => atlasTexture;
        public int Columns => columns <= 0 ? 4 : columns;
        public int Rows => rows <= 0 ? 4 : rows;
        public Vector2Int TileSize => tileSize == Vector2Int.zero ? new Vector2Int(16, 16) : tileSize;
        public Vector2Int Padding => padding;
        public bool RowMajor => rowMajor;

#if UNITY_EDITOR
        public void Configure(Texture2D texture, int configuredColumns = 4, int configuredRows = 4, Vector2Int? configuredTileSize = null, Vector2Int? configuredPadding = null, bool configuredRowMajor = true)
        {
            atlasTexture = texture;
            columns = configuredColumns;
            rows = configuredRows;
            tileSize = configuredTileSize ?? new Vector2Int(16, 16);
            padding = configuredPadding ?? Vector2Int.zero;
            rowMajor = configuredRowMajor;
        }
#endif
    }

    [Serializable]
    public sealed class DualGridLegacyTopologyAssets
    {
        [SerializeField]
        [InspectorLabel("墙体轮廓瓦片")]
        private Tile[] wallContourTiles = Array.Empty<Tile>();

        [SerializeField]
        [InspectorLabel("危险轮廓瓦片")]
        private Tile[] dangerContourTiles = Array.Empty<Tile>();

        public Tile[] WallContourTiles => wallContourTiles ?? Array.Empty<Tile>();
        public Tile[] DangerContourTiles => dangerContourTiles ?? Array.Empty<Tile>();

#if UNITY_EDITOR
        public void Configure(Tile[] wallContours, Tile[] dangerContours)
        {
            wallContourTiles = wallContours ?? Array.Empty<Tile>();
            dangerContourTiles = dangerContours ?? Array.Empty<Tile>();
        }
#endif
    }

    [Serializable]
    public sealed class DualGridTerrainFamilyProfile
    {
        private const int CanonicalTileCount = 6;

        [SerializeField]
        [InspectorLabel("图层")]
        private TerrainRenderLayerId layerId;

        [SerializeField]
        [InspectorLabel("启用")]
        private bool enabled = true;

        [SerializeField]
        [InspectorLabel("制作模式")]
        private DualGridAuthoringMode authoringMode = DualGridAuthoringMode.Explicit16;

        [SerializeField]
        [InspectorLabel("16 格图集源")]
        private DualGridAtlas16Source atlas16Source;

        [SerializeField]
        [InspectorLabel("显式 16 格瓦片")]
        private Tile[] explicit16Tiles = Array.Empty<Tile>();

        [SerializeField]
        [InspectorLabel("规范 6 格瓦片")]
        private Tile[] canonical6Tiles = Array.Empty<Tile>();

        [SerializeField]
        [InspectorLabel("按索引覆盖 16 格瓦片")]
        private Tile[] perIndexOverrides16 = Array.Empty<Tile>();

        [SerializeField]
        [InspectorLabel("缺失时允许自动补全")]
        private bool allowGeneratedFallbackForMissing = true;

        [SerializeField]
        [InspectorLabel("允许规范 6 格自动旋转")]
        private bool allowAutoRotateCanonical = true;

        [SerializeField]
        [InspectorLabel("最终 16 格瓦片")]
        private Tile[] resolved16Tiles = Array.Empty<Tile>();

        public TerrainRenderLayerId LayerId => layerId;
        public bool Enabled => enabled;
        public DualGridAuthoringMode AuthoringMode => authoringMode;
        public DualGridAtlas16Source Atlas16Source => atlas16Source;
        public Tile[] Explicit16Tiles => explicit16Tiles ?? Array.Empty<Tile>();
        public Tile[] Canonical6Tiles => canonical6Tiles ?? Array.Empty<Tile>();
        public Tile[] PerIndexOverrides16 => perIndexOverrides16 ?? Array.Empty<Tile>();
        public bool AllowGeneratedFallbackForMissing => allowGeneratedFallbackForMissing;
        public bool AllowAutoRotateCanonical => allowAutoRotateCanonical;
        public Tile[] Resolved16Tiles => resolved16Tiles ?? Array.Empty<Tile>();

        public Tile[] ResolveTiles(TileBase[] legacyTiles)
        {
            var resolved = new Tile[DualGridTerrain.TileCount];

            CopyIfSized(resolved16Tiles, resolved);
            if (!HasAnyTile(resolved))
            {
                switch (authoringMode)
                {
                    case DualGridAuthoringMode.Canonical6:
                        ApplyCanonicalTiles(canonical6Tiles, resolved, allowAutoRotateCanonical);
                        break;
                    case DualGridAuthoringMode.Explicit16:
                        CopyIfSized(explicit16Tiles, resolved);
                        break;
                }
            }

            ApplyOverrides(perIndexOverrides16, resolved);

            if (legacyTiles != null && legacyTiles.Length > 0)
            {
                FillMissingFromTileBase(resolved, legacyTiles);
            }

            if (allowGeneratedFallbackForMissing)
            {
                FillMissing(resolved, DualGridTerrainFallbackTiles.CreateTileSet(layerId));
            }

            return resolved;
        }

        public IEnumerable<string> GetValidationIssues()
        {
            string layerLabel = GetLayerLabel(layerId);
            if (resolved16Tiles != null && resolved16Tiles.Length > 0 && resolved16Tiles.Length != DualGridTerrain.TileCount)
            {
                yield return $"{layerLabel}：最终 16 格瓦片数量必须为 {DualGridTerrain.TileCount}。";
            }

            if (explicit16Tiles != null && explicit16Tiles.Length > 0 && explicit16Tiles.Length != DualGridTerrain.TileCount)
            {
                yield return $"{layerLabel}：显式 16 格瓦片数量必须为 {DualGridTerrain.TileCount}。";
            }

            if (perIndexOverrides16 != null && perIndexOverrides16.Length > 0 && perIndexOverrides16.Length != DualGridTerrain.TileCount)
            {
                yield return $"{layerLabel}：按索引覆盖 16 格数量必须为 {DualGridTerrain.TileCount}。";
            }

            if (canonical6Tiles != null && canonical6Tiles.Length > 0 && canonical6Tiles.Length != CanonicalTileCount)
            {
                yield return $"{layerLabel}：规范 6 格瓦片数量必须为 {CanonicalTileCount}。";
            }

            Tile[] resolved = ResolveTiles(null);
            for (int i = 0; i < resolved.Length; i++)
            {
                if (resolved[i] == null)
                {
                    yield return $"{layerLabel}：缺少索引 {i:00} 的最终瓦片。";
                }
            }
        }

        public void CopyFrom(DualGridTerrainFamilyProfile source)
        {
            if (source == null)
            {
                return;
            }

            enabled = source.enabled;
            authoringMode = source.authoringMode;
            atlas16Source = source.atlas16Source;
            explicit16Tiles = source.explicit16Tiles ?? Array.Empty<Tile>();
            canonical6Tiles = source.canonical6Tiles ?? Array.Empty<Tile>();
            perIndexOverrides16 = source.perIndexOverrides16 ?? Array.Empty<Tile>();
            allowGeneratedFallbackForMissing = source.allowGeneratedFallbackForMissing;
            allowAutoRotateCanonical = source.allowAutoRotateCanonical;
            resolved16Tiles = source.resolved16Tiles ?? Array.Empty<Tile>();
        }

#if UNITY_EDITOR
        public void ConfigureResolvedTiles(Tile[] tiles)
        {
            resolved16Tiles = tiles ?? Array.Empty<Tile>();
            authoringMode = DualGridAuthoringMode.Explicit16;
        }

        public void ConfigureLegacyMigration(Tile[] explicitTiles, Tile[] resolvedTiles)
        {
            explicit16Tiles = explicitTiles ?? Array.Empty<Tile>();
            resolved16Tiles = resolvedTiles ?? Array.Empty<Tile>();
            authoringMode = DualGridAuthoringMode.Explicit16;
        }

        public void SetLayerId(TerrainRenderLayerId value)
        {
            layerId = value;
        }
#endif

        public static DualGridTerrainFamilyProfile[] CreateDefaults()
        {
            TerrainRenderLayerId[] materialFamilies = DualGridTerrain.MaterialFamilies;
            var result = new DualGridTerrainFamilyProfile[materialFamilies.Length];
            for (int i = 0; i < materialFamilies.Length; i++)
            {
                result[i] = new DualGridTerrainFamilyProfile
                {
                    layerId = materialFamilies[i],
                    enabled = true,
                    authoringMode = DualGridAuthoringMode.Explicit16,
                    allowGeneratedFallbackForMissing = true,
                    allowAutoRotateCanonical = true,
                    explicit16Tiles = Array.Empty<Tile>(),
                    canonical6Tiles = Array.Empty<Tile>(),
                    perIndexOverrides16 = Array.Empty<Tile>(),
                    resolved16Tiles = Array.Empty<Tile>()
                };
            }

            return result;
        }

        private static void CopyIfSized(Tile[] source, Tile[] destination)
        {
            if (source == null || destination == null || source.Length != destination.Length)
            {
                return;
            }

            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] = source[i];
            }
        }

        private static void ApplyOverrides(Tile[] overrides, Tile[] destination)
        {
            if (overrides == null)
            {
                return;
            }

            int count = Mathf.Min(overrides.Length, destination.Length);
            for (int i = 0; i < count; i++)
            {
                if (overrides[i] != null)
                {
                    destination[i] = overrides[i];
                }
            }
        }

        private static void FillMissing(Tile[] destination, Tile[] fallback)
        {
            if (fallback == null)
            {
                return;
            }

            int count = Mathf.Min(destination.Length, fallback.Length);
            for (int i = 0; i < count; i++)
            {
                if (destination[i] == null)
                {
                    destination[i] = fallback[i];
                }
            }
        }

        private static bool HasAnyTile(Tile[] tiles)
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

        private static void ApplyCanonicalTiles(Tile[] canonicalTiles, Tile[] destination, bool allowAutoRotate)
        {
            if (canonicalTiles == null || canonicalTiles.Length == 0)
            {
                return;
            }

            SetIfPresent(destination, 0, canonicalTiles, 0);
            SetIfPresent(destination, 15, canonicalTiles, 5);
            SetOrbit(destination, canonicalTiles, 1, new[] { 1, 2, 4, 8 }, allowAutoRotate);
            SetOrbit(destination, canonicalTiles, 2, new[] { 3, 5, 10, 12 }, allowAutoRotate);
            SetOrbit(destination, canonicalTiles, 3, new[] { 6, 9 }, allowAutoRotate);
            SetOrbit(destination, canonicalTiles, 4, new[] { 7, 11, 13, 14 }, allowAutoRotate);
        }

        private static void SetOrbit(Tile[] destination, Tile[] canonicalTiles, int canonicalIndex, int[] orbit, bool allowAutoRotate)
        {
            if (canonicalTiles.Length <= canonicalIndex || canonicalTiles[canonicalIndex] == null)
            {
                return;
            }

            for (int i = 0; i < orbit.Length; i++)
            {
                if (!allowAutoRotate && i > 0)
                {
                    break;
                }

                destination[orbit[i]] = canonicalTiles[canonicalIndex];
            }
        }

        private static void SetIfPresent(Tile[] destination, int destinationIndex, Tile[] source, int sourceIndex)
        {
            if (source.Length > sourceIndex && source[sourceIndex] != null)
            {
                destination[destinationIndex] = source[sourceIndex];
            }
        }

        private static string GetLayerLabel(TerrainRenderLayerId layerId)
        {
            return layerId switch
            {
                TerrainRenderLayerId.Floor => "地板层",
                TerrainRenderLayerId.Soil => "土层",
                TerrainRenderLayerId.Stone => "石层",
                TerrainRenderLayerId.HardRock => "硬岩层",
                TerrainRenderLayerId.UltraHard => "超硬岩层",
                TerrainRenderLayerId.Boundary => "边界层",
                _ => "未知图层"
            };
        }

        private static void FillMissingFromTileBase(Tile[] destination, TileBase[] fallback)
        {
            int count = Mathf.Min(destination.Length, fallback.Length);
            for (int i = 0; i < count; i++)
            {
                if (destination[i] == null && fallback[i] is Tile tile)
                {
                    destination[i] = tile;
                }
            }
        }
    }

    [CreateAssetMenu(menuName = "Minebot/表现/双网格地形配置")]
    public sealed class DualGridTerrainProfile : ScriptableObject
    {
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
        [InspectorLabel("允许回退到旧美术集")]
        private bool allowLegacyArtSetFallback = true;

        public DualGridTerrainLayoutSettings LayoutSettings => layoutSettings.DisplayOffset == default && layoutSettings.SortingOrderStep == 0
            ? DualGridTerrainLayoutSettings.CreateDefault()
            : layoutSettings;

        public DualGridTerrainFamilyProfile[] Families => EnsureFamilies();
        public DualGridLegacyTopologyAssets LegacyTopology => legacyTopology ?? new DualGridLegacyTopologyAssets();
        public bool AllowLegacyArtSetFallback => allowLegacyArtSetFallback;

        public TileBase[] ResolveFamilyTiles(TerrainRenderLayerId layerId, TileBase[] legacyTiles)
        {
            DualGridTerrainFamilyProfile family = FindFamily(layerId);
            if (family == null || !family.Enabled)
            {
                return ResolveFallbackTiles(layerId, legacyTiles);
            }

            Tile[] resolved = family.ResolveTiles(allowLegacyArtSetFallback ? legacyTiles : null);
            return resolved.Length > 0 ? resolved : ResolveFallbackTiles(layerId, legacyTiles);
        }

        public TileBase[] ResolveWallContourTiles(TileBase[] legacyTiles)
        {
            return ResolveLegacyTiles(LegacyTopology.WallContourTiles, legacyTiles);
        }

        public TileBase[] ResolveDangerContourTiles(TileBase[] legacyTiles)
        {
            return ResolveLegacyTiles(LegacyTopology.DangerContourTiles, legacyTiles);
        }

        public IEnumerable<string> GetValidationIssues()
        {
            foreach (DualGridTerrainFamilyProfile family in Families)
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
        }

#if UNITY_EDITOR
        public void ConfigureLayout(DualGridTerrainLayoutSettings settings)
        {
            layoutSettings = settings;
        }

        public void ConfigureFamilyTiles(TerrainRenderLayerId layerId, TileBase[] tiles)
        {
            DualGridTerrainFamilyProfile family = GetOrCreateFamily(layerId);
            var tileArray = new Tile[tiles.Length];
            for (int i = 0; i < tiles.Length; i++)
            {
                tileArray[i] = tiles[i] as Tile;
            }
            family.ConfigureLegacyMigration(tileArray, tileArray);
        }

        public void ConfigureLegacyTopology(Tile[] wallContours, Tile[] dangerContours)
        {
            legacyTopology = legacyTopology ?? new DualGridLegacyTopologyAssets();
            legacyTopology.Configure(wallContours, dangerContours);
        }
#endif

        private DualGridTerrainFamilyProfile[] EnsureFamilies()
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

        private DualGridTerrainFamilyProfile FindFamily(TerrainRenderLayerId layerId)
        {
            DualGridTerrainFamilyProfile[] configuredFamilies = EnsureFamilies();
            for (int i = 0; i < configuredFamilies.Length; i++)
            {
                if (configuredFamilies[i] != null && configuredFamilies[i].LayerId == layerId)
                {
                    return configuredFamilies[i];
                }
            }

            return null;
        }

        private TileBase[] ResolveFallbackTiles(TerrainRenderLayerId layerId, TileBase[] legacyTiles)
        {
            if (allowLegacyArtSetFallback && legacyTiles != null && legacyTiles.Length > 0)
            {
                var normalized = new TileBase[DualGridTerrain.TileCount];
                for (int i = 0; i < normalized.Length; i++)
                {
                    normalized[i] = i < legacyTiles.Length ? legacyTiles[i] : null;
                }

                FillMissingBase(normalized, DualGridTerrainFallbackTiles.CreateTileSet(layerId));
                return normalized;
            }

            return DualGridTerrainFallbackTiles.CreateTileSet(layerId);
        }

        private static TileBase[] ResolveLegacyTiles(Tile[] profileLegacyTiles, TileBase[] artSetLegacyTiles)
        {
            TileBase[] preferred = profileLegacyTiles != null && profileLegacyTiles.Length > 0 ? profileLegacyTiles : artSetLegacyTiles;
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

#if UNITY_EDITOR
        private DualGridTerrainFamilyProfile GetOrCreateFamily(TerrainRenderLayerId layerId)
        {
            DualGridTerrainFamilyProfile family = FindFamily(layerId);
            if (family != null)
            {
                return family;
            }

            DualGridTerrainFamilyProfile[] configuredFamilies = EnsureFamilies();
            for (int i = 0; i < configuredFamilies.Length; i++)
            {
                if (configuredFamilies[i] != null)
                {
                    continue;
                }

                configuredFamilies[i] = new DualGridTerrainFamilyProfile();
                configuredFamilies[i].SetLayerId(layerId);
                return configuredFamilies[i];
            }

            return configuredFamilies[0];
        }
#endif

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

            if (destinationFamily.Explicit16Tiles.Length > 0
                || destinationFamily.Resolved16Tiles.Length > 0
                || destinationFamily.Canonical6Tiles.Length > 0
                || destinationFamily.PerIndexOverrides16.Length > 0)
            {
                return;
            }

            destinationFamily.CopyFrom(wallSource);
        }

        private static void FillMissing(Tile[] destination, Tile[] fallback)
        {
            int count = Mathf.Min(destination.Length, fallback.Length);
            for (int i = 0; i < count; i++)
            {
                if (destination[i] == null)
                {
                    destination[i] = fallback[i];
                }
            }
        }

        private static void FillMissingBase(TileBase[] destination, Tile[] fallback)
        {
            int count = Mathf.Min(destination.Length, fallback.Length);
            for (int i = 0; i < count; i++)
            {
                if (destination[i] == null && fallback[i] != null)
                {
                    destination[i] = fallback[i];
                }
            }
        }

        private static void FillMissingFromTileBase(Tile[] destination, TileBase[] fallback)
        {
            int count = Mathf.Min(destination.Length, fallback.Length);
            for (int i = 0; i < count; i++)
            {
                if (destination[i] == null && fallback[i] is Tile tile)
                {
                    destination[i] = tile;
                }
            }
        }
    }
}
