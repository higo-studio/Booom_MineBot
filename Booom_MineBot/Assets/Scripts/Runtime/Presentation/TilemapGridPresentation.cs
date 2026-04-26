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
        private BuildingDefinition buildPreviewDefinition;
        private GridPosition? buildPreviewOrigin;
        private bool buildPreviewIsValid;
        private Vector2Int cachedGridSize;
        private bool[] wallMaskCache;
        private bool[] dangerMaskCache;

        public Tilemap TerrainTilemap { get; private set; }
        public Tilemap FacilityTilemap { get; private set; }
        public Tilemap MarkerTilemap { get; private set; }
        public Tilemap DangerTilemap { get; private set; }
        public Tilemap WallContourTilemap { get; private set; }
        public Tilemap DangerContourTilemap { get; private set; }
        public Tilemap BuildPreviewTilemap { get; private set; }

        internal void Configure(
            Tilemap terrainTilemap,
            Tilemap facilityTilemap,
            Tilemap markerTilemap,
            Tilemap wallContourTilemap,
            Tilemap dangerTilemap,
            Tilemap dangerContourTilemap,
            Tilemap buildPreviewTilemap,
            MinebotPresentationAssets presentationAssets)
        {
            TerrainTilemap = terrainTilemap;
            FacilityTilemap = facilityTilemap;
            MarkerTilemap = markerTilemap;
            WallContourTilemap = wallContourTilemap;
            DangerTilemap = dangerTilemap;
            DangerContourTilemap = dangerContourTilemap;
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
            if (services == null || assets == null || TerrainTilemap == null)
            {
                return;
            }

            TerrainTilemap.ClearAllTiles();
            FacilityTilemap.ClearAllTiles();
            MarkerTilemap.ClearAllTiles();
            DangerTilemap.ClearAllTiles();
            BuildPreviewTilemap.ClearAllTiles();

            LogicalGridState grid = services.Grid;
            bool fullRebuild = EnsureMaskCaches(grid.Size);
            bool dangerChanged = fullRebuild;
            var changedWallCells = new System.Collections.Generic.HashSet<GridPosition>();
            foreach (GridPosition position in grid.Positions())
            {
                GridCellState cell = grid.GetCell(position);
                Vector3Int tilePosition = ToTilePosition(position);

                TerrainTilemap.SetTile(tilePosition, TileForTerrain(cell));

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

                bool wallMask = cell.TerrainKind == TerrainKind.MineableWall;
                bool dangerMask = cell.TerrainKind == TerrainKind.Empty && cell.IsDangerZone;
                if (dangerMask)
                {
                    DangerTilemap.SetTile(tilePosition, assets.DangerTile);
                }

                int cellIndex = ToMaskIndex(grid.Size, position);
                if (fullRebuild || wallMaskCache[cellIndex] != wallMask)
                {
                    wallMaskCache[cellIndex] = wallMask;
                    changedWallCells.Add(position);
                }

                if (fullRebuild || dangerMaskCache[cellIndex] != dangerMask)
                {
                    dangerMaskCache[cellIndex] = dangerMask;
                    dangerChanged = true;
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

            RefreshWallContours(grid, fullRebuild, changedWallCells);
            if (dangerChanged)
            {
                RefreshDangerContours(grid);
            }

            TerrainTilemap.CompressBounds();
            FacilityTilemap.CompressBounds();
            MarkerTilemap.CompressBounds();
            WallContourTilemap.CompressBounds();
            DangerTilemap.CompressBounds();
            DangerContourTilemap.CompressBounds();
            BuildPreviewTilemap.CompressBounds();
        }

        public static Vector3Int ToTilePosition(GridPosition position)
        {
            return new Vector3Int(position.X, position.Y, 0);
        }

        private Tile TileForTerrain(GridCellState cell)
        {
            switch (cell.TerrainKind)
            {
                case TerrainKind.Empty:
                    return assets.EmptyTile;
                case TerrainKind.MineableWall:
                    return assets.WallTileForHardness(cell.HardnessTier);
                case TerrainKind.Indestructible:
                    return assets.BoundaryTile;
                default:
                    return assets.BoundaryTile;
            }
        }

        private void RefreshWallContours(LogicalGridState grid, bool fullRebuild, System.Collections.Generic.ICollection<GridPosition> changedWallCells)
        {
            if (WallContourTilemap == null)
            {
                return;
            }

            if (fullRebuild)
            {
                WallContourTilemap.ClearAllTiles();
                for (int y = 0; y <= grid.Size.y; y++)
                {
                    for (int x = 0; x <= grid.Size.x; x++)
                    {
                        UpdateWallContourAt(grid, new Vector3Int(x, y, 0));
                    }
                }

                return;
            }

            foreach (GridPosition changed in changedWallCells)
            {
                Vector3Int[] affected = DualGridContour.GetAffectedContourCells(changed);
                for (int i = 0; i < affected.Length; i++)
                {
                    UpdateWallContourAt(grid, affected[i]);
                }
            }
        }

        private void RefreshDangerContours(LogicalGridState grid)
        {
            if (DangerContourTilemap == null)
            {
                return;
            }

            DangerContourTilemap.ClearAllTiles();
            for (int y = 0; y <= grid.Size.y; y++)
            {
                for (int x = 0; x <= grid.Size.x; x++)
                {
                    int contourIndex = DualGridContour.ComputeIndex(grid, x, y, IsDangerMask);
                    Tile tile = assets.DangerContourTileForIndex(contourIndex);
                    DangerContourTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        private void UpdateWallContourAt(LogicalGridState grid, Vector3Int contourPosition)
        {
            int contourIndex = DualGridContour.ComputeIndex(grid, contourPosition.x, contourPosition.y, IsWallMask);
            Tile tile = assets.WallContourTileForIndex(contourIndex);
            WallContourTilemap.SetTile(contourPosition, tile);
        }

        private static bool IsWallMask(GridCellState cell)
        {
            return cell.TerrainKind == TerrainKind.MineableWall;
        }

        private static bool IsDangerMask(GridCellState cell)
        {
            return cell.TerrainKind == TerrainKind.Empty && cell.IsDangerZone;
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

        private bool EnsureMaskCaches(Vector2Int size)
        {
            if (wallMaskCache != null && dangerMaskCache != null && cachedGridSize == size)
            {
                return false;
            }

            cachedGridSize = size;
            int cellCount = size.x * size.y;
            wallMaskCache = new bool[cellCount];
            dangerMaskCache = new bool[cellCount];
            return true;
        }

        private static int ToMaskIndex(Vector2Int size, GridPosition position)
        {
            return position.Y * size.x + position.X;
        }
    }
}
