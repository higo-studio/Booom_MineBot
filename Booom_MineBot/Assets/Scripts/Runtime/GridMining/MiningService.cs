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
            return TryMineFrom(player.Position, player.DrillTier, target, out reward);
        }

        public MineInteractionResult TryMineFrom(GridPosition actorPosition, HardnessTier drillTier, GridPosition target, out ResourceAmount reward)
        {
            reward = ResourceAmount.Zero;
            if (!grid.IsInside(target) || actorPosition.ManhattanDistance(target) != 1)
            {
                return MineInteractionResult.InvalidTarget;
            }

            ref GridCellState cell = ref grid.GetCellRef(target);
            if (!cell.IsMineable)
            {
                return MineInteractionResult.BlockedByTerrain;
            }

            if (cell.HardnessTier > drillTier)
            {
                return MineInteractionResult.DrillTooWeak;
            }

            reward = cell.Reward;
            bool bomb = cell.HasBomb;
            cell.TerrainKind = TerrainKind.Empty;
            cell.IsRevealed = true;
            cell.IsMarked = false;
            return bomb ? MineInteractionResult.TriggeredBomb : MineInteractionResult.Mined;
        }
    }
}
