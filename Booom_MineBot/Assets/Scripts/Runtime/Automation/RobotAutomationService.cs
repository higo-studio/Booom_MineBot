using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Automation
{
    public sealed class RobotState
    {
        public RobotState(GridPosition position)
        {
            Position = position;
            IsActive = true;
        }

        public GridPosition Position { get; private set; }
        public bool IsActive { get; private set; }

        public void MoveTo(GridPosition position)
        {
            Position = position;
        }

        public void Destroy()
        {
            IsActive = false;
        }
    }

    public sealed class RobotAutomationService
    {
        private readonly LogicalGridState grid;

        public RobotAutomationService(LogicalGridState grid)
        {
            this.grid = grid;
        }

        public bool TrySelectNearestSafeMineTarget(RobotState robot, out GridPosition target)
        {
            target = default;
            int bestDistance = int.MaxValue;
            bool found = false;

            foreach (GridPosition position in grid.Positions())
            {
                GridCellState cell = grid.GetCell(position);
                if (!cell.IsMineable || cell.IsMarked || cell.HasBomb)
                {
                    continue;
                }

                int distance = robot.Position.ManhattanDistance(position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    target = position;
                    found = true;
                }
            }

            return found;
        }

        public bool StepToward(RobotState robot, GridPosition target)
        {
            GridPosition delta = target - robot.Position;
            GridPosition step = Mathf.Abs(delta.X) > Mathf.Abs(delta.Y)
                ? new GridPosition(delta.X > 0 ? 1 : -1, 0)
                : new GridPosition(0, delta.Y > 0 ? 1 : -1);

            GridPosition next = robot.Position + step;
            if (!grid.IsInside(next) || !grid.GetCell(next).IsPassable)
            {
                return false;
            }

            robot.MoveTo(next);
            return true;
        }
    }
}
