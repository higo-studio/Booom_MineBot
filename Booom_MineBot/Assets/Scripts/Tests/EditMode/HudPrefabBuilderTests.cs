using Minebot.Editor;
using Minebot.UI;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Minebot.Tests.EditMode
{
    public sealed class HudPrefabBuilderTests
    {
        private const string DefaultArtSetAssetPath = "Assets/Resources/Minebot/MinebotPresentationArtSet_Default.asset";

        [Test]
        public void CreatesHudPrefabsWithShellAndPanelBindings()
        {
            Dictionary<string, byte[]> snapshots = CaptureAssetSnapshots(
                MinebotHudView.PrefabAssetPath,
                MinebotHudDefaults.StatusPanelAssetPath,
                MinebotHudDefaults.InteractionPanelAssetPath,
                MinebotHudDefaults.FeedbackPanelAssetPath,
                MinebotHudDefaults.WarningPanelAssetPath,
                MinebotHudDefaults.GameOverPanelAssetPath,
                MinebotHudDefaults.MinimapPanelAssetPath,
                MinebotHudDefaults.UpgradePanelAssetPath,
                MinebotHudDefaults.BuildPanelAssetPath,
                MinebotHudDefaults.BuildingInteractionPanelAssetPath,
                MinebotHudDefaults.BootstrapMenuAssetPath,
                DefaultArtSetAssetPath);

            try
            {
                MinebotHudPrefabBuilder.CreateOrUpdatePrefabs();

                GameObject rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudView.PrefabAssetPath);
                Assert.That(rootPrefab, Is.Not.Null);
                Assert.That(rootPrefab.GetComponent<MinebotHudView>(), Is.Not.Null);
                Assert.That(rootPrefab.transform.Find("Upper Left"), Is.Not.Null);
                Assert.That(rootPrefab.transform.Find("Upper Center"), Is.Not.Null);
                Assert.That(rootPrefab.transform.Find("Lower Left"), Is.Not.Null);
                Assert.That(rootPrefab.transform.Find("Lower Right"), Is.Not.Null);
                Assert.That(rootPrefab.transform.Find(MinebotHudView.StatusSlotName), Is.Null);
                Assert.That(rootPrefab.transform.Find(MinebotHudView.WarningSlotName), Is.Null);
                Assert.That(rootPrefab.transform.Find(MinebotHudView.MinimapSlotName), Is.Null);
                Assert.That(rootPrefab.transform.Find(MinebotHudView.BuildSlotName), Is.Null);

                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudDefaults.StatusPanelAssetPath), Is.Not.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudDefaults.WarningPanelAssetPath), Is.Not.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudDefaults.GameOverPanelAssetPath), Is.Not.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudDefaults.MinimapPanelAssetPath), Is.Not.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudDefaults.UpgradePanelAssetPath), Is.Not.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudDefaults.BuildPanelAssetPath), Is.Not.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudDefaults.BuildingInteractionPanelAssetPath), Is.Not.Null);
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudDefaults.BootstrapMenuAssetPath), Is.Not.Null);

                GameObject instance = Object.Instantiate(rootPrefab);
                try
                {
                    MinebotHudView view = instance.GetComponent<MinebotHudView>();
                    view.EnsureDefaultStructure(MinebotHudDefaults.MinimumBuildButtonCount);

                    RectTransform rootRect = instance.GetComponent<RectTransform>();
                    Assert.That(rootRect.localScale, Is.EqualTo(Vector3.one));
                    Assert.That(rootRect.anchorMin, Is.EqualTo(Vector2.zero));
                    Assert.That(rootRect.anchorMax, Is.EqualTo(Vector2.one));
                    Assert.That(view.UsesTemplateHud, Is.True);
                    Assert.That(instance.transform.Find(MinebotHudView.StatusSlotName), Is.Null);
                    Assert.That(instance.transform.Find(MinebotHudView.WarningSlotName), Is.Null);
                    Assert.That(instance.transform.Find(MinebotHudView.MinimapSlotName), Is.Null);
                    Assert.That(instance.transform.Find(MinebotHudView.BuildSlotName), Is.Null);
                    Assert.That(instance.transform.Find(MinebotHudView.GameOverSlotName), Is.Not.Null);
                    Assert.That(instance.transform.Find(MinebotHudView.UpgradeSlotName), Is.Not.Null);
                    Assert.That(instance.transform.Find(MinebotHudView.BuildingInteractionSlotName), Is.Not.Null);
                    Assert.That(view.StatusPanel, Is.Null);
                    Assert.That(view.WarningPanel, Is.Null);
                    Assert.That(view.MinimapPanel, Is.Null);
                    Assert.That(view.GameOverPanel, Is.Not.Null);
                    Assert.That(view.UpgradePanel, Is.Not.Null);
                    Assert.That(view.BuildPanel, Is.Null);
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

                GameObject bootstrapMenuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudDefaults.BootstrapMenuAssetPath);
                Assert.That(bootstrapMenuPrefab, Is.Not.Null);
                Assert.That(bootstrapMenuPrefab.GetComponent<MinebotBootstrapMenuView>(), Is.Not.Null);
            }
            finally
            {
                RestoreAssetSnapshots(snapshots);
            }
        }

        private static Dictionary<string, byte[]> CaptureAssetSnapshots(params string[] assetPaths)
        {
            var snapshots = new Dictionary<string, byte[]>(assetPaths.Length);
            for (int i = 0; i < assetPaths.Length; i++)
            {
                string assetPath = assetPaths[i];
                string fullPath = Path.GetFullPath(assetPath);
                snapshots[assetPath] = File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : null;
            }

            return snapshots;
        }

        private static void RestoreAssetSnapshots(IReadOnlyDictionary<string, byte[]> snapshots)
        {
            foreach (KeyValuePair<string, byte[]> snapshot in snapshots)
            {
                string fullPath = Path.GetFullPath(snapshot.Key);
                if (snapshot.Value == null)
                {
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                        AssetDatabase.DeleteAsset(snapshot.Key);
                    }

                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllBytes(fullPath, snapshot.Value);
                AssetDatabase.ImportAsset(snapshot.Key, ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
    }
}
