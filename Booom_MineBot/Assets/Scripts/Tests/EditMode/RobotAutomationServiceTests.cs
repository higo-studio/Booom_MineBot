using System.Collections.Generic;
using Minebot.Automation;
using Minebot.Bootstrap;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.HazardInference;
using Minebot.Progression;
using NUnit.Framework;
using UnityEngine;

namespace Minebot.Tests.EditMode
{
    public sealed class RobotAutomationServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            MinebotServices.ResetForTests();
        }

        [Test]
        public void TargetSelectionDoesNotReadHiddenBombTruth()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(5, 5), new GridPosition(2, 2));
            GridPosition bombTarget = new GridPosition(2, 3);
            SetWall(grid, bombTarget, CellStaticFlags.Bomb);
            var robot = new RobotState(grid.PlayerSpawn);
            var automation = new RobotAutomationService(grid, 5, 0f);

            bool found = automation.TrySelectNearestSafeMineTarget(robot, HardnessTier.Soil, out GridPosition target);

            Assert.That(found, Is.True);
            Assert.That(target, Is.EqualTo(bombTarget));
        }

        [Test]
        public void TargetSelectionAvoidsMarkedAndDangerTargets()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            GridPosition marked = new GridPosition(3, 4);
            GridPosition danger = new GridPosition(4, 3);
            GridPosition safe = new GridPosition(3, 2);
            SetWall(grid, marked);
            SetWall(grid, danger);
            SetWall(grid, safe);
            grid.GetCellRef(marked).IsMarked = true;
            grid.GetCellRef(danger).IsDangerZone = true;
            var robot = new RobotState(grid.PlayerSpawn);
            var automation = new RobotAutomationService(grid, 5, 0f);

            bool found = automation.TrySelectNearestSafeMineTarget(robot, HardnessTier.Soil, out GridPosition target);

            Assert.That(found, Is.True);
            Assert.That(target, Is.EqualTo(safe));
        }

        [Test]
        public void TargetSelectionAvoidsDangerZoneStagingCells()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            GridPosition unsafeStagingTarget = new GridPosition(3, 5);
            GridPosition safeTarget = new GridPosition(5, 3);
            SetWall(grid, unsafeStagingTarget);
            SetWall(grid, safeTarget);
            grid.GetCellRef(new GridPosition(3, 4)).IsDangerZone = true;
            grid.GetCellRef(new GridPosition(2, 5)).IsDangerZone = true;
            grid.GetCellRef(new GridPosition(4, 5)).IsDangerZone = true;
            var robot = new RobotState(grid.PlayerSpawn);
            var automation = new RobotAutomationService(grid, 7, 0f);

            bool found = automation.TrySelectNearestSafeMineTarget(robot, HardnessTier.Soil, out GridPosition target);

            Assert.That(found, Is.True);
            Assert.That(target, Is.EqualTo(safeTarget));
        }

        [Test]
        public void TargetSelectionSkipsUnreachableNearestTarget()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            GridPosition unreachableTarget = new GridPosition(3, 5);
            GridPosition reachableTarget = new GridPosition(5, 3);
            SetWall(grid, unreachableTarget);
            SetWall(grid, reachableTarget);
            BlockCell(grid, new GridPosition(3, 4));
            BlockCell(grid, new GridPosition(2, 5));
            BlockCell(grid, new GridPosition(4, 5));
            var robot = new RobotState(grid.PlayerSpawn);
            var automation = new RobotAutomationService(grid, 7, 0f);

            bool found = automation.TrySelectNearestSafeMineTarget(robot, HardnessTier.Soil, out GridPosition target);

            Assert.That(found, Is.True);
            Assert.That(target, Is.EqualTo(reachableTarget));
        }

        [Test]
        public void TargetSelectionTreatsBuildingOccupiedCellsAsBlocked()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            GridPosition blockedTarget = new GridPosition(3, 5);
            GridPosition reachableTarget = new GridPosition(5, 3);
            SetWall(grid, blockedTarget);
            SetWall(grid, reachableTarget);
            grid.GetCellRef(new GridPosition(3, 4)).IsOccupiedByBuilding = true;
            grid.GetCellRef(new GridPosition(2, 5)).IsOccupiedByBuilding = true;
            grid.GetCellRef(new GridPosition(4, 5)).IsOccupiedByBuilding = true;
            var robot = new RobotState(grid.PlayerSpawn);
            var automation = new RobotAutomationService(grid, 7, 0f);

            bool found = automation.TrySelectNearestSafeMineTarget(robot, HardnessTier.Soil, out GridPosition target);

            Assert.That(found, Is.True);
            Assert.That(target, Is.EqualTo(reachableTarget));
        }

        [Test]
        public void TargetSelectionUsesDeterministicTieBreak()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            GridPosition lowerYTarget = new GridPosition(4, 2);
            GridPosition higherYTarget = new GridPosition(2, 4);
            SetWall(grid, lowerYTarget);
            SetWall(grid, higherYTarget);
            var robot = new RobotState(grid.PlayerSpawn);
            var automation = new RobotAutomationService(grid, 7, 0f);

            bool found = automation.TrySelectNearestSafeMineTarget(robot, HardnessTier.Soil, out GridPosition target);

            Assert.That(found, Is.True);
            Assert.That(target, Is.EqualTo(lowerYTarget));
        }

        [Test]
        public void RobotTickUsesPathAroundBlockedStraightLine()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            GridPosition blocker = new GridPosition(4, 3);
            GridPosition target = new GridPosition(5, 3);
            SetWall(grid, blocker);
            SetWall(grid, target);
            var robot = new RobotState(grid.PlayerSpawn);
            robot.SetTarget(target);
            var automation = new RobotAutomationService(grid, 7, 0f);
            var mining = new MiningService(grid);

            RobotAutomationResult result = automation.TickRobot(robot, HardnessTier.Soil, mining, 1f);

            Assert.That(result.Kind, Is.EqualTo(RobotAutomationResultKind.Moved));
            Assert.That(robot.Position, Is.Not.EqualTo(grid.PlayerSpawn));
            Assert.That(grid.GetCell(robot.Position).IsPassable, Is.True);
        }

        [Test]
        public void RobotTickReselectsWhenCurrentTargetBecomesInvalid()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            GridPosition markedTarget = new GridPosition(3, 5);
            GridPosition replacementTarget = new GridPosition(5, 3);
            SetWall(grid, markedTarget);
            SetWall(grid, replacementTarget);
            grid.GetCellRef(markedTarget).IsMarked = true;
            var robot = new RobotState(grid.PlayerSpawn);
            robot.SetTarget(markedTarget);
            var automation = new RobotAutomationService(grid, 7, 0f);
            var mining = new MiningService(grid);

            RobotAutomationResult result = automation.TickRobot(robot, HardnessTier.Soil, mining, 1f);

            Assert.That(result.Kind, Is.EqualTo(RobotAutomationResultKind.TargetAcquired));
            Assert.That(robot.TargetPosition, Is.EqualTo(replacementTarget));
        }

        [Test]
        public void RobotTickDoesNotMineFromDangerZoneStagingCell()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            GridPosition target = new GridPosition(3, 4);
            SetWall(grid, target);
            grid.GetCellRef(grid.PlayerSpawn).IsDangerZone = true;
            var robot = new RobotState(grid.PlayerSpawn);
            robot.SetTarget(target);
            var automation = new RobotAutomationService(grid, 7, 0f);
            var mining = new MiningService(grid);

            RobotAutomationResult result = automation.TickRobot(robot, HardnessTier.Soil, mining, 1f);

            Assert.That(result.Kind, Is.EqualTo(RobotAutomationResultKind.Moved));
            Assert.That(grid.GetCell(target).TerrainKind, Is.EqualTo(TerrainKind.MineableWall));
            Assert.That(grid.GetCell(robot.Position).IsDangerZone, Is.False);
        }

        [Test]
        public void SessionRobotTickMinesAndGrantsReward()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);
            GridPosition target = new GridPosition(registry.Grid.PlayerSpawn.X, registry.Grid.PlayerSpawn.Y - 2);
            registry.Grid.GetCellRef(target).ClearBomb();
            registry.Economy.Add(new ResourceAmount(5, 0, 0));
            Assert.That(registry.RobotFactory.TryProduce(registry.Grid.PlayerSpawn, out RobotState robot), Is.True);
            int metalBefore = registry.Economy.Resources.Metal;
            int experienceBefore = registry.Experience.Experience;

            RunRobotTicks(registry, 4);

            Assert.That(robot.IsActive, Is.True);
            Assert.That(registry.Grid.GetCell(target).TerrainKind, Is.EqualTo(TerrainKind.Empty));
            Assert.That(registry.Economy.Resources.Metal, Is.EqualTo(metalBefore));
            Assert.That(registry.Experience.Experience, Is.EqualTo(experienceBefore));
            Assert.That(registry.WorldPickups.ActivePickups.Count, Is.GreaterThan(0));

            bool collected = registry.Session.TickWorldPickups(1f, ToWorldCenter(target));

            Assert.That(collected, Is.True);
            Assert.That(registry.WorldPickups.ActivePickups, Is.Empty);
            Assert.That(registry.Economy.Resources.Metal, Is.GreaterThan(metalBefore));
            Assert.That(registry.Experience.Experience, Is.GreaterThan(experienceBefore));
        }

        [Test]
        public void SessionRobotTickDestroysRobotOnUnmarkedBombWithoutPlayerDamage()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);
            GridPosition target = registry.Grid.PlayerSpawn + GridPosition.Up;
            ref GridCellState cell = ref registry.Grid.GetCellRef(target);
            cell.TerrainKind = TerrainKind.MineableWall;
            cell.HardnessTier = HardnessTier.Soil;
            cell.StaticFlags |= CellStaticFlags.Bomb;
            cell.Reward = new ResourceAmount(1, 0, 1);
            registry.Economy.Add(new ResourceAmount(5, 0, 0));
            Assert.That(registry.RobotFactory.TryProduce(registry.Grid.PlayerSpawn, out RobotState robot), Is.True);
            int healthBefore = registry.Vitals.CurrentHealth;

            RunRobotTicks(registry, 2);

            Assert.That(robot.IsActive, Is.False);
            Assert.That(robot.Activity, Is.EqualTo(RobotActivity.Destroyed));
            Assert.That(registry.Vitals.CurrentHealth, Is.EqualTo(healthBefore));
            Assert.That(registry.Grid.GetCell(target).TerrainKind, Is.EqualTo(TerrainKind.Empty));
        }

        [Test]
        public void RobotBombDestructionCanDropConfiguredRecyclePickupBeforeCollection()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            GridPosition target = grid.PlayerSpawn + GridPosition.Up;
            SetWall(grid, target, CellStaticFlags.Bomb);
            var player = new PlayerMiningState(grid.PlayerSpawn, HardnessTier.Soil);
            var mining = new MiningService(grid);
            var hazards = new HazardService(grid);
            var economy = new PlayerEconomy(new ResourceAmount(0, 0, 0));
            var experience = new ExperienceService(4);
            var worldPickups = new WorldPickupService();
            var vitals = new PlayerVitals(3);
            var robot = new RobotState(grid.PlayerSpawn);
            robot.SetTarget(target);
            var robots = new List<RobotState> { robot };
            var automation = new RobotAutomationService(grid, 7, 0f);
            var session = new GameSessionService(
                player,
                mining,
                hazards,
                null,
                economy,
                experience,
                worldPickups,
                vitals,
                automation,
                robots,
                new ResourceAmount(2, 0, 0),
                true,
                HardnessTier.Soil);

            bool changed = session.TickRobots(1f);

            Assert.That(changed, Is.True);
            Assert.That(robot.IsActive, Is.False);
            Assert.That(worldPickups.ActivePickups.Count, Is.EqualTo(1));
            Assert.That(economy.Resources.Metal, Is.EqualTo(0));

            bool collected = session.TickWorldPickups(1f, ToWorldCenter(grid.PlayerSpawn));

            Assert.That(collected, Is.True);
            Assert.That(worldPickups.ActivePickups, Is.Empty);
            Assert.That(economy.Resources.Metal, Is.EqualTo(2));
        }

        [Test]
        public void SessionRobotTickPausesDuringUpgradeSelection()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);
            registry.Economy.Add(new ResourceAmount(5, 0, 0));
            Assert.That(registry.RobotFactory.TryProduce(registry.Grid.PlayerSpawn, out RobotState robot), Is.True);
            registry.Experience.AddExperience(registry.Experience.NextThreshold);
            GridPosition before = robot.Position;

            bool changed = registry.Session.TickRobots(1f);

            Assert.That(changed, Is.False);
            Assert.That(robot.Position, Is.EqualTo(before));
            Assert.That(robot.StatusReason, Does.Contain("暂停"));
        }

        [Test]
        public void RobotTickIdlesWhenNoEligibleTargetsExist()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(5, 5), new GridPosition(2, 2));
            var robot = new RobotState(grid.PlayerSpawn);
            var automation = new RobotAutomationService(grid, 5, 0f);
            var mining = new MiningService(grid);

            RobotAutomationResult result = automation.TickRobot(robot, HardnessTier.Soil, mining, 1f);

            Assert.That(result.Kind, Is.EqualTo(RobotAutomationResultKind.Idle));
            Assert.That(robot.Activity, Is.EqualTo(RobotActivity.Idle));
        }

        private static void RunRobotTicks(RuntimeServiceRegistry registry, int count)
        {
            for (int i = 0; i < count; i++)
            {
                registry.Session.TickRobots(1f);
            }
        }

        private static LogicalGridState CreateOpenGrid(Vector2Int size, GridPosition spawn)
        {
            var cells = new List<GridCellState>(size.x * size.y);
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    bool border = x == 0 || y == 0 || x == size.x - 1 || y == size.y - 1;
                    cells.Add(new GridCellState(
                        border ? TerrainKind.Indestructible : TerrainKind.Empty,
                        HardnessTier.Soil,
                        CellStaticFlags.None,
                        ResourceAmount.Zero));
                }
            }

            return new LogicalGridState(size, spawn, cells);
        }

        private static void SetWall(LogicalGridState grid, GridPosition position, CellStaticFlags flags = CellStaticFlags.None)
        {
            ref GridCellState cell = ref grid.GetCellRef(position);
            cell.TerrainKind = TerrainKind.MineableWall;
            cell.HardnessTier = HardnessTier.Soil;
            cell.StaticFlags = flags;
            cell.Reward = new ResourceAmount(1, 0, 1);
            cell.IsRevealed = false;
        }

        private static void BlockCell(LogicalGridState grid, GridPosition position)
        {
            ref GridCellState cell = ref grid.GetCellRef(position);
            cell.TerrainKind = TerrainKind.Indestructible;
            cell.StaticFlags = CellStaticFlags.None;
            cell.IsDangerZone = false;
            cell.IsMarked = false;
        }

        private static Vector2 ToWorldCenter(GridPosition position)
        {
            return new Vector2(position.X + 0.5f, position.Y + 0.5f);
        }
    }
}
