using Minebot.Common;

namespace Minebot.GridMining
{
    public static class GridBombCounter
    {
        public static int CountBombsInScanSquare(LogicalGridState grid, GridPosition origin)
        {
            return CountBombsInSquare(grid, origin, 1);
        }

        private static int CountBombsInSquare(LogicalGridState grid, GridPosition origin, int radius)
        {
            int count = 0;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    GridPosition position = new GridPosition(origin.X + x, origin.Y + y);
                    if (grid.IsInside(position) && grid.GetCell(position).HasBomb)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
