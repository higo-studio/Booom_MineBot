using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Presentation
{
    public static class DualGridContour
    {
        public const int TileCount = 16;

        public static int ComputeIndex(bool topLeft, bool topRight, bool bottomLeft, bool bottomRight)
        {
            int index = 0;
            if (topLeft)
            {
                index |= 1 << 3;
            }

            if (topRight)
            {
                index |= 1 << 2;
            }

            if (bottomLeft)
            {
                index |= 1 << 1;
            }

            if (bottomRight)
            {
                index |= 1;
            }

            return index;
        }

        public static int ComputeIndex(LogicalGridState grid, int contourX, int contourY, CellMaskDelegate cellMask)
        {
            return ComputeIndex(
                SampleMask(grid, contourX - 1, contourY, cellMask),
                SampleMask(grid, contourX, contourY, cellMask),
                SampleMask(grid, contourX - 1, contourY - 1, cellMask),
                SampleMask(grid, contourX, contourY - 1, cellMask));
        }

        public static Vector3Int[] GetAffectedContourCells(GridPosition cell)
        {
            return new[]
            {
                new Vector3Int(cell.X, cell.Y, 0),
                new Vector3Int(cell.X + 1, cell.Y, 0),
                new Vector3Int(cell.X, cell.Y + 1, 0),
                new Vector3Int(cell.X + 1, cell.Y + 1, 0)
            };
        }

        private static bool SampleMask(LogicalGridState grid, int x, int y, CellMaskDelegate cellMask)
        {
            var position = new GridPosition(x, y);
            return grid.IsInside(position) && cellMask(grid.GetCell(position));
        }

        public delegate bool CellMaskDelegate(GridCellState cell);
    }
}
