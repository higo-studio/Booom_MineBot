using System;
using System.Collections.Generic;
using Minebot.Bootstrap;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.Progression;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Presentation
{
    public sealed class TilemapGridPresentation : MonoBehaviour
    {
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

        public Tilemap TerrainTilemap => GetTerrainTilemap(TerrainRenderLayerId.Floor);
        public IReadOnlyList<Tilemap> TerrainTilemaps { get; private set; } = Array.Empty<Tilemap>();
        public Tilemap FogNearTilemap { get; private set; }
        public Tilemap FogDeepTilemap { get; private set; }
        public Tilemap FacilityTilemap { get; private set; }
        public Tilemap MarkerTilemap { get; private set; }
        public Tilemap DangerTilemap { get; private set; }
        public Tilemap BuildPreviewTilemap { get; private set; }

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

        public void Refresh(RuntimeServiceRegistry services, GridPosition repairStationPosition, GridPosition robotFactoryPosition)
        {
            if (services == null || assets == null || TerrainTilemaps.Count == 0)
            {
                return;
            }

            FacilityTilemap.ClearAllTiles();
            MarkerTilemap.ClearAllTiles();
            DangerTilemap.ClearAllTiles();
            BuildPreviewTilemap.ClearAllTiles();

            LogicalGridState grid = services.Grid;
            bool fullRebuild = EnsureGridCaches(grid.Size);
            var changedTerrainCells = new HashSet<GridPosition>();
            var changedFogCells = new HashSet<GridPosition>();
            foreach (GridPosition position in grid.Positions())
            {
                GridCellState cell = grid.GetCell(position);
                Vector3Int tilePosition = ToTilePosition(position);

                if (position.Equals(repairStationPosition))
                {
                    FacilityTilemap.SetTile(tilePosition, assets.RepairStationTile);
                }
                else if (position.Equals(robotFactoryPosition))
                {
                    FacilityTilemap.SetTile(tilePosition, assets.RobotFactoryTile);
                }

                if (cell.IsMarked)
                {
                    MarkerTilemap.SetTile(tilePosition, assets.MarkerTile);
                }

                bool dangerMask = cell.TerrainKind == TerrainKind.Empty && cell.IsDangerZone;
                if (dangerMask)
                {
                    int currentWave = services.Waves != null ? services.Waves.CurrentWave : 0;
                    DangerTilemap.SetTile(tilePosition, assets.ResolveDangerOverlayTile(DangerOverlayGeometryKind.Base, currentWave));
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

            if (buildPreviewDefinition != null && buildPreviewOrigin.HasValue)
            {
                TileBase previewTile = buildPreviewIsValid ? assets.BuildPreviewValidTile : assets.BuildPreviewInvalidTile;
                foreach (GridPosition previewPosition in FootprintCells(buildPreviewDefinition, buildPreviewOrigin.Value))
                {
                    if (grid.IsInside(previewPosition))
                    {
                        BuildPreviewTilemap.SetTile(ToTilePosition(previewPosition), previewTile);
                    }
                }
            }

            RefreshTerrain(grid, fullRebuild, changedTerrainCells);
            RefreshFog(grid, fullRebuild, changedFogCells);

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

        public static Vector3Int ToTilePosition(GridPosition position)
        {
            return new Vector3Int(position.X, position.Y, 0);
        }

        public Tilemap GetTerrainTilemap(TerrainRenderLayerId layerId)
        {
            int index = DualGridTerrain.GetOrderedLayerIndex(layerId);
            return index >= 0 && index < TerrainTilemaps.Count ? TerrainTilemaps[index] : null;
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
