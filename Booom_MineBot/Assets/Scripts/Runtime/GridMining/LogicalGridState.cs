using System;
using System.Collections.Generic;
using Minebot.Common;
using UnityEngine;

namespace Minebot.GridMining
{
    public sealed class LogicalGridState
    {
        private readonly GridCellState[] cells;

        public LogicalGridState(Vector2Int size, GridPosition playerSpawn, IReadOnlyList<GridCellState> initialCells)
        {
            if (size.x <= 0 || size.y <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            if (initialCells == null || initialCells.Count != size.x * size.y)
            {
                throw new ArgumentException("Initial cell count must match map size.", nameof(initialCells));
            }

            Size = size;
            PlayerSpawn = playerSpawn;
            cells = new GridCellState[initialCells.Count];
            for (int i = 0; i < initialCells.Count; i++)
            {
                cells[i] = initialCells[i];
            }
        }

        public Vector2Int Size { get; }
        public GridPosition PlayerSpawn { get; }

        public bool IsInside(GridPosition position)
        {
            return position.X >= 0 && position.Y >= 0 && position.X < Size.x && position.Y < Size.y;
        }

        public ref GridCellState GetCellRef(GridPosition position)
        {
            if (!IsInside(position))
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return ref cells[ToIndex(position)];
        }

        public GridCellState GetCell(GridPosition position)
        {
            return GetCellRef(position);
        }

        public IEnumerable<GridPosition> Positions()
        {
            for (int y = 0; y < Size.y; y++)
            {
                for (int x = 0; x < Size.x; x++)
                {
                    yield return new GridPosition(x, y);
                }
            }
        }

        public IEnumerable<GridPosition> Neighbors(GridPosition origin, GridPosition[] directions)
        {
            foreach (GridPosition direction in directions)
            {
                GridPosition candidate = origin + direction;
                if (IsInside(candidate))
                {
                    yield return candidate;
                }
            }
        }

        private int ToIndex(GridPosition position)
        {
            return position.Y * Size.x + position.X;
        }
    }
}
