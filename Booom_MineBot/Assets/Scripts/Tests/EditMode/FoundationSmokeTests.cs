using Minebot.Bootstrap;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.HazardInference;
using Minebot.Automation;
using Minebot.Editor;
using Minebot.Presentation;
using Minebot.Progression;
using Minebot.WaveSurvival;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

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
            Assert.That(grid.GetCell(spawn + GridPosition.Up + GridPosition.Left).TerrainKind, Is.EqualTo(TerrainKind.Empty));
            Assert.That(grid.GetCell(spawn + GridPosition.Up + GridPosition.Right).TerrainKind, Is.EqualTo(TerrainKind.Empty));
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
        public void DefaultBootstrapSeedsBombsOutsideStarterSafeRadius()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);
            int bombCount = 0;

            foreach (GridPosition position in registry.Grid.Positions())
            {
                GridCellState cell = registry.Grid.GetCell(position);
                if (!cell.HasBomb)
                {
                    continue;
                }

                bombCount++;
                Assert.That(position.ManhattanDistance(registry.Grid.PlayerSpawn), Is.GreaterThan(HazardRules.DefaultBombSafeRadius));
            }

            Assert.That(bombCount, Is.GreaterThan(0));
        }

        [Test]
        public void GeneratedMapContainsEnergyRewardPockets()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);
            int energyRewardCells = 0;

            foreach (GridPosition position in registry.Grid.Positions())
            {
                if (registry.Grid.GetCell(position).Reward.Energy > 0)
                {
                    energyRewardCells++;
                }
            }

            Assert.That(energyRewardCells, Is.GreaterThan(0));
        }

        [Test]
        public void SessionMiningAddsEconomyAndExperienceRewards()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);
            int metalBefore = registry.Economy.Resources.Metal;
            GridPosition target = new GridPosition(registry.Grid.PlayerSpawn.X, registry.Grid.PlayerSpawn.Y + 2);
            Assert.That(registry.Session.Move(GridPosition.Up), Is.EqualTo(MineInteractionResult.Moved));

            MineInteractionResult result = registry.Session.Mine(target);

            Assert.That(result, Is.EqualTo(MineInteractionResult.Mined));
            Assert.That(registry.Economy.Resources.Metal, Is.EqualTo(metalBefore + 1));
            Assert.That(registry.Experience.Experience, Is.EqualTo(1));
        }

        [Test]
        public void ScanConsumesEnergyAndReturnsGroupedFrontierReadings()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            SetMineableWall(grid, new GridPosition(3, 5), true);
            SetMineableWall(grid, new GridPosition(4, 4), true);
            SetMineableWall(grid, new GridPosition(1, 1), true);
            GameSessionService session = CreateSession(grid, new ResourceAmount(0, 4, 0), out PlayerEconomy economy);

            ScanResult result = session.Scan(grid.PlayerSpawn);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Readings.Count, Is.EqualTo(2));
            Assert.That(FindReading(result.Readings, new GridPosition(3, 5)).BombCount, Is.EqualTo(2));
            Assert.That(FindReading(result.Readings, new GridPosition(4, 4)).BombCount, Is.EqualTo(2));
            Assert.That(economy.Resources.Energy, Is.EqualTo(4 - HazardRules.DefaultScanEnergyCost));
        }

        [Test]
        public void ScanFailsWhenEnergyIsInsufficient()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            SetMineableWall(grid, new GridPosition(3, 5), true);
            GameSessionService session = CreateSession(grid, ResourceAmount.Zero, out PlayerEconomy economy);

            ScanResult result = session.Scan(grid.PlayerSpawn);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Readings, Is.Empty);
            Assert.That(economy.Resources.Energy, Is.EqualTo(0));
        }

        [Test]
        public void ScanConsumesEnergyAndReturnsEmptyWhenNoFrontierWallsExist()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            GameSessionService session = CreateSession(grid, new ResourceAmount(0, 3, 0), out PlayerEconomy economy);

            ScanResult result = session.Scan(grid.PlayerSpawn);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Readings, Is.Empty);
            Assert.That(economy.Resources.Energy, Is.EqualTo(3 - HazardRules.DefaultScanEnergyCost));
        }

        [Test]
        public void HazardServiceOnlyReturnsNearbyWallsThatTouchEmptyFrontier()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(8, 8), new GridPosition(3, 3));
            var hazards = new HazardService(grid);

            SetMineableWall(grid, new GridPosition(3, 5), false);
            SetMineableWall(grid, new GridPosition(6, 6), false);
            SetMineableWall(grid, new GridPosition(2, 2), false);
            SetMineableWall(grid, new GridPosition(2, 3), false);
            SetMineableWall(grid, new GridPosition(3, 2), false);
            SetMineableWall(grid, new GridPosition(1, 3), false);
            SetMineableWall(grid, new GridPosition(2, 4), false);

            IReadOnlyList<ScanReading> readings = hazards.ScanFrontierWalls(grid.PlayerSpawn, HazardRules.DefaultScanFrontierRange);

            Assert.That(ContainsReading(readings, new GridPosition(3, 5)), Is.True);
            Assert.That(ContainsReading(readings, new GridPosition(6, 6)), Is.False);
            Assert.That(ContainsReading(readings, new GridPosition(2, 2)), Is.False);
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
        public void RobotAutomationAvoidsMarkedTargets()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);
            var robot = new RobotState(registry.Grid.PlayerSpawn);
            GridPosition unsafeTarget = new GridPosition(registry.Grid.PlayerSpawn.X, registry.Grid.PlayerSpawn.Y + 2);
            registry.Grid.GetCellRef(unsafeTarget).IsMarked = true;

            bool found = registry.RobotAutomation.TrySelectNearestSafeMineTarget(robot, registry.PlayerMiningState.DrillTier, out GridPosition target);

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

        [Test]
        public void EvaluateDangerZonesMarksOnlyEdgeBandAtThicknessOne()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            SetMineableWall(grid, new GridPosition(3, 3), false);
            SetMineableWall(grid, new GridPosition(3, 4), false);
            SetMineableWall(grid, new GridPosition(3, 5), false);
            var waves = new WaveSurvivalService(grid, ScriptableObject.CreateInstance<WaveConfig>());

            waves.EvaluateDangerZones();

            Assert.That(grid.GetCell(new GridPosition(2, 3)).IsDangerZone, Is.True);
            Assert.That(grid.GetCell(new GridPosition(4, 4)).IsDangerZone, Is.True);
            Assert.That(grid.GetCell(new GridPosition(1, 3)).IsDangerZone, Is.False);
            Assert.That(grid.GetCell(new GridPosition(5, 4)).IsDangerZone, Is.False);
        }

        [Test]
        public void GeneratedStarterCavityProducesConnectedDangerRingAtThicknessOne()
        {
            GridPosition spawn = new GridPosition(3, 3);
            LogicalGridState grid = MapGenerator.Generate(new MapGenerationSettings(new Vector2Int(9, 9), spawn, 1));
            var waves = new WaveSurvivalService(grid, ScriptableObject.CreateInstance<WaveConfig>());

            waves.EvaluateDangerZones();

            Assert.That(grid.GetCell(spawn).IsDangerZone, Is.False);
            Assert.That(grid.GetCell(spawn + GridPosition.Up).IsDangerZone, Is.True);
            Assert.That(grid.GetCell(spawn + GridPosition.Down).IsDangerZone, Is.True);
            Assert.That(grid.GetCell(spawn + GridPosition.Left).IsDangerZone, Is.True);
            Assert.That(grid.GetCell(spawn + GridPosition.Right).IsDangerZone, Is.True);
            Assert.That(grid.GetCell(spawn + GridPosition.Up + GridPosition.Left).IsDangerZone, Is.True);
            Assert.That(grid.GetCell(spawn + GridPosition.Up + GridPosition.Right).IsDangerZone, Is.True);
            Assert.That(grid.GetCell(spawn + GridPosition.Down + GridPosition.Left).IsDangerZone, Is.True);
            Assert.That(grid.GetCell(spawn + GridPosition.Down + GridPosition.Right).IsDangerZone, Is.True);
        }

        [Test]
        public void DangerZonesDoNotMarkDiagonalOnlyWallAdjacency()
        {
            GridPosition spawn = new GridPosition(4, 4);
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(9, 9), spawn);
            SetMineableWall(grid, spawn + GridPosition.Up + GridPosition.Right, false);
            var waves = new WaveSurvivalService(grid, ScriptableObject.CreateInstance<WaveConfig>());

            waves.EvaluateDangerZones();

            Assert.That(grid.GetCell(spawn).IsDangerZone, Is.False);
            Assert.That(grid.GetCell(spawn + GridPosition.Up).IsDangerZone, Is.True);
            Assert.That(grid.GetCell(spawn + GridPosition.Right).IsDangerZone, Is.True);
        }

        [Test]
        public void EvaluateDangerZonesCollapsesDisconnectedSafeIslandOutsidePrimaryCavity()
        {
            GridPosition spawn = new GridPosition(3, 3);
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(10, 10), spawn);

            for (int y = 1; y < 9; y++)
            {
                for (int x = 1; x < 9; x++)
                {
                    SetMineableWall(grid, new GridPosition(x, y), false);
                }
            }

            CarveEmptyRoom(grid, new GridPosition(2, 2), 3, 3);
            CarveEmptyRoom(grid, new GridPosition(6, 6), 3, 3);

            var waves = new WaveSurvivalService(grid, ScriptableObject.CreateInstance<WaveConfig>());

            waves.EvaluateDangerZones();

            Assert.That(grid.GetCell(spawn).IsDangerZone, Is.False);
            Assert.That(grid.GetCell(new GridPosition(7, 7)).IsDangerZone, Is.True);
        }

        [Test]
        public void EvaluateDangerZonesKeepsSafePocketInsideSpawnConnectedCavity()
        {
            GridPosition spawn = new GridPosition(4, 4);
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(9, 9), spawn);

            for (int y = 1; y < 8; y++)
            {
                for (int x = 1; x < 8; x++)
                {
                    SetMineableWall(grid, new GridPosition(x, y), false);
                }
            }

            GridPosition[] carvedCells =
            {
                new(4, 1),
                new(1, 2), new(4, 2),
                new(1, 3), new(2, 3), new(3, 3), new(4, 3), new(5, 3),
                new(1, 4), new(2, 4), new(3, 4), new(4, 4), new(5, 4),
                new(1, 5), new(2, 5), new(3, 5), new(4, 5), new(5, 5), new(6, 5),
                new(1, 6), new(3, 6), new(4, 6), new(5, 6), new(6, 6), new(7, 6),
                new(1, 7), new(2, 7), new(3, 7), new(6, 7)
            };

            for (int i = 0; i < carvedCells.Length; i++)
            {
                SetEmpty(grid, carvedCells[i]);
            }

            var waves = new WaveSurvivalService(grid, ScriptableObject.CreateInstance<WaveConfig>());

            waves.EvaluateDangerZones();

            Assert.That(grid.GetCell(spawn).IsDangerZone, Is.False);
            Assert.That(grid.GetCell(new GridPosition(6, 6)).IsDangerZone, Is.False);
            Assert.That(grid.GetCell(new GridPosition(7, 6)).IsDangerZone, Is.True);
            Assert.That(grid.GetCell(new GridPosition(6, 7)).IsDangerZone, Is.True);
        }

        [Test]
        public void EvaluateDangerZonesExpandsInwardAsWaveThicknessGrows()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(9, 9), new GridPosition(4, 4));
            for (int y = 2; y <= 6; y++)
            {
                SetMineableWall(grid, new GridPosition(2, y), false);
            }

            var waves = new WaveSurvivalService(grid, ScriptableObject.CreateInstance<WaveConfig>());
            waves.ResolveWave(new GridPosition(4, 4), new PlayerVitals(3), new List<RobotState>());
            waves.ResolveWave(new GridPosition(4, 4), new PlayerVitals(3), new List<RobotState>());

            waves.EvaluateDangerZones();

            Assert.That(grid.GetCell(new GridPosition(3, 4)).IsDangerZone, Is.True);
            Assert.That(grid.GetCell(new GridPosition(4, 4)).IsDangerZone, Is.True);
            Assert.That(grid.GetCell(new GridPosition(5, 4)).IsDangerZone, Is.False);
        }

        [Test]
        public void PixelArtPipelineBindsContourFamilyAssetsIntoDefaultArtSet()
        {
            AssetDatabase.Refresh();
            MinebotPixelArtAssetPipeline.EnsureDefaultAssets();

            MinebotPresentationArtSet artSet = AssetDatabase.LoadAssetAtPath<MinebotPresentationArtSet>(
                "Assets/Resources/Minebot/MinebotPresentationArtSet_Default.asset");

            Assert.That(artSet, Is.Not.Null);
            Assert.That(artSet.WallContourTiles.Length, Is.EqualTo(DualGridContour.TileCount));
            Assert.That(artSet.BuildPreviewValidTile, Is.Not.Null);
            Assert.That(artSet.BuildPreviewInvalidTile, Is.Not.Null);
            Assert.That(artSet.SoilDetailTile, Is.Not.Null);
            Assert.That(artSet.StoneDetailTile, Is.Not.Null);
            Assert.That(artSet.HardRockDetailTile, Is.Not.Null);
            Assert.That(artSet.UltraHardDetailTile, Is.Not.Null);
            Assert.That(artSet.WallContourTiles[5], Is.Not.Null);

            Assert.That(AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Minebot/Tiles/Tile_WallContour_05.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Minebot/Tiles/Tile_BuildPreviewValid.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Minebot/Tiles/Tile_DetailUltraHard.asset"), Is.Not.Null);
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

        private static void SetMineableWall(LogicalGridState grid, GridPosition position, bool hasBomb)
        {
            ref GridCellState cell = ref grid.GetCellRef(position);
            cell.TerrainKind = TerrainKind.MineableWall;
            cell.HardnessTier = HardnessTier.Soil;
            cell.IsRevealed = false;
            cell.IsMarked = false;
            cell.IsOccupiedByBuilding = false;
            cell.StaticFlags = hasBomb ? CellStaticFlags.Bomb : CellStaticFlags.None;
        }

        private static void SetEmpty(LogicalGridState grid, GridPosition position)
        {
            ref GridCellState cell = ref grid.GetCellRef(position);
            cell = new GridCellState(TerrainKind.Empty, HardnessTier.Soil, CellStaticFlags.None, ResourceAmount.Zero);
        }

        private static void CarveEmptyRoom(LogicalGridState grid, GridPosition origin, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    SetEmpty(grid, new GridPosition(origin.X + x, origin.Y + y));
                }
            }
        }

        private static ScanReading FindReading(IReadOnlyList<ScanReading> readings, GridPosition position)
        {
            for (int i = 0; i < readings.Count; i++)
            {
                if (readings[i].WallPosition.Equals(position))
                {
                    return readings[i];
                }
            }

            Assert.Fail($"Missing scan reading for {position}.");
            return default;
        }

        private static bool ContainsReading(IReadOnlyList<ScanReading> readings, GridPosition position)
        {
            for (int i = 0; i < readings.Count; i++)
            {
                if (readings[i].WallPosition.Equals(position))
                {
                    return true;
                }
            }

            return false;
        }

        private static GameSessionService CreateSession(LogicalGridState grid, ResourceAmount startingResources, out PlayerEconomy economy)
        {
            var player = new PlayerMiningState(grid.PlayerSpawn, HardnessTier.Soil);
            var mining = new MiningService(grid);
            var hazards = new HazardService(grid);
            economy = new PlayerEconomy(startingResources);
            var experience = new ExperienceService(4);
            var vitals = new PlayerVitals(3);
            var robots = new List<RobotState>();
            var robotAutomation = new RobotAutomationService(grid);

            return new GameSessionService(
                player,
                mining,
                hazards,
                null,
                economy,
                experience,
                vitals,
                robotAutomation,
                robots,
                ResourceAmount.Zero,
                true,
                HardnessTier.Soil);
        }
    }
}
