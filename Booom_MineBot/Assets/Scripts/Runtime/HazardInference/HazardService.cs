using System.Collections.Generic;
using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.HazardInference
{
    public readonly struct ScanReading
    {
        public ScanReading(GridPosition cellPosition, int bombCount)
        {
            CellPosition = cellPosition;
            BombCount = bombCount;
        }

        public GridPosition CellPosition { get; }
        public GridPosition WallPosition => CellPosition;
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
            Debug.Log($"[HazardService] 开始生成炸弹 - 概率: {clampedChance:F4}, 安全半径: {safeRadius}");
            int mineableCount = 0;
            int bombCount = 0;
            foreach (GridPosition position in grid.Positions())
            {
                ref GridCellState cell = ref grid.GetCellRef(position);
                if (position.ManhattanDistance(safeOrigin) <= safeRadius)
                {
                    continue;
                }

                if (cell.IsMineable)
                {
                    mineableCount++;
                    if (!cell.IsRevealed && random.Value() < clampedChance)
                    {
                        cell.StaticFlags |= CellStaticFlags.Bomb;
                        bombCount++;
                    }
                }
            }
            Debug.Log($"[HazardService] 炸弹生成完成 - 可挖墙体: {mineableCount}, 生成炸弹: {bombCount}");
        }

        public IReadOnlyList<ScanReading> ScanNearbyEmptyCells(GridPosition playerPosition)
        {
            return ScanNearbyEmptyCells(
                playerPosition,
                HazardRules.DefaultScanFrontierRange,
                HazardRules.DefaultScanUsesEightWayNeighbors);
        }

        public IReadOnlyList<ScanReading> ScanNearbyEmptyCells(
            GridPosition playerPosition,
            int frontierRange,
            bool useEightWayNeighbors)
        {
            var results = new List<ScanReading>();
            int clampedRange = Mathf.Max(0, frontierRange);
            for (int y = -clampedRange; y <= clampedRange; y++)
            {
                for (int x = -clampedRange; x <= clampedRange; x++)
                {
                    if (!IsWithinScanRange(x, y, clampedRange, useEightWayNeighbors))
                    {
                        continue;
                    }

                    GridPosition position = new GridPosition(playerPosition.X + x, playerPosition.Y + y);
                    if (!IsScannableEmptyCell(position, useEightWayNeighbors))
                    {
                        continue;
                    }

                    results.Add(new ScanReading(position, GridBombCounter.CountBombsInScanSquare(grid, position)));
                }
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

        private bool IsScannableEmptyCell(GridPosition position, bool useEightWayNeighbors)
        {
            if (!grid.IsInside(position))
            {
                return false;
            }

            GridCellState cell = grid.GetCell(position);
            if (cell.TerrainKind != TerrainKind.Empty || cell.IsOccupiedByBuilding)
            {
                return false;
            }

            GridPosition[] directions = useEightWayNeighbors ? GridDirections.EightWay : GridDirections.Cardinal;
            foreach (GridPosition direction in directions)
            {
                GridPosition neighbor = position + direction;
                if (!grid.IsInside(neighbor))
                {
                    continue;
                }

                if (grid.GetCell(neighbor).IsMineable)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsWithinScanRange(int offsetX, int offsetY, int frontierRange, bool useEightWayNeighbors)
        {
            int absX = Mathf.Abs(offsetX);
            int absY = Mathf.Abs(offsetY);
            return useEightWayNeighbors
                ? Mathf.Max(absX, absY) <= frontierRange
                : absX + absY <= frontierRange;
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
