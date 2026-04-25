using System.Collections;
using Minebot.Bootstrap;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.Presentation;
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
            Assert.That(presentation, Is.Not.Null);
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<GameplayInputController>(), Is.Not.Null);
            Assert.That(GameObject.Find(MinebotGameplayPresentation.PlayerViewName), Is.Not.Null);
            GameObject hud = GameObject.Find(MinebotGameplayPresentation.HudRootName);
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.GetComponentInChildren<TMP_Text>(), Is.Not.Null);
            Assert.That(hud.GetComponentsInChildren<Text>().Length, Is.EqualTo(0));

            Tilemap terrain = GameObject.Find(MinebotGameplayPresentation.TerrainTilemapName).GetComponent<Tilemap>();
            Assert.That(terrain, Is.Not.Null);
            Assert.That(terrain.GetTile(TilemapGridPresentation.ToTilePosition(MinebotServices.Current.Grid.PlayerSpawn)), Is.Not.Null);

            Tilemap overlay = GameObject.Find(MinebotGameplayPresentation.OverlayTilemapName).GetComponent<Tilemap>();
            Assert.That(overlay.GetTile(new Vector3Int(1, 1, 0)), Is.Not.Null);
            Assert.That(presentation.HudSummary, Does.Contain("HP"));
            Assert.That(presentation.HudSummary, Does.Contain("波次"));
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
            Assert.That(input.ToggleMarkerFacingCell(), Is.True);
            Assert.That(services.Grid.GetCell(minedPosition).IsMarked, Is.True);
            Assert.That(presentation.FeedbackMessage, Does.Contain("机器人会避开"));

            Assert.That(input.MineFacingCell(), Is.True);
            yield return null;

            Assert.That(services.Grid.GetCell(minedPosition).TerrainKind, Is.EqualTo(TerrainKind.Empty));
            Assert.That(services.Grid.GetCell(minedPosition).IsMarked, Is.False);
            Assert.That(terrain.GetTile(TilemapGridPresentation.ToTilePosition(minedPosition)), Is.Not.EqualTo(beforeTile));
            Assert.That(presentation.HudSummary, Does.Contain("经验"));
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
            Assert.That(input.SelectUpgrade(0), Is.True);
            yield return null;
            Assert.That(presentation.IsUpgradePanelShowing, Is.False);

            services.Economy.Add(new ResourceAmount(12, 0, 0));
            services.Vitals.Damage(1);
            services.PlayerMiningState.Teleport(presentation.RepairStationPosition);
            Assert.That(input.Repair(), Is.True);
            yield return null;
            Assert.That(services.Vitals.CurrentHealth, Is.EqualTo(services.Vitals.MaxHealth));
            Assert.That(presentation.HudSummary, Does.Contain($"HP {services.Vitals.MaxHealth}/{services.Vitals.MaxHealth}"));

            services.PlayerMiningState.Teleport(presentation.RobotFactoryPosition);
            Assert.That(input.BuildRobot(), Is.True);
            yield return null;
            Assert.That(services.Robots.Count, Is.EqualTo(1));
            Assert.That(presentation.ActiveRobotViewCount, Is.EqualTo(1));
            Assert.That(GameObject.Find("Robot View 1"), Is.Not.Null);

            services.Vitals.Damage(services.Vitals.MaxHealth);
            presentation.RefreshAll();
            yield return null;
            Assert.That(presentation.IsGameOver, Is.True);
        }

        private static IEnumerator LoadBootstrapAndWaitForGameplay()
        {
            MinebotServices.ResetForTests();
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            yield return WaitUntilSceneIsActive("Gameplay");
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
