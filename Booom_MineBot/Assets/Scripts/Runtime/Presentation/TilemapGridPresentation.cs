using Minebot.Bootstrap;
using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Presentation
{
    public sealed class TilemapGridPresentation : MonoBehaviour
    {
        private MinebotPresentationAssets assets;
        private GridPosition? scanOrigin;

        public Tilemap TerrainTilemap { get; private set; }
        public Tilemap FacilityTilemap { get; private set; }
        public Tilemap OverlayTilemap { get; private set; }
        public Tilemap HintTilemap { get; private set; }

        internal void Configure(
            Tilemap terrainTilemap,
            Tilemap facilityTilemap,
            Tilemap overlayTilemap,
            Tilemap hintTilemap,
            MinebotPresentationAssets presentationAssets)
        {
            TerrainTilemap = terrainTilemap;
            FacilityTilemap = facilityTilemap;
            OverlayTilemap = overlayTilemap;
            HintTilemap = hintTilemap;
            assets = presentationAssets;
        }

        public void ShowScanAt(GridPosition origin)
        {
            scanOrigin = origin;
        }

        public void Refresh(RuntimeServiceRegistry services, GridPosition repairStationPosition, GridPosition robotFactoryPosition)
        {
            if (services == null || assets == null || TerrainTilemap == null)
            {
                return;
            }

            TerrainTilemap.ClearAllTiles();
            FacilityTilemap.ClearAllTiles();
            OverlayTilemap.ClearAllTiles();
            HintTilemap.ClearAllTiles();

            LogicalGridState grid = services.Grid;
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
                    OverlayTilemap.SetTile(tilePosition, assets.MarkerTile);
                }
                else if (cell.IsDangerZone)
                {
                    OverlayTilemap.SetTile(tilePosition, assets.DangerTile);
                }

                if (scanOrigin.HasValue && scanOrigin.Value.Equals(position))
                {
                    HintTilemap.SetTile(tilePosition, assets.ScanHintTile);
                }
            }

            TerrainTilemap.CompressBounds();
            FacilityTilemap.CompressBounds();
            OverlayTilemap.CompressBounds();
            HintTilemap.CompressBounds();
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
                    return assets.WallTile;
                case TerrainKind.Indestructible:
                    return assets.BoundaryTile;
                default:
                    return assets.BoundaryTile;
            }
        }
    }
}
