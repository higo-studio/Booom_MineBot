using Minebot.Editor;
using Minebot.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Minebot.Tests.EditMode
{
    public sealed class HudPrefabBuilderTests
    {
        [Test]
        public void CreatesHudPrefabsWithShellAndPanelBindings()
        {
            MinebotHudPrefabBuilder.CreateOrUpdatePrefabs();

            GameObject rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudView.PrefabAssetPath);
            Assert.That(rootPrefab, Is.Not.Null);
            Assert.That(rootPrefab.GetComponent<MinebotHudView>(), Is.Not.Null);
            RectTransform rootRect = rootPrefab.GetComponent<RectTransform>();
            Assert.That(rootRect.localScale, Is.EqualTo(Vector3.one));
            Assert.That(rootRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rootRect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rootPrefab.transform.Find(MinebotHudView.StatusSlotName), Is.Not.Null);
            Assert.That(rootPrefab.transform.Find(MinebotHudView.WarningSlotName), Is.Not.Null);
            Assert.That(rootPrefab.transform.Find(MinebotHudView.MinimapSlotName), Is.Not.Null);
            Assert.That(rootPrefab.transform.Find(MinebotHudView.BuildSlotName), Is.Not.Null);
            Assert.That(rootPrefab.transform.Find(MinebotHudView.BuildingInteractionSlotName), Is.Not.Null);

            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudDefaults.StatusPanelAssetPath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudDefaults.WarningPanelAssetPath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudDefaults.MinimapPanelAssetPath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudDefaults.UpgradePanelAssetPath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudDefaults.BuildPanelAssetPath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudDefaults.BuildingInteractionPanelAssetPath), Is.Not.Null);

            GameObject instance = Object.Instantiate(rootPrefab);
            try
            {
                MinebotHudView view = instance.GetComponent<MinebotHudView>();
                view.EnsureDefaultStructure(null, MinebotHudDefaults.MinimumBuildButtonCount);

                Assert.That(view.StatusPanel, Is.Not.Null);
                Assert.That(view.WarningPanel, Is.Not.Null);
                Assert.That(view.MinimapPanel, Is.Not.Null);
                Assert.That(view.UpgradePanel, Is.Not.Null);
                Assert.That(view.BuildPanel, Is.Not.Null);
                Assert.That(view.BuildingInteractionPanel, Is.Not.Null);
                Assert.That(view.RepairStationInteractionButton, Is.Not.Null);
                Assert.That(view.RepairStationInteractionButton.name, Is.EqualTo(MinebotHudView.RepairStationInteractionButtonName));
                Assert.That(view.RobotFactoryInteractionButton, Is.Not.Null);
                Assert.That(view.RobotFactoryInteractionButton.name, Is.EqualTo(MinebotHudView.RobotFactoryInteractionButtonName));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
