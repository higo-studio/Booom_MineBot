using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Presentation
{
    public enum DualGridFogBandKind : byte
    {
        Near = 0,
        Deep = 1
    }

    public static class DualGridFog
    {
        public const int TileCount = DualGridContour.TileCount;
        public static readonly Vector3 DisplayOffset = DualGridTerrain.DisplayOffset;

        public static bool IsSolid(GridCellState cell)
        {
            return !cell.IsRevealed && cell.TerrainKind != TerrainKind.Empty;
        }

        public static bool IsNear(LogicalGridState grid, GridPosition position)
        {
            if (!grid.IsInside(position) || !IsSolid(grid.GetCell(position)))
            {
                return false;
            }

            for (int y = position.Y - 1; y <= position.Y + 1; y++)
            {
                for (int x = position.X - 1; x <= position.X + 1; x++)
                {
                    if (x == position.X && y == position.Y)
                    {
                        continue;
                    }

                    var neighbor = new GridPosition(x, y);
                    if (grid.IsInside(neighbor) && grid.GetCell(neighbor).IsRevealed)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsDeep(LogicalGridState grid, GridPosition position)
        {
            return grid.IsInside(position)
                && IsSolid(grid.GetCell(position))
                && !IsNear(grid, position);
        }

        public static int ComputeIndex(LogicalGridState grid, int displayX, int displayY, DualGridFogBandKind bandKind)
        {
            return DualGridContour.ComputeIndex(
                SampleBand(grid, displayX - 1, displayY, bandKind),
                SampleBand(grid, displayX, displayY, bandKind),
                SampleBand(grid, displayX - 1, displayY - 1, bandKind),
                SampleBand(grid, displayX, displayY - 1, bandKind));
        }

        public static Vector3Int[] GetAffectedDisplayCells(GridPosition cell)
        {
            return DualGridContour.GetAffectedContourCells(cell);
        }

        private static bool SampleBand(LogicalGridState grid, int x, int y, DualGridFogBandKind bandKind)
        {
            var position = new GridPosition(x, y);
            if (!grid.IsInside(position))
            {
                return false;
            }

            return bandKind == DualGridFogBandKind.Near
                ? IsNear(grid, position)
                : IsDeep(grid, position);
        }
    }
}
