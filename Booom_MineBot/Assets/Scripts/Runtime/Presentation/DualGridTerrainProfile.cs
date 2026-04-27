using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Presentation
{
    public enum DualGridAuthoringMode : byte
    {
        Atlas16 = 0,
        Explicit16 = 1,
        Canonical6 = 2
    }

    [Serializable]
    public struct DualGridAtlas16Source
    {
        [SerializeField]
        private Texture2D atlasTexture;

        [SerializeField]
        private int columns;

        [SerializeField]
        private int rows;

        [SerializeField]
        private Vector2Int tileSize;

        [SerializeField]
        private Vector2Int padding;

        [SerializeField]
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
        private Tile[] wallContourTiles = Array.Empty<Tile>();

        [SerializeField]
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
        private TerrainRenderLayerId layerId;

        [SerializeField]
        private bool enabled = true;

        [SerializeField]
        private DualGridAuthoringMode authoringMode = DualGridAuthoringMode.Explicit16;

        [SerializeField]
        private DualGridAtlas16Source atlas16Source;

        [SerializeField]
        private Tile[] explicit16Tiles = Array.Empty<Tile>();

        [SerializeField]
        private Tile[] canonical6Tiles = Array.Empty<Tile>();

        [SerializeField]
        private Tile[] perIndexOverrides16 = Array.Empty<Tile>();

        [SerializeField]
        private bool allowGeneratedFallbackForMissing = true;

        [SerializeField]
        private bool allowAutoRotateCanonical = true;

        [SerializeField]
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

        public Tile[] ResolveTiles(Tile[] legacyTiles)
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
                FillMissing(resolved, legacyTiles);
            }

            if (allowGeneratedFallbackForMissing)
            {
                FillMissing(resolved, DualGridTerrainFallbackTiles.CreateTileSet(layerId));
            }

            return resolved;
        }

        public IEnumerable<string> GetValidationIssues()
        {
            if (resolved16Tiles != null && resolved16Tiles.Length > 0 && resolved16Tiles.Length != DualGridTerrain.TileCount)
            {
                yield return $"{layerId}: resolved16Tiles length must be {DualGridTerrain.TileCount}.";
            }

            if (explicit16Tiles != null && explicit16Tiles.Length > 0 && explicit16Tiles.Length != DualGridTerrain.TileCount)
            {
                yield return $"{layerId}: explicit16Tiles length must be {DualGridTerrain.TileCount}.";
            }

            if (perIndexOverrides16 != null && perIndexOverrides16.Length > 0 && perIndexOverrides16.Length != DualGridTerrain.TileCount)
            {
                yield return $"{layerId}: perIndexOverrides16 length must be {DualGridTerrain.TileCount}.";
            }

            if (canonical6Tiles != null && canonical6Tiles.Length > 0 && canonical6Tiles.Length != CanonicalTileCount)
            {
                yield return $"{layerId}: canonical6Tiles length must be {CanonicalTileCount}.";
            }

            Tile[] resolved = ResolveTiles(null);
            for (int i = 0; i < resolved.Length; i++)
            {
                if (resolved[i] == null)
                {
                    yield return $"{layerId}: missing resolved tile at index {i:00}.";
                }
            }
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
            TerrainRenderLayerId[] orderedLayers = DualGridTerrainLayout.OrderedLayers;
            var result = new DualGridTerrainFamilyProfile[orderedLayers.Length];
            for (int i = 0; i < orderedLayers.Length; i++)
            {
                result[i] = new DualGridTerrainFamilyProfile
                {
                    layerId = orderedLayers[i],
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
    }

    [CreateAssetMenu(menuName = "Minebot/Presentation/Dual Grid Terrain Profile")]
    public sealed class DualGridTerrainProfile : ScriptableObject
    {
        [SerializeField]
        private DualGridTerrainLayoutSettings layoutSettings = default;

        [SerializeField]
        private DualGridTerrainFamilyProfile[] families = Array.Empty<DualGridTerrainFamilyProfile>();

        [SerializeField]
        private DualGridLegacyTopologyAssets legacyTopology = new DualGridLegacyTopologyAssets();

        [SerializeField]
        private bool allowLegacyArtSetFallback = true;

        public DualGridTerrainLayoutSettings LayoutSettings => layoutSettings.DisplayOffset == default && layoutSettings.SortingOrderStep == 0
            ? DualGridTerrainLayoutSettings.CreateDefault()
            : layoutSettings;

        public DualGridTerrainFamilyProfile[] Families => EnsureFamilies();
        public DualGridLegacyTopologyAssets LegacyTopology => legacyTopology ?? new DualGridLegacyTopologyAssets();
        public bool AllowLegacyArtSetFallback => allowLegacyArtSetFallback;

        public Tile[] ResolveFamilyTiles(TerrainRenderLayerId layerId, Tile[] legacyTiles)
        {
            DualGridTerrainFamilyProfile family = FindFamily(layerId);
            if (family == null || !family.Enabled)
            {
                return ResolveFallbackTiles(layerId, legacyTiles);
            }

            Tile[] resolved = family.ResolveTiles(allowLegacyArtSetFallback ? legacyTiles : null);
            return resolved.Length > 0 ? resolved : ResolveFallbackTiles(layerId, legacyTiles);
        }

        public Tile[] ResolveWallContourTiles(Tile[] legacyTiles)
        {
            return ResolveLegacyTiles(LegacyTopology.WallContourTiles, legacyTiles);
        }

        public Tile[] ResolveDangerContourTiles(Tile[] legacyTiles)
        {
            return ResolveLegacyTiles(LegacyTopology.DangerContourTiles, legacyTiles);
        }

        public IEnumerable<string> GetValidationIssues()
        {
            foreach (DualGridTerrainFamilyProfile family in Families)
            {
                if (family == null)
                {
                    yield return "Dual-grid profile has a missing family entry.";
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

        public void ConfigureFamilyTiles(TerrainRenderLayerId layerId, Tile[] tiles)
        {
            DualGridTerrainFamilyProfile family = GetOrCreateFamily(layerId);
            family.ConfigureLegacyMigration(tiles, tiles);
        }

        public void ConfigureLegacyTopology(Tile[] wallContours, Tile[] dangerContours)
        {
            legacyTopology = legacyTopology ?? new DualGridLegacyTopologyAssets();
            legacyTopology.Configure(wallContours, dangerContours);
        }
#endif

        private DualGridTerrainFamilyProfile[] EnsureFamilies()
        {
            if (families != null && families.Length == DualGridTerrainLayout.RenderLayerCount)
            {
                return families;
            }

            families = DualGridTerrainFamilyProfile.CreateDefaults();
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

        private Tile[] ResolveFallbackTiles(TerrainRenderLayerId layerId, Tile[] legacyTiles)
        {
            if (allowLegacyArtSetFallback && legacyTiles != null && legacyTiles.Length > 0)
            {
                var normalized = new Tile[DualGridTerrain.TileCount];
                for (int i = 0; i < normalized.Length; i++)
                {
                    normalized[i] = i < legacyTiles.Length ? legacyTiles[i] : null;
                }

                FillMissing(normalized, DualGridTerrainFallbackTiles.CreateTileSet(layerId));
                return normalized;
            }

            return DualGridTerrainFallbackTiles.CreateTileSet(layerId);
        }

        private static Tile[] ResolveLegacyTiles(Tile[] profileLegacyTiles, Tile[] artSetLegacyTiles)
        {
            Tile[] preferred = profileLegacyTiles != null && profileLegacyTiles.Length > 0 ? profileLegacyTiles : artSetLegacyTiles;
            if (preferred == null || preferred.Length == 0)
            {
                return Array.Empty<Tile>();
            }

            var normalized = new Tile[DualGridContour.TileCount];
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
    }
}
