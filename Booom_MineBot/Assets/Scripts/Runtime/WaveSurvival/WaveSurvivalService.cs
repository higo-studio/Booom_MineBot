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
            timeUntilNextWave = config != null ? config.FirstWaveDelay : 30f;
        }

        public int CurrentWave { get; private set; }
        public int BestSurvivedWave { get; private set; }
        public float TimeUntilNextWave => timeUntilNextWave;

        public bool Tick(float deltaTime)
        {
            timeUntilNextWave -= UnityEngine.Mathf.Max(0f, deltaTime);
            return timeUntilNextWave <= 0f;
        }

        public void EvaluateDangerZones(IEnumerable<GridPosition> unstableOrigins)
        {
            foreach (GridPosition position in grid.Positions())
            {
                ref GridCellState cell = ref grid.GetCellRef(position);
                cell.IsDangerZone = false;
            }

            int radius = config != null ? config.DangerRadiusForWave(CurrentWave + 1) : 1;
            foreach (GridPosition origin in unstableOrigins)
            {
                foreach (GridPosition position in grid.Positions())
                {
                    if (position.ManhattanDistance(origin) <= radius)
                    {
                        ref GridCellState cell = ref grid.GetCellRef(position);
                        cell.IsDangerZone = true;
                    }
                }
            }
        }

        public WaveResolution ResolveWave(GridPosition playerPosition, PlayerVitals vitals, IList<RobotState> robots)
        {
            CurrentWave++;
            timeUntilNextWave = config != null ? config.FirstWaveDelay : 30f;
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
