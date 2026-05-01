using System.Collections.Generic;
using Minebot.Common;

namespace Minebot.GridMining
{
    public enum MineInteractionResult
    {
        InvalidTarget,
        Moved,
        BlockedByTerrain,
        DrillTooWeak,
        Mined,
        TriggeredBomb
    }

    public readonly struct MineClearedCell
    {
        public MineClearedCell(GridPosition position, ResourceAmount reward, bool wasBomb)
        {
            Position = position;
            Reward = reward;
            WasBomb = wasBomb;
        }

        public GridPosition Position { get; }
        public ResourceAmount Reward { get; }
        public bool WasBomb { get; }
    }

    public readonly struct MineResolution
    {
        public MineResolution(MineInteractionResult result, IReadOnlyList<MineClearedCell> clearedCells, ResourceAmount totalReward)
        {
            Result = result;
            ClearedCells = clearedCells ?? System.Array.Empty<MineClearedCell>();
            TotalReward = totalReward;
        }

        public MineInteractionResult Result { get; }
        public IReadOnlyList<MineClearedCell> ClearedCells { get; }
        public ResourceAmount TotalReward { get; }
    }

    public sealed class PlayerMiningState
    {
        public PlayerMiningState(GridPosition position, HardnessTier drillTier)
        {
            Position = position;
            DrillTier = drillTier;
        }

        public GridPosition Position { get; private set; }
        public HardnessTier DrillTier { get; set; }

        public void Teleport(GridPosition position)
        {
            Position = position;
        }
    }

    public sealed class MiningService
    {
        private readonly LogicalGridState grid;

        public MiningService(LogicalGridState grid)
        {
            this.grid = grid;
        }

        public MineInteractionResult Move(PlayerMiningState player, GridPosition direction)
        {
            GridPosition target = player.Position + direction;
            if (!grid.IsInside(target))
            {
                return MineInteractionResult.InvalidTarget;
            }

            if (!grid.GetCell(target).IsPassable)
            {
                return MineInteractionResult.BlockedByTerrain;
            }

            player.Teleport(target);
            return MineInteractionResult.Moved;
        }

        public MineInteractionResult TryMine(PlayerMiningState player, GridPosition target, out ResourceAmount reward)
        {
            MineResolution resolution = TryMineDetailed(player, target);
            reward = resolution.TotalReward;
            return resolution.Result;
        }

        public MineInteractionResult TryMineFrom(GridPosition actorPosition, HardnessTier drillTier, GridPosition target, out ResourceAmount reward)
        {
            MineResolution resolution = TryMineDetailedFrom(actorPosition, drillTier, target);
            reward = resolution.TotalReward;
            return resolution.Result;
        }

        public MineResolution TryMineDetailed(PlayerMiningState player, GridPosition target)
        {
            return TryMineDetailedFrom(player.Position, player.DrillTier, target);
        }

        public MineResolution TryMineDetailedFrom(GridPosition actorPosition, HardnessTier drillTier, GridPosition target)
        {
            if (!grid.IsInside(target) || actorPosition.ManhattanDistance(target) != 1)
            {
                return new MineResolution(MineInteractionResult.InvalidTarget, System.Array.Empty<MineClearedCell>(), ResourceAmount.Zero);
            }

            ref GridCellState cell = ref grid.GetCellRef(target);
            if (!cell.IsMineable)
            {
                return new MineResolution(MineInteractionResult.BlockedByTerrain, System.Array.Empty<MineClearedCell>(), ResourceAmount.Zero);
            }

            if (cell.HardnessTier > drillTier)
            {
                return new MineResolution(MineInteractionResult.DrillTooWeak, System.Array.Empty<MineClearedCell>(), ResourceAmount.Zero);
            }

            var clearedCells = new List<MineClearedCell>();
            if (cell.HasBomb)
            {
                ResourceAmount reward = OpenCell(target, clearBomb: true);
                clearedCells.Add(new MineClearedCell(target, reward, true));
                return new MineResolution(MineInteractionResult.TriggeredBomb, clearedCells, reward);
            }

            ExpandSafeRegion(target, drillTier, clearedCells);
            return new MineResolution(MineInteractionResult.Mined, clearedCells, SumRewards(clearedCells));
        }

        private void ExpandSafeRegion(GridPosition origin, HardnessTier drillTier, List<MineClearedCell> clearedCells)
        {
            var pending = new Queue<GridPosition>();
            var visited = new HashSet<GridPosition>();
            pending.Enqueue(origin);

            while (pending.Count > 0)
            {
                GridPosition current = pending.Dequeue();
                if (!visited.Add(current) || !grid.IsInside(current))
                {
                    continue;
                }

                GridCellState currentCell = grid.GetCell(current);
                if (!currentCell.IsMineable || currentCell.HardnessTier > drillTier || currentCell.HasBomb)
                {
                    continue;
                }

                int bombCount = GridBombCounter.CountBombsInScanSquare(grid, current);
                ResourceAmount reward = OpenCell(current, clearBomb: false);
                clearedCells.Add(new MineClearedCell(current, reward, false));
                if (bombCount > 0)
                {
                    continue;
                }

                foreach (GridPosition neighbor in grid.Neighbors(current, GridDirections.EightWay))
                {
                    if (!visited.Contains(neighbor))
                    {
                        pending.Enqueue(neighbor);
                    }
                }
            }
        }

        private ResourceAmount OpenCell(GridPosition position, bool clearBomb)
        {
            ref GridCellState cell = ref grid.GetCellRef(position);
            ResourceAmount reward = cell.Reward;
            cell.TerrainKind = TerrainKind.Empty;
            cell.IsRevealed = true;
            cell.IsMarked = false;
            if (clearBomb)
            {
                cell.ClearBomb();
            }

            return reward;
        }

        private static ResourceAmount SumRewards(IReadOnlyList<MineClearedCell> clearedCells)
        {
            ResourceAmount total = ResourceAmount.Zero;
            for (int i = 0; i < clearedCells.Count; i++)
            {
                total += clearedCells[i].Reward;
            }

            return total;
        }
    }
}
