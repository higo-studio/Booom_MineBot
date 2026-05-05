using System;
using System.Collections.Generic;
using System.IO;
using JSAM;
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
        private const string ManagedAudioFolderName = "Audio";
        private const string ManagedAudioMusicFolderName = "Music";
        private const string ManagedAudioSoundFolderName = "Sounds";
        private const string DefaultAudioConfigAssetRelativePath = "Audio/Minebot Audio Config.asset";
        private const string DefaultJsamSettingsPath = "Assets/Settings/Resources/JSAMSettings.asset";

        private static readonly AudioCueDescriptor[] ManagedAudioCues =
        {
            new AudioCueDescriptor("music.gameplayLoop", "Bgm_GameplayLoop.asset", CueAssetKind.Music, AudioCuePreset.MusicLoop),
            new AudioCueDescriptor("music.waveWarning", "Bgm_WaveWarning.asset", CueAssetKind.Music, AudioCuePreset.MusicLoop),
            new AudioCueDescriptor("music.waveResolution", "Bgm_WaveResolution.asset", CueAssetKind.Music, AudioCuePreset.MusicLoop),
            new AudioCueDescriptor("modeAndUi.modeMarkerToggle", "Ui_ModeMarkerToggle.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("modeAndUi.modeBuildToggle", "Ui_ModeBuildToggle.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("modeAndUi.buildingSelect", "Ui_BuildingSelect.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("modeAndUi.markerSet", "Ui_MarkerSet.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("modeAndUi.markerClear", "Ui_MarkerClear.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("modeAndUi.actionDenied", "Ui_ActionDenied.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("playerAndTerrain.playerMove", "Player_Move.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("playerAndTerrain.playerBlock", "Player_Block.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("playerAndTerrain.playerMiningLoop", "Player_MiningLoop.asset", CueAssetKind.Sound, AudioCuePreset.WorldLoop),
            new AudioCueDescriptor("playerAndTerrain.playerMiningWeak", "Player_MiningWeak.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("playerAndTerrain.terrainWallBreak", "Terrain_WallBreak.asset", CueAssetKind.Sound, AudioCuePreset.WorldOneShot),
            new AudioCueDescriptor("playerAndTerrain.hazardBombExplosion", "Hazard_BombExplosion.asset", CueAssetKind.Sound, AudioCuePreset.WorldOneShot),
            new AudioCueDescriptor("playerAndTerrain.playerDamage", "Player_Damage.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("pickupAndGrowth.pickupMetalAbsorb", "Pickup_MetalAbsorb.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("pickupAndGrowth.pickupEnergyAbsorb", "Pickup_EnergyAbsorb.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("pickupAndGrowth.pickupExpAbsorb", "Pickup_ExpAbsorb.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("pickupAndGrowth.upgradeAvailable", "Sting_UpgradeAvailable.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("pickupAndGrowth.upgradeApply", "Upgrade_Apply.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("baseOps.repairSuccess", "Repair_Success.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("baseOps.robotBuildSuccess", "Robot_BuildSuccess.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("baseOps.buildPlaceSuccess", "Build_PlaceSuccess.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("robots.robotMiningLoop", "Robot_MiningLoop.asset", CueAssetKind.Sound, AudioCuePreset.WorldLoop),
            new AudioCueDescriptor("robots.robotWallBreak", "Robot_WallBreak.asset", CueAssetKind.Sound, AudioCuePreset.WorldOneShot),
            new AudioCueDescriptor("robots.robotDestroyed", "Robot_Destroyed.asset", CueAssetKind.Sound, AudioCuePreset.WorldOneShot),
            new AudioCueDescriptor("waveAndFailure.gameOver", "Sting_GameOver.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("waveAndFailure.waveWarningStart", "Wave_WarningStart.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("waveAndFailure.waveDangerRefresh", "Wave_DangerRefresh.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("waveAndFailure.waveCollapse", "Wave_Collapse.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot),
            new AudioCueDescriptor("waveAndFailure.waveSurvived", "Wave_Survived.asset", CueAssetKind.Sound, AudioCuePreset.UiOneShot)
        };

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
            changed |= EnsureManagedAudioAssets(serializedObject, rootFolder);
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

        public static JSAMSettings GetOrCreateJsamSettingsAsset()
        {
            return GetOrCreateJsamSettingsAsset(out _);
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

        private static bool EnsureManagedAudioAssets(SerializedObject serializedObject, string rootFolder)
        {
            bool changed = EnsureManagedReference<MinebotAudioConfig>(
                serializedObject,
                "audioConfig",
                rootFolder,
                DefaultAudioConfigAssetRelativePath);

            JSAMSettings settings = GetOrCreateJsamSettingsAsset(out bool createdSettingsAsset);
            changed |= createdSettingsAsset && settings != null;

            SerializedProperty property = serializedObject.FindProperty("audioConfig");
            if (property?.objectReferenceValue is not MinebotAudioConfig audioConfig)
            {
                return changed;
            }

            string audioRoot = CombineAssetPath(rootFolder, ManagedAudioFolderName);
            if (!EnsureManagedAudioCueAssets(audioConfig, audioRoot))
            {
                return changed;
            }

            return true;
        }

        private static bool EnsureManagedAudioCueAssets(MinebotAudioConfig audioConfig, string audioRoot)
        {
            if (audioConfig == null)
            {
                return false;
            }

            string musicFolder = CombineAssetPath(audioRoot, ManagedAudioMusicFolderName);
            string soundFolder = CombineAssetPath(audioRoot, ManagedAudioSoundFolderName);
            EnsureFolder(musicFolder);
            EnsureFolder(soundFolder);

            SerializedObject serializedObject = new SerializedObject(audioConfig);
            serializedObject.UpdateIfRequiredOrScript();

            bool changed = false;
            for (int i = 0; i < ManagedAudioCues.Length; i++)
            {
                changed |= EnsureManagedAudioCueAsset(serializedObject, musicFolder, soundFolder, ManagedAudioCues[i]);
            }

            if (!changed)
            {
                return false;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(audioConfig);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static bool EnsureManagedAudioCueAsset(
            SerializedObject serializedObject,
            string musicFolder,
            string soundFolder,
            AudioCueDescriptor descriptor)
        {
            SerializedProperty property = serializedObject.FindProperty(descriptor.PropertyPath);
            if (property == null)
            {
                return false;
            }

            string folder = descriptor.Kind == CueAssetKind.Music ? musicFolder : soundFolder;
            BaseAudioFileObject current = property.objectReferenceValue as BaseAudioFileObject;
            string expectedPath = CombineAssetPath(folder, descriptor.AssetName);

            if (current != null && string.Equals(AssetDatabase.GetAssetPath(current), expectedPath, StringComparison.Ordinal))
            {
                return false;
            }

            BaseAudioFileObject asset = descriptor.Kind == CueAssetKind.Music
                ? GetOrCreateManagedAudioAssetExact<MusicFileObject>(folder, descriptor.AssetName, descriptor.Preset)
                : GetOrCreateManagedAudioAssetExact<SoundFileObject>(folder, descriptor.AssetName, descriptor.Preset);
            property.objectReferenceValue = asset;
            return true;
        }

        private static T GetOrCreateManagedAudioAssetExact<T>(string folder, string assetName, AudioCuePreset preset)
            where T : BaseAudioFileObject
        {
            string assetPath = CombineAssetPath(folder, assetName);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = CreateAsset<T>(assetPath);
            ConfigureManagedAudioAsset(asset, preset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void ConfigureManagedAudioAsset(BaseAudioFileObject asset, AudioCuePreset preset)
        {
            if (asset == null)
            {
                return;
            }

            asset.loopMode = LoopMode.NoLooping;
            asset.channelOverride = asset is MusicFileObject ? VolumeChannel.Music : VolumeChannel.Sound;
            asset.priority = asset is MusicFileObject ? Priority.Music : Priority.Default;
            asset.spatialize = false;
            asset.maxDistance = 0f;
            asset.maxPlayingInstances = 4;
            asset.relativeVolume = 1f;
            asset.startingPitch = 1f;
            asset.pitchShift = 0.05f;
            asset.ignoreTimeScale = false;

            switch (preset)
            {
                case AudioCuePreset.MusicLoop:
                    asset.loopMode = LoopMode.Looping;
                    asset.priority = Priority.Music;
                    asset.maxPlayingInstances = 1;
                    asset.pitchShift = 0f;
                    break;
                case AudioCuePreset.WorldLoop:
                    asset.loopMode = LoopMode.Looping;
                    asset.priority = Priority.High;
                    asset.spatialize = true;
                    asset.maxDistance = 8f;
                    asset.maxPlayingInstances = 1;
                    asset.pitchShift = 0f;
                    break;
                case AudioCuePreset.WorldOneShot:
                    asset.priority = Priority.High;
                    asset.spatialize = true;
                    asset.maxDistance = 8f;
                    asset.maxPlayingInstances = 8;
                    break;
                case AudioCuePreset.UiOneShot:
                    asset.priority = Priority.High;
                    asset.maxPlayingInstances = 2;
                    asset.pitchShift = 0.02f;
                    break;
            }
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
            string preferredFileName = Path.GetFileName(preferredAssetName);
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
                if (string.Equals(Path.GetFileName(candidatePaths[i]), preferredFileName, StringComparison.Ordinal))
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

        private static JSAMSettings GetOrCreateJsamSettingsAsset(out bool created)
        {
            created = false;

            JSAMSettings settings = AssetDatabase.LoadAssetAtPath<JSAMSettings>(DefaultJsamSettingsPath);
            if (settings != null)
            {
                return settings;
            }

            settings = FindFirstAsset<JSAMSettings>(null, Path.GetFileName(DefaultJsamSettingsPath));
            if (settings != null)
            {
                return settings;
            }

            settings = CreateAsset<JSAMSettings>(DefaultJsamSettingsPath);
            created = settings != null;
            return settings;
        }

        private enum CueAssetKind
        {
            Sound,
            Music
        }

        private enum AudioCuePreset
        {
            UiOneShot,
            WorldOneShot,
            WorldLoop,
            MusicLoop
        }

        private readonly struct AudioCueDescriptor
        {
            public AudioCueDescriptor(string propertyPath, string assetName, CueAssetKind kind, AudioCuePreset preset)
            {
                PropertyPath = propertyPath;
                AssetName = assetName;
                Kind = kind;
                Preset = preset;
            }

            public string PropertyPath { get; }
            public string AssetName { get; }
            public CueAssetKind Kind { get; }
            public AudioCuePreset Preset { get; }
        }
    }
}
