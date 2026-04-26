using System.Collections.Generic;
using Minebot.Bootstrap;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.Presentation;
using Minebot.Progression;
using NUnit.Framework;
using UnityEngine;

namespace Minebot.Tests.EditMode
{
    public sealed class FirstRoundGameplayFeedbackTests
    {
        [TearDown]
        public void TearDown()
        {
            MinebotServices.ResetForTests();
        }

        [Test]
        public void ContactProbeMapsWorldPositionAndMoveDirectionToGridCells()
        {
            var world = new Vector2(3.75f, 4.65f);

            GridPosition current = ActorContactProbe.WorldToGrid(world);
            GridPosition contact = ActorContactProbe.ResolveContactCell(world, Vector2.right);

            Assert.That(current, Is.EqualTo(new GridPosition(3, 4)));
            Assert.That(contact, Is.EqualTo(new GridPosition(4, 4)));
        }

        [Test]
        public void ContactProbeBlocksMovementIntoOccupiedOrWallCells()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(5, 5), new GridPosition(2, 2));
            GridPosition wall = new GridPosition(3, 2);
            ref GridCellState wallCell = ref grid.GetCellRef(wall);
            wallCell.TerrainKind = TerrainKind.MineableWall;

            bool moved = ActorContactProbe.TryResolveMove(
                grid,
                ActorContactProbe.GridToWorldCenter(grid.PlayerSpawn),
                Vector2.right,
                out _,
                out GridPosition contact);

            Assert.That(moved, Is.False);
            Assert.That(contact, Is.EqualTo(wall));
        }

        [Test]
        public void BuildingPlacementSupportsMultiCellFootprintAndOccupancy()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(6, 6), new GridPosition(1, 1));
            var economy = new PlayerEconomy(new ResourceAmount(10, 0, 0));
            var buildings = new BuildingPlacementService(grid, economy);
            BuildingDefinition definition = BuildingDefinition.CreateRuntime(
                "test-drill",
                "测试钻机",
                new ResourceAmount(3, 0, 0),
                new Vector2Int(2, 2));

            bool placed = buildings.TryPlace(definition, new GridPosition(2, 2), out BuildingInstance instance, out BuildingPlacementFailure failure);

            Assert.That(placed, Is.True);
            Assert.That(failure, Is.EqualTo(BuildingPlacementFailure.None));
            Assert.That(instance, Is.Not.Null);
            Assert.That(grid.GetCell(new GridPosition(2, 2)).IsOccupiedByBuilding, Is.True);
            Assert.That(grid.GetCell(new GridPosition(3, 3)).IsPassable, Is.False);
            Assert.That(economy.Resources.Metal, Is.EqualTo(7));
        }

        [Test]
        public void BuildingPlacementRejectsWallsOccupiedCellsAndInsufficientResources()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(6, 6), new GridPosition(1, 1));
            var economy = new PlayerEconomy(new ResourceAmount(1, 0, 0));
            var buildings = new BuildingPlacementService(grid, economy);
            BuildingDefinition expensive = BuildingDefinition.CreateRuntime(
                "expensive",
                "昂贵建筑",
                new ResourceAmount(5, 0, 0),
                Vector2Int.one);
            BuildingDefinition free = BuildingDefinition.CreateRuntime(
                "free",
                "免费建筑",
                ResourceAmount.Zero,
                Vector2Int.one);
            ref GridCellState wall = ref grid.GetCellRef(new GridPosition(2, 2));
            wall.TerrainKind = TerrainKind.MineableWall;

            Assert.That(buildings.TryPlace(expensive, new GridPosition(1, 1), out _, out BuildingPlacementFailure expensiveFailure), Is.False);
            Assert.That(expensiveFailure, Is.EqualTo(BuildingPlacementFailure.InsufficientResources));
            Assert.That(buildings.TryPlace(free, new GridPosition(2, 2), out _, out BuildingPlacementFailure wallFailure), Is.False);
            Assert.That(wallFailure, Is.EqualTo(BuildingPlacementFailure.TerrainBlocked));
            Assert.That(buildings.TryPlace(free, new GridPosition(1, 1), out _, out _), Is.True);
            Assert.That(buildings.TryPlace(free, new GridPosition(1, 1), out _, out BuildingPlacementFailure occupiedFailure), Is.False);
            Assert.That(occupiedFailure, Is.EqualTo(BuildingPlacementFailure.Occupied));
        }

        [Test]
        public void SessionMiningStillAppliesHardnessBombAndRewardsForAutoMineTargets()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);
            GridPosition staging = registry.Grid.PlayerSpawn + GridPosition.Up;
            GridPosition target = staging + GridPosition.Up;
            registry.Session.Move(GridPosition.Up);
            ref GridCellState targetCell = ref registry.Grid.GetCellRef(target);
            targetCell.TerrainKind = TerrainKind.MineableWall;
            targetCell.HardnessTier = HardnessTier.UltraHard;
            targetCell.Reward = new ResourceAmount(3, 1, 2);

            MineInteractionResult weakResult = registry.Session.Mine(target);
            registry.PlayerMiningState.DrillTier = HardnessTier.UltraHard;
            MineInteractionResult minedResult = registry.Session.Mine(target);

            Assert.That(weakResult, Is.EqualTo(MineInteractionResult.DrillTooWeak));
            Assert.That(minedResult, Is.EqualTo(MineInteractionResult.Mined));
            Assert.That(registry.Grid.GetCell(target).TerrainKind, Is.EqualTo(TerrainKind.Empty));
            Assert.That(registry.Economy.Resources.Metal, Is.GreaterThanOrEqualTo(3));
            Assert.That(registry.Experience.Experience, Is.GreaterThanOrEqualTo(2));
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
    }
}
