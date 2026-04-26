using System.Collections;
using System.Collections.Generic;
using Minebot.Automation;
using Minebot.Bootstrap;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.Presentation;
using Minebot.Progression;
using Minebot.UI;
using TMPro;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Minebot.Tests.PlayMode
{
    public sealed class RenderedGameplayPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            MinebotServices.ResetForTests();
        }

        [UnityTest]
        public IEnumerator BootstrapToGameplayCreatesRenderedMvp()
        {
            yield return LoadBootstrapAndWaitForGameplay();
            yield return null;

            MinebotGameplayPresentation presentation = Object.FindAnyObjectByType<MinebotGameplayPresentation>();
            RuntimeServiceRegistry services = MinebotServices.Current;
            Assert.That(presentation, Is.Not.Null);
            Assert.That(presentation.IsUsingConfiguredArtSet, Is.True);
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<GameplayInputController>(), Is.Not.Null);
            Assert.That(GameObject.Find(MinebotGameplayPresentation.PlayerViewName), Is.Not.Null);
            Assert.That(GameObject.Find(MinebotGameplayPresentation.PlayerViewName).GetComponent<FreeformActorController>(), Is.Not.Null);
            Assert.That(GameObject.Find(MinebotGameplayPresentation.PlayerViewName).GetComponent<CircleCollider2D>(), Is.Not.Null);
            GameObject hud = GameObject.Find(MinebotGameplayPresentation.HudRootName);
            Assert.That(hud, Is.Not.Null);
            Assert.That(Resources.Load<MinebotHudView>(MinebotHudView.ResourcePath), Is.Not.Null);
            Assert.That(Resources.Load<GameObject>(MinebotHudDefaults.StatusPanelResourcePath), Is.Not.Null);
            Assert.That(Resources.Load<GameObject>(MinebotHudDefaults.BuildPanelResourcePath), Is.Not.Null);
            Assert.That(Resources.Load<GameObject>(MinebotHudDefaults.BuildingInteractionPanelResourcePath), Is.Not.Null);
            MinebotHudView hudView = hud.GetComponent<MinebotHudView>();
            Assert.That(hudView, Is.Not.Null);
            Assert.That(hudView.StatusPanel, Is.Not.Null);
            Assert.That(hudView.WarningPanel, Is.Not.Null);
            Assert.That(hudView.UpgradePanel, Is.Not.Null);
            Assert.That(hudView.BuildPanel, Is.Not.Null);
            Assert.That(hudView.BuildingInteractionPanel, Is.Not.Null);
            TMP_Text hudText = hud.GetComponentInChildren<TMP_Text>();
            Assert.That(hudText, Is.Not.Null);
            Assert.That(hudText.font, Is.Not.Null);
            Assert.That(hudText.font.name, Does.Contain("NotoSansSC"));
            Assert.That(hudText.font.HasCharacter('生'), Is.True);
            Assert.That(hudText.font.HasCharacter('探'), Is.True);
            Assert.That(hudText.font.HasCharacter('震'), Is.True);
            Assert.That(hudText.font.HasCharacter('机'), Is.True);
            Assert.That(hud.GetComponentsInChildren<Text>().Length, Is.EqualTo(0));

            IReadOnlyList<Tilemap> terrainFamilies = GetTerrainFamilyTilemaps();
            Assert.That(terrainFamilies.Count, Is.EqualTo(DualGridTerrain.RenderLayerCount));
            for (int i = 0; i < terrainFamilies.Count; i++)
            {
                Assert.That(terrainFamilies[i], Is.Not.Null);
                Assert.That(terrainFamilies[i].name, Is.EqualTo(DualGridTerrain.GetTilemapName((TerrainRenderLayerId)i)));
                Assert.That(terrainFamilies[i].transform.localPosition, Is.EqualTo(DualGridTerrain.DisplayOffset));
                Assert.That(terrainFamilies[i].GetComponent<TilemapRenderer>().sortingOrder, Is.EqualTo(DualGridTerrain.GetSortingOrder((TerrainRenderLayerId)i)));
            }

            Assert.That(HasAnyDisplayTileAroundCell(terrainFamilies[(int)TerrainRenderLayerId.Floor], services.Grid.PlayerSpawn), Is.True);
            GridPosition hardRockPosition = new GridPosition(services.Grid.PlayerSpawn.X, services.Grid.PlayerSpawn.Y + 2);
            SetMineableHardness(services, hardRockPosition, HardnessTier.HardRock);
            SetEmpty(services, new GridPosition(1, 1));
            presentation.RefreshAll();
            Assert.That(HasAnyDisplayTileAroundCell(terrainFamilies[(int)TerrainRenderLayerId.HardRock], hardRockPosition), Is.True);

            Tilemap marker = GameObject.Find(MinebotGameplayPresentation.MarkerTilemapName).GetComponent<Tilemap>();
            Tilemap danger = GameObject.Find(MinebotGameplayPresentation.DangerTilemapName).GetComponent<Tilemap>();
            Tilemap buildPreview = GameObject.Find(MinebotGameplayPresentation.BuildPreviewTilemapName).GetComponent<Tilemap>();
            Assert.That(marker, Is.Not.Null);
            Assert.That(danger, Is.Not.Null);
            Assert.That(buildPreview, Is.Not.Null);
            Assert.That(GameObject.Find(MinebotGameplayPresentation.ScanIndicatorRootName), Is.Not.Null);

            Assert.That(danger.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(HasAnyTile(danger), Is.True);
            CircleCollider2D playerCollider = GameObject.Find(MinebotGameplayPresentation.PlayerViewName).GetComponent<CircleCollider2D>();
            FreeformActorController freeform = GameObject.Find(MinebotGameplayPresentation.PlayerViewName).GetComponent<FreeformActorController>();
            Assert.That(playerCollider.radius, Is.EqualTo(freeform.CollisionRadius).Within(0.001f));
            Assert.That(presentation.HudSummary, Does.Contain("生命"));
            Assert.That(presentation.HudSummary, Does.Contain("波次"));
            Assert.That(hud.transform.Find(MinebotHudView.BuildSlotName), Is.Not.Null);
            Assert.That(hud.transform.Find(MinebotHudView.BuildingInteractionSlotName), Is.Not.Null);
            Assert.That(services.Buildings.Buildings.Count, Is.GreaterThanOrEqualTo(2));
        }

        [UnityTest]
        public IEnumerator MovementAndMiningRefreshRulesAndPresentation()
        {
            yield return LoadBootstrapAndWaitForGameplay();
            yield return null;

            RuntimeServiceRegistry services = MinebotServices.Current;
            MinebotGameplayPresentation presentation = Object.FindAnyObjectByType<MinebotGameplayPresentation>();
            GameplayInputController input = Object.FindAnyObjectByType<GameplayInputController>();
            GridPosition start = services.PlayerMiningState.Position;
            GridPosition minedPosition = new GridPosition(start.X, start.Y + 2);

            GridPosition movedPosition = start + GridPosition.Up;
            SetEmpty(services, movedPosition);
            services.PlayerMiningState.Teleport(movedPosition);
            presentation.SnapPlayerToLogicalPosition();
            presentation.RefreshAll();
            yield return null;

            Assert.That(services.PlayerMiningState.Position, Is.EqualTo(movedPosition));
            Assert.That(GameObject.Find(MinebotGameplayPresentation.PlayerViewName).transform.position, Is.EqualTo(presentation.GridToWorld(services.PlayerMiningState.Position)));

            SetBombWall(services, minedPosition);
            SetBombWall(services, minedPosition + GridPosition.Right);
            presentation.RefreshAll();
            yield return null;

            IReadOnlyList<Tilemap> terrainFamilies = presentation.GridPresentation.TerrainTilemaps;
            string beforeTerrainSignature = GetTerrainSignature(terrainFamilies, minedPosition);
            Assert.That(HasAnyDisplayTileAroundCell(terrainFamilies[(int)TerrainRenderLayerId.Soil], minedPosition), Is.True);

            Assert.That(input.ScanCurrentCell(), Is.True);
            yield return null;
            Assert.That(presentation.FeedbackMessage, Does.Contain("探测完成"));
            Assert.That(presentation.WarningSummary, Does.Contain("最近探测"));
            Assert.That(ActiveScanLabelCount(), Is.GreaterThan(0));
            Assert.That(input.ToggleMarkerMode(), Is.True);
            Assert.That(presentation.InteractionMode, Is.EqualTo(GameplayInteractionMode.Marker));
            Assert.That(input.ClickGridCell(minedPosition), Is.True);
            Assert.That(services.Grid.GetCell(minedPosition).IsMarked, Is.True);
            Assert.That(presentation.FeedbackMessage, Does.Contain("机器人会避开"));
            Assert.That(services.Grid.GetCell(movedPosition).IsDangerZone, Is.True);
            Assert.That(presentation.WarningSummary, Does.Contain("你位于危险区"));
            Assert.That(presentation.GridPresentation.MarkerTilemap.GetTile(TilemapGridPresentation.ToTilePosition(minedPosition)), Is.Not.Null);
            Assert.That(presentation.GridPresentation.DangerTilemap.GetTile(TilemapGridPresentation.ToTilePosition(movedPosition)), Is.Not.Null);
            Assert.That(GetTerrainSignature(terrainFamilies, minedPosition), Is.EqualTo(beforeTerrainSignature));
            Assert.That(input.ToggleMarkerMode(), Is.True);
            Assert.That(presentation.InteractionMode, Is.EqualTo(GameplayInteractionMode.Normal));

            for (int i = 0; i < 4 && services.Grid.GetCell(minedPosition).TerrainKind != TerrainKind.Empty; i++)
            {
                Assert.That(input.MineFacingCell(), Is.True);
                yield return null;
            }

            Assert.That(services.Grid.GetCell(minedPosition).TerrainKind, Is.EqualTo(TerrainKind.Empty));
            Assert.That(services.Grid.GetCell(minedPosition).IsMarked, Is.False);
            Assert.That(GetTerrainSignature(terrainFamilies, minedPosition), Is.Not.EqualTo(beforeTerrainSignature));
            Assert.That(presentation.HudSummary, Does.Contain("经验"));
        }

        [UnityTest]
        public IEnumerator DualGridTerrainDrawsFilledInteriorForContinuousHardRockBlock()
        {
            yield return LoadBootstrapAndWaitForGameplay();
            yield return null;

            RuntimeServiceRegistry services = MinebotServices.Current;
            MinebotGameplayPresentation presentation = Object.FindAnyObjectByType<MinebotGameplayPresentation>();
            GridPosition blockOrigin = services.Grid.PlayerSpawn + GridPosition.Up + GridPosition.Right;
            GridPosition[] blockCells =
            {
                blockOrigin,
                blockOrigin + GridPosition.Right,
                blockOrigin + GridPosition.Up,
                blockOrigin + GridPosition.Up + GridPosition.Right
            };

            for (int y = blockOrigin.Y - 1; y <= blockOrigin.Y + 2; y++)
            {
                for (int x = blockOrigin.X - 1; x <= blockOrigin.X + 2; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    if (services.Grid.IsInside(position))
                    {
                        SetEmpty(services, position);
                    }
                }
            }

            for (int i = 0; i < blockCells.Length; i++)
            {
                SetMineableHardness(services, blockCells[i], HardnessTier.HardRock);
            }

            presentation.RefreshAll();
            yield return null;

            Tilemap hardRockLayer = presentation.GridPresentation.GetTerrainTilemap(TerrainRenderLayerId.HardRock);
            Tilemap floorLayer = presentation.GridPresentation.GetTerrainTilemap(TerrainRenderLayerId.Floor);
            Vector3Int innerContourPosition = new Vector3Int(blockOrigin.X + 1, blockOrigin.Y + 1, 0);
            Vector3Int outerContourPosition = new Vector3Int(blockOrigin.X, blockOrigin.Y + 1, 0);
            Assert.That(hardRockLayer.GetTile(innerContourPosition).name, Does.Contain("Tile_DG_HardRock_15"));
            Assert.That(hardRockLayer.GetTile(outerContourPosition).name, Does.Not.Contain("Tile_DG_HardRock_15"));
            Assert.That(floorLayer.GetTile(innerContourPosition), Is.Null);
        }

        [UnityTest]
        public IEnumerator DifferentHardnessWallsDrawDualGridLayersAtSharedBoundary()
        {
            yield return LoadBootstrapAndWaitForGameplay();
            yield return null;

            RuntimeServiceRegistry services = MinebotServices.Current;
            MinebotGameplayPresentation presentation = Object.FindAnyObjectByType<MinebotGameplayPresentation>();
            GridPosition origin = services.Grid.PlayerSpawn + GridPosition.Up + GridPosition.Right;
            GridPosition[] blockCells =
            {
                origin,
                origin + GridPosition.Right,
                origin + GridPosition.Up,
                origin + GridPosition.Up + GridPosition.Right
            };

            for (int y = origin.Y - 1; y <= origin.Y + 2; y++)
            {
                for (int x = origin.X - 1; x <= origin.X + 2; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    if (services.Grid.IsInside(position))
                    {
                        SetEmpty(services, position);
                    }
                }
            }

            SetMineableHardness(services, origin, HardnessTier.Soil);
            SetMineableHardness(services, origin + GridPosition.Up, HardnessTier.Soil);
            SetMineableHardness(services, origin + GridPosition.Right, HardnessTier.Stone);
            SetMineableHardness(services, origin + GridPosition.Up + GridPosition.Right, HardnessTier.Stone);
            presentation.RefreshAll();
            yield return null;

            Tilemap soilLayer = presentation.GridPresentation.GetTerrainTilemap(TerrainRenderLayerId.Soil);
            Tilemap stoneLayer = presentation.GridPresentation.GetTerrainTilemap(TerrainRenderLayerId.Stone);
            Vector3Int sharedBoundary = new Vector3Int(origin.X + 1, origin.Y + 1, 0);
            Assert.That(soilLayer.GetTile(sharedBoundary), Is.Not.Null);
            Assert.That(stoneLayer.GetTile(sharedBoundary), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator PlayerCanLeaveSpawnAtGameplayStart()
        {
            yield return LoadBootstrapAndWaitForGameplay();
            yield return null;

            RuntimeServiceRegistry services = MinebotServices.Current;
            GameplayInputController input = Object.FindAnyObjectByType<GameplayInputController>();
            GridPosition start = services.PlayerMiningState.Position;

            bool moved = false;
            for (int i = 0; i < 6; i++)
            {
                moved = input.Move(GridPosition.Up);
                yield return null;
                if (!services.PlayerMiningState.Position.Equals(start))
                {
                    break;
                }
            }

            Assert.That(moved, Is.True);
            Assert.That(services.PlayerMiningState.Position, Is.Not.EqualTo(start));
        }

        [UnityTest]
        public IEnumerator PlayerCanLeaveSpawnWithHeldFreeformInput()
        {
            yield return LoadBootstrapAndWaitForGameplay();
            yield return null;

            RuntimeServiceRegistry services = MinebotServices.Current;
            GameplayInputController input = Object.FindAnyObjectByType<GameplayInputController>();
            GridPosition start = services.PlayerMiningState.Position;

            for (int i = 0; i < 30 && services.PlayerMiningState.Position.Equals(start); i++)
            {
                input.MoveFreeform(Vector2.up, 1f / 60f);
                yield return null;
            }

            Assert.That(services.PlayerMiningState.Position, Is.Not.EqualTo(start));
        }

        [UnityTest]
        public IEnumerator FreeformMovementSlidesAlongWallWithoutImmediateAutoMine()
        {
            yield return LoadBootstrapAndWaitForGameplay();
            yield return null;

            RuntimeServiceRegistry services = MinebotServices.Current;
            MinebotGameplayPresentation presentation = Object.FindAnyObjectByType<MinebotGameplayPresentation>();
            GameplayInputController input = Object.FindAnyObjectByType<GameplayInputController>();
            FreeformActorController freeform = GameObject.Find(MinebotGameplayPresentation.PlayerViewName).GetComponent<FreeformActorController>();
            GridPosition start = services.PlayerMiningState.Position;
            GridPosition wall = start + GridPosition.Right;

            SetMineableHardness(services, wall, HardnessTier.Stone);
            SetEmpty(services, start + GridPosition.Up);
            presentation.RefreshAll();
            yield return null;

            Vector3 startWorld = freeform.transform.position;
            Assert.That(input.MoveFreeform(new Vector2(1f, 1f), 0.25f), Is.True);
            yield return null;

            Vector3 endWorld = freeform.transform.position;
            Assert.That(endWorld.y, Is.GreaterThan(startWorld.y + 0.2f));
            Assert.That(endWorld.x, Is.LessThan(wall.X - freeform.CollisionRadius + 0.05f));
            Assert.That(services.PlayerMiningState.Position, Is.EqualTo(start + GridPosition.Up));
            Assert.That(presentation.FeedbackMessage, Does.Not.Contain("正在挖掘"));
        }

        [UnityTest]
        public IEnumerator FreeformMovementSlidesPastConvexCornerWithoutSnagging()
        {
            yield return LoadBootstrapAndWaitForGameplay();
            yield return null;

            RuntimeServiceRegistry services = MinebotServices.Current;
            MinebotGameplayPresentation presentation = Object.FindAnyObjectByType<MinebotGameplayPresentation>();
            FreeformActorController freeform = GameObject.Find(MinebotGameplayPresentation.PlayerViewName).GetComponent<FreeformActorController>();
            GridPosition start = new GridPosition(1, 1);
            GridPosition cornerWall = start + GridPosition.Right + GridPosition.Up;

            services.PlayerMiningState.Teleport(start);
            presentation.SnapPlayerToLogicalPosition();
            SetEmpty(services, start + GridPosition.Right);
            SetEmpty(services, start + GridPosition.Up);
            SetEmpty(services, start);
            SetMineableHardness(services, cornerWall, HardnessTier.Stone);
            presentation.RefreshAll();
            yield return null;

            SetEmpty(services, start);
            SetEmpty(services, start + GridPosition.Right);
            SetEmpty(services, start + GridPosition.Up);
            GridCellState aboveCell = services.Grid.GetCell(start + GridPosition.Up);
            Assert.That(aboveCell.IsPassable, Is.True);

            freeform.transform.position = new Vector3(start.X + 0.5f, start.Y + 0.8f, freeform.transform.position.z);
            Vector3 startWorld = freeform.transform.position;
            CharacterMoveResult2D result = presentation.TryMovePlayerFreeform(Vector2.right, 0.25f);
            Assert.That(result.HasMoved, Is.True);
            Assert.That(result.WasSliding, Is.True);

            Vector3 endWorld = freeform.transform.position;
            Assert.That(endWorld.x, Is.GreaterThan(startWorld.x + 0.2f));
            Assert.That(endWorld.y, Is.LessThan(startWorld.y - 0.05f));
            Assert.That(presentation.FeedbackMessage, Does.Not.Contain("正在挖掘"));
        }

        [UnityTest]
        public IEnumerator BuildModePlacesConfiguredBuildingAndBlocksCell()
        {
            yield return LoadBootstrapAndWaitForGameplay();
            yield return null;

            RuntimeServiceRegistry services = MinebotServices.Current;
            MinebotGameplayPresentation presentation = Object.FindAnyObjectByType<MinebotGameplayPresentation>();
            GameplayInputController input = Object.FindAnyObjectByType<GameplayInputController>();
            services.Economy.Add(new ResourceAmount(10, 0, 0));
            BuildingDefinition drill = BuildingDefinition.CreateRuntime(
                "test-drill",
                "测试钻机",
                new ResourceAmount(2, 0, 0),
                new Vector2Int(2, 1));
            GridPosition origin = services.Grid.PlayerSpawn + GridPosition.Up + GridPosition.Up + GridPosition.Left;
            PrepareBuildableChamber(services, origin, drill.FootprintSize, 2);
            presentation.RefreshAll();
            Assert.That(presentation.CanPlaceBuildingAt(drill, origin), Is.True);

            Assert.That(input.ToggleBuildMode(), Is.True);
            presentation.SetSelectedBuilding(drill);
            presentation.SetBuildPreview(origin);
            Assert.That(presentation.GridPresentation.BuildPreviewTilemap.GetTile(TilemapGridPresentation.ToTilePosition(origin)), Is.Not.Null);
            Assert.That(presentation.TryPlaceBuildingAt(drill, origin), Is.True);
            yield return null;

            Assert.That(services.Buildings.Buildings.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(services.Grid.GetCell(origin).IsOccupiedByBuilding, Is.True);
            Assert.That(services.Grid.GetCell(origin + GridPosition.Right).IsPassable, Is.False);
            Assert.That(GameObject.Find("Building View 3 - 测试钻机"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator UpgradeRepairAndRobotHaveVisibleFeedback()
        {
            yield return LoadBootstrapAndWaitForGameplay();
            yield return null;

            RuntimeServiceRegistry services = MinebotServices.Current;
            MinebotGameplayPresentation presentation = Object.FindAnyObjectByType<MinebotGameplayPresentation>();
            GameplayInputController input = Object.FindAnyObjectByType<GameplayInputController>();

            services.Experience.AddExperience(services.Experience.NextThreshold);
            presentation.RefreshAll();
            yield return null;
            Assert.That(presentation.IsUpgradePanelShowing, Is.True);
            Assert.That(presentation.IsRepairInteractionButtonShowing, Is.False);
            Assert.That(presentation.IsRobotFactoryInteractionButtonShowing, Is.False);
            Button repairButton = FindBuildingInteractionButton(MinebotGameplayPresentation.RepairStationInteractionButtonName);
            Button factoryButton = FindBuildingInteractionButton(MinebotGameplayPresentation.RobotFactoryInteractionButtonName);
            services.Economy.Add(new ResourceAmount(12, 0, 0));
            services.Vitals.Damage(1);
            int healthBeforeLockedRepair = services.Vitals.CurrentHealth;
            int robotsBeforeLockedFactory = services.Robots.Count;
            repairButton.onClick.Invoke();
            factoryButton.onClick.Invoke();
            yield return null;
            Assert.That(services.Vitals.CurrentHealth, Is.EqualTo(healthBeforeLockedRepair));
            Assert.That(services.Robots.Count, Is.EqualTo(robotsBeforeLockedFactory));

            GridPosition positionBeforeUpgrade = services.PlayerMiningState.Position;
            Assert.That(input.Move(GridPosition.Up), Is.False);
            Assert.That(services.PlayerMiningState.Position, Is.EqualTo(positionBeforeUpgrade));
            Assert.That(presentation.FeedbackMessage, Does.Contain("升级待选择"));
            Assert.That(presentation.SelectUpgradeIndex(0), Is.True);
            yield return null;
            Assert.That(presentation.IsUpgradePanelShowing, Is.False);
            Assert.That(presentation.InteractionMode, Is.EqualTo(GameplayInteractionMode.Normal));
            Assert.That(input.Move(GridPosition.Up), Is.True);
            yield return null;
            presentation.RefreshAll();
            yield return null;
            Assert.That(presentation.IsRepairInteractionButtonShowing, Is.False);
            Assert.That(presentation.IsRobotFactoryInteractionButtonShowing, Is.False);

            services.PlayerMiningState.Teleport(presentation.RepairStationPosition);
            presentation.RefreshAll();
            yield return null;
            Assert.That(presentation.IsRepairInteractionButtonShowing, Is.True);
            Assert.That(presentation.IsRobotFactoryInteractionButtonShowing, Is.False);
            repairButton.onClick.Invoke();
            yield return null;
            Assert.That(services.Vitals.CurrentHealth, Is.EqualTo(services.Vitals.MaxHealth));
            Assert.That(presentation.HudSummary, Does.Contain($"生命 {services.Vitals.MaxHealth}/{services.Vitals.MaxHealth}"));

            services.PlayerMiningState.Teleport(presentation.RobotFactoryPosition);
            presentation.RefreshAll();
            yield return null;
            Assert.That(presentation.IsRobotFactoryInteractionButtonShowing, Is.True);
            int robotsBeforeMarkerClick = services.Robots.Count;
            Assert.That(input.ToggleMarkerMode(), Is.True);
            yield return null;
            Assert.That(presentation.IsRobotFactoryInteractionButtonShowing, Is.False);
            factoryButton.onClick.Invoke();
            yield return null;
            Assert.That(services.Robots.Count, Is.EqualTo(robotsBeforeMarkerClick));
            Assert.That(input.ToggleMarkerMode(), Is.True);
            yield return null;
            Assert.That(presentation.InteractionMode, Is.EqualTo(GameplayInteractionMode.Normal));
            Assert.That(presentation.IsRobotFactoryInteractionButtonShowing, Is.True);

            int robotsBeforeBuildModeClick = services.Robots.Count;
            Assert.That(input.ToggleBuildMode(), Is.True);
            yield return null;
            Assert.That(presentation.IsRobotFactoryInteractionButtonShowing, Is.False);
            factoryButton.onClick.Invoke();
            yield return null;
            Assert.That(services.Robots.Count, Is.EqualTo(robotsBeforeBuildModeClick));
            Assert.That(input.ToggleBuildMode(), Is.True);
            yield return null;
            Assert.That(presentation.InteractionMode, Is.EqualTo(GameplayInteractionMode.Normal));
            presentation.RefreshAll();
            yield return null;
            Assert.That(presentation.IsRobotFactoryInteractionButtonShowing, Is.True);
            factoryButton.onClick.Invoke();
            yield return null;
            Assert.That(services.Robots.Count, Is.EqualTo(1));
            Assert.That(presentation.ActiveRobotViewCount, Is.EqualTo(1));
            Assert.That(GameObject.Find("Robot View 1"), Is.Not.Null);
            RobotState robot = services.Robots[0];
            GridPosition robotStart = robot.Position;
            int metalBeforeAutomation = services.Economy.Resources.Metal;
            for (int i = 0; i < 5; i++)
            {
                services.Session.TickRobots(1f);
                presentation.RefreshAll();
                yield return null;
                if (!robot.IsActive || !robot.Position.Equals(robotStart) || services.Economy.Resources.Metal > metalBeforeAutomation)
                {
                    break;
                }
            }

            Assert.That(!robot.IsActive || !robot.Position.Equals(robotStart) || services.Economy.Resources.Metal > metalBeforeAutomation, Is.True);
            Assert.That(presentation.HudSummary, Does.Contain("从属机器人"));

            services.Vitals.Damage(services.Vitals.MaxHealth);
            presentation.RefreshAll();
            yield return null;
            Assert.That(presentation.IsGameOver, Is.True);
            int robotsBeforeGameOverClick = services.Robots.Count;
            int healthBeforeGameOverClick = services.Vitals.CurrentHealth;
            repairButton.onClick.Invoke();
            factoryButton.onClick.Invoke();
            yield return null;
            Assert.That(services.Vitals.CurrentHealth, Is.EqualTo(healthBeforeGameOverClick));
            Assert.That(services.Robots.Count, Is.EqualTo(robotsBeforeGameOverClick));
        }

        [UnityTest]
        public IEnumerator LayeredOverlaysKeepMarkerDangerBuildPreviewAndScanIndicatorsIndependent()
        {
            yield return LoadBootstrapAndWaitForGameplay();
            yield return null;

            RuntimeServiceRegistry services = MinebotServices.Current;
            MinebotGameplayPresentation presentation = Object.FindAnyObjectByType<MinebotGameplayPresentation>();
            GameplayInputController input = Object.FindAnyObjectByType<GameplayInputController>();
            GridPosition scanWall = services.Grid.PlayerSpawn + GridPosition.Up + GridPosition.Up;
            GridPosition previewOrigin = services.Grid.PlayerSpawn + GridPosition.Down;

            SetBombWall(services, scanWall);
            SetBombWall(services, scanWall + GridPosition.Right);
            SetEmpty(services, new GridPosition(1, 1));
            SetEmpty(services, previewOrigin);
            presentation.RefreshAll();
            yield return null;

            Assert.That(input.ScanCurrentCell(), Is.True);
            yield return null;
            Assert.That(HasScanLabelAboveWall(presentation, scanWall), Is.True);

            Assert.That(input.ToggleMarkerMode(), Is.True);
            Assert.That(input.ClickGridCell(scanWall), Is.True);
            yield return null;
            Assert.That(presentation.GridPresentation.MarkerTilemap.GetTile(TilemapGridPresentation.ToTilePosition(scanWall)), Is.Not.Null);

            Assert.That(input.ToggleMarkerMode(), Is.True);
            Assert.That(input.ToggleBuildMode(), Is.True);
            presentation.SetBuildPreview(previewOrigin);
            yield return null;

            Assert.That(presentation.GridPresentation.BuildPreviewTilemap.GetTile(TilemapGridPresentation.ToTilePosition(previewOrigin)), Is.Not.Null);
            Assert.That(presentation.GridPresentation.MarkerTilemap.GetTile(TilemapGridPresentation.ToTilePosition(scanWall)), Is.Not.Null);
            Assert.That(HasAnyTile(presentation.GridPresentation.DangerTilemap), Is.True);
            Assert.That(ActiveScanLabelCount(), Is.GreaterThan(0));
        }

        private static IEnumerator LoadBootstrapAndWaitForGameplay()
        {
            MinebotServices.ResetForTests();
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            yield return WaitUntilSceneIsActive("Gameplay");
        }

        private static void SetMineableHardness(RuntimeServiceRegistry services, GridPosition position, HardnessTier hardness)
        {
            ref GridCellState cell = ref services.Grid.GetCellRef(position);
            cell.TerrainKind = TerrainKind.MineableWall;
            cell.HardnessTier = hardness;
        }

        private static void SetBombWall(RuntimeServiceRegistry services, GridPosition position)
        {
            ref GridCellState cell = ref services.Grid.GetCellRef(position);
            cell.TerrainKind = TerrainKind.MineableWall;
            cell.HardnessTier = HardnessTier.Soil;
            cell.IsRevealed = false;
            cell.StaticFlags |= CellStaticFlags.Bomb;
            cell.IsMarked = false;
        }

        private static void SetEmpty(RuntimeServiceRegistry services, GridPosition position)
        {
            ref GridCellState cell = ref services.Grid.GetCellRef(position);
            cell.TerrainKind = TerrainKind.Empty;
            cell.HardnessTier = HardnessTier.Soil;
            cell.IsMarked = false;
            cell.IsDangerZone = false;
            cell.IsRevealed = true;
            cell.IsOccupiedByBuilding = false;
            cell.OccupyingBuildingId = null;
            cell.StaticFlags &= ~CellStaticFlags.Bomb;
        }

        private static Button FindBuildingInteractionButton(string buttonName)
        {
            MinebotHudView hud = Object.FindAnyObjectByType<MinebotHudView>();
            Assert.That(hud, Is.Not.Null);
            Transform button = hud.transform.Find($"{MinebotHudView.BuildingInteractionSlotName}/{MinebotGameplayPresentation.BuildingInteractionPanelName}/{buttonName}");
            Assert.That(button, Is.Not.Null);
            Button component = button.GetComponent<Button>();
            Assert.That(component, Is.Not.Null);
            return component;
        }

        private static IEnumerator WaitUntilSceneIsActive(string sceneName)
        {
            float timeoutAt = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != sceneName)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(timeoutAt), $"Timed out waiting for {sceneName}.");
                yield return null;
            }
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

        private static int ActiveScanLabelCount()
        {
            int count = 0;
            Transform scanRoot = GameObject.Find(MinebotGameplayPresentation.ScanIndicatorRootName).transform;
            foreach (Transform child in scanRoot)
            {
                if (child.gameObject.activeSelf)
                {
                    count++;
                }
            }

            return count;
        }

        private static IReadOnlyList<Tilemap> GetTerrainFamilyTilemaps()
        {
            return new[]
            {
                GameObject.Find(MinebotGameplayPresentation.DgFloorTilemapName).GetComponent<Tilemap>(),
                GameObject.Find(MinebotGameplayPresentation.DgSoilTilemapName).GetComponent<Tilemap>(),
                GameObject.Find(MinebotGameplayPresentation.DgStoneTilemapName).GetComponent<Tilemap>(),
                GameObject.Find(MinebotGameplayPresentation.DgHardRockTilemapName).GetComponent<Tilemap>(),
                GameObject.Find(MinebotGameplayPresentation.DgUltraHardTilemapName).GetComponent<Tilemap>(),
                GameObject.Find(MinebotGameplayPresentation.DgBoundaryTilemapName).GetComponent<Tilemap>()
            };
        }

        private static bool HasAnyDisplayTileAroundCell(Tilemap tilemap, GridPosition worldCell)
        {
            Vector3Int[] positions = DualGridTerrain.GetAffectedDisplayCells(worldCell);
            for (int i = 0; i < positions.Length; i++)
            {
                if (tilemap.GetTile(positions[i]) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetTerrainSignature(IReadOnlyList<Tilemap> terrainTilemaps, GridPosition worldCell)
        {
            Vector3Int[] positions = DualGridTerrain.GetAffectedDisplayCells(worldCell);
            var names = new List<string>(positions.Length * terrainTilemaps.Count);
            for (int tilemapIndex = 0; tilemapIndex < terrainTilemaps.Count; tilemapIndex++)
            {
                Tilemap terrainTilemap = terrainTilemaps[tilemapIndex];
                for (int i = 0; i < positions.Length; i++)
                {
                    TileBase tile = terrainTilemap != null ? terrainTilemap.GetTile(positions[i]) : null;
                    names.Add(tile != null ? tile.name : "<null>");
                }
            }

            return string.Join("|", names);
        }

        private static bool HasScanLabelAboveWall(MinebotGameplayPresentation presentation, GridPosition wallPosition)
        {
            Transform scanRoot = GameObject.Find(MinebotGameplayPresentation.ScanIndicatorRootName).transform;
            Vector3 wallCenter = presentation.GridToWorld(wallPosition);
            foreach (Transform child in scanRoot)
            {
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                TMP_Text label = child.GetComponent<TMP_Text>();
                if (label == null)
                {
                    continue;
                }

                bool alignedToWall = Mathf.Abs(label.transform.position.x - wallCenter.x) < 0.1f;
                bool aboveWall = label.transform.position.y > wallCenter.y;
                if (alignedToWall && aboveWall)
                {
                    return true;
                }
            }

            return false;
        }

        private static void PrepareBuildableChamber(RuntimeServiceRegistry services, GridPosition origin, Vector2Int footprint, int padding)
        {
            int minX = origin.X - padding;
            int maxX = origin.X + footprint.x - 1 + padding;
            int minY = origin.Y - padding;
            int maxY = origin.Y + footprint.y - 1 + padding;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    if (!services.Grid.IsInside(position))
                    {
                        continue;
                    }

                    SetEmpty(services, position);
                }
            }
        }
    }
}
