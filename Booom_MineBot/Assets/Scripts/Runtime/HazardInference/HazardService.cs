using System.Collections.Generic;
using Minebot.Common;
using Minebot.GridMining;

namespace Minebot.HazardInference
{
    public readonly struct ScanReading
    {
        public ScanReading(GridPosition wallPosition, int bombCount)
        {
            WallPosition = wallPosition;
            BombCount = bombCount;
        }

        public GridPosition WallPosition { get; }
        public int BombCount { get; }
    }

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

        public IReadOnlyList<ScanReading> ScanFrontierWalls(GridPosition playerPosition, int frontierRange)
        {
            var results = new List<ScanReading>();
            int maxFrontierRange = UnityEngine.Mathf.Max(0, frontierRange);
            foreach (GridPosition position in grid.Positions())
            {
                if (!IsScannableFrontierWall(position, playerPosition, maxFrontierRange))
                {
                    continue;
                }

                results.Add(new ScanReading(position, GridBombCounter.CountBombsInScanSquare(grid, position)));
            }

            return results;
        }

        public int CountBombsInScanSquare(GridPosition origin)
        {
            return GridBombCounter.CountBombsInScanSquare(grid, origin);
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

        private bool IsScannableFrontierWall(GridPosition wallPosition, GridPosition playerPosition, int frontierRange)
        {
            if (!grid.IsInside(wallPosition))
            {
                return false;
            }

            GridCellState wallCell = grid.GetCell(wallPosition);
            if (!wallCell.IsMineable)
            {
                return false;
            }

            int nearestFrontierDistance = int.MaxValue;
            foreach (GridPosition neighbor in grid.Neighbors(wallPosition, GridDirections.Cardinal))
            {
                GridCellState neighborCell = grid.GetCell(neighbor);
                if (neighborCell.TerrainKind != TerrainKind.Empty || neighborCell.IsOccupiedByBuilding)
                {
                    continue;
                }

                nearestFrontierDistance = UnityEngine.Mathf.Min(
                    nearestFrontierDistance,
                    playerPosition.ManhattanDistance(neighbor));
            }

            return nearestFrontierDistance != int.MaxValue && nearestFrontierDistance <= frontierRange;
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
