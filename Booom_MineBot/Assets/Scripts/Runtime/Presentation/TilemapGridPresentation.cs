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
        private readonly IDualGridTerrainResolver terrainResolver = new LayeredBinaryTerrainResolver();
        private readonly RenderLayerCommand[] terrainCommandBuffer = new RenderLayerCommand[DualGridTerrain.RenderLayerCount];
        private BuildingDefinition buildPreviewDefinition;
        private GridPosition? buildPreviewOrigin;
        private bool buildPreviewIsValid;
        private Vector2Int cachedGridSize;
        private TerrainMaterialId[] terrainMaterialCache;

        public Tilemap TerrainTilemap => GetTerrainTilemap(TerrainRenderLayerId.Floor);
        public IReadOnlyList<Tilemap> TerrainTilemaps { get; private set; } = Array.Empty<Tilemap>();
        public Tilemap FacilityTilemap { get; private set; }
        public Tilemap MarkerTilemap { get; private set; }
        public Tilemap DangerTilemap { get; private set; }
        public Tilemap BuildPreviewTilemap { get; private set; }

        public void Configure(
            Tilemap[] terrainTilemaps,
            Tilemap facilityTilemap,
            Tilemap markerTilemap,
            Tilemap dangerTilemap,
            Tilemap buildPreviewTilemap,
            MinebotPresentationAssets presentationAssets)
        {
            TerrainTilemaps = terrainTilemaps ?? Array.Empty<Tilemap>();
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
            bool fullRebuild = EnsureMaterialCaches(grid.Size);
            var changedTerrainCells = new HashSet<GridPosition>();
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
                    DangerTilemap.SetTile(tilePosition, assets.DangerTile);
                }

                int cellIndex = ToMaterialIndex(grid.Size, position);
                TerrainMaterialId material = DualGridTerrain.MaterialForCell(cell);
                if (fullRebuild || terrainMaterialCache[cellIndex] != material)
                {
                    terrainMaterialCache[cellIndex] = material;
                    changedTerrainCells.Add(position);
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

            for (int i = 0; i < TerrainTilemaps.Count; i++)
            {
                TerrainTilemaps[i]?.CompressBounds();
            }

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
            int index = (int)layerId;
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
                ClearTerrainTiles();
                for (int y = 0; y <= grid.Size.y; y++)
                {
                    for (int x = 0; x <= grid.Size.x; x++)
                    {
                        UpdateTerrainAt(grid, new Vector3Int(x, y, 0));
                    }
                }

                return;
            }

            var affectedDisplayCells = new HashSet<Vector3Int>();
            foreach (GridPosition changed in changedTerrainCells)
            {
                Vector3Int[] affected = DualGridTerrain.GetAffectedDisplayCells(changed);
                for (int i = 0; i < affected.Length; i++)
                {
                    affectedDisplayCells.Add(affected[i]);
                }
            }

            foreach (Vector3Int position in affectedDisplayCells)
            {
                UpdateTerrainAt(grid, position);
            }
        }

        private void UpdateTerrainAt(LogicalGridState grid, Vector3Int displayPosition)
        {
            CornerMaterialSample sample = DualGridTerrain.Sample(grid, displayPosition.x, displayPosition.y);
            terrainResolver.Resolve(sample, terrainCommandBuffer);
            for (int i = 0; i < TerrainTilemaps.Count && i < terrainCommandBuffer.Length; i++)
            {
                Tilemap terrainTilemap = TerrainTilemaps[i];
                if (terrainTilemap == null)
                {
                    continue;
                }

                RenderLayerCommand command = terrainCommandBuffer[i];
                Tile tile = command.HasContent ? assets.DualGridTerrainTileFor(command.LayerId, command.AtlasIndex) : null;
                terrainTilemap.SetTile(displayPosition, tile);
            }
        }

        private void ClearTerrainTiles()
        {
            for (int i = 0; i < TerrainTilemaps.Count; i++)
            {
                TerrainTilemaps[i]?.ClearAllTiles();
            }
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

        private bool EnsureMaterialCaches(Vector2Int size)
        {
            if (terrainMaterialCache != null && cachedGridSize == size)
            {
                return false;
            }

            cachedGridSize = size;
            int cellCount = size.x * size.y;
            terrainMaterialCache = new TerrainMaterialId[cellCount];
            return true;
        }

        private static int ToMaterialIndex(Vector2Int size, GridPosition position)
        {
            return position.Y * size.x + position.X;
        }
    }
}
