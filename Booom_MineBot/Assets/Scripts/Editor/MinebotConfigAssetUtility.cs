using System;
using System.Collections.Generic;
using System.IO;
using Minebot.Bootstrap;
using Minebot.GridMining;
using Minebot.HazardInference;
using Minebot.Progression;
using Minebot.WaveSurvival;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Minebot.Editor
{
    public static class MinebotConfigAssetUtility
    {
        public const string DefaultConfigRoot = "Assets/Settings/Gameplay";

        private const string DefaultBootstrapAssetName = "Bootstrap.asset";
        private const string DefaultInputActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string DefaultBalanceAssetName = "GameBlance.asset";
        private const string DefaultUpgradePoolAssetName = "UpgradePool.asset";
        private const string DefaultHazardRulesAssetName = "Hazard Rules.asset";
        private const string DefaultMiningRulesAssetName = "Mining Rules.asset";
        private const string DefaultWaveConfigAssetName = "Wave Config.asset";
        private const string DefaultMapAssetName = "Map Definition.asset";
        private const string DefaultBuildingAssetName = "Building Definition.asset";
        private const string ManagedBuildingsFolderName = "Buildings";

        public static BootstrapConfig GetOrCreateBootstrapConfig()
        {
            BootstrapConfig bootstrapConfig =
                AssetDatabase.LoadAssetAtPath<BootstrapConfig>(CombineAssetPath(DefaultConfigRoot, DefaultBootstrapAssetName));

            if (bootstrapConfig == null)
            {
                bootstrapConfig = FindFirstAsset<BootstrapConfig>(null, DefaultBootstrapAssetName);
            }

            if (bootstrapConfig == null)
            {
                EnsureFolder(DefaultConfigRoot);
                bootstrapConfig = CreateAsset<BootstrapConfig>(CombineAssetPath(DefaultConfigRoot, DefaultBootstrapAssetName));
            }

            EnsureManagedAssets(bootstrapConfig);
            return bootstrapConfig;
        }

        public static void EnsureManagedAssets(BootstrapConfig bootstrapConfig)
        {
            if (bootstrapConfig == null)
            {
                return;
            }

            string bootstrapPath = AssetDatabase.GetAssetPath(bootstrapConfig);
            if (string.IsNullOrWhiteSpace(bootstrapPath))
            {
                return;
            }

            string rootFolder = GetDirectoryPath(bootstrapPath);
            EnsureFolder(rootFolder);

            SerializedObject serializedObject = new SerializedObject(bootstrapConfig);
            serializedObject.UpdateIfRequiredOrScript();

            bool changed = false;
            changed |= EnsureInputActions(serializedObject);
            changed |= EnsureManagedReference<GameBalanceConfig>(serializedObject, "balanceConfig", rootFolder, DefaultBalanceAssetName);
            changed |= EnsureManagedReference<UpgradePoolConfig>(serializedObject, "upgradePool", rootFolder, DefaultUpgradePoolAssetName);
            changed |= EnsureManagedReference<HazardRules>(serializedObject, "hazardRules", rootFolder, DefaultHazardRulesAssetName);
            changed |= EnsureManagedReference<MiningRules>(serializedObject, "miningRules", rootFolder, DefaultMiningRulesAssetName);
            changed |= EnsureManagedReference<WaveConfig>(serializedObject, "waveConfig", rootFolder, DefaultWaveConfigAssetName);
            changed |= SyncBuildingDefinitions(serializedObject, rootFolder);

            if (!changed)
            {
                return;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bootstrapConfig);
            AssetDatabase.SaveAssets();
        }

        public static MapDefinition AssignOrCreateDefaultMap(BootstrapConfig bootstrapConfig)
        {
            if (bootstrapConfig == null)
            {
                return null;
            }

            string rootFolder = GetAssetRootFolder(bootstrapConfig);
            SerializedObject serializedObject = new SerializedObject(bootstrapConfig);
            serializedObject.UpdateIfRequiredOrScript();

            SerializedProperty property = serializedObject.FindProperty("defaultMap");
            MapDefinition defaultMap = property?.objectReferenceValue as MapDefinition;
            if (!IsPersistentAsset(defaultMap))
            {
                defaultMap = FindFirstAsset<MapDefinition>(rootFolder, DefaultMapAssetName);
                if (defaultMap == null)
                {
                    defaultMap = CreateAsset<MapDefinition>(CombineAssetPath(rootFolder, DefaultMapAssetName));
                }

                if (property != null)
                {
                    property.objectReferenceValue = defaultMap;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(bootstrapConfig);
                    AssetDatabase.SaveAssets();
                }
            }

            return defaultMap;
        }

        public static BuildingDefinition CreateBuildingDefinitionAsset(BootstrapConfig bootstrapConfig)
        {
            if (bootstrapConfig == null)
            {
                return null;
            }

            string folder = GetManagedBuildingsFolder(bootstrapConfig);
            EnsureFolder(folder);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(CombineAssetPath(folder, DefaultBuildingAssetName));
            BuildingDefinition created = CreateAsset<BuildingDefinition>(assetPath);
            SyncBuildingDefinitions(bootstrapConfig);
            return created;
        }

        public static void SyncBuildingDefinitions(BootstrapConfig bootstrapConfig)
        {
            if (bootstrapConfig == null)
            {
                return;
            }

            string rootFolder = GetAssetRootFolder(bootstrapConfig);
            SerializedObject serializedObject = new SerializedObject(bootstrapConfig);
            serializedObject.UpdateIfRequiredOrScript();

            if (!SyncBuildingDefinitions(serializedObject, rootFolder))
            {
                return;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bootstrapConfig);
            AssetDatabase.SaveAssets();
        }

        public static IReadOnlyList<BuildingDefinition> GetBuildingDefinitions(BootstrapConfig bootstrapConfig)
        {
            var results = new List<BuildingDefinition>();
            if (bootstrapConfig == null)
            {
                return results;
            }

            SerializedObject serializedObject = new SerializedObject(bootstrapConfig);
            SerializedProperty property = serializedObject.FindProperty("buildingDefinitions");
            if (property == null)
            {
                return results;
            }

            for (int i = 0; i < property.arraySize; i++)
            {
                if (property.GetArrayElementAtIndex(i).objectReferenceValue is BuildingDefinition definition)
                {
                    results.Add(definition);
                }
            }

            return results;
        }

        public static string GetManagedBuildingsFolder(BootstrapConfig bootstrapConfig)
        {
            return CombineAssetPath(GetAssetRootFolder(bootstrapConfig), ManagedBuildingsFolderName);
        }

        public static string GetAssetRootFolder(BootstrapConfig bootstrapConfig)
        {
            if (bootstrapConfig == null)
            {
                return DefaultConfigRoot;
            }

            string assetPath = AssetDatabase.GetAssetPath(bootstrapConfig);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return DefaultConfigRoot;
            }

            return GetDirectoryPath(assetPath);
        }

        private static bool EnsureInputActions(SerializedObject serializedObject)
        {
            SerializedProperty property = serializedObject.FindProperty("inputActions");
            if (property == null || IsPersistentAsset(property.objectReferenceValue))
            {
                return false;
            }

            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(DefaultInputActionsPath);
            if (inputActions == null)
            {
                inputActions = FindFirstAsset<InputActionAsset>(null, Path.GetFileName(DefaultInputActionsPath));
            }

            if (inputActions == null)
            {
                return false;
            }

            property.objectReferenceValue = inputActions;
            return true;
        }

        private static bool EnsureManagedReference<T>(
            SerializedObject serializedObject,
            string propertyName,
            string rootFolder,
            string preferredAssetName)
            where T : ScriptableObject
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || IsPersistentAsset(property.objectReferenceValue))
            {
                return false;
            }

            T asset = FindFirstAsset<T>(rootFolder, preferredAssetName);
            if (asset == null)
            {
                asset = CreateAsset<T>(CombineAssetPath(rootFolder, preferredAssetName));
            }

            property.objectReferenceValue = asset;
            return true;
        }

        private static bool SyncBuildingDefinitions(SerializedObject serializedObject, string rootFolder)
        {
            SerializedProperty property = serializedObject.FindProperty("buildingDefinitions");
            if (property == null)
            {
                return false;
            }

            List<BuildingDefinition> definitions = CollectBuildingDefinitions(serializedObject, rootFolder);
            if (definitions.Count == 0)
            {
                string managedFolder = CombineAssetPath(rootFolder, ManagedBuildingsFolderName);
                EnsureFolder(managedFolder);
                definitions.Add(CreateAsset<BuildingDefinition>(
                    AssetDatabase.GenerateUniqueAssetPath(CombineAssetPath(managedFolder, DefaultBuildingAssetName))));
            }

            definitions.Sort((left, right) => string.CompareOrdinal(
                AssetDatabase.GetAssetPath(left),
                AssetDatabase.GetAssetPath(right)));

            bool changed = property.arraySize != definitions.Count;
            if (!changed)
            {
                for (int i = 0; i < definitions.Count; i++)
                {
                    if (property.GetArrayElementAtIndex(i).objectReferenceValue != definitions[i])
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed)
            {
                return false;
            }

            property.arraySize = definitions.Count;
            for (int i = 0; i < definitions.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            }

            return true;
        }

        private static List<BuildingDefinition> CollectBuildingDefinitions(SerializedObject serializedObject, string rootFolder)
        {
            var definitions = new List<BuildingDefinition>();
            var knownPaths = new HashSet<string>(StringComparer.Ordinal);

            SerializedProperty property = serializedObject.FindProperty("buildingDefinitions");
            if (property != null)
            {
                for (int i = 0; i < property.arraySize; i++)
                {
                    if (property.GetArrayElementAtIndex(i).objectReferenceValue is BuildingDefinition definition)
                    {
                        AddIfUnique(definitions, knownPaths, definition);
                    }
                }
            }

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(BuildingDefinition)}", new[] { rootFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                BuildingDefinition definition = AssetDatabase.LoadAssetAtPath<BuildingDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));
                AddIfUnique(definitions, knownPaths, definition);
            }

            return definitions;
        }

        private static void AddIfUnique(
            ICollection<BuildingDefinition> definitions,
            ISet<string> knownPaths,
            BuildingDefinition definition)
        {
            if (!IsPersistentAsset(definition))
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(definition);
            if (string.IsNullOrWhiteSpace(assetPath) || !knownPaths.Add(assetPath))
            {
                return;
            }

            definitions.Add(definition);
        }

        private static T FindFirstAsset<T>(string searchFolder, string preferredAssetName)
            where T : UnityEngine.Object
        {
            if (!string.IsNullOrWhiteSpace(searchFolder))
            {
                T preferred = AssetDatabase.LoadAssetAtPath<T>(CombineAssetPath(searchFolder, preferredAssetName));
                if (preferred != null)
                {
                    return preferred;
                }
            }

            string[] guids = string.IsNullOrWhiteSpace(searchFolder)
                ? AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                : AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { searchFolder });

            var candidatePaths = new List<string>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string candidatePath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrWhiteSpace(candidatePath))
                {
                    candidatePaths.Add(candidatePath);
                }
            }

            candidatePaths.Sort(StringComparer.Ordinal);

            for (int i = 0; i < candidatePaths.Count; i++)
            {
                if (string.Equals(Path.GetFileName(candidatePaths[i]), preferredAssetName, StringComparison.Ordinal))
                {
                    T preferred = AssetDatabase.LoadAssetAtPath<T>(candidatePaths[i]);
                    if (preferred != null)
                    {
                        return preferred;
                    }
                }
            }

            for (int i = 0; i < candidatePaths.Count; i++)
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(candidatePaths[i]);
                if (asset != null)
                {
                    return asset;
                }
            }

            return null;
        }

        private static T CreateAsset<T>(string assetPath)
            where T : ScriptableObject
        {
            EnsureFolder(GetDirectoryPath(assetPath));
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(asset, assetPath);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static bool IsPersistentAsset(UnityEngine.Object asset)
        {
            return asset != null && !string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(asset));
        }

        private static string GetDirectoryPath(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath);
            return string.IsNullOrWhiteSpace(directory)
                ? DefaultConfigRoot
                : directory.Replace('\\', '/');
        }

        private static string CombineAssetPath(string left, string right)
        {
            return $"{left.TrimEnd('/')}/{right.TrimStart('/')}";
        }

        private static void EnsureFolder(string folderPath)
        {
            string normalized = folderPath.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(normalized) || string.Equals(normalized, "Assets", StringComparison.Ordinal))
            {
                return;
            }

            string parent = GetDirectoryPath(normalized);
            string name = Path.GetFileName(normalized);
            EnsureFolder(parent);

            if (!AssetDatabase.IsValidFolder(normalized))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
