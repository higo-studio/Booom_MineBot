using System.Collections.Generic;
using Minebot.Automation;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.Progression;

namespace Minebot.WaveSurvival
{
    public readonly struct WaveResolution
    {
        public WaveResolution(bool playerKilled, int robotsDestroyed, ResourceAmount droppedResources, int survivedWave)
        {
            PlayerKilled = playerKilled;
            RobotsDestroyed = robotsDestroyed;
            DroppedResources = droppedResources;
            SurvivedWave = survivedWave;
        }

        public bool PlayerKilled { get; }
        public int RobotsDestroyed { get; }
        public ResourceAmount DroppedResources { get; }
        public int SurvivedWave { get; }
    }

    public sealed class WaveSurvivalService
    {
        private readonly LogicalGridState grid;
        private readonly WaveConfig config;
        private float timeUntilNextWave;

        public WaveSurvivalService(LogicalGridState grid, WaveConfig config)
        {
            this.grid = grid;
            this.config = config;
            timeUntilNextWave = config != null ? config.FirstWaveDelay : WaveConfig.DefaultFirstWaveDelay;
        }

        public int CurrentWave { get; private set; }
        public int BestSurvivedWave { get; private set; }
        public float TimeUntilNextWave => timeUntilNextWave;
        public int NextDangerRadius => config != null
            ? config.DangerRadiusForWave(CurrentWave + 1)
            : WaveConfig.DefaultBaseDangerRadius + CurrentWave / WaveConfig.DefaultRadiusGrowthEveryWaves;

        public bool Tick(float deltaTime)
        {
            timeUntilNextWave -= UnityEngine.Mathf.Max(0f, deltaTime);
            return timeUntilNextWave <= 0f;
        }

        public void EvaluateDangerZones()
        {
            foreach (GridPosition position in grid.Positions())
            {
                ref GridCellState cell = ref grid.GetCellRef(position);
                cell.IsDangerZone = false;
            }

            int thickness = NextDangerRadius;
            if (thickness <= 0)
            {
                return;
            }

            var frontier = new Queue<(GridPosition Position, int Distance)>();
            var visited = new HashSet<GridPosition>();
            foreach (GridPosition position in grid.Positions())
            {
                GridCellState cell = grid.GetCell(position);
                if (cell.TerrainKind != TerrainKind.Empty)
                {
                    continue;
                }

                if (!HasAdjacentMineableWall(position))
                {
                    continue;
                }

                frontier.Enqueue((position, 0));
                visited.Add(position);
                ref GridCellState frontierCell = ref grid.GetCellRef(position);
                frontierCell.IsDangerZone = true;
            }

            while (frontier.Count > 0)
            {
                (GridPosition position, int distance) = frontier.Dequeue();
                if (distance >= thickness - 1)
                {
                    continue;
                }

                TryExpand(frontier, visited, position + GridPosition.Up, distance + 1);
                TryExpand(frontier, visited, position + GridPosition.Down, distance + 1);
                TryExpand(frontier, visited, position + GridPosition.Left, distance + 1);
                TryExpand(frontier, visited, position + GridPosition.Right, distance + 1);
            }

            CollapseSafeCellsOutsidePrimaryCavity();
        }

        private void TryExpand(Queue<(GridPosition Position, int Distance)> frontier, HashSet<GridPosition> visited, GridPosition candidate, int distance)
        {
            if (!grid.IsInside(candidate) || visited.Contains(candidate))
            {
                return;
            }

            ref GridCellState cell = ref grid.GetCellRef(candidate);
            if (cell.TerrainKind != TerrainKind.Empty)
            {
                return;
            }

            cell.IsDangerZone = true;
            visited.Add(candidate);
            frontier.Enqueue((candidate, distance));
        }

        private bool HasAdjacentMineableWall(GridPosition position)
        {
            return IsMineableWall(position + GridPosition.Up)
                || IsMineableWall(position + GridPosition.Down)
                || IsMineableWall(position + GridPosition.Left)
                || IsMineableWall(position + GridPosition.Right);
        }

        private bool IsMineableWall(GridPosition position)
        {
            return grid.IsInside(position) && grid.GetCell(position).TerrainKind == TerrainKind.MineableWall;
        }

        private void CollapseSafeCellsOutsidePrimaryCavity()
        {
            HashSet<GridPosition> preservedEmptyCells = CollectPrimaryEmptyComponent();

            foreach (GridPosition position in grid.Positions())
            {
                if (!IsSafeEmpty(position) || preservedEmptyCells.Contains(position))
                {
                    continue;
                }

                ref GridCellState cell = ref grid.GetCellRef(position);
                cell.IsDangerZone = true;
            }
        }

        private HashSet<GridPosition> CollectPrimaryEmptyComponent()
        {
            if (IsEmpty(grid.PlayerSpawn))
            {
                var preserved = new HashSet<GridPosition>();
                CollectEmptyComponent(grid.PlayerSpawn, preserved);
                return preserved;
            }

            var visited = new HashSet<GridPosition>();
            List<GridPosition> largestComponent = null;
            foreach (GridPosition position in grid.Positions())
            {
                if (visited.Contains(position) || !IsEmpty(position))
                {
                    continue;
                }

                List<GridPosition> component = CollectEmptyComponent(position, visited);
                if (largestComponent == null || component.Count > largestComponent.Count)
                {
                    largestComponent = component;
                }
            }

            return largestComponent != null
                ? new HashSet<GridPosition>(largestComponent)
                : new HashSet<GridPosition>();
        }

        private List<GridPosition> CollectEmptyComponent(GridPosition origin, HashSet<GridPosition> visited)
        {
            var pending = new Queue<GridPosition>();
            var component = new List<GridPosition>();
            pending.Enqueue(origin);
            visited.Add(origin);

            while (pending.Count > 0)
            {
                GridPosition current = pending.Dequeue();
                component.Add(current);

                TryVisitEmptyNeighbor(current + GridPosition.Up, pending, visited);
                TryVisitEmptyNeighbor(current + GridPosition.Down, pending, visited);
                TryVisitEmptyNeighbor(current + GridPosition.Left, pending, visited);
                TryVisitEmptyNeighbor(current + GridPosition.Right, pending, visited);
            }

            return component;
        }

        private void TryVisitEmptyNeighbor(GridPosition candidate, Queue<GridPosition> pending, HashSet<GridPosition> visited)
        {
            if (!grid.IsInside(candidate) || visited.Contains(candidate) || !IsEmpty(candidate))
            {
                return;
            }

            visited.Add(candidate);
            pending.Enqueue(candidate);
        }

        private bool IsEmpty(GridPosition position)
        {
            return grid.GetCell(position).TerrainKind == TerrainKind.Empty;
        }

        private bool IsSafeEmpty(GridPosition position)
        {
            GridCellState cell = grid.GetCell(position);
            return cell.TerrainKind == TerrainKind.Empty && !cell.IsDangerZone;
        }
        
        [System.Obsolete("Use EvaluateDangerZones() to derive danger bands directly from grid state.")]
        public void EvaluateDangerZones(IEnumerable<GridPosition> unstableOrigins)
        {
            EvaluateDangerZones();
        }

        public WaveResolution ResolveWave(GridPosition playerPosition, PlayerVitals vitals, IList<RobotState> robots)
        {
            CurrentWave++;
            timeUntilNextWave = config != null ? config.FirstWaveDelay : WaveConfig.DefaultFirstWaveDelay;
            bool playerKilled = grid.IsInside(playerPosition) && grid.GetCell(playerPosition).IsDangerZone;
            if (playerKilled)
            {
                vitals.Damage(vitals.CurrentHealth);
            }

            int robotsDestroyed = 0;
            ResourceAmount drops = ResourceAmount.Zero;
            if (robots != null)
            {
                foreach (RobotState robot in robots)
                {
                    if (robot.IsActive && grid.IsInside(robot.Position) && grid.GetCell(robot.Position).IsDangerZone)
                    {
                        robot.Destroy();
                        robotsDestroyed++;
                        drops += config != null ? config.RobotRecycleDrop : ResourceAmount.Zero;
                    }
                }
            }

            int survivedWave = playerKilled ? CurrentWave - 1 : CurrentWave;
            if (survivedWave > BestSurvivedWave)
            {
                BestSurvivedWave = survivedWave;
            }

            return new WaveResolution(playerKilled, robotsDestroyed, drops, survivedWave);
        }
    }
}
