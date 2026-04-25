using Minebot.Bootstrap;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.HazardInference;
using Minebot.Automation;
using Minebot.Progression;
using Minebot.WaveSurvival;
using NUnit.Framework;
using UnityEngine;

namespace Minebot.Tests.EditMode
{
    public sealed class FoundationSmokeTests
    {
        [TearDown]
        public void TearDown()
        {
            MinebotServices.ResetForTests();
        }

        [Test]
        public void GeneratedMapCreatesSafeSpawnAndAdjacentMineTargets()
        {
            var spawn = new GridPosition(3, 3);
            LogicalGridState grid = MapGenerator.Generate(new MapGenerationSettings(new Vector2Int(7, 7), spawn, 1));

            Assert.That(grid.GetCell(spawn).TerrainKind, Is.EqualTo(TerrainKind.Empty));
            Assert.That(grid.GetCell(new GridPosition(3, 5)).TerrainKind, Is.EqualTo(TerrainKind.MineableWall));
        }

        [Test]
        public void MiningChangesWallToPassableAndReturnsReward()
        {
            var spawn = new GridPosition(3, 3);
            LogicalGridState grid = MapGenerator.Generate(new MapGenerationSettings(new Vector2Int(7, 7), spawn, 1));
            var service = new MiningService(grid);
            var player = new PlayerMiningState(spawn, HardnessTier.Soil);
            Assert.That(service.Move(player, GridPosition.Up), Is.EqualTo(MineInteractionResult.Moved));

            MineInteractionResult result = service.TryMine(player, new GridPosition(3, 5), out ResourceAmount reward);

            Assert.That(result, Is.EqualTo(MineInteractionResult.Mined));
            Assert.That(grid.GetCell(new GridPosition(3, 5)).TerrainKind, Is.EqualTo(TerrainKind.Empty));
            Assert.That(reward.Metal, Is.EqualTo(1));
        }

        [Test]
        public void HazardMarkersPersistUntilCellIsDestroyed()
        {
            var spawn = new GridPosition(3, 3);
            LogicalGridState grid = MapGenerator.Generate(new MapGenerationSettings(new Vector2Int(7, 7), spawn, 1));
            var hazards = new HazardService(grid);
            GridPosition target = new GridPosition(3, 5);

            Assert.That(hazards.ToggleMarker(target), Is.True);
            Assert.That(grid.GetCell(target).IsMarked, Is.True);

            hazards.ResolveExplosion(target, 1, 1);

            Assert.That(grid.GetCell(target).IsMarked, Is.False);
            Assert.That(grid.GetCell(target).TerrainKind, Is.EqualTo(TerrainKind.Empty));
        }

        [Test]
        public void BootstrapCreatesRuntimeRegistry()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);

            Assert.That(registry.Grid, Is.Not.Null);
            Assert.That(registry.Mining, Is.Not.Null);
            Assert.That(registry.Hazards, Is.Not.Null);
            Assert.That(registry.Session, Is.Not.Null);
        }

        [Test]
        public void SessionMiningAddsEconomyAndExperienceRewards()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);
            GridPosition target = new GridPosition(registry.Grid.PlayerSpawn.X, registry.Grid.PlayerSpawn.Y + 2);
            Assert.That(registry.Session.Move(GridPosition.Up), Is.EqualTo(MineInteractionResult.Moved));

            MineInteractionResult result = registry.Session.Mine(target);

            Assert.That(result, Is.EqualTo(MineInteractionResult.Mined));
            Assert.That(registry.Economy.Resources.Metal, Is.EqualTo(1));
            Assert.That(registry.Experience.Experience, Is.EqualTo(1));
        }

        [Test]
        public void ScanConsumesEnergyAndReturnsBombCount()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);
            GridPosition target = new GridPosition(registry.Grid.PlayerSpawn.X, registry.Grid.PlayerSpawn.Y + 1);
            ref GridCellState targetCell = ref registry.Grid.GetCellRef(target);
            targetCell.StaticFlags |= CellStaticFlags.Bomb;

            ScanResult result = registry.Session.Scan(registry.Grid.PlayerSpawn);

            Assert.That(result.Success, Is.True);
            Assert.That(result.BombCount, Is.EqualTo(1));
            Assert.That(registry.Economy.Resources.Energy, Is.EqualTo(2));
        }

        [Test]
        public void BombChainDamagesPlayerOnce()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);
            GridPosition first = new GridPosition(registry.Grid.PlayerSpawn.X, registry.Grid.PlayerSpawn.Y + 2);
            GridPosition second = new GridPosition(registry.Grid.PlayerSpawn.X, registry.Grid.PlayerSpawn.Y + 3);
            registry.Grid.GetCellRef(first).StaticFlags |= CellStaticFlags.Bomb;
            registry.Grid.GetCellRef(second).StaticFlags |= CellStaticFlags.Bomb;
            Assert.That(registry.Session.Move(GridPosition.Up), Is.EqualTo(MineInteractionResult.Moved));

            MineInteractionResult result = registry.Session.Mine(first);

            Assert.That(result, Is.EqualTo(MineInteractionResult.TriggeredBomb));
            Assert.That(registry.Vitals.CurrentHealth, Is.EqualTo(registry.Vitals.MaxHealth - 1));
            Assert.That(registry.Grid.GetCell(second).TerrainKind, Is.EqualTo(TerrainKind.Empty));
        }

        [Test]
        public void UpgradeSelectionAppliesImmediately()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);
            registry.Experience.AddExperience(5);

            UpgradeDefinition[] candidates = registry.Upgrades.GetCandidates(3);
            bool selected = registry.Upgrades.Select(candidates[0]);

            Assert.That(selected, Is.True);
            Assert.That(registry.Experience.HasPendingUpgrade, Is.False);
            Assert.That(registry.PlayerMiningState.DrillTier, Is.EqualTo(HardnessTier.Stone));
        }

        [Test]
        public void RobotFactoryConsumesMetalAndCreatesRobot()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);
            registry.Economy.Add(new ResourceAmount(5, 0, 0));

            bool created = registry.RobotFactory.TryProduce(registry.Grid.PlayerSpawn, out RobotState robot);

            Assert.That(created, Is.True);
            Assert.That(robot, Is.Not.Null);
            Assert.That(registry.Robots.Count, Is.EqualTo(1));
        }

        [Test]
        public void RobotAutomationAvoidsMarkedAndBombTargets()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);
            var robot = new RobotState(registry.Grid.PlayerSpawn);
            GridPosition unsafeTarget = new GridPosition(registry.Grid.PlayerSpawn.X, registry.Grid.PlayerSpawn.Y + 2);
            registry.Grid.GetCellRef(unsafeTarget).IsMarked = true;

            bool found = registry.RobotAutomation.TrySelectNearestSafeMineTarget(robot, out GridPosition target);

            Assert.That(found, Is.True);
            Assert.That(target, Is.Not.EqualTo(unsafeTarget));
        }

        [Test]
        public void WaveResolutionKillsPlayerAndRecyclesRobotInDangerZone()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);
            var robot = new RobotState(registry.Grid.PlayerSpawn);
            var robots = new System.Collections.Generic.List<RobotState> { robot };
            registry.Grid.GetCellRef(registry.Grid.PlayerSpawn).IsDangerZone = true;

            WaveResolution resolution = registry.Waves.ResolveWave(registry.Grid.PlayerSpawn, registry.Vitals, robots);

            Assert.That(resolution.PlayerKilled, Is.True);
            Assert.That(registry.Vitals.IsDead, Is.True);
            Assert.That(resolution.RobotsDestroyed, Is.EqualTo(1));
            Assert.That(registry.Waves.BestSurvivedWave, Is.EqualTo(0));
        }
    }
}
