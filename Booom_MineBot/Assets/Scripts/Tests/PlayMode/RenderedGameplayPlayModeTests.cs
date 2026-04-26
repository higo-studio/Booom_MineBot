using System.Collections;
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

            Tilemap terrain = GameObject.Find(MinebotGameplayPresentation.TerrainTilemapName).GetComponent<Tilemap>();
            Assert.That(terrain, Is.Not.Null);
            Assert.That(terrain.GetTile(TilemapGridPresentation.ToTilePosition(services.Grid.PlayerSpawn)).name, Does.Contain("FloorCave"));
            GridPosition hardRockPosition = new GridPosition(services.Grid.PlayerSpawn.X, services.Grid.PlayerSpawn.Y + 2);
            SetMineableHardness(services, hardRockPosition, HardnessTier.HardRock);
            presentation.RefreshAll();
            Assert.That(terrain.GetTile(TilemapGridPresentation.ToTilePosition(hardRockPosition)).name, Does.Contain("WallHardRock"));

            Tilemap overlay = GameObject.Find(MinebotGameplayPresentation.OverlayTilemapName).GetComponent<Tilemap>();
            Assert.That(overlay.GetTile(new Vector3Int(1, 1, 0)), Is.Not.Null);
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

            Assert.That(input.Move(GridPosition.Up), Is.True);
            yield return null;
            Assert.That(services.PlayerMiningState.Position, Is.EqualTo(start + GridPosition.Up));
            Assert.That(GameObject.Find(MinebotGameplayPresentation.PlayerViewName).transform.position, Is.EqualTo(presentation.GridToWorld(services.PlayerMiningState.Position)));

            Tilemap terrain = presentation.GridPresentation.TerrainTilemap;
            TileBase beforeTile = terrain.GetTile(TilemapGridPresentation.ToTilePosition(minedPosition));
            Assert.That(beforeTile, Is.Not.Null);

            Assert.That(input.ScanCurrentCell(), Is.True);
            Assert.That(presentation.FeedbackMessage, Does.Contain("探测"));
            Assert.That(presentation.WarningSummary, Does.Contain("周边 8 格炸药"));
            Assert.That(input.ToggleMarkerMode(), Is.True);
            Assert.That(presentation.InteractionMode, Is.EqualTo(GameplayInteractionMode.Marker));
            Assert.That(input.ClickGridCell(minedPosition), Is.True);
            Assert.That(services.Grid.GetCell(minedPosition).IsMarked, Is.True);
            Assert.That(presentation.FeedbackMessage, Does.Contain("机器人会避开"));
            Assert.That(presentation.WarningSummary, Does.Contain("已标记 1 格"));
            Assert.That(input.ToggleMarkerMode(), Is.True);
            Assert.That(presentation.InteractionMode, Is.EqualTo(GameplayInteractionMode.Normal));

            Assert.That(input.Move(GridPosition.Up), Is.True);
            yield return null;

            Assert.That(services.Grid.GetCell(minedPosition).TerrainKind, Is.EqualTo(TerrainKind.Empty));
            Assert.That(services.Grid.GetCell(minedPosition).IsMarked, Is.False);
            Assert.That(terrain.GetTile(TilemapGridPresentation.ToTilePosition(minedPosition)), Is.Not.EqualTo(beforeTile));
            Assert.That(presentation.HudSummary, Does.Contain("经验"));
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
            GridPosition origin = services.Grid.PlayerSpawn + GridPosition.Down;
            SetEmpty(services, origin);
            SetEmpty(services, origin + GridPosition.Right);
            presentation.RefreshAll();

            Assert.That(input.ToggleBuildMode(), Is.True);
            presentation.SetSelectedBuilding(drill);
            presentation.SetBuildPreview(origin);
            Assert.That(presentation.GridPresentation.HintTilemap.GetTile(TilemapGridPresentation.ToTilePosition(origin)), Is.Not.Null);
            Assert.That(input.ClickGridCell(origin), Is.True);
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

        private static void SetEmpty(RuntimeServiceRegistry services, GridPosition position)
        {
            ref GridCellState cell = ref services.Grid.GetCellRef(position);
            cell.TerrainKind = TerrainKind.Empty;
            cell.IsMarked = false;
            cell.IsOccupiedByBuilding = false;
            cell.OccupyingBuildingId = null;
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
    }
}
