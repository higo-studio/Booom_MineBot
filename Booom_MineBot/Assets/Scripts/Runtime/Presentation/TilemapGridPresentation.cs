using System;
using System.Collections.Generic;
using Minebot.Bootstrap;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.Progression;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Presentation
{
    public sealed class TilemapGridPresentation : MonoBehaviour
    {
        private static readonly ProfilerMarker RefreshProfilerMarker = new("Minebot.TilemapGridPresentation.Refresh");
        private static readonly ProfilerMarker RefreshScanGridProfilerMarker = new("Minebot.TilemapGridPresentation.Refresh.ScanGrid");
        private static readonly ProfilerMarker RefreshOverlaysProfilerMarker = new("Minebot.TilemapGridPresentation.Refresh.Overlays");
        private static readonly ProfilerMarker RefreshTerrainProfilerMarker = new("Minebot.TilemapGridPresentation.Refresh.Terrain");
        private static readonly ProfilerMarker RefreshFogProfilerMarker = new("Minebot.TilemapGridPresentation.Refresh.Fog");
        private static readonly ProfilerMarker CompressBoundsProfilerMarker = new("Minebot.TilemapGridPresentation.Refresh.CompressBounds");

        private const float DangerFlashThreshold = 3f;
        private const float FlashInterval = 0.12f;
        
        private MinebotPresentationAssets assets;
        private readonly DualGridRenderer terrainRenderer = new DualGridRenderer(new LayeredBinaryTerrainResolver());
        private readonly DualGridFogRenderer fogRenderer = new DualGridFogRenderer();
        private BuildingDefinition buildPreviewDefinition;
        private GridPosition? buildPreviewOrigin;
        private bool buildPreviewIsValid;
        private Vector2Int cachedGridSize;
        private TerrainMaterialId[] terrainMaterialCache;
        private bool[] fogNearMaskCache;
        private bool[] fogDeepMaskCache;
        
        // Caches for overlay tilemaps to preserve animated tile state
        private Dictionary<Vector3Int, TileBase> facilityTileCache = new Dictionary<Vector3Int, TileBase>();
        private Dictionary<Vector3Int, TileBase> markerTileCache = new Dictionary<Vector3Int, TileBase>();
        private Dictionary<Vector3Int, TileBase> dangerTileCache = new Dictionary<Vector3Int, TileBase>();
        private Dictionary<Vector3Int, TileBase> buildPreviewTileCache = new Dictionary<Vector3Int, TileBase>();
        private HashSet<Vector3Int> lastDangerPositions = new HashSet<Vector3Int>();
        private int lastDangerWave = -1;
        private float lastFlashTime;
        private bool isFlashingVisible = true;
        private bool isWaveCountdownPaused;

        public Tilemap TerrainTilemap => GetTerrainTilemap(TerrainRenderLayerId.Floor);
        public IReadOnlyList<Tilemap> TerrainTilemaps { get; private set; } = Array.Empty<Tilemap>();
        public Tilemap FogNearTilemap { get; private set; }
        public Tilemap FogDeepTilemap { get; private set; }
        public Tilemap FacilityTilemap { get; private set; }
        public Tilemap MarkerTilemap { get; private set; }
        public Tilemap DangerTilemap { get; private set; }
        public Tilemap BuildPreviewTilemap { get; private set; }
        public float TimeUntilNextWave { get; private set; }

        public void Configure(
            Tilemap[] terrainTilemaps,
            Tilemap fogNearTilemap,
            Tilemap fogDeepTilemap,
            Tilemap facilityTilemap,
            Tilemap markerTilemap,
            Tilemap dangerTilemap,
            Tilemap buildPreviewTilemap,
            MinebotPresentationAssets presentationAssets)
        {
            TerrainTilemaps = terrainTilemaps ?? Array.Empty<Tilemap>();
            FogNearTilemap = fogNearTilemap;
            FogDeepTilemap = fogDeepTilemap;
            FacilityTilemap = facilityTilemap;
            MarkerTilemap = markerTilemap;
            DangerTilemap = dangerTilemap;
            BuildPreviewTilemap = buildPreviewTilemap;
            assets = presentationAssets;
        }

        public void ShowBuildPreview(BuildingDefinition definition, GridPosition? origin, bool isValid)
        {
            buildPreviewDefinition = definition;
            buildPreviewOrigin = origin;
            buildPreviewIsValid = isValid;
        }

        public void SetWaveCountdownPaused(bool paused)
        {
            if (isWaveCountdownPaused == paused)
            {
                return;
            }

            isWaveCountdownPaused = paused;
            if (!paused || DangerTilemap == null || isFlashingVisible)
            {
                return;
            }

            isFlashingVisible = true;
            foreach (KeyValuePair<Vector3Int, TileBase> kvp in dangerTileCache)
            {
                DangerTilemap.SetTile(kvp.Key, kvp.Value);
            }
        }

        public void RefreshBuildPreviewOnly(LogicalGridState grid)
        {
            if (grid == null || BuildPreviewTilemap == null || assets == null)
            {
                return;
            }

            RefreshBuildPreviewOverlay(grid);
        }

        public void RefreshLocalTerrainChange(
            RuntimeServiceRegistry services,
            GridPosition repairStationPosition,
            GridPosition robotFactoryPosition,
            ICollection<GridPosition> changedCells)
        {
            if (services == null || assets == null || TerrainTilemaps.Count == 0 || changedCells == null || changedCells.Count == 0)
            {
                return;
            }

            LogicalGridState grid = services.Grid;
            TimeUntilNextWave = services.Waves?.TimeUntilNextWave ?? float.MaxValue;
            if (EnsureGridCaches(grid.Size))
            {
                Refresh(services, repairStationPosition, robotFactoryPosition);
                return;
            }

            var terrainDirtyCells = new HashSet<GridPosition>();
            var fogDirtyCells = new HashSet<GridPosition>();
            var markerDirtyCells = new HashSet<GridPosition>();
            foreach (GridPosition changed in changedCells)
            {
                if (!grid.IsInside(changed))
                {
                    continue;
                }

                terrainDirtyCells.Add(changed);
                markerDirtyCells.Add(changed);

                for (int y = changed.Y - 1; y <= changed.Y + 1; y++)
                {
                    for (int x = changed.X - 1; x <= changed.X + 1; x++)
                    {
                        GridPosition neighbor = new GridPosition(x, y);
                        if (grid.IsInside(neighbor))
                        {
                            fogDirtyCells.Add(neighbor);
                        }
                    }
                }
            }

            UpdateTerrainCache(grid, terrainDirtyCells);
            UpdateFogCache(grid, fogDirtyCells);
            RefreshMarkerCells(grid, markerDirtyCells);
            RefreshTerrain(grid, false, terrainDirtyCells);
            RefreshFog(grid, false, fogDirtyCells);
        }

        public void RefreshMarkerCellsOnly(LogicalGridState grid, ICollection<GridPosition> changedCells)
        {
            if (grid == null || changedCells == null || changedCells.Count == 0 || assets == null)
            {
                return;
            }

            RefreshMarkerCells(grid, changedCells);
        }

        public void RefreshDangerOverlayOnly(RuntimeServiceRegistry services)
        {
            if (services == null || assets == null || DangerTilemap == null)
            {
                return;
            }

            LogicalGridState grid = services.Grid;
            TimeUntilNextWave = services.Waves?.TimeUntilNextWave ?? float.MaxValue;
            var dangerPositions = new HashSet<Vector3Int>();
            foreach (GridPosition position in grid.Positions())
            {
                GridCellState cell = grid.GetCell(position);
                if (cell.TerrainKind == TerrainKind.Empty && cell.IsDangerZone)
                {
                    dangerPositions.Add(ToTilePosition(position));
                }
            }

            RefreshDangerOverlayTilemap(DangerTilemap, dangerPositions, dangerTileCache, services.Waves?.CurrentWave ?? 0);
        }

        private void Update()
        {
            if (isWaveCountdownPaused)
            {
                return;
            }

            TimeUntilNextWave = Mathf.Max(0f, TimeUntilNextWave - Time.deltaTime);
            
            // Handle danger zone flashing when wave is about to hit
            if (DangerTilemap != null && dangerTileCache.Count > 0 && TimeUntilNextWave <= DangerFlashThreshold && TimeUntilNextWave > 0)
            {
                if (Time.time - lastFlashTime >= FlashInterval)
                {
                    lastFlashTime = Time.time;
                    isFlashingVisible = !isFlashingVisible;
                    
                    if (isFlashingVisible)
                    {
                        // Restore tiles
                        foreach (var kvp in dangerTileCache)
                        {
                            DangerTilemap.SetTile(kvp.Key, kvp.Value);
                        }
                    }
                    else
                    {
                        // Hide tiles
                        foreach (var pos in dangerTileCache.Keys)
                        {
                            DangerTilemap.SetTile(pos, null);
                        }
                    }
                }
            }
        }

        public void Refresh(RuntimeServiceRegistry services, GridPosition repairStationPosition, GridPosition robotFactoryPosition)
        {
            using (RefreshProfilerMarker.Auto())
            {
                if (services == null || assets == null || TerrainTilemaps.Count == 0)
                {
                    return;
                }

                LogicalGridState grid = services.Grid;
                TimeUntilNextWave = services.Waves?.TimeUntilNextWave ?? float.MaxValue;
                bool fullRebuild = EnsureGridCaches(grid.Size);
                var changedTerrainCells = new HashSet<GridPosition>();
                var changedFogCells = new HashSet<GridPosition>();

                // Collect all positions that should have tiles
                var facilityPositions = new HashSet<Vector3Int>();
                var markerPositions = new HashSet<Vector3Int>();
                var dangerPositions = new HashSet<Vector3Int>();

                using (RefreshScanGridProfilerMarker.Auto())
                {
                    foreach (GridPosition position in grid.Positions())
                    {
                        GridCellState cell = grid.GetCell(position);
                        Vector3Int tilePosition = ToTilePosition(position);

                        if (position.Equals(repairStationPosition))
                        {
                            facilityPositions.Add(tilePosition);
                        }
                        else if (position.Equals(robotFactoryPosition))
                        {
                            facilityPositions.Add(tilePosition);
                        }

                        if (cell.IsMarked)
                        {
                            markerPositions.Add(tilePosition);
                        }

                        bool dangerMask = cell.TerrainKind == TerrainKind.Empty && cell.IsDangerZone;
                        if (dangerMask)
                        {
                            dangerPositions.Add(tilePosition);
                        }

                        int cellIndex = ToMaterialIndex(grid.Size, position);
                        TerrainMaterialId material = DualGridTerrain.MaterialForCell(cell);
                        if (fullRebuild || terrainMaterialCache[cellIndex] != material)
                        {
                            terrainMaterialCache[cellIndex] = material;
                            changedTerrainCells.Add(position);
                        }

                        bool fogNear = DualGridFog.IsNear(grid, position);
                        bool fogDeep = DualGridFog.IsDeep(grid, position);
                        if (fullRebuild || fogNearMaskCache[cellIndex] != fogNear || fogDeepMaskCache[cellIndex] != fogDeep)
                        {
                            fogNearMaskCache[cellIndex] = fogNear;
                            fogDeepMaskCache[cellIndex] = fogDeep;
                            changedFogCells.Add(position);
                        }
                    }
                }

                using (RefreshOverlaysProfilerMarker.Auto())
                {
                    RefreshOverlayTilemapWithFacility(FacilityTilemap, facilityPositions, facilityTileCache, repairStationPosition, robotFactoryPosition);
                    RefreshOverlayTilemap(MarkerTilemap, markerPositions, markerTileCache, _ => assets.MarkerTile);

                    // Danger zone: always refresh to keep animations in sync
                    RefreshDangerOverlayTilemap(DangerTilemap, dangerPositions, dangerTileCache,
                        services.Waves?.CurrentWave ?? 0);

                    RefreshBuildPreviewOverlay(grid);
                }

                using (RefreshTerrainProfilerMarker.Auto())
                {
                    RefreshTerrain(grid, fullRebuild, changedTerrainCells);
                }

                using (RefreshFogProfilerMarker.Auto())
                {
                    RefreshFog(grid, fullRebuild, changedFogCells);
                }

                using (CompressBoundsProfilerMarker.Auto())
                {
                    for (int i = 0; i < TerrainTilemaps.Count; i++)
                    {
                        TerrainTilemaps[i]?.CompressBounds();
                    }

                    FogNearTilemap?.CompressBounds();
                    FogDeepTilemap?.CompressBounds();
                    FacilityTilemap.CompressBounds();
                    MarkerTilemap.CompressBounds();
                    DangerTilemap.CompressBounds();
                    BuildPreviewTilemap.CompressBounds();
                }
            }
        }
        
        private void RefreshOverlayTilemap(Tilemap tilemap, HashSet<Vector3Int> activePositions, Dictionary<Vector3Int, TileBase> cache, Func<Vector3Int, TileBase> tileResolver)
        {
            if (tilemap == null) return;
            
            // Remove tiles that are no longer active
            var toRemove = new List<Vector3Int>();
            foreach (var cachedPos in cache.Keys)
            {
                if (!activePositions.Contains(cachedPos))
                {
                    tilemap.SetTile(cachedPos, null);
                    toRemove.Add(cachedPos);
                }
            }
            foreach (var pos in toRemove)
            {
                cache.Remove(pos);
            }
            
            // Add or update active tiles
            foreach (var pos in activePositions)
            {
                TileBase newTile = tileResolver(pos);
                if (cache.TryGetValue(pos, out TileBase existingTile) && existingTile == newTile)
                {
                    // Same tile, skip to preserve animation
                    continue;
                }
                cache[pos] = newTile;
                tilemap.SetTile(pos, newTile);
            }
        }
        
        private void RefreshOverlayTilemapWithFacility(Tilemap tilemap, HashSet<Vector3Int> activePositions, Dictionary<Vector3Int, TileBase> cache, GridPosition repairStationPosition, GridPosition robotFactoryPosition)
        {
            if (tilemap == null) return;
            
            // Remove tiles that are no longer active
            var toRemove = new List<Vector3Int>();
            foreach (var cachedPos in cache.Keys)
            {
                if (!activePositions.Contains(cachedPos))
                {
                    tilemap.SetTile(cachedPos, null);
                    toRemove.Add(cachedPos);
                }
            }
            foreach (var pos in toRemove)
            {
                cache.Remove(pos);
            }
            
            // Add or update active tiles
            foreach (var pos in activePositions)
            {
                TileBase newTile = pos.Equals(ToTilePosition(repairStationPosition)) ? assets.RepairStationTile : assets.RobotFactoryTile;
                if (cache.TryGetValue(pos, out TileBase existingTile) && existingTile == newTile)
                {
                    // Same tile, skip to preserve animation
                    continue;
                }
                cache[pos] = newTile;
                tilemap.SetTile(pos, newTile);
            }
        }
        
        private void RefreshDangerOverlayTilemap(Tilemap tilemap, HashSet<Vector3Int> activePositions, Dictionary<Vector3Int, TileBase> cache, int currentWave)
        {
            if (tilemap == null) return;
            
            // Check if danger zone has actually changed
            bool positionsChanged = !lastDangerPositions.SetEquals(activePositions);
            bool waveChanged = lastDangerWave != currentWave;
            
            if (!positionsChanged && !waveChanged)
            {
                // No change, skip refresh to avoid interrupting animations
                return;
            }
            
            // Update tracked state
            lastDangerPositions = new HashSet<Vector3Int>(activePositions);
            lastDangerWave = currentWave;
            
            // Clear all and refresh danger tiles to keep animations in sync
            tilemap.ClearAllTiles();
            cache.Clear();
            
            foreach (var pos in activePositions)
            {
                TileBase newTile = assets.ResolveDangerOverlayTile(DangerOverlayGeometryKind.Base, currentWave);
                cache[pos] = newTile;
                tilemap.SetTile(pos, newTile);
            }
        }

        private void RefreshBuildPreviewOverlay(LogicalGridState grid)
        {
            var buildPreviewPositions = new HashSet<Vector3Int>();
            if (buildPreviewDefinition != null && buildPreviewOrigin.HasValue)
            {
                foreach (GridPosition previewPosition in FootprintCells(buildPreviewDefinition, buildPreviewOrigin.Value))
                {
                    if (grid.IsInside(previewPosition))
                    {
                        buildPreviewPositions.Add(ToTilePosition(previewPosition));
                    }
                }
            }

            RefreshOverlayTilemap(
                BuildPreviewTilemap,
                buildPreviewPositions,
                buildPreviewTileCache,
                _ => buildPreviewIsValid ? assets.BuildPreviewValidTile : assets.BuildPreviewInvalidTile);
        }

        public static Vector3Int ToTilePosition(GridPosition position)
        {
            return new Vector3Int(position.X, position.Y, 0);
        }

        public Tilemap GetTerrainTilemap(TerrainRenderLayerId layerId)
        {
            int index = DualGridTerrain.GetOrderedLayerIndex(layerId);
            return index >= 0 && index < TerrainTilemaps.Count ? TerrainTilemaps[index] : null;
        }

        private void UpdateTerrainCache(LogicalGridState grid, ICollection<GridPosition> changedCells)
        {
            foreach (GridPosition position in changedCells)
            {
                terrainMaterialCache[ToMaterialIndex(grid.Size, position)] = DualGridTerrain.MaterialForCell(grid.GetCell(position));
            }
        }

        private void UpdateFogCache(LogicalGridState grid, ICollection<GridPosition> changedCells)
        {
            foreach (GridPosition position in changedCells)
            {
                int cellIndex = ToMaterialIndex(grid.Size, position);
                fogNearMaskCache[cellIndex] = DualGridFog.IsNear(grid, position);
                fogDeepMaskCache[cellIndex] = DualGridFog.IsDeep(grid, position);
            }
        }

        private void RefreshMarkerCells(LogicalGridState grid, ICollection<GridPosition> changedCells)
        {
            if (MarkerTilemap == null)
            {
                return;
            }

            foreach (GridPosition position in changedCells)
            {
                Vector3Int tilePosition = ToTilePosition(position);
                TileBase tile = grid.GetCell(position).IsMarked ? assets.MarkerTile : null;
                if (tile != null)
                {
                    markerTileCache[tilePosition] = tile;
                    MarkerTilemap.SetTile(tilePosition, tile);
                    continue;
                }

                markerTileCache.Remove(tilePosition);
                MarkerTilemap.SetTile(tilePosition, null);
            }
        }

        private void RefreshTerrain(LogicalGridState grid, bool fullRebuild, ICollection<GridPosition> changedTerrainCells)
        {
            if (TerrainTilemaps.Count == 0)
            {
                return;
            }

            if (fullRebuild)
            {
                var source = new LogicalGridMaterialSource(grid);
                var target = new TilemapDualGridRenderTarget(TerrainTilemaps, assets);
                terrainRenderer.RebuildAll(source, target);
                return;
            }

            terrainRenderer.RefreshChanged(
                new LogicalGridMaterialSource(grid),
                new TilemapDualGridRenderTarget(TerrainTilemaps, assets),
                changedTerrainCells);
        }

        private void RefreshFog(LogicalGridState grid, bool fullRebuild, ICollection<GridPosition> changedFogCells)
        {
            if (FogNearTilemap == null && FogDeepTilemap == null)
            {
                return;
            }

            if (fullRebuild)
            {
                fogRenderer.RebuildAll(grid, FogNearTilemap, FogDeepTilemap, assets);
                return;
            }

            fogRenderer.RefreshChanged(grid, FogNearTilemap, FogDeepTilemap, assets, changedFogCells);
        }

        private static System.Collections.Generic.IEnumerable<GridPosition> FootprintCells(BuildingDefinition definition, GridPosition origin)
        {
            Vector2Int size = definition.FootprintSize;
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    yield return new GridPosition(origin.X + x, origin.Y + y);
                }
            }
        }

        private bool EnsureGridCaches(Vector2Int size)
        {
            if (terrainMaterialCache != null && fogNearMaskCache != null && fogDeepMaskCache != null && cachedGridSize == size)
            {
                return false;
            }

            cachedGridSize = size;
            int cellCount = size.x * size.y;
            terrainMaterialCache = new TerrainMaterialId[cellCount];
            fogNearMaskCache = new bool[cellCount];
            fogDeepMaskCache = new bool[cellCount];
            return true;
        }

        private static int ToMaterialIndex(Vector2Int size, GridPosition position)
        {
            return position.Y * size.x + position.X;
        }
    }
}
