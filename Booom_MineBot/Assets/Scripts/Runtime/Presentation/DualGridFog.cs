using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Presentation
{
    public enum DualGridFogBandKind : byte
    {
        [InspectorName("近雾")]
        Near = 0,
        [InspectorName("深雾")]
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
            ClassifyCell(grid, position, out bool near, out _);
            return near;
        }

        public static bool IsDeep(LogicalGridState grid, GridPosition position)
        {
            ClassifyCell(grid, position, out _, out bool deep);
            return deep;
        }

        public static int ComputeIndex(LogicalGridState grid, int displayX, int displayY, DualGridFogBandKind bandKind)
        {
            return DualGridContour.ComputeIndex(
                SampleBand(grid, displayX - 1, displayY, bandKind),
                SampleBand(grid, displayX, displayY, bandKind),
                SampleBand(grid, displayX - 1, displayY - 1, bandKind),
                SampleBand(grid, displayX, displayY - 1, bandKind));
        }

        public static int ComputeIndex(Vector2Int size, bool[] bandMask, int displayX, int displayY)
        {
            return DualGridContour.ComputeIndex(
                SampleBand(size, bandMask, displayX - 1, displayY),
                SampleBand(size, bandMask, displayX, displayY),
                SampleBand(size, bandMask, displayX - 1, displayY - 1),
                SampleBand(size, bandMask, displayX, displayY - 1));
        }

        public static Vector3Int[] GetAffectedDisplayCells(GridPosition cell)
        {
            return DualGridContour.GetAffectedContourCells(cell);
        }

        public static void ClassifyCell(LogicalGridState grid, GridPosition position, out bool near, out bool deep)
        {
            near = false;
            deep = false;

            if (!grid.IsInside(position) || !IsSolid(grid.GetCell(position)))
            {
                return;
            }

            near = HasRevealedNeighbor(grid, position);
            deep = !near;
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

        private static bool SampleBand(Vector2Int size, bool[] bandMask, int x, int y)
        {
            return bandMask != null
                && x >= 0
                && y >= 0
                && x < size.x
                && y < size.y
                && bandMask[(y * size.x) + x];
        }

        private static bool HasRevealedNeighbor(LogicalGridState grid, GridPosition position)
        {
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
    }
}
