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
        public void MotorStopsAtWallBoundaryAndReportsContactCell()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(5, 5), new GridPosition(2, 2));
            GridPosition wall = new GridPosition(3, 2);
            ref GridCellState wallCell = ref grid.GetCellRef(wall);
            wallCell.TerrainKind = TerrainKind.MineableWall;

            CharacterMoveResult2D result = MoveWithMotor(grid, Vector2.right * 0.6f);

            Assert.That(result.HasMoved, Is.True);
            Assert.That(result.HasStableContact, Is.True);
            Assert.That(result.StableContactCell, Is.EqualTo(wall));
            Assert.That(result.FinalPosition.x, Is.LessThan(3f - (ActorContactProbe.DefaultCollisionRadius + 0.02f) + 0.01f));
        }

        [Test]
        public void MotorSlidesAlongWallWhenDiagonalInputHasFreeTangent()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(6, 6), new GridPosition(2, 2));
            GridPosition wall = new GridPosition(3, 2);
            grid.GetCellRef(wall).TerrainKind = TerrainKind.MineableWall;

            Vector2 start = ActorContactProbe.GridToWorldCenter(grid.PlayerSpawn);
            CharacterMoveResult2D result = MoveWithMotor(grid, new Vector2(0.6f, 0.6f));

            Assert.That(result.HasMoved, Is.True);
            Assert.That(result.WasSliding, Is.True);
            Assert.That(result.HasStableContact, Is.True);
            Assert.That(result.StableContactCell, Is.EqualTo(wall));
            Assert.That(result.FinalPosition.x, Is.LessThan(3f - (ActorContactProbe.DefaultCollisionRadius + 0.02f) + 0.01f));
            Assert.That(result.FinalPosition.y, Is.GreaterThan(start.y + 0.2f));
        }

        [Test]
        public void MotorSlidesPastConvexCornerInsteadOfSnaggingOnSquareBounds()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(2, 2));
            GridPosition cornerWall = new GridPosition(3, 3);
            grid.GetCellRef(cornerWall).TerrainKind = TerrainKind.MineableWall;

            Vector2 start = new Vector2(2.5f, 2.8f);
            Vector2 desired = Vector2.right * 0.8f;
            CharacterMoveResult2D result = MoveWithMotor(grid, desired, start);

            Assert.That(result.HasMoved, Is.True);
            Assert.That(result.WasSliding, Is.True);
            Assert.That(result.HasStableContact, Is.True);
            Assert.That(result.StableContactCell, Is.EqualTo(cornerWall));
            Assert.That(result.FinalPosition.x, Is.GreaterThan(start.x + 0.2f));
            Assert.That(result.FinalPosition.y, Is.LessThan(start.y - 0.05f));
        }

        [Test]
        public void MotorPreventsDiagonalCornerSlip()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(6, 6), new GridPosition(2, 2));
            grid.GetCellRef(new GridPosition(3, 2)).TerrainKind = TerrainKind.MineableWall;
            grid.GetCellRef(new GridPosition(2, 3)).TerrainKind = TerrainKind.MineableWall;

            CharacterMoveResult2D result = MoveWithMotor(grid, new Vector2(0.45f, 0.45f));

            Assert.That(ActorContactProbe.WorldToGrid(result.FinalPosition), Is.EqualTo(grid.PlayerSpawn));
            Assert.That(result.WasBlocked, Is.True);
            Assert.That(result.HasStableContact, Is.True);
            Assert.That(
                result.StableContactCell.Equals(new GridPosition(3, 2))
                || result.StableContactCell.Equals(new GridPosition(2, 3)),
                Is.True);
        }

        [Test]
        public void MotorReportsSameWallAcrossRepeatedBlockedMoves()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(6, 6), new GridPosition(2, 2));
            GridPosition wall = new GridPosition(3, 2);
            grid.GetCellRef(wall).TerrainKind = TerrainKind.MineableWall;

            CharacterMoveResult2D first = MoveWithMotor(grid, Vector2.right * 0.6f);
            CharacterMoveResult2D second = MoveWithMotor(grid, Vector2.right * 0.25f, first.FinalPosition);

            Assert.That(first.HasStableContact, Is.True);
            Assert.That(second.HasStableContact, Is.True);
            Assert.That(first.StableContactCell, Is.EqualTo(wall));
            Assert.That(second.StableContactCell, Is.EqualTo(wall));
        }

        [Test]
        public void MotorRecoversFromInitialOverlapBeforeResolvingMovement()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(6, 6), new GridPosition(2, 2));
            GridPosition wall = new GridPosition(3, 2);
            grid.GetCellRef(wall).TerrainKind = TerrainKind.MineableWall;
            var start = new Vector2(2.7f, 2.5f);

            CharacterMoveResult2D result = MoveWithMotor(grid, Vector2.right * 0.2f, start);

            Assert.That(result.HadInitialOverlap, Is.True);
            Assert.That(result.WasDepenetrated, Is.True);
            Assert.That(result.FinalPosition.x, Is.LessThan(start.x));
            Assert.That(result.HasStableContact, Is.True);
            Assert.That(result.StableContactCell, Is.EqualTo(wall));
        }

        [Test]
        public void AutoMineContactResolverRequiresStableBlockedContactBeforeMining()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(6, 6), new GridPosition(2, 2));
            GridPosition wall = new GridPosition(3, 2);
            grid.GetCellRef(wall).TerrainKind = TerrainKind.MineableWall;
            CharacterMoveResult2D blocked = CreateBlockedMoveResult(wall, CharacterCollisionFlags2D.Hit | CharacterCollisionFlags2D.Blocked);
            CharacterMoveResult2D sliding = CreateBlockedMoveResult(wall, CharacterCollisionFlags2D.Hit | CharacterCollisionFlags2D.Blocked | CharacterCollisionFlags2D.Sliding);

            AutoMineContactDecision first = AutoMineContactResolver.Advance(
                AutoMineContactState.None,
                blocked,
                grid,
                grid.PlayerSpawn,
                GridPosition.Right,
                0.05f,
                0.18f);
            AutoMineContactDecision second = AutoMineContactResolver.Advance(
                first.NextState,
                blocked,
                grid,
                grid.PlayerSpawn,
                GridPosition.Right,
                0.14f,
                0.18f);
            AutoMineContactDecision slide = AutoMineContactResolver.Advance(
                AutoMineContactState.None,
                sliding,
                grid,
                grid.PlayerSpawn,
                GridPosition.Right,
                0.25f,
                0.18f);

            Assert.That(first.ShouldShowFeedback, Is.True);
            Assert.That(first.ShouldMine, Is.False);
            Assert.That(first.NextState.HasContact, Is.True);
            Assert.That(first.TargetCell, Is.EqualTo(wall));
            Assert.That(second.ShouldMine, Is.True);
            Assert.That(second.TargetCell, Is.EqualTo(wall));
            Assert.That(slide.ShouldMine, Is.False);
            Assert.That(slide.NextState.HasContact, Is.False);
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

        private static CharacterMoveResult2D MoveWithMotor(LogicalGridState grid, Vector2 displacement, Vector2? start = null)
        {
            var motor = new KinematicCharacterMotor2D();
            var collisionWorld = new GridCharacterCollisionWorld(grid);
            var request = new CharacterMoveRequest2D(
                start ?? ActorContactProbe.GridToWorldCenter(grid.PlayerSpawn),
                displacement,
                new CharacterMotorConfig2D(
                    ActorContactProbe.DefaultCollisionRadius,
                    0.02f,
                    0.0005f,
                    4,
                    true));
            return motor.Move(request, collisionWorld);
        }

        private static CharacterMoveResult2D CreateBlockedMoveResult(GridPosition contactCell, CharacterCollisionFlags2D flags)
        {
            var hit = new CharacterSweepHit2D(
                Vector2.zero,
                Vector2.left,
                0f,
                0f,
                contactCell,
                false);
            return new CharacterMoveResult2D(
                ActorContactProbe.GridToWorldCenter(new GridPosition(2, 2)),
                ActorContactProbe.GridToWorldCenter(new GridPosition(2, 2)),
                Vector2.right * 0.1f,
                Vector2.zero,
                flags,
                hit,
                true,
                contactCell,
                true,
                1);
        }
    }
}
