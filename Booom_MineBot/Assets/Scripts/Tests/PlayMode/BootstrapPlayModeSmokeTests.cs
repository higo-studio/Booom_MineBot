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

namespace Minebot.Tests.PlayMode
{
    public sealed class BootstrapPlayModeSmokeTests
    {
        [TearDown]
        public void TearDown()
        {
            MinebotServices.ResetForTests();
            PlayerPrefs.DeleteKey("minebot.local_leaderboard.v1");
        }

        [UnityTest]
        public IEnumerator BootstrapSceneLoaderInitializesServices()
        {
            bool previousIgnore = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                var root = new GameObject("Bootstrap Smoke");
                BootstrapSceneLoader loader = root.AddComponent<BootstrapSceneLoader>();

                yield return null;

                Assert.That(loader.RuntimeContext, Is.Not.Null);
                Assert.That(loader.RuntimeContext.Container, Is.Not.Null);
                Assert.That(loader.Services, Is.Not.Null);
                Assert.That(MinebotServices.CurrentContainer, Is.SameAs(loader.RuntimeContext.Container));
                Assert.That(MinebotServices.Current, Is.SameAs(loader.Services));
                Object.Destroy(root);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnore;
            }
        }

        [UnityTest]
        public IEnumerator GameplaySceneSupportsMiningUpgradeRepairAndRobotLoop()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            yield return LoadGameplayFromBootstrap();

            RuntimeServiceRegistry services = ResolveRuntimeServices();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Gameplay"));
            Assert.That(services, Is.Not.Null);

            MineEnoughForUpgradeRepairAndRobot(services);
            if (!services.Experience.HasPendingUpgrade)
            {
                services.Experience.AddExperience(Mathf.Max(0, services.Experience.NextThreshold - services.Experience.Experience));
            }

            Assert.That(services.Experience.HasPendingUpgrade, Is.True);

            int upgradesApplied = 0;
            while (services.Experience.HasPendingUpgrade && upgradesApplied < 8)
            {
                UpgradeDefinition[] candidates = services.Upgrades.GetCandidates(2);
                Assert.That(candidates.Length, Is.GreaterThan(0));
                Assert.That(services.Upgrades.Select(candidates[0]), Is.True);
                upgradesApplied++;
            }

            Assert.That(upgradesApplied, Is.GreaterThan(0));
            Assert.That(services.Experience.HasPendingUpgrade, Is.False);

            services.Vitals.Damage(1);
            Assert.That(services.BaseOps.TryRepair(new ResourceAmount(2, 0, 0)), Is.True);
            Assert.That(services.Vitals.CurrentHealth, Is.EqualTo(services.Vitals.MaxHealth));

            Assert.That(services.RobotFactory.TryProduce(services.Grid.PlayerSpawn, out RobotState robot), Is.True);
            Assert.That(robot, Is.Not.Null);
            Assert.That(services.Robots.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator GameplayPresentationReceivesInjectedServicesFromRuntimeContext()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            yield return LoadGameplayFromBootstrap();

            BootstrapSceneLoader loader = Object.FindAnyObjectByType<BootstrapSceneLoader>();
            MinebotGameplayPresentation presentation = Object.FindAnyObjectByType<MinebotGameplayPresentation>();

            Assert.That(loader, Is.Not.Null);
            Assert.That(loader.RuntimeContext, Is.Not.Null);
            Assert.That(loader.RuntimeContext.Container, Is.Not.Null);
            Assert.That(loader.Services, Is.Not.Null);
            Assert.That(presentation, Is.Not.Null);
            Assert.That(presentation.Services, Is.Not.Null);
            Assert.That(presentation.Services.Session, Is.Not.Null);
            Assert.That(presentation.Services.Grid, Is.Not.Null);
            Assert.That(presentation.Services.Upgrades, Is.Not.Null);
            Assert.That(presentation.ActiveBootstrapConfig, Is.EqualTo(loader.Config));
            Assert.That(MinebotRuntimeDiscovery.TryResolveContainer(out MinebotContainer discoveredContainer), Is.True);
            Assert.That(discoveredContainer, Is.Not.Null);
            Assert.That(MinebotRuntimeDiscovery.TryResolveRuntimeServices(out RuntimeServiceRegistry discoveredServices, out BootstrapConfig discoveredConfig), Is.True);
            Assert.That(discoveredServices, Is.Not.Null);
            Assert.That(discoveredConfig, Is.EqualTo(loader.Config));
            Assert.That(MinebotRuntimeDiscovery.TryResolveBootstrapConfig(out BootstrapConfig discoveredOnlyConfig), Is.True);
            Assert.That(discoveredOnlyConfig, Is.EqualTo(loader.Config));
            Assert.That(MinebotServices.CurrentContainer, Is.Not.Null);
            Assert.That(MinebotServices.Current, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator BootstrapStartPageUsesPrefabUiAndCanLoadGameplay()
        {
            LocalLeaderboardService.TryAddEntry("AAA", 123, 7, out _);

            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            yield return null;

            BootstrapSceneLoader loader = Object.FindAnyObjectByType<BootstrapSceneLoader>();
            Assert.That(loader, Is.Not.Null);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Bootstrap"));

            MinebotBootstrapMenuView menu = Object.FindAnyObjectByType<MinebotBootstrapMenuView>();
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.GetComponent<Canvas>(), Is.Null);
            Assert.That(menu.GetComponentInParent<Canvas>(), Is.Not.Null);
            Assert.That(menu.StartButton, Is.Not.Null);
            Assert.That(menu.QuitButton, Is.Not.Null);
            Assert.That(menu.LeaderboardButton, Is.Not.Null);
            Assert.That(menu.StatusText, Is.Null);
            Assert.That(menu.LeaderboardEntriesText, Is.Null);

            menu.LeaderboardButton.onClick.Invoke();
            yield return null;

            GameObject rankPanel = GameObject.Find("Rank Panel");
            Assert.That(rankPanel, Is.Not.Null);
            Assert.That(rankPanel.GetComponent<Canvas>(), Is.Null);
            Assert.That(rankPanel.GetComponentInParent<Canvas>(), Is.Not.Null);
            TMP_Text[] rankTexts = rankPanel.GetComponentsInChildren<TMP_Text>(true);
            Assert.That(System.Array.Exists(rankTexts, text => text != null && text.text == "AAA"), Is.True);
            Assert.That(System.Array.Exists(rankTexts, text => text != null && text.text == "123"), Is.True);
            Assert.That(System.Array.Exists(rankTexts, text => text != null && text.text == "Wave 7"), Is.True);

            menu.StartButton.onClick.Invoke();
            yield return WaitUntilSceneIsActive(loader.Config != null ? loader.Config.GameplaySceneName : "Gameplay");

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(loader.Config != null ? loader.Config.GameplaySceneName : "Gameplay"));
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

        private static IEnumerator LoadGameplayFromBootstrap()
        {
            BootstrapSceneLoader loader = Object.FindAnyObjectByType<BootstrapSceneLoader>();
            Assert.That(loader, Is.Not.Null);

            string gameplaySceneName = loader.Config != null ? loader.Config.GameplaySceneName : "Gameplay";
            if (SceneManager.GetActiveScene().name != gameplaySceneName)
            {
                yield return SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);
            }

            yield return WaitUntilSceneIsActive(gameplaySceneName);
        }

        private static void MineEnoughForUpgradeRepairAndRobot(RuntimeServiceRegistry services)
        {
            GridPosition spawn = services.Grid.PlayerSpawn;
            ClearBombs(
                services,
                Offset(spawn, 0, 2),
                Offset(spawn, 0, 3),
                Offset(spawn, 0, 4),
                Offset(spawn, 1, 4),
                Offset(spawn, 1, 3),
                Offset(spawn, -1, 4),
                Offset(spawn, -1, 3));

            Assert.That(services.Session.Move(GridPosition.Up), Is.EqualTo(MineInteractionResult.Moved));

            MineCollectAndEnter(services, Offset(spawn, 0, 2), GridPosition.Up);
            MineCollectAndEnter(services, Offset(spawn, 0, 3), GridPosition.Up);
            MineCollectAndEnter(services, Offset(spawn, 0, 4), GridPosition.Up);
            MineCollectAndEnter(services, Offset(spawn, 1, 4), GridPosition.Right);
            MineAndCollect(services, Offset(spawn, 1, 3));
            Assert.That(services.Session.Move(GridPosition.Left), Is.EqualTo(MineInteractionResult.Moved));
            MineAndCollect(services, Offset(spawn, -1, 4));
            Assert.That(services.Session.Move(GridPosition.Left), Is.EqualTo(MineInteractionResult.Moved));
            MineAndCollect(services, Offset(spawn, -1, 3));

            Assert.That(services.Economy.Resources.Metal, Is.GreaterThan(0));
            Assert.That(services.Experience.Experience, Is.GreaterThan(0));
            if (services.Economy.Resources.Metal < 7)
            {
                services.Economy.Add(new ResourceAmount(7 - services.Economy.Resources.Metal, 0, 0));
            }

            if (services.Experience.Experience < 5)
            {
                services.Experience.AddExperience(5 - services.Experience.Experience);
            }

            Assert.That(services.Economy.Resources.Metal, Is.GreaterThanOrEqualTo(7));
            Assert.That(services.Experience.Experience, Is.GreaterThanOrEqualTo(5));
        }

        private static void ClearBombs(RuntimeServiceRegistry services, params GridPosition[] positions)
        {
            foreach (GridPosition position in positions)
            {
                if (services.Grid.IsInside(position))
                {
                    services.Grid.GetCellRef(position).ClearBomb();
                }
            }
        }

        private static void MineCollectAndEnter(RuntimeServiceRegistry services, GridPosition target, GridPosition direction)
        {
            MineAndCollect(services, target);
            Assert.That(services.Session.Move(direction), Is.EqualTo(MineInteractionResult.Moved));
        }

        private static void MineAndCollect(RuntimeServiceRegistry services, GridPosition target)
        {
            int attempts = 0;
            while (services.Grid.GetCell(target).IsMineable && attempts < 12)
            {
                MineInteractionResult result = services.Session.Mine(target);
                Assert.That(
                    result,
                    Is.EqualTo(MineInteractionResult.MiningInProgress)
                        .Or.EqualTo(MineInteractionResult.Mined)
                        .Or.EqualTo(MineInteractionResult.TriggeredBomb));
                attempts++;
            }

            Assert.That(services.Grid.GetCell(target).IsMineable, Is.False);
            services.Session.TickWorldPickups(1f, ToWorldCenter(target));
        }

        private static Vector2 ToWorldCenter(GridPosition position)
        {
            return new Vector2(position.X + 0.5f, position.Y + 0.5f);
        }

        private static GridPosition Offset(GridPosition origin, int x, int y)
        {
            return new GridPosition(origin.X + x, origin.Y + y);
        }

        private static RuntimeServiceRegistry ResolveRuntimeServices()
        {
            BootstrapSceneLoader loader = Object.FindAnyObjectByType<BootstrapSceneLoader>();
            if (loader != null && loader.Services != null)
            {
                return loader.Services;
            }

            return MinebotServices.Current;
        }
    }
}
