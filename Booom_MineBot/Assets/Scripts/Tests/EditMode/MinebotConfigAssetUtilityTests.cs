using JSAM;
using Minebot.Bootstrap;
using Minebot.Editor;
using Minebot.GridMining;
using NUnit.Framework;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Minebot.Tests.EditMode
{
    public sealed class MinebotConfigAssetUtilityTests
    {
        private const string TestRoot = "Assets/Temp/MinebotConfigAssetUtilityTests";
        private string[] jsamSettingsGuidsBeforeTest;

        [SetUp]
        public void SetUp()
        {
            jsamSettingsGuidsBeforeTest = AssetDatabase.FindAssets($"t:{nameof(JSAMSettings)}");
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestRoot);
            if (jsamSettingsGuidsBeforeTest == null || jsamSettingsGuidsBeforeTest.Length == 0)
            {
                string[] currentSettingsGuids = AssetDatabase.FindAssets($"t:{nameof(JSAMSettings)}");
                for (int i = 0; i < currentSettingsGuids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(currentSettingsGuids[i]);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        AssetDatabase.DeleteAsset(path);
                    }
                }
            }

            AssetDatabase.SaveAssets();
        }

        [Test]
        public void EnsureManagedAssetsCreatesAndAssignsMissingConfigAssets()
        {
            BootstrapConfig bootstrapConfig = CreateBootstrapAsset();

            MinebotConfigAssetUtility.EnsureManagedAssets(bootstrapConfig);

            SerializedObject serializedObject = new SerializedObject(bootstrapConfig);
            Assert.That(serializedObject.FindProperty("inputActions")?.objectReferenceValue, Is.Not.Null);
            AssertManagedReference(serializedObject, "balanceConfig");
            AssertManagedReference(serializedObject, "upgradePool");
            AssertManagedReference(serializedObject, "hazardRules");
            AssertManagedReference(serializedObject, "miningRules");
            AssertManagedReference(serializedObject, "waveConfig");
            AssertManagedReference(serializedObject, "audioConfig");

            SerializedProperty buildingDefinitions = serializedObject.FindProperty("buildingDefinitions");
            Assert.That(buildingDefinitions, Is.Not.Null);
            Assert.That(buildingDefinitions.arraySize, Is.GreaterThan(0));
            Assert.That(buildingDefinitions.GetArrayElementAtIndex(0).objectReferenceValue, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(buildingDefinitions.GetArrayElementAtIndex(0).objectReferenceValue), Does.StartWith(TestRoot));

            SerializedObject audioConfigObject = new SerializedObject(serializedObject.FindProperty("audioConfig").objectReferenceValue);
            AssertManagedReference(audioConfigObject, "music.gameplayLoop");
            AssertManagedReference(audioConfigObject, "modeAndUi.actionDenied");
            AssertManagedReference(audioConfigObject, "playerAndTerrain.playerMiningLoop");
            AssertManagedReference(audioConfigObject, "waveAndFailure.gameOver");

            JSAMSettings settings = MinebotConfigAssetUtility.GetOrCreateJsamSettingsAsset();
            Assert.That(settings, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(settings), Is.EqualTo("Assets/Settings/Resources/JSAMSettings.asset"));

            Assert.That(serializedObject.FindProperty("defaultMap")?.objectReferenceValue, Is.Null);
        }

        [Test]
        public void AssignOrCreateDefaultMapCreatesMapOnlyWhenRequested()
        {
            BootstrapConfig bootstrapConfig = CreateBootstrapAsset();

            MinebotConfigAssetUtility.EnsureManagedAssets(bootstrapConfig);
            Assert.That(new SerializedObject(bootstrapConfig).FindProperty("defaultMap")?.objectReferenceValue, Is.Null);

            MapDefinition defaultMap = MinebotConfigAssetUtility.AssignOrCreateDefaultMap(bootstrapConfig);

            Assert.That(defaultMap, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(defaultMap), Is.EqualTo(Path.Combine(TestRoot, "Map Definition.asset").Replace('\\', '/')));
            Assert.That(new SerializedObject(bootstrapConfig).FindProperty("defaultMap")?.objectReferenceValue, Is.SameAs(defaultMap));
        }

        [Test]
        public void CreateBuildingDefinitionAssetAppendsToBootstrapList()
        {
            BootstrapConfig bootstrapConfig = CreateBootstrapAsset();
            MinebotConfigAssetUtility.EnsureManagedAssets(bootstrapConfig);

            SerializedProperty beforeProperty = new SerializedObject(bootstrapConfig).FindProperty("buildingDefinitions");
            int beforeCount = beforeProperty.arraySize;

            Object created = MinebotConfigAssetUtility.CreateBuildingDefinitionAsset(bootstrapConfig);

            Assert.That(created, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(created), Does.StartWith($"{TestRoot}/Buildings/"));

            SerializedProperty afterProperty = new SerializedObject(bootstrapConfig).FindProperty("buildingDefinitions");
            Assert.That(afterProperty.arraySize, Is.EqualTo(beforeCount + 1));

            bool foundCreated = false;
            for (int i = 0; i < afterProperty.arraySize; i++)
            {
                if (afterProperty.GetArrayElementAtIndex(i).objectReferenceValue == created)
                {
                    foundCreated = true;
                    break;
                }
            }

            Assert.That(foundCreated, Is.True);
        }

        private static BootstrapConfig CreateBootstrapAsset()
        {
            EnsureFolder(TestRoot);

            var bootstrapConfig = ScriptableObject.CreateInstance<BootstrapConfig>();
            AssetDatabase.CreateAsset(bootstrapConfig, $"{TestRoot}/Bootstrap.asset");
            AssetDatabase.SaveAssets();
            return bootstrapConfig;
        }

        private static void AssertManagedReference(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null);
            Assert.That(property.objectReferenceValue, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(property.objectReferenceValue), Does.StartWith(TestRoot));
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
        }
    }
}
