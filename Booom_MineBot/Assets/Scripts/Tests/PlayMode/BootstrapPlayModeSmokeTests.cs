using System.Collections;
using Minebot.Automation;
using Minebot.Bootstrap;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.Progression;
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
        }

        [UnityTest]
        public IEnumerator BootstrapSceneLoaderInitializesServices()
        {
            var root = new GameObject("Bootstrap Smoke");
            root.AddComponent<BootstrapSceneLoader>();

            yield return null;

            Assert.That(MinebotServices.IsInitialized, Is.True);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator GameplaySceneSupportsMiningUpgradeRepairAndRobotLoop()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            yield return WaitUntilSceneIsActive("Gameplay");

            RuntimeServiceRegistry services = MinebotServices.Current;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Gameplay"));
            Assert.That(services, Is.Not.Null);

            MineEnoughForUpgradeRepairAndRobot(services);
            Assert.That(services.Experience.HasPendingUpgrade, Is.True);

            UpgradeDefinition[] candidates = services.Upgrades.GetCandidates(3);
            Assert.That(services.Upgrades.Select(candidates[0]), Is.True);
            Assert.That(services.Experience.HasPendingUpgrade, Is.False);

            services.Vitals.Damage(1);
            Assert.That(services.BaseOps.TryRepair(new ResourceAmount(2, 0, 0)), Is.True);
            Assert.That(services.Vitals.CurrentHealth, Is.EqualTo(services.Vitals.MaxHealth));

            Assert.That(services.RobotFactory.TryProduce(services.Grid.PlayerSpawn, out RobotState robot), Is.True);
            Assert.That(robot, Is.Not.Null);
            Assert.That(services.Robots.Count, Is.EqualTo(1));
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

        private static void MineEnoughForUpgradeRepairAndRobot(RuntimeServiceRegistry services)
        {
            ClearBombs(
                services,
                new GridPosition(6, 8),
                new GridPosition(6, 9),
                new GridPosition(6, 10),
                new GridPosition(7, 10),
                new GridPosition(7, 9),
                new GridPosition(5, 10),
                new GridPosition(5, 9));

            Assert.That(services.Session.Move(GridPosition.Up), Is.EqualTo(MineInteractionResult.Moved));

            MineCollectAndEnter(services, new GridPosition(6, 8), GridPosition.Up);
            MineCollectAndEnter(services, new GridPosition(6, 9), GridPosition.Up);
            MineCollectAndEnter(services, new GridPosition(6, 10), GridPosition.Up);
            MineCollectAndEnter(services, new GridPosition(7, 10), GridPosition.Right);
            MineAndCollect(services, new GridPosition(7, 9));
            Assert.That(services.Session.Move(GridPosition.Left), Is.EqualTo(MineInteractionResult.Moved));
            MineAndCollect(services, new GridPosition(5, 10));
            Assert.That(services.Session.Move(GridPosition.Left), Is.EqualTo(MineInteractionResult.Moved));
            MineAndCollect(services, new GridPosition(5, 9));

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
            Assert.That(services.Session.Mine(target), Is.EqualTo(MineInteractionResult.Mined));
            bool collected = services.Session.TickWorldPickups(1f, ToWorldCenter(target));
            Assert.That(collected, Is.True);
        }

        private static Vector2 ToWorldCenter(GridPosition position)
        {
            return new Vector2(position.X + 0.5f, position.Y + 0.5f);
        }
    }
}
