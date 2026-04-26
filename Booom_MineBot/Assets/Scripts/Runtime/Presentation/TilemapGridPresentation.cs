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

        public Tilemap TerrainTilemap { get; private set; }
        public Tilemap FacilityTilemap { get; private set; }
        public Tilemap MarkerTilemap { get; private set; }
        public Tilemap DangerTilemap { get; private set; }
        public Tilemap BuildPreviewTilemap { get; private set; }

        internal void Configure(
            Tilemap terrainTilemap,
            Tilemap facilityTilemap,
            Tilemap markerTilemap,
            Tilemap dangerTilemap,
            Tilemap buildPreviewTilemap,
            MinebotPresentationAssets presentationAssets)
        {
            TerrainTilemap = terrainTilemap;
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
            Tile dangerOutline = assets.DangerOutlineTileForWave(services.Waves != null ? services.Waves.CurrentWave : 0);
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
                if (cell.IsDangerZone && cell.TerrainKind == TerrainKind.Empty)
                {
                    DangerTilemap.SetTile(tilePosition, dangerOutline);
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

            TerrainTilemap.CompressBounds();
            FacilityTilemap.CompressBounds();
            MarkerTilemap.CompressBounds();
            DangerTilemap.CompressBounds();
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
    }
}
