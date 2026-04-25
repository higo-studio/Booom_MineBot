using System.Collections.Generic;
using Minebot.Common;
using Minebot.GridMining;

namespace Minebot.HazardInference
{
    public readonly struct ExplosionResolution
    {
        public ExplosionResolution(int destroyedCells, int directDamage)
        {
            DestroyedCells = destroyedCells;
            DirectDamage = directDamage;
        }

        public int DestroyedCells { get; }
        public int DirectDamage { get; }
    }

    public sealed class HazardService
    {
        private readonly LogicalGridState grid;

        public HazardService(LogicalGridState grid)
        {
            this.grid = grid;
        }

        public void SeedBombs(int seed, float chance)
        {
            SeedBombs(seed, chance, grid.PlayerSpawn, 0);
        }

        public void SeedBombs(int seed, float chance, GridPosition safeOrigin, int safeRadius)
        {
            var random = new DeterministicRandom(seed);
            float clampedChance = UnityEngine.Mathf.Clamp01(chance);
            foreach (GridPosition position in grid.Positions())
            {
                ref GridCellState cell = ref grid.GetCellRef(position);
                if (position.ManhattanDistance(safeOrigin) <= safeRadius)
                {
                    continue;
                }

                if (cell.IsMineable && !cell.IsRevealed && random.Value() < clampedChance)
                {
                    cell.StaticFlags |= CellStaticFlags.Bomb;
                }
            }
        }

        public int ScanBombCount(GridPosition origin, bool eightWay)
        {
            GridPosition[] directions = eightWay ? GridDirections.EightWay : GridDirections.Cardinal;
            int count = 0;
            foreach (GridPosition position in grid.Neighbors(origin, directions))
            {
                if (grid.GetCell(position).HasBomb)
                {
                    count++;
                }
            }

            return count;
        }

        public bool ToggleMarker(GridPosition position)
        {
            if (!grid.IsInside(position))
            {
                return false;
            }

            ref GridCellState cell = ref grid.GetCellRef(position);
            if (!cell.IsMineable || cell.IsRevealed)
            {
                return false;
            }

            cell.IsMarked = !cell.IsMarked;
            return cell.IsMarked;
        }

        public ExplosionResolution ResolveExplosion(GridPosition origin, int radius, int directDamage)
        {
            var pending = new Queue<GridPosition>();
            var visited = new HashSet<GridPosition>();
            pending.Enqueue(origin);
            int destroyed = 0;

            while (pending.Count > 0)
            {
                GridPosition current = pending.Dequeue();
                if (!grid.IsInside(current) || !visited.Add(current))
                {
                    continue;
                }

                ref GridCellState cell = ref grid.GetCellRef(current);
                bool hadBomb = cell.HasBomb;
                if (cell.TerrainKind != TerrainKind.Indestructible)
                {
                    cell.TerrainKind = TerrainKind.Empty;
                    cell.IsRevealed = true;
                    cell.IsMarked = false;
                    cell.ClearBomb();
                    destroyed++;
                }

                foreach (GridPosition position in PositionsInRadius(current, radius))
                {
                    if (!grid.IsInside(position))
                    {
                        continue;
                    }

                    ref GridCellState affected = ref grid.GetCellRef(position);
                    if (affected.TerrainKind != TerrainKind.Indestructible)
                    {
                        affected.TerrainKind = TerrainKind.Empty;
                        affected.IsRevealed = true;
                        affected.IsMarked = false;
                    }

                    if (hadBomb && affected.HasBomb)
                    {
                        pending.Enqueue(position);
                    }
                }
            }

            return new ExplosionResolution(destroyed, directDamage);
        }

        private static IEnumerable<GridPosition> PositionsInRadius(GridPosition origin, int radius)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (System.Math.Abs(x) + System.Math.Abs(y) <= radius)
                    {
                        yield return new GridPosition(origin.X + x, origin.Y + y);
                    }
                }
            }
        }
    }
}
