using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Presentation
{
    public readonly struct AutoMineContactState
    {
        public AutoMineContactState(GridPosition contactCell, float elapsedTime, bool hasContact)
        {
            ContactCell = contactCell;
            ElapsedTime = Mathf.Max(0f, elapsedTime);
            HasContact = hasContact;
        }

        public static AutoMineContactState None => new AutoMineContactState(default, 0f, false);

        public GridPosition ContactCell { get; }
        public float ElapsedTime { get; }
        public bool HasContact { get; }
    }

    public readonly struct AutoMineContactDecision
    {
        public AutoMineContactDecision(AutoMineContactState nextState, GridPosition targetCell, bool shouldMine, bool shouldShowFeedback)
        {
            NextState = nextState;
            TargetCell = targetCell;
            ShouldMine = shouldMine;
            ShouldShowFeedback = shouldShowFeedback;
        }

        public AutoMineContactState NextState { get; }
        public GridPosition TargetCell { get; }
        public bool ShouldMine { get; }
        public bool ShouldShowFeedback { get; }
    }

    public static class AutoMineContactResolver
    {
        public static AutoMineContactDecision Advance(
            AutoMineContactState state,
            CharacterMoveResult2D moveResult,
            LogicalGridState grid,
            GridPosition actorPosition,
            GridPosition facingDirection,
            float deltaTime,
            float autoMineInterval)
        {
            if (grid == null || moveResult.HasMoved || moveResult.WasSliding || !moveResult.WasBlocked || !moveResult.HasStableContact)
            {
                return new AutoMineContactDecision(AutoMineContactState.None, default, false, false);
            }

            GridPosition targetCell = moveResult.StableContactCell;
            if (!IsValidMineTarget(grid, actorPosition, targetCell))
            {
                GridPosition facingCell = actorPosition + facingDirection;
                if (!IsValidMineTarget(grid, actorPosition, facingCell))
                {
                    return new AutoMineContactDecision(AutoMineContactState.None, default, false, false);
                }

                targetCell = facingCell;
            }

            bool sameContact = state.HasContact && state.ContactCell.Equals(targetCell);
            float elapsed = (sameContact ? state.ElapsedTime : 0f) + Mathf.Max(0f, deltaTime);
            bool ready = elapsed >= Mathf.Max(0.01f, autoMineInterval);
            AutoMineContactState nextState = ready
                ? AutoMineContactState.None
                : new AutoMineContactState(targetCell, elapsed, true);

            return new AutoMineContactDecision(nextState, targetCell, ready, !sameContact);
        }

        private static bool IsValidMineTarget(LogicalGridState grid, GridPosition actorPosition, GridPosition target)
        {
            return grid.IsInside(target)
                && grid.GetCell(target).IsMineable
                && actorPosition.ManhattanDistance(target) == 1;
        }
    }
}
