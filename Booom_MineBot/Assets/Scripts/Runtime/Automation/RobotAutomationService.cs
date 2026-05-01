using System;
using System.Collections.Generic;
using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Automation
{
    public enum RobotActivity
    {
        Idle,
        Moving,
        Mining,
        Blocked,
        Destroyed
    }

    public enum RobotAutomationResultKind
    {
        None,
        Waiting,
        Idle,
        TargetAcquired,
        Moved,
        Mined,
        TriggeredBomb,
        Blocked,
        Destroyed,
        Paused
    }

    public readonly struct RobotAutomationResult
    {
        public RobotAutomationResult(RobotAutomationResultKind kind, RobotState robot, GridPosition target, ResourceAmount reward, string status, IReadOnlyList<MineClearedCell> clearedCells = null)
        {
            Kind = kind;
            Robot = robot;
            Target = target;
            Reward = reward;
            Status = status;
            ClearedCells = clearedCells ?? Array.Empty<MineClearedCell>();
        }

        public RobotAutomationResultKind Kind { get; }
        public RobotState Robot { get; }
        public GridPosition Target { get; }
        public ResourceAmount Reward { get; }
        public string Status { get; }
        public IReadOnlyList<MineClearedCell> ClearedCells { get; }
        public bool HasStateChange => Kind != RobotAutomationResultKind.None && Kind != RobotAutomationResultKind.Waiting;

        public static RobotAutomationResult None(RobotState robot)
        {
            return new RobotAutomationResult(RobotAutomationResultKind.None, robot, GridPosition.Zero, ResourceAmount.Zero, string.Empty);
        }
    }

    public sealed class RobotState
    {
        public RobotState(GridPosition position)
        {
            Position = position;
            IsActive = true;
            Activity = RobotActivity.Idle;
            StatusReason = "待机";
            ActionTimer = 0f;
        }

        public GridPosition Position { get; private set; }
        public bool IsActive { get; private set; }
        public RobotActivity Activity { get; private set; }
        public GridPosition? TargetPosition { get; private set; }
        public string StatusReason { get; private set; }
        public float ActionTimer { get; private set; }

        public void AddActionTime(float deltaTime)
        {
            ActionTimer += Mathf.Max(0f, deltaTime);
        }

        public void ResetActionTimer()
        {
            ActionTimer = 0f;
        }

        public void SetTarget(GridPosition position)
        {
            TargetPosition = position;
        }

        public void ClearTarget()
        {
            TargetPosition = null;
        }

        public void SetActivity(RobotActivity activity, string reason)
        {
            Activity = activity;
            StatusReason = reason ?? string.Empty;
        }

        public void MoveTo(GridPosition position)
        {
            Position = position;
            Activity = RobotActivity.Moving;
            StatusReason = "移动中";
        }

        public void Destroy(string reason = "已损毁")
        {
            IsActive = false;
            Activity = RobotActivity.Destroyed;
            StatusReason = reason;
            TargetPosition = null;
            ActionTimer = 0f;
        }
    }

    public sealed class RobotAutomationService
    {
        public const int DefaultMaxTargetDistance = 7;

        private readonly LogicalGridState grid;
        private readonly int maxTargetDistance;
        private readonly float actionInterval;

        public RobotAutomationService(LogicalGridState grid, int maxTargetDistance = DefaultMaxTargetDistance, float actionInterval = 0.5f)
        {
            this.grid = grid;
            this.maxTargetDistance = Mathf.Max(1, maxTargetDistance);
            this.actionInterval = Mathf.Max(0f, actionInterval);
        }

        public bool TrySelectNearestSafeMineTarget(RobotState robot, out GridPosition target, bool avoidDangerZones = true)
        {
            return TrySelectNearestSafeMineTarget(robot, HardnessTier.UltraHard, out target, avoidDangerZones);
        }

        public bool TrySelectNearestSafeMineTarget(RobotState robot, HardnessTier drillTier, out GridPosition target, bool avoidDangerZones = true)
        {
            return TrySelectNearestSafeMineTarget(robot, drillTier, out target, out _, out _, avoidDangerZones);
        }

        public RobotAutomationResult TickRobot(RobotState robot, HardnessTier drillTier, MiningService mining, float deltaTime, bool avoidDangerZones = true)
        {
            if (robot == null)
            {
                return RobotAutomationResult.None(null);
            }

            if (!robot.IsActive)
            {
                robot.SetActivity(RobotActivity.Destroyed, "已损毁");
                return new RobotAutomationResult(RobotAutomationResultKind.Destroyed, robot, GridPosition.Zero, ResourceAmount.Zero, "机器人已损毁。");
            }

            robot.AddActionTime(deltaTime);
            if (actionInterval > 0f && robot.ActionTimer < actionInterval)
            {
                return new RobotAutomationResult(RobotAutomationResultKind.Waiting, robot, robot.TargetPosition ?? GridPosition.Zero, ResourceAmount.Zero, robot.StatusReason);
            }

            robot.ResetActionTimer();
            if (robot.TargetPosition.HasValue && !IsEligibleTarget(robot.TargetPosition.Value, drillTier, avoidDangerZones))
            {
                robot.ClearTarget();
            }

            if (!robot.TargetPosition.HasValue)
            {
                if (!TrySelectNearestSafeMineTarget(robot, drillTier, out GridPosition selectedTarget, out _, out _, avoidDangerZones))
                {
                    robot.SetActivity(RobotActivity.Idle, "没有安全目标，待机");
                    return new RobotAutomationResult(RobotAutomationResultKind.Idle, robot, GridPosition.Zero, ResourceAmount.Zero, robot.StatusReason);
                }

                robot.SetTarget(selectedTarget);
                robot.SetActivity(RobotActivity.Moving, $"前往 {selectedTarget}");
                return new RobotAutomationResult(RobotAutomationResultKind.TargetAcquired, robot, selectedTarget, ResourceAmount.Zero, robot.StatusReason);
            }

            GridPosition target = robot.TargetPosition.Value;
            if (robot.Position.ManhattanDistance(target) == 1 && IsSafeStagingCell(robot.Position, avoidDangerZones))
            {
                robot.SetActivity(RobotActivity.Mining, $"挖掘 {target}");
                MineResolution mineResolution = mining.TryMineDetailedFrom(robot.Position, drillTier, target);
                MineInteractionResult mineResult = mineResolution.Result;
                ResourceAmount reward = mineResolution.TotalReward;
                robot.ClearTarget();
                if (mineResult == MineInteractionResult.Mined)
                {
                    robot.SetActivity(RobotActivity.Idle, "挖掘完成");
                    return new RobotAutomationResult(RobotAutomationResultKind.Mined, robot, target, reward, "机器人完成挖掘。", mineResolution.ClearedCells);
                }

                if (mineResult == MineInteractionResult.TriggeredBomb)
                {
                    robot.SetActivity(RobotActivity.Mining, "误挖炸药");
                    return new RobotAutomationResult(RobotAutomationResultKind.TriggeredBomb, robot, target, reward, "机器人误挖炸药。", mineResolution.ClearedCells);
                }

                robot.SetActivity(RobotActivity.Blocked, "目标无法挖掘");
                return new RobotAutomationResult(RobotAutomationResultKind.Blocked, robot, target, ResourceAmount.Zero, "机器人目标无法挖掘。");
            }

            if (!TryFindPathToAdjacentTarget(robot.Position, target, out GridPosition nextStep, out _, avoidDangerZones))
            {
                robot.ClearTarget();
                robot.SetActivity(RobotActivity.Blocked, "路径受阻");
                return new RobotAutomationResult(RobotAutomationResultKind.Blocked, robot, target, ResourceAmount.Zero, "机器人路径受阻。");
            }

            robot.MoveTo(nextStep);
            return new RobotAutomationResult(RobotAutomationResultKind.Moved, robot, target, ResourceAmount.Zero, "机器人移动中。");
        }

        public bool StepToward(RobotState robot, GridPosition target, bool avoidDangerZones = true)
        {
            if (!TryFindPathToAdjacentTarget(robot.Position, target, out GridPosition nextStep, out _, avoidDangerZones))
            {
                return false;
            }

            robot.MoveTo(nextStep);
            return true;
        }

        private bool TrySelectNearestSafeMineTarget(RobotState robot, HardnessTier drillTier, out GridPosition target, out GridPosition stagingCell, out int bestPathDistance, bool avoidDangerZones)
        {
            target = default;
            stagingCell = default;
            bestPathDistance = int.MaxValue;
            int bestTargetDistance = int.MaxValue;
            bool found = false;

            foreach (GridPosition position in grid.Positions())
            {
                if (!IsEligibleTarget(position, drillTier, avoidDangerZones))
                {
                    continue;
                }

                int targetDistance = robot.Position.ManhattanDistance(position);
                if (targetDistance > maxTargetDistance)
                {
                    continue;
                }

                if (!TryFindPathToAdjacentTarget(robot.Position, position, out GridPosition candidateStaging, out int pathDistance, avoidDangerZones))
                {
                    continue;
                }

                bool better = !found
                    || pathDistance < bestPathDistance
                    || (pathDistance == bestPathDistance && targetDistance < bestTargetDistance)
                    || (pathDistance == bestPathDistance && targetDistance == bestTargetDistance && IsEarlier(position, target));

                if (better)
                {
                    bestPathDistance = pathDistance;
                    bestTargetDistance = targetDistance;
                    target = position;
                    stagingCell = candidateStaging;
                    found = true;
                }
            }

            return found;
        }

        private bool IsEligibleTarget(GridPosition position, HardnessTier drillTier, bool avoidDangerZones)
        {
            if (!grid.IsInside(position))
            {
                return false;
            }

            GridCellState cell = grid.GetCell(position);
            return cell.IsMineable
                && !cell.IsMarked
                && (!avoidDangerZones || !cell.IsDangerZone)
                && cell.HardnessTier <= drillTier;
        }

        private bool TryFindPathToAdjacentTarget(GridPosition start, GridPosition target, out GridPosition nextStep, out int distance, bool avoidDangerZones)
        {
            nextStep = default;
            distance = int.MaxValue;
            bool found = false;
            GridPosition bestStaging = default;
            int bestDistance = int.MaxValue;

            foreach (GridPosition direction in GridDirections.Cardinal)
            {
                GridPosition candidate = target + direction;
                if (!grid.IsInside(candidate) || !IsSafeStagingCell(candidate, avoidDangerZones))
                {
                    continue;
                }

                if (!TryFindPath(start, candidate, out GridPosition candidateNextStep, out int candidateDistance, avoidDangerZones))
                {
                    continue;
                }

                bool better = !found
                    || candidateDistance < bestDistance
                    || (candidateDistance == bestDistance && IsEarlier(candidate, bestStaging));
                if (better)
                {
                    bestDistance = candidateDistance;
                    bestStaging = candidate;
                    nextStep = candidateNextStep;
                    distance = candidateDistance;
                    found = true;
                }
            }

            return found;
        }

        private bool TryFindPath(GridPosition start, GridPosition destination, out GridPosition nextStep, out int distance, bool avoidDangerZones)
        {
            nextStep = start;
            distance = 0;
            if (start.Equals(destination))
            {
                return true;
            }

            var pending = new Queue<GridPosition>();
            var previous = new Dictionary<GridPosition, GridPosition>();
            pending.Enqueue(start);
            previous[start] = start;

            while (pending.Count > 0)
            {
                GridPosition current = pending.Dequeue();
                foreach (GridPosition direction in GridDirections.Cardinal)
                {
                    GridPosition candidate = current + direction;
                    if (!grid.IsInside(candidate) || previous.ContainsKey(candidate) || !IsWalkable(candidate, start, avoidDangerZones))
                    {
                        continue;
                    }

                    previous[candidate] = current;
                    if (candidate.Equals(destination))
                    {
                        GridPosition cursor = destination;
                        int steps = 0;
                        while (!previous[cursor].Equals(start))
                        {
                            cursor = previous[cursor];
                            steps++;
                        }

                        nextStep = cursor;
                        distance = steps + 1;
                        return true;
                    }

                    pending.Enqueue(candidate);
                }
            }

            return false;
        }

        private bool IsWalkable(GridPosition position, GridPosition start, bool avoidDangerZones)
        {
            if (position.Equals(start))
            {
                return true;
            }

            return IsSafeStagingCell(position, avoidDangerZones);
        }

        private bool IsSafeStagingCell(GridPosition position, bool avoidDangerZones)
        {
            GridCellState cell = grid.GetCell(position);
            return cell.IsPassable && (!avoidDangerZones || !cell.IsDangerZone);
        }

        private static bool IsEarlier(GridPosition left, GridPosition right)
        {
            return left.Y < right.Y || (left.Y == right.Y && left.X < right.X);
        }
    }
}
