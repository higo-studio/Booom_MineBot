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
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

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
            grid.GetCellRef(new GridPosition(4, 5)).StaticFlags |= CellStaticFlags.Bomb;

            MineInteractionResult result = service.TryMine(player, new GridPosition(3, 5), out ResourceAmount reward);

            Assert.That(result, Is.EqualTo(MineInteractionResult.Mined));
            Assert.That(grid.GetCell(new GridPosition(3, 5)).TerrainKind, Is.EqualTo(TerrainKind.Empty));
            Assert.That(reward.Metal, Is.EqualTo(1));
        }

        [Test]
        public void MiningZeroWallCascadesAcrossConnectedSafeWalls()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(8, 8), new GridPosition(3, 3));
            GridPosition[] walls =
            {
                new GridPosition(3, 4),
                new GridPosition(4, 4),
                new GridPosition(3, 5),
                new GridPosition(4, 5)
            };

            for (int i = 0; i < walls.Length; i++)
            {
                SetMineableWall(grid, walls[i], false);
            }

            var player = new PlayerMiningState(grid.PlayerSpawn, HardnessTier.Soil);
            var mining = new MiningService(grid);

            MineResolution resolution = mining.TryMineDetailed(player, new GridPosition(3, 4));

            Assert.That(resolution.Result, Is.EqualTo(MineInteractionResult.Mined));
            Assert.That(resolution.ClearedCells.Count, Is.EqualTo(walls.Length));
            for (int i = 0; i < walls.Length; i++)
            {
                Assert.That(grid.GetCell(walls[i]).TerrainKind, Is.EqualTo(TerrainKind.Empty));
            }
        }

        [Test]
        public void MiningNumberedWallStopsExpansionAtCurrentCell()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(8, 8), new GridPosition(3, 3));
            GridPosition target = new GridPosition(3, 4);
            GridPosition safeNeighbor = new GridPosition(4, 4);
            GridPosition bombWall = new GridPosition(4, 5);
            SetMineableWall(grid, target, false);
            SetMineableWall(grid, safeNeighbor, false);
            SetMineableWall(grid, bombWall, true);

            var player = new PlayerMiningState(grid.PlayerSpawn, HardnessTier.Soil);
            var mining = new MiningService(grid);

            MineResolution resolution = mining.TryMineDetailed(player, target);

            Assert.That(resolution.Result, Is.EqualTo(MineInteractionResult.Mined));
            Assert.That(resolution.ClearedCells.Count, Is.EqualTo(1));
            Assert.That(grid.GetCell(target).TerrainKind, Is.EqualTo(TerrainKind.Empty));
            Assert.That(grid.GetCell(safeNeighbor).TerrainKind, Is.EqualTo(TerrainKind.MineableWall));
            Assert.That(grid.GetCell(bombWall).TerrainKind, Is.EqualTo(TerrainKind.MineableWall));
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
        public void DefaultGeneratedMapSettingsExpandCurrentMapByTwentyTimes()
        {
            MapGenerationSettings settings = MapGenerationSettings.CreateDefault();

            Assert.That(settings.Size, Is.EqualTo(new Vector2Int(240, 240)));
            Assert.That(settings.Spawn, Is.EqualTo(new GridPosition(120, 120)));
        }

        [Test]
        public void BootstrapGeneratedMapUsesExpandedDefaultSize()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);

            Assert.That(registry.Grid.Size, Is.EqualTo(new Vector2Int(240, 240)));
            Assert.That(registry.Grid.PlayerSpawn, Is.EqualTo(new GridPosition(120, 120)));
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
        public void GeneratedMapForcesUltraHardOutsideConfiguredOuterRadius()
        {
            MapGenerationSettings settings = new MapGenerationSettings(
                new Vector2Int(61, 61),
                new GridPosition(30, 30),
                1,
                0.75f,
                0.25f,
                new Vector2(0.06f, 0.06f),
                new Vector2(11.3f, 47.9f),
                1f,
                0.8f,
                0.24f,
                0.49f,
                0.71f);
            LogicalGridState grid = MapGenerator.Generate(settings);

            foreach (GridPosition position in grid.Positions())
            {
                GridCellState cell = grid.GetCell(position);
                if (cell.TerrainKind != TerrainKind.MineableWall)
                {
                    continue;
                }

                if (ComputeRadialDistance01(settings, position) >= settings.ForcedUltraHardDistanceNormalized)
                {
                    Assert.That(cell.HardnessTier, Is.EqualTo(HardnessTier.UltraHard), $"Expected outer ring cell {position} to be UltraHard.");
                }
            }
        }

        [Test]
        public void GeneratedMapBlendsRadialGradientAndNoiseIntoVariedHardnessBands()
        {
            MapGenerationSettings settings = new MapGenerationSettings(
                new Vector2Int(81, 81),
                new GridPosition(40, 40),
                1,
                0.7f,
                0.3f,
                new Vector2(0.08f, 0.08f),
                new Vector2(3.1f, 19.7f),
                1.05f,
                0.86f,
                0.22f,
                0.46f,
                0.7f);
            LogicalGridState grid = MapGenerator.Generate(settings);
            var hardnessCounts = new Dictionary<HardnessTier, int>();
            var hardnessByRingBucket = new Dictionary<int, HashSet<HardnessTier>>();

            foreach (GridPosition position in grid.Positions())
            {
                GridCellState cell = grid.GetCell(position);
                if (cell.TerrainKind != TerrainKind.MineableWall)
                {
                    continue;
                }

                hardnessCounts[cell.HardnessTier] = hardnessCounts.TryGetValue(cell.HardnessTier, out int count) ? count + 1 : 1;

                int bucket = Mathf.RoundToInt(ComputeRadialDistance01(settings, position) * 12f);
                if (!hardnessByRingBucket.TryGetValue(bucket, out HashSet<HardnessTier> ringValues))
                {
                    ringValues = new HashSet<HardnessTier>();
                    hardnessByRingBucket.Add(bucket, ringValues);
                }

                ringValues.Add(cell.HardnessTier);
            }

            Assert.That(hardnessCounts.ContainsKey(HardnessTier.Soil), Is.True);
            Assert.That(hardnessCounts.ContainsKey(HardnessTier.Stone), Is.True);
            Assert.That(hardnessCounts.ContainsKey(HardnessTier.HardRock), Is.True);
            Assert.That(hardnessCounts.ContainsKey(HardnessTier.UltraHard), Is.True);

            bool foundRingWithNoiseVariation = false;
            foreach (KeyValuePair<int, HashSet<HardnessTier>> pair in hardnessByRingBucket)
            {
                if (pair.Value.Count > 1)
                {
                    foundRingWithNoiseVariation = true;
                    break;
                }
            }

            Assert.That(foundRingWithNoiseVariation, Is.True);
        }

        [Test]
        public void SessionMiningAddsEconomyAndExperienceRewards()
        {
            RuntimeServiceRegistry registry = MinebotServices.Initialize(null);
            int metalBefore = registry.Economy.Resources.Metal;
            int experienceBefore = registry.Experience.Experience;
            GridPosition target = new GridPosition(registry.Grid.PlayerSpawn.X, registry.Grid.PlayerSpawn.Y + 2);
            Assert.That(registry.Session.Move(GridPosition.Up), Is.EqualTo(MineInteractionResult.Moved));
            registry.Grid.GetCellRef(target).ClearBomb();
            registry.Grid.GetCellRef(target + GridPosition.Right).StaticFlags |= CellStaticFlags.Bomb;

            MineInteractionResult result = registry.Session.Mine(target);

            Assert.That(result, Is.EqualTo(MineInteractionResult.Mined));
            Assert.That(registry.Economy.Resources.Metal, Is.EqualTo(metalBefore));
            Assert.That(registry.Experience.Experience, Is.EqualTo(experienceBefore));
            Assert.That(registry.WorldPickups.ActivePickups.Count, Is.GreaterThanOrEqualTo(2));

            bool collected = registry.Session.TickWorldPickups(1f, ToWorldCenter(target));

            Assert.That(collected, Is.True);
            Assert.That(registry.WorldPickups.ActivePickups, Is.Empty);
            Assert.That(registry.Economy.Resources.Metal, Is.GreaterThanOrEqualTo(metalBefore + 1));
            Assert.That(registry.Experience.Experience, Is.GreaterThanOrEqualTo(experienceBefore + 1));
        }

        [Test]
        public void ExperiencePickupTriggersPendingUpgradeOnlyAfterAbsorb()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            GridPosition target = new GridPosition(3, 5);
            SetMineableWall(grid, target, false);
            grid.GetCellRef(target).Reward = new ResourceAmount(0, 0, 4);
            GameSessionService session = CreateSession(grid, ResourceAmount.Zero, out _, out ExperienceService experience);

            Assert.That(session.Move(GridPosition.Up), Is.EqualTo(MineInteractionResult.Moved));
            MineInteractionResult result = session.Mine(target);

            Assert.That(result, Is.EqualTo(MineInteractionResult.Mined));
            Assert.That(session.WorldPickups.ActivePickups.Count, Is.EqualTo(1));
            Assert.That(experience.HasPendingUpgrade, Is.False);

            bool collected = session.TickWorldPickups(1f, ToWorldCenter(target));

            Assert.That(collected, Is.True);
            Assert.That(session.WorldPickups.ActivePickups, Is.Empty);
            Assert.That(experience.Experience, Is.EqualTo(4));
            Assert.That(experience.HasPendingUpgrade, Is.True);
        }

        [Test]
        public void PassiveHazardSenseRefreshesGroupedFrontierReadingsWithoutConsumingEnergy()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            SetMineableWall(grid, new GridPosition(3, 5), true);
            SetMineableWall(grid, new GridPosition(4, 4), true);
            SetMineableWall(grid, new GridPosition(1, 1), true);
            GameSessionService session = CreateSession(grid, new ResourceAmount(0, 4, 0), out PlayerEconomy economy);

            IReadOnlyList<ScanReading> readings = session.RefreshPassiveHazardSense();

            Assert.That(readings.Count, Is.EqualTo(2));
            Assert.That(FindReading(readings, new GridPosition(3, 5)).BombCount, Is.EqualTo(2));
            Assert.That(FindReading(readings, new GridPosition(4, 4)).BombCount, Is.EqualTo(2));
            Assert.That(economy.Resources.Energy, Is.EqualTo(4));
        }

        [Test]
        public void PassiveHazardSenseTimerWaitsForConfiguredInterval()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            SetMineableWall(grid, new GridPosition(3, 5), true);
            GameSessionService session = CreateSession(grid, ResourceAmount.Zero, out _);
            IReadOnlyList<ScanReading> latestReadings = Array.Empty<ScanReading>();
            int updateCount = 0;
            session.PassiveHazardSenseUpdated += readings =>
            {
                latestReadings = readings;
                updateCount++;
            };

            bool beforeInterval = session.TickPassiveHazardSense(HazardRules.DefaultPassiveHazardSenseIntervalSeconds - 0.01f);
            bool atInterval = session.TickPassiveHazardSense(0.02f);

            Assert.That(beforeInterval, Is.False);
            Assert.That(atInterval, Is.True);
            Assert.That(updateCount, Is.EqualTo(1));
            Assert.That(latestReadings.Count, Is.EqualTo(1));
            Assert.That(latestReadings[0].WallPosition, Is.EqualTo(new GridPosition(3, 5)));
        }

        [Test]
        public void PassiveHazardSenseReturnsEmptyWhenNoFrontierWallsExist()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            GameSessionService session = CreateSession(grid, new ResourceAmount(0, 3, 0), out PlayerEconomy economy);

            IReadOnlyList<ScanReading> readings = session.RefreshPassiveHazardSense();

            Assert.That(readings, Is.Empty);
            Assert.That(economy.Resources.Energy, Is.EqualTo(3));
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
        public void BombHitDamagesPlayerWithoutClearingAdjacentWalls()
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
            Assert.That(registry.Grid.GetCell(second).TerrainKind, Is.EqualTo(TerrainKind.MineableWall));
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
        public void WaveResolutionCollapsesDangerCellsBackIntoMineableWalls()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            GridPosition collapsedCell = new GridPosition(3, 2);
            ref GridCellState cell = ref grid.GetCellRef(collapsedCell);
            cell.TerrainKind = TerrainKind.Empty;
            cell.IsDangerZone = true;
            cell.IsMarked = true;
            cell.StaticFlags |= CellStaticFlags.Bomb;

            var waves = new WaveSurvivalService(grid, ScriptableObject.CreateInstance<WaveConfig>());
            WaveResolution resolution = waves.ResolveWave(grid.PlayerSpawn, new PlayerVitals(3), new List<RobotState>());
            GridCellState collapsed = grid.GetCell(collapsedCell);

            Assert.That(resolution.PlayerKilled, Is.False);
            Assert.That(collapsed.TerrainKind, Is.EqualTo(TerrainKind.MineableWall));
            Assert.That(collapsed.HardnessTier, Is.EqualTo(HardnessTier.Soil));
            Assert.That(collapsed.Reward, Is.EqualTo(new ResourceAmount(1, 0, 1)));
            Assert.That(collapsed.IsDangerZone, Is.False);
            Assert.That(collapsed.IsMarked, Is.False);
            Assert.That(collapsed.HasBomb, Is.False);
            Assert.That(collapsed.IsRevealed, Is.False);
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
        public void LayeredBinaryTerrainResolverProducesStableOrderedCommandsForMixedSample()
        {
            var resolver = new LayeredBinaryTerrainResolver();
            var sample = new CornerMaterialSample(
                TerrainMaterialId.Soil,
                TerrainMaterialId.Stone,
                TerrainMaterialId.Floor,
                TerrainMaterialId.Boundary);
            var first = new RenderLayerCommand[DualGridTerrain.RenderLayerCount];
            var second = new RenderLayerCommand[DualGridTerrain.RenderLayerCount];

            resolver.Resolve(sample, first);
            resolver.Resolve(sample, second);

            Assert.That(first, Is.EqualTo(second));
            for (int i = 0; i < first.Length; i++)
            {
                Assert.That(DualGridTerrain.GetOrderedLayerIndex(first[i].LayerId), Is.EqualTo(i));
            }

            Assert.That(CommandAtLayer(first, TerrainRenderLayerId.Floor), Is.EqualTo(new RenderLayerCommand(TerrainRenderLayerId.Floor, 2, true)));
            Assert.That(CommandAtLayer(first, TerrainRenderLayerId.Soil), Is.EqualTo(new RenderLayerCommand(TerrainRenderLayerId.Stone, 12, true)));
            Assert.That(CommandAtLayer(first, TerrainRenderLayerId.Boundary), Is.EqualTo(new RenderLayerCommand(TerrainRenderLayerId.Boundary, 1, true)));
        }

        [Test]
        public void LayeredBinaryTerrainResolverCoversPrimaryFloorRockAndBoundaryCases()
        {
            var resolver = new LayeredBinaryTerrainResolver();

            AssertResolverScenario(
                resolver,
                new CornerMaterialSample(
                    TerrainMaterialId.Floor,
                    TerrainMaterialId.Floor,
                    TerrainMaterialId.Floor,
                    TerrainMaterialId.Floor),
                new RenderLayerCommand(TerrainRenderLayerId.Floor, 15, true));

            AssertResolverScenario(
                resolver,
                new CornerMaterialSample(
                    TerrainMaterialId.HardRock,
                    TerrainMaterialId.HardRock,
                    TerrainMaterialId.HardRock,
                    TerrainMaterialId.HardRock),
                new RenderLayerCommand(TerrainRenderLayerId.HardRock, 15, true));

            AssertResolverScenario(
                resolver,
                new CornerMaterialSample(
                    TerrainMaterialId.Soil,
                    TerrainMaterialId.Soil,
                    TerrainMaterialId.Floor,
                    TerrainMaterialId.Floor),
                new RenderLayerCommand(TerrainRenderLayerId.Floor, 3, true),
                new RenderLayerCommand(TerrainRenderLayerId.Soil, 12, true));

            AssertResolverScenario(
                resolver,
                new CornerMaterialSample(
                    TerrainMaterialId.Soil,
                    TerrainMaterialId.Stone,
                    TerrainMaterialId.Soil,
                    TerrainMaterialId.Stone),
                new RenderLayerCommand(TerrainRenderLayerId.Stone, 15, true));

            AssertResolverScenario(
                resolver,
                new CornerMaterialSample(
                    TerrainMaterialId.Boundary,
                    TerrainMaterialId.Boundary,
                    TerrainMaterialId.Floor,
                    TerrainMaterialId.Floor),
                new RenderLayerCommand(TerrainRenderLayerId.Floor, 3, true),
                new RenderLayerCommand(TerrainRenderLayerId.Boundary, 12, true));
        }

        [Test]
        public void TilemapGridPresentationPromotesDeepFogCellsToNearBandWhenFrontierAdvances()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            GridPosition changedCell = new GridPosition(3, 4);
            GridPosition deepCell = new GridPosition(3, 3);
            for (int y = 2; y <= 4; y++)
            {
                for (int x = 2; x <= 4; x++)
                {
                    SetMineableWall(grid, new GridPosition(x, y), false);
                }
            }

            RuntimeServiceRegistry services = CreatePresentationRegistry(grid);
            var root = new GameObject("DualGridPresentationEditModeTest");
            try
            {
                Tilemap[] terrainTilemaps = CreateTerrainTilemaps(root.transform);
                TilemapGridPresentation presentation = root.AddComponent<TilemapGridPresentation>();
                Tilemap fogNearTilemap = CreateTilemap(root.transform, "Fog Near Test Tilemap", DualGridFog.DisplayOffset);
                Tilemap fogDeepTilemap = CreateTilemap(root.transform, "Fog Deep Test Tilemap", DualGridFog.DisplayOffset);
                presentation.Configure(
                    terrainTilemaps,
                    fogNearTilemap,
                    fogDeepTilemap,
                    CreateTilemap(root.transform, "Facility Test Tilemap", Vector3.zero),
                    CreateTilemap(root.transform, "Marker Test Tilemap", Vector3.zero),
                    CreateTilemap(root.transform, "Danger Test Tilemap", Vector3.zero),
                    CreateTilemap(root.transform, "Build Preview Test Tilemap", Vector3.zero),
                    LoadDefaultPresentationAssets());

                presentation.Refresh(services, new GridPosition(-1, -1), new GridPosition(-2, -2));
                Dictionary<Vector3Int, string> before = SnapshotTerrainDisplay(terrainTilemaps);
                Dictionary<Vector3Int, string> fogNearBefore = SnapshotTilemapDisplay(fogNearTilemap);
                Dictionary<Vector3Int, string> fogDeepBefore = SnapshotTilemapDisplay(fogDeepTilemap);
                Assert.That(HasAnyDisplayTileAroundCell(fogNearTilemap, changedCell), Is.True);
                Assert.That(HasAnyDisplayTileAroundCell(fogDeepTilemap, deepCell), Is.True);

                SetEmpty(grid, changedCell);
                presentation.Refresh(services, new GridPosition(-1, -1), new GridPosition(-2, -2));
                Dictionary<Vector3Int, string> after = SnapshotTerrainDisplay(terrainTilemaps);
                Dictionary<Vector3Int, string> fogNearAfter = SnapshotTilemapDisplay(fogNearTilemap);
                Dictionary<Vector3Int, string> fogDeepAfter = SnapshotTilemapDisplay(fogDeepTilemap);

                IReadOnlyCollection<Vector3Int> changedDisplayCells = CollectChangedDisplayCells(before, after);
                IReadOnlyCollection<Vector3Int> changedFogNearDisplayCells = CollectChangedDisplayCells(fogNearBefore, fogNearAfter);
                IReadOnlyCollection<Vector3Int> changedFogDeepDisplayCells = CollectChangedDisplayCells(fogDeepBefore, fogDeepAfter);
                Assert.That(changedDisplayCells, Is.EquivalentTo(DualGridTerrain.GetAffectedDisplayCells(changedCell)));
                Assert.That(changedFogNearDisplayCells.Count, Is.GreaterThan(0));
                Assert.That(changedFogDeepDisplayCells.Count, Is.GreaterThan(0));
                Assert.That(HasAnyDisplayTileAroundCell(fogNearTilemap, deepCell), Is.True);
                Assert.That(HasAnyDisplayTileAroundCell(fogDeepTilemap, deepCell), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FogTilemapsRenderCollapsedDangerCellBackIntoNearBand()
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(7, 7), new GridPosition(3, 3));
            GridPosition collapsedCell = new GridPosition(3, 2);
            ref GridCellState cell = ref grid.GetCellRef(collapsedCell);
            cell.TerrainKind = TerrainKind.Empty;
            cell.IsDangerZone = true;
            cell.IsRevealed = true;

            var waves = new WaveSurvivalService(grid, ScriptableObject.CreateInstance<WaveConfig>());
            RuntimeServiceRegistry services = CreatePresentationRegistry(grid);
            var root = new GameObject("FogCollapseEditModeTest");
            try
            {
                Tilemap[] terrainTilemaps = CreateTerrainTilemaps(root.transform);
                Tilemap fogNearTilemap = CreateTilemap(root.transform, "Fog Near Test Tilemap", DualGridFog.DisplayOffset);
                Tilemap fogDeepTilemap = CreateTilemap(root.transform, "Fog Deep Test Tilemap", DualGridFog.DisplayOffset);
                TilemapGridPresentation presentation = root.AddComponent<TilemapGridPresentation>();
                presentation.Configure(
                    terrainTilemaps,
                    fogNearTilemap,
                    fogDeepTilemap,
                    CreateTilemap(root.transform, "Facility Test Tilemap", Vector3.zero),
                    CreateTilemap(root.transform, "Marker Test Tilemap", Vector3.zero),
                    CreateTilemap(root.transform, "Danger Test Tilemap", Vector3.zero),
                    CreateTilemap(root.transform, "Build Preview Test Tilemap", Vector3.zero),
                    LoadDefaultPresentationAssets());

                presentation.Refresh(services, new GridPosition(-1, -1), new GridPosition(-2, -2));
                Assert.That(HasAnyDisplayTileAroundCell(fogNearTilemap, collapsedCell), Is.False);
                Assert.That(HasAnyDisplayTileAroundCell(fogDeepTilemap, collapsedCell), Is.False);

                waves.ResolveWave(grid.PlayerSpawn, new PlayerVitals(3), new List<RobotState>());
                presentation.Refresh(services, new GridPosition(-1, -1), new GridPosition(-2, -2));

                Assert.That(grid.GetCell(collapsedCell).TerrainKind, Is.EqualTo(TerrainKind.MineableWall));
                Assert.That(grid.GetCell(collapsedCell).IsRevealed, Is.False);
                Assert.That(HasAnyDisplayTileAroundCell(fogNearTilemap, collapsedCell), Is.True);
                Assert.That(HasAnyDisplayTileAroundCell(fogDeepTilemap, collapsedCell), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PixelArtPipelineExposesDualGridTerrainFamiliesIntoDefaultArtSet()
        {
            AssetDatabase.Refresh();
            MinebotPixelArtAssetPipeline.EnsureDefaultAssets();

            MinebotPresentationArtSet artSet = AssetDatabase.LoadAssetAtPath<MinebotPresentationArtSet>(
                "Assets/Resources/Minebot/MinebotPresentationArtSet_Default.asset");
            var serializedArtSet = new SerializedObject(artSet);
            DualGridTerrainProfile dualGridProfile = AssetDatabase.LoadAssetAtPath<DualGridTerrainProfile>(
                "Assets/Resources/Minebot/MinebotDualGridTerrainProfile_Default.asset");

            Assert.That(artSet, Is.Not.Null);
            Assert.That(dualGridProfile, Is.Not.Null);
            Assert.That(artSet.DualGridTerrainProfile, Is.EqualTo(dualGridProfile));
            Assert.That(dualGridProfile.Families.Length, Is.EqualTo(DualGridTerrain.MaterialFamilies.Length));
            Assert.That(dualGridProfile.LayoutSettings.DisplayOffset, Is.EqualTo(DualGridTerrain.DisplayOffset));
            Assert.That(artSet.FloorDualGridTiles.Length, Is.EqualTo(DualGridTerrain.TileCount));
            Assert.That(artSet.SoilDualGridTiles.Length, Is.EqualTo(DualGridTerrain.TileCount));
            Assert.That(artSet.StoneDualGridTiles.Length, Is.EqualTo(DualGridTerrain.TileCount));
            Assert.That(artSet.HardRockDualGridTiles.Length, Is.EqualTo(DualGridTerrain.TileCount));
            Assert.That(artSet.UltraHardDualGridTiles.Length, Is.EqualTo(DualGridTerrain.TileCount));
            Assert.That(artSet.BoundaryDualGridTiles.Length, Is.EqualTo(DualGridTerrain.TileCount));
            Assert.That(artSet.FogNearDualGridTiles.Length, Is.EqualTo(DualGridFog.TileCount));
            Assert.That(artSet.FogDeepDualGridTiles.Length, Is.EqualTo(DualGridFog.TileCount));
            Assert.That(serializedArtSet.FindProperty("floorDualGridTiles").arraySize, Is.EqualTo(DualGridTerrain.TileCount));
            Assert.That(serializedArtSet.FindProperty("soilDualGridTiles").arraySize, Is.EqualTo(DualGridTerrain.TileCount));
            Assert.That(serializedArtSet.FindProperty("stoneDualGridTiles").arraySize, Is.EqualTo(DualGridTerrain.TileCount));
            Assert.That(serializedArtSet.FindProperty("hardRockDualGridTiles").arraySize, Is.EqualTo(DualGridTerrain.TileCount));
            Assert.That(serializedArtSet.FindProperty("ultraHardDualGridTiles").arraySize, Is.EqualTo(DualGridTerrain.TileCount));
            Assert.That(serializedArtSet.FindProperty("boundaryDualGridTiles").arraySize, Is.EqualTo(DualGridTerrain.TileCount));
            Assert.That(serializedArtSet.FindProperty("fogNearDualGridTiles").arraySize, Is.EqualTo(DualGridFog.TileCount));
            Assert.That(serializedArtSet.FindProperty("fogDeepDualGridTiles").arraySize, Is.EqualTo(DualGridFog.TileCount));
            Assert.That(artSet.BuildPreviewValidTile, Is.Not.Null);
            Assert.That(artSet.BuildPreviewInvalidTile, Is.Not.Null);
            Assert.That(artSet.SoilDetailTile, Is.Not.Null);
            Assert.That(artSet.StoneDetailTile, Is.Not.Null);
            Assert.That(artSet.HardRockDetailTile, Is.Not.Null);
            Assert.That(artSet.UltraHardDetailTile, Is.Not.Null);
            Assert.That(artSet.HologramOverlayAtlas, Is.Not.Null);
            Assert.That(artSet.BitmapGlyphAtlas, Is.Not.Null);
            Assert.That(artSet.BitmapGlyphDescriptor, Is.Not.Null);
            Assert.That(artSet.BitmapGlyphFont, Is.Not.Null);
            Assert.That(artSet.ActorResources.PlayerPrefab, Is.Not.Null);
            Assert.That(artSet.ActorResources.HelperRobotPrefab, Is.Not.Null);
            Assert.That(artSet.ActorResources.PlayerStates.ForState(PresentationActorState.Mining), Is.Not.Null);
            Assert.That(artSet.ActorResources.HelperRobotStates.ForState(PresentationActorState.Destroyed), Is.Not.Null);
            Assert.That(artSet.PickupResources.MetalPickupPrefab, Is.Not.Null);
            Assert.That(artSet.PickupResources.EnergyPickupPrefab, Is.Not.Null);
            Assert.That(artSet.PickupResources.ExperiencePickupPrefab, Is.Not.Null);
            Assert.That(artSet.CellFxResources.MiningCrackPrefab, Is.Not.Null);
            Assert.That(artSet.CellFxResources.WallBreakPrefab, Is.Not.Null);
            Assert.That(artSet.CellFxResources.ExplosionPrefab, Is.Not.Null);
            Assert.That(artSet.CellFxResources.MiningCrackSequence, Is.Not.Null);
            Assert.That(artSet.CellFxResources.WallBreakSequence, Is.Not.Null);
            Assert.That(artSet.CellFxResources.ExplosionSequence, Is.Not.Null);
            Assert.That(artSet.HudResources.HudPrefab, Is.Not.Null);
            Assert.That(artSet.HudResources.PanelBackground, Is.Not.Null);
            Assert.That(artSet.HudResources.StatusIcon, Is.Not.Null);
            Assert.That(artSet.HudResources.BuildingInteractionIcon, Is.Not.Null);
            Assert.That(artSet.DangerOutlineTiles.Length, Is.EqualTo(3));
            Assert.That(artSet.FloorDualGridTiles[5], Is.Not.Null);
            Assert.That(artSet.BoundaryDualGridTiles[15], Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Minebot/Tiles/DualGridTerrain/Tile_DG_Floor_15.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Minebot/Tiles/DualGridTerrain/Tile_DG_HardRock_15.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Minebot/Tiles/DualGridTerrain/Tile_DG_Boundary_15.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Minebot/Tiles/DualGridFogNear/Tile_DG_FogNear_15.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Minebot/Tiles/DualGridFogDeep/Tile_DG_FogDeep_15.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Minebot/Tiles/Tile_BuildPreviewValid.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Minebot/Tiles/Tile_DetailUltraHard.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<BitmapGlyphFontDefinition>("Assets/Resources/Minebot/MinebotBitmapGlyphFont_Default.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Art/Minebot/Sprites/UI/Hologram/hologram_bmfont_digits.fnt"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Minebot/Sprites/UI/Hologram/hologram_overlay_atlas.png"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Minebot/Sprites/Actors/States/player_mining_0.png"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Minebot/Sprites/Pickups/pickup_metal.png"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Minebot/Sprites/Effects/wall_break_2.png"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Minebot/Sprites/UI/HUD/hud_icon_warning.png"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Minebot/Presentation/Actors/PlayerActor.prefab"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Minebot/Presentation/Pickups/PickupMetal.prefab"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Minebot/Presentation/CellFx/ExplosionFx.prefab"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<SpriteSequenceAsset>("Assets/Resources/Minebot/Presentation/Sequences/Player_Mining.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<SpriteSequenceAsset>("Assets/Resources/Minebot/Presentation/Sequences/Fx_Explosion.asset"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Art/Minebot/Docs/prefab-gameplay-art-record-template.md"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Art/Minebot/Generated/Prompts/minebot-prefab-gameplay-art-batch-001.md"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Art/Minebot/Generated/Selected/minebot-prefab-gameplay-art-manifest-001.md"), Is.Not.Null);

            foreach (DualGridTerrainFamilyProfile family in dualGridProfile.Families)
            {
                Assert.That(family, Is.Not.Null);
                Assert.That(family.ResolveTiles(Array.Empty<Tile>()).Length, Is.EqualTo(DualGridTerrain.TileCount));
            }
        }

        [Test]
        public void DefaultPresentationAssetsProvideNearAndDeepFogTileSets()
        {
            MinebotPresentationAssets assets = LoadDefaultPresentationAssets();

            Assert.That(MinebotPresentationAssets.LoadDefaultArtSet(), Is.Not.Null);
            Assert.That(assets.IsUsingConfiguredArtSet, Is.True);
            Assert.That(assets.FogNearDualGridTiles.Length, Is.EqualTo(DualGridFog.TileCount));
            Assert.That(assets.FogDeepDualGridTiles.Length, Is.EqualTo(DualGridFog.TileCount));
            Assert.That(assets.FogNearDualGridTileForIndex(15), Is.Not.Null);
            Assert.That(assets.FogDeepDualGridTileForIndex(15), Is.Not.Null);
        }

        [Test]
        public void DualGridFogEditorBakePreservesQuarterCellGeometry()
        {
            Texture2D texture = DualGridFogFallbackTiles.CreateTexture(DualGridFogBandKind.Near, 8, "QuarterFogTest");
            try
            {
                Assert.That(texture.GetPixel(4, 12).a, Is.GreaterThan(0.1f));
                Assert.That(texture.GetPixel(12, 12).a, Is.EqualTo(0f).Within(0.001f));
                Assert.That(texture.GetPixel(4, 4).a, Is.EqualTo(0f).Within(0.001f));
                Assert.That(texture.GetPixel(12, 4).a, Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void DualGridPreviewHostRefreshBuildsPreviewWithoutInitializingRuntimeServices()
        {
            MinebotServices.ResetForTests();

            var root = new GameObject("DualGridPreviewHostEditModeTest", typeof(Grid));
            TilemapBakeProfile bakeProfile = ScriptableObject.CreateInstance<TilemapBakeProfile>();
            DualGridTerrainProfile profile = ScriptableObject.CreateInstance<DualGridTerrainProfile>();
            Tile sourceTile = CreateRuntimeTile("Preview Soil Source");
            try
            {
                Tilemap sourceTerrainTilemap = CreateTilemap(root.transform, "Source Terrain Tilemap", Vector3.zero);
                sourceTerrainTilemap.SetTile(Vector3Int.zero, sourceTile);
                ConfigureBakeProfileForSingleTile(bakeProfile, sourceTile, TerrainKind.MineableWall, HardnessTier.Soil);
                ConfigureProfileWithDefaultDualGridTiles(profile);

                DualGridPreviewHost host = root.AddComponent<DualGridPreviewHost>();
                host.Configure(sourceTerrainTilemap, bakeProfile, configuredProfileOverride: profile);

                bool rebuilt = host.RebuildPreview();

                Assert.That(rebuilt, Is.True);
                Assert.That(MinebotServices.IsInitialized, Is.False);
                Assert.That(sourceTerrainTilemap.GetTile(Vector3Int.zero), Is.EqualTo(sourceTile));
                Assert.That(host.PreviewRoot, Is.Not.Null);

                Tilemap[] previewTilemaps = CreateTerrainTilemaps(host.PreviewRoot);
                Assert.That(previewTilemaps.Length, Is.EqualTo(DualGridTerrain.RenderLayerCount));
                Assert.That(HasAnyTile(TerrainLayerTilemap(previewTilemaps, TerrainRenderLayerId.Soil)), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(bakeProfile);
                Object.DestroyImmediate(profile);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GeneratedActorStateFramesUseTransparentBackground()
        {
            MinebotPixelArtAssetPipeline.EnsureDefaultAssets();

            Texture2D playerTexture = LoadTextureFromPngFile("Assets/Art/Minebot/Sprites/Actors/States/player_idle_0.png");
            Texture2D robotTexture = LoadTextureFromPngFile("Assets/Art/Minebot/Sprites/Actors/States/robot_idle_0.png");

            try
            {
                AssertTransparentCorners(playerTexture);
                AssertTransparentCorners(robotTexture);
                Assert.That(playerTexture.GetPixel(16, 16).a, Is.GreaterThan(0.9f));
                Assert.That(robotTexture.GetPixel(16, 16).a, Is.GreaterThan(0.9f));
            }
            finally
            {
                Object.DestroyImmediate(playerTexture);
                Object.DestroyImmediate(robotTexture);
            }
        }

        [Test]
        public void HologramAssetsExposeBitmapGlyphsAndDangerGeometryCompatibility()
        {
            MinebotPixelArtAssetPipeline.EnsureDefaultAssets();
            MinebotPresentationArtSet artSet = AssetDatabase.LoadAssetAtPath<MinebotPresentationArtSet>(
                "Assets/Resources/Minebot/MinebotPresentationArtSet_Default.asset");
            MinebotPresentationAssets assets = MinebotPresentationAssets.Create(artSet);

            Assert.That(artSet, Is.Not.Null);
            Assert.That(assets.BitmapGlyphFont, Is.Not.Null);
            Assert.That(assets.BitmapGlyphFont.TryGetGlyph('3', out BitmapGlyphFontDefinition.GlyphDefinition glyph), Is.True);
            Assert.That(glyph, Is.Not.Null);
            Assert.That(glyph.Sprite, Is.Not.Null);
            Assert.That(glyph.Advance, Is.GreaterThan(0f));
            Assert.That(assets.ResolveDangerOverlayTile(DangerOverlayGeometryKind.Base, 0), Is.Not.Null);
            Assert.That(assets.ResolveDangerOverlayTile(DangerOverlayGeometryKind.Outline, 1), Is.Not.Null);
            Assert.That(assets.ResolveDangerOverlayTile(DangerOverlayGeometryKind.Contour, 7), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Art/Minebot/Docs/holographic-feedback-record-template.md"), Is.Not.Null);
        }

        [Test]
        public void HudMockupSourceSlicesProduceExpectedSpriteSizesAndBorders()
        {
            MinebotPixelArtAssetPipeline.EnsureDefaultAssets();

            Assert.That(
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Minebot/Generated/Selected/minebot-hud-uiux-mockup-source.png"),
                Is.Not.Null);

            Sprite panelBackground = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Minebot/Sprites/UI/HUD/hud_panel_background.png");
            Sprite statusIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Minebot/Sprites/UI/HUD/hud_icon_status.png");
            Sprite interactionIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Minebot/Sprites/UI/HUD/hud_icon_interaction.png");
            Sprite feedbackIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Minebot/Sprites/UI/HUD/hud_icon_feedback.png");
            Sprite buildIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Minebot/Sprites/UI/HUD/hud_icon_build.png");

            Assert.That(panelBackground, Is.Not.Null);
            Assert.That(panelBackground.rect.width, Is.EqualTo(48).Within(0.5f));
            Assert.That(panelBackground.rect.height, Is.EqualTo(48).Within(0.5f));
            Assert.That(panelBackground.border.x, Is.EqualTo(12f).Within(0.01f));
            Assert.That(panelBackground.border.y, Is.EqualTo(12f).Within(0.01f));
            Assert.That(statusIcon.rect.width, Is.EqualTo(54).Within(0.5f));
            Assert.That(statusIcon.rect.height, Is.EqualTo(53).Within(0.5f));
            Assert.That(interactionIcon.rect.width, Is.EqualTo(60).Within(0.5f));
            Assert.That(interactionIcon.rect.height, Is.EqualTo(55).Within(0.5f));
            Assert.That(feedbackIcon.rect.width, Is.EqualTo(42).Within(0.5f));
            Assert.That(feedbackIcon.rect.height, Is.EqualTo(42).Within(0.5f));
            Assert.That(buildIcon.rect.width, Is.EqualTo(58).Within(0.5f));
            Assert.That(buildIcon.rect.height, Is.EqualTo(58).Within(0.5f));
        }

        [Test]
        public void TerrainArtRefreshDoesNotChangeCollisionMiningDangerOrBuildingRules()
        {
            MinebotPixelArtAssetPipeline.EnsureDefaultAssets();
            MinebotPresentationArtSet artSet = AssetDatabase.LoadAssetAtPath<MinebotPresentationArtSet>(
                "Assets/Resources/Minebot/MinebotPresentationArtSet_Default.asset");
            Assert.That(artSet, Is.Not.Null);

            RuleProbe baseline = CreateRuleProbe(useConfiguredArt: false, artSet);
            RuleProbe configured = CreateRuleProbe(useConfiguredArt: true, artSet);

            Assert.That(configured.MoveResult, Is.EqualTo(baseline.MoveResult));
            Assert.That(configured.MineResult, Is.EqualTo(baseline.MineResult));
            Assert.That(configured.CanBuild, Is.EqualTo(baseline.CanBuild));
            Assert.That(configured.BuildFailure, Is.EqualTo(baseline.BuildFailure));
            Assert.That(configured.DangerZoneAtProbe, Is.EqualTo(baseline.DangerZoneAtProbe));
            Assert.That(configured.WallPassable, Is.EqualTo(baseline.WallPassable));
        }

        private static void AssertResolverScenario(
            LayeredBinaryTerrainResolver resolver,
            CornerMaterialSample sample,
            params RenderLayerCommand[] expectedCommands)
        {
            var commands = new RenderLayerCommand[DualGridTerrain.RenderLayerCount];
            resolver.Resolve(sample, commands);

            for (int i = 0; i < commands.Length; i++)
            {
                Assert.That(DualGridTerrain.GetOrderedLayerIndex(commands[i].LayerId), Is.EqualTo(i));
            }

            foreach (TerrainRenderLayerId layerId in DualGridTerrain.OrderedLayers)
            {
                int orderedIndex = DualGridTerrain.GetOrderedLayerIndex(layerId);
                RenderLayerCommand? expected = null;
                for (int i = 0; i < expectedCommands.Length; i++)
                {
                    if (DualGridTerrain.GetOrderedLayerIndex(expectedCommands[i].LayerId) != orderedIndex)
                    {
                        continue;
                    }

                    expected = expectedCommands[i];
                    break;
                }

                if (expected.HasValue)
                {
                    Assert.That(commands[orderedIndex], Is.EqualTo(expected.Value));
                }
                else
                {
                    Assert.That(commands[orderedIndex].HasContent, Is.False);
                }
            }
        }

        private static RenderLayerCommand CommandAtLayer(RenderLayerCommand[] commands, TerrainRenderLayerId layerId)
        {
            return commands[DualGridTerrain.GetOrderedLayerIndex(layerId)];
        }

        private static RuleProbe CreateRuleProbe(bool useConfiguredArt, MinebotPresentationArtSet artSet)
        {
            LogicalGridState grid = CreateOpenGrid(new Vector2Int(9, 9), new GridPosition(4, 4));
            GridPosition wallTarget = new GridPosition(6, 6);
            GridPosition actorStart = new GridPosition(6, 5);
            GridPosition buildOrigin = new GridPosition(2, 2);
            SetMineableWall(grid, wallTarget, false);
            var economy = new PlayerEconomy(new ResourceAmount(10, 0, 0));
            var buildings = new BuildingPlacementService(grid, economy);
            var waves = new WaveSurvivalService(grid, null);
            waves.EvaluateDangerZones();

            var root = new GameObject(useConfiguredArt ? "ConfiguredArtRuleProbe" : "FallbackArtRuleProbe");
            try
            {
                Tilemap[] terrainTilemaps = CreateTerrainTilemaps(root.transform);
                TilemapGridPresentation presentation = root.AddComponent<TilemapGridPresentation>();
                presentation.Configure(
                    terrainTilemaps,
                    CreateTilemap(root.transform, "Fog Near Test Tilemap", DualGridFog.DisplayOffset),
                    CreateTilemap(root.transform, "Fog Deep Test Tilemap", DualGridFog.DisplayOffset),
                    CreateTilemap(root.transform, "Facility Test Tilemap", Vector3.zero),
                    CreateTilemap(root.transform, "Marker Test Tilemap", Vector3.zero),
                    CreateTilemap(root.transform, "Danger Test Tilemap", Vector3.zero),
                    CreateTilemap(root.transform, "Build Preview Test Tilemap", Vector3.zero),
                    useConfiguredArt ? MinebotPresentationAssets.Create(artSet) : LoadDefaultPresentationAssets());
                presentation.Refresh(CreatePresentationRegistry(grid), new GridPosition(-1, -1), new GridPosition(-2, -2));

                var moveMining = new MiningService(grid);
                var movePlayer = new PlayerMiningState(actorStart, HardnessTier.Soil);
                MineInteractionResult moveResult = moveMining.Move(movePlayer, GridPosition.Up);

                var digMining = new MiningService(grid);
                var digPlayer = new PlayerMiningState(actorStart, HardnessTier.Soil);
                MineInteractionResult mineResult = digMining.TryMine(digPlayer, wallTarget, out _);

                BuildingDefinition drill = BuildingDefinition.CreateRuntime(
                    "probe-drill",
                    "探针钻机",
                    new ResourceAmount(2, 0, 0),
                    new Vector2Int(2, 1));
                bool canBuild = buildings.CanPlace(drill, buildOrigin, out BuildingPlacementFailure buildFailure);

                return new RuleProbe(
                    moveResult,
                    mineResult,
                    canBuild,
                    buildFailure,
                    grid.GetCell(actorStart).IsDangerZone,
                    grid.GetCell(wallTarget).IsPassable);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static RuntimeServiceRegistry CreatePresentationRegistry(LogicalGridState grid)
        {
            return new RuntimeServiceRegistry(
                grid,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                new List<RobotState>(),
                null);
        }

        private static Tilemap[] CreateTerrainTilemaps(Transform parent)
        {
            var tilemaps = new Tilemap[DualGridTerrain.OrderedLayers.Length];
            for (int i = 0; i < tilemaps.Length; i++)
            {
                TerrainRenderLayerId layerId = DualGridTerrain.OrderedLayers[i];
                tilemaps[i] = FindOrCreateTilemap(parent, DualGridTerrain.GetTilemapName(layerId), DualGridTerrain.DisplayOffset);
            }

            return tilemaps;
        }

        private static Tilemap CreateTilemap(Transform parent, string name, Vector3 localPosition)
        {
            var child = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            return child.GetComponent<Tilemap>();
        }

        private static Tilemap FindOrCreateTilemap(Transform parent, string name, Vector3 localPosition)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                child.localPosition = localPosition;
                Tilemap existingTilemap = child.GetComponent<Tilemap>();
                if (existingTilemap != null)
                {
                    return existingTilemap;
                }
            }

            return CreateTilemap(parent, name, localPosition);
        }

        private static bool HasAnyTile(Tilemap tilemap)
        {
            foreach (Vector3Int position in tilemap.cellBounds.allPositionsWithin)
            {
                if (tilemap.GetTile(position) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasAnyDisplayTileAroundCell(Tilemap tilemap, GridPosition worldCell)
        {
            Vector3Int[] positions = DualGridFog.GetAffectedDisplayCells(worldCell);
            for (int i = 0; i < positions.Length; i++)
            {
                if (tilemap.GetTile(positions[i]) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static Tilemap TerrainLayerTilemap(IReadOnlyList<Tilemap> terrainTilemaps, TerrainRenderLayerId layerId)
        {
            return terrainTilemaps[DualGridTerrain.GetOrderedLayerIndex(layerId)];
        }

        private static void ConfigureProfileWithDefaultDualGridTiles(DualGridTerrainProfile profile)
        {
            profile.ConfigureLayout(DualGridTerrainLayoutSettings.CreateDefault());
            MinebotPresentationAssets defaultAssets = LoadDefaultPresentationAssets();
            foreach (TerrainRenderLayerId layerId in DualGridTerrain.MaterialFamilies)
            {
                profile.ConfigureFamilyTiles(layerId, ResolveFallbackTiles(defaultAssets, layerId));
            }
        }

        private static MinebotPresentationAssets LoadDefaultPresentationAssets()
        {
            MinebotPixelArtAssetPipeline.EnsureDefaultAssets();
            return MinebotPresentationAssets.Create(null);
        }

        private static void ConfigureBakeProfileForSingleTile(
            TilemapBakeProfile profile,
            TileBase tile,
            TerrainKind terrainKind,
            HardnessTier hardnessTier)
        {
            var terrainRules = new[]
            {
                new TerrainTileRule
                {
                    tile = tile,
                    terrainKind = terrainKind,
                    hardnessTier = hardnessTier,
                    staticFlags = CellStaticFlags.None,
                    reward = ResourceAmount.Zero
                }
            };

            typeof(TilemapBakeProfile)
                .GetField("terrainRules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(profile, terrainRules);
        }

        private static Tile CreateRuntimeTile(string name)
        {
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            tile.colliderType = Tile.ColliderType.None;
            return tile;
        }

        private static TileBase[] ResolveFallbackTiles(MinebotPresentationAssets assets, TerrainRenderLayerId layerId)
        {
            switch (layerId)
            {
                case TerrainRenderLayerId.Soil:
                    return assets.SoilDualGridTiles;
                case TerrainRenderLayerId.Stone:
                    return assets.StoneDualGridTiles;
                case TerrainRenderLayerId.HardRock:
                    return assets.HardRockDualGridTiles;
                case TerrainRenderLayerId.UltraHard:
                    return assets.UltraHardDualGridTiles;
                case TerrainRenderLayerId.Boundary:
                    return assets.BoundaryDualGridTiles;
                default:
                    return assets.FloorDualGridTiles;
            }
        }

        private static Dictionary<Vector3Int, string> SnapshotTerrainDisplay(IReadOnlyList<Tilemap> terrainTilemaps)
        {
            var positions = new HashSet<Vector3Int>();
            for (int i = 0; i < terrainTilemaps.Count; i++)
            {
                foreach (Vector3Int position in terrainTilemaps[i].cellBounds.allPositionsWithin)
                {
                    positions.Add(position);
                }
            }

            var snapshot = new Dictionary<Vector3Int, string>();
            foreach (Vector3Int position in positions)
            {
                snapshot[position] = TerrainDisplaySignatureAt(terrainTilemaps, position);
            }

            return snapshot;
        }

        private static Dictionary<Vector3Int, string> SnapshotTilemapDisplay(Tilemap tilemap)
        {
            var snapshot = new Dictionary<Vector3Int, string>();
            foreach (Vector3Int position in tilemap.cellBounds.allPositionsWithin)
            {
                snapshot[position] = TilemapDisplaySignatureAt(tilemap, position);
            }

            return snapshot;
        }

        private static IReadOnlyCollection<Vector3Int> CollectChangedDisplayCells(
            IReadOnlyDictionary<Vector3Int, string> before,
            IReadOnlyDictionary<Vector3Int, string> after)
        {
            var positions = new HashSet<Vector3Int>(before.Keys);
            positions.UnionWith(after.Keys);

            var changed = new List<Vector3Int>();
            foreach (Vector3Int position in positions)
            {
                before.TryGetValue(position, out string beforeSignature);
                after.TryGetValue(position, out string afterSignature);
                if (!string.Equals(beforeSignature, afterSignature))
                {
                    changed.Add(position);
                }
            }

            return changed;
        }

        private static string TerrainDisplaySignatureAt(IReadOnlyList<Tilemap> terrainTilemaps, Vector3Int position)
        {
            var names = new List<string>(terrainTilemaps.Count);
            for (int i = 0; i < terrainTilemaps.Count; i++)
            {
                TileBase tile = terrainTilemaps[i].GetTile(position);
                names.Add(tile != null ? tile.name : "<null>");
            }

            return string.Join("|", names);
        }

        private static string TilemapDisplaySignatureAt(Tilemap tilemap, Vector3Int position)
        {
            TileBase tile = tilemap != null ? tilemap.GetTile(position) : null;
            return tile != null ? tile.name : "<null>";
        }

        private readonly struct RuleProbe
        {
            public RuleProbe(
                MineInteractionResult moveResult,
                MineInteractionResult mineResult,
                bool canBuild,
                BuildingPlacementFailure buildFailure,
                bool dangerZoneAtProbe,
                bool wallPassable)
            {
                MoveResult = moveResult;
                MineResult = mineResult;
                CanBuild = canBuild;
                BuildFailure = buildFailure;
                DangerZoneAtProbe = dangerZoneAtProbe;
                WallPassable = wallPassable;
            }

            public MineInteractionResult MoveResult { get; }
            public MineInteractionResult MineResult { get; }
            public bool CanBuild { get; }
            public BuildingPlacementFailure BuildFailure { get; }
            public bool DangerZoneAtProbe { get; }
            public bool WallPassable { get; }
        }

        private static float ComputeRadialDistance01(MapGenerationSettings settings, GridPosition position)
        {
            float maxDistance = Mathf.Max(
                DistanceTo(settings.Spawn, 1, 1),
                DistanceTo(settings.Spawn, settings.Size.x - 2, 1),
                DistanceTo(settings.Spawn, 1, settings.Size.y - 2),
                DistanceTo(settings.Spawn, settings.Size.x - 2, settings.Size.y - 2));
            if (maxDistance <= 0.001f)
            {
                return 1f;
            }

            float distance = DistanceTo(settings.Spawn, position.X, position.Y);
            return Mathf.Pow(Mathf.Clamp01(distance / maxDistance), settings.RadialExponent);
        }

        private static float DistanceTo(GridPosition origin, int x, int y)
        {
            float dx = origin.X - x;
            float dy = origin.Y - y;
            return Mathf.Sqrt(dx * dx + dy * dy);
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

        private static Texture2D LoadTextureFromPngFile(string assetPath)
        {
            byte[] bytes = File.ReadAllBytes(assetPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(ImageConversion.LoadImage(texture, bytes, false), Is.True, $"Failed to load PNG bytes from {assetPath}.");
            return texture;
        }

        private static void AssertTransparentCorners(Texture2D texture)
        {
            Assert.That(texture.GetPixel(0, 0).a, Is.EqualTo(0f).Within(0.001f));
            Assert.That(texture.GetPixel(texture.width - 1, 0).a, Is.EqualTo(0f).Within(0.001f));
            Assert.That(texture.GetPixel(0, texture.height - 1).a, Is.EqualTo(0f).Within(0.001f));
            Assert.That(texture.GetPixel(texture.width - 1, texture.height - 1).a, Is.EqualTo(0f).Within(0.001f));
        }

        private static GameSessionService CreateSession(LogicalGridState grid, ResourceAmount startingResources, out PlayerEconomy economy)
        {
            return CreateSession(grid, startingResources, out economy, out _);
        }

        private static GameSessionService CreateSession(
            LogicalGridState grid,
            ResourceAmount startingResources,
            out PlayerEconomy economy,
            out ExperienceService experience)
        {
            var player = new PlayerMiningState(grid.PlayerSpawn, HardnessTier.Soil);
            var mining = new MiningService(grid);
            var hazards = new HazardService(grid);
            economy = new PlayerEconomy(startingResources);
            experience = new ExperienceService(4);
            var worldPickups = new WorldPickupService();
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
                worldPickups,
                vitals,
                robotAutomation,
                robots,
                null,
                ResourceAmount.Zero,
                true,
                HardnessTier.Soil);
        }

        private static Vector2 ToWorldCenter(GridPosition position)
        {
            return new Vector2(position.X + 0.5f, position.Y + 0.5f);
        }
    }
}
