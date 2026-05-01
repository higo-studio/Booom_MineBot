using System;
using System.Collections.Generic;
using System.IO;
using Minebot.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Editor
{
    public static class DualGridMigrationTools
    {
        [MenuItem("Minebot/Dual Grid/Migrate Default Configuration")]
        public static void MigrateDefaultConfiguration()
        {
            MinebotPresentationArtSet artSet = MinebotPixelArtAssetPipeline.EnsureDefaultDualGridConfiguration();
            Debug.Log(
                artSet != null && artSet.DualGridTerrainProfile != null
                    ? $"Migrated default dual-grid configuration to '{AssetDatabase.GetAssetPath(artSet.DualGridTerrainProfile)}'."
                    : "Failed to migrate default dual-grid configuration.");
        }

        [MenuItem("Minebot/Dual Grid/Migrate Selected Art Sets", true)]
        private static bool CanMigrateSelectedArtSets()
        {
            return Selection.GetFiltered<MinebotPresentationArtSet>(SelectionMode.Assets).Length > 0;
        }

        [MenuItem("Minebot/Dual Grid/Migrate Selected Art Sets")]
        public static void MigrateSelectedArtSets()
        {
            MinebotPresentationArtSet[] artSets = Selection.GetFiltered<MinebotPresentationArtSet>(SelectionMode.Assets);
            if (artSets.Length == 0)
            {
                Debug.LogWarning("Select at least one MinebotPresentationArtSet asset to migrate.");
                return;
            }

            var migratedProfiles = new List<string>(artSets.Length);
            foreach (MinebotPresentationArtSet artSet in artSets)
            {
                DualGridTerrainProfile profile = MigrateArtSet(artSet);
                migratedProfiles.Add($"{artSet.name} -> {AssetDatabase.GetAssetPath(profile)}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Migrated {artSets.Length} dual-grid art set(s):\n{string.Join("\n", migratedProfiles)}");
        }

        [MenuItem("Minebot/Dual Grid/Validate Selected Assets", true)]
        private static bool CanValidateSelectedAssets()
        {
            return Selection.GetFiltered<MinebotPresentationArtSet>(SelectionMode.Assets).Length > 0
                || Selection.GetFiltered<DualGridTerrainProfile>(SelectionMode.Assets).Length > 0;
        }

        [MenuItem("Minebot/Dual Grid/Validate Selected Assets")]
        public static void ValidateSelectedAssets()
        {
            var messages = new List<string>();
            MinebotPresentationArtSet[] artSets = Selection.GetFiltered<MinebotPresentationArtSet>(SelectionMode.Assets);
            DualGridTerrainProfile[] profiles = Selection.GetFiltered<DualGridTerrainProfile>(SelectionMode.Assets);

            foreach (MinebotPresentationArtSet artSet in artSets)
            {
                DualGridTerrainProfile profile = artSet.DualGridTerrainProfile;
                if (profile == null)
                {
                    messages.Add($"{artSet.name}: missing dual-grid terrain profile reference.");
                    continue;
                }

                AppendProfileIssues(profile, $"{artSet.name}", messages);
            }

            foreach (DualGridTerrainProfile profile in profiles)
            {
                AppendProfileIssues(profile, profile.name, messages);
            }

            if (messages.Count == 0)
            {
                Debug.Log("Selected dual-grid assets validated without issues.");
                return;
            }

            Debug.LogWarning($"Selected dual-grid assets have {messages.Count} issue(s):\n{string.Join("\n", messages)}");
        }

        private static DualGridTerrainProfile MigrateArtSet(MinebotPresentationArtSet artSet)
        {
            if (artSet == null)
            {
                throw new ArgumentNullException(nameof(artSet));
            }

            DualGridTerrainProfile profile = artSet.DualGridTerrainProfile;
            if (profile == null)
            {
                string profilePath = BuildProfilePath(artSet);
                profile = AssetDatabase.LoadAssetAtPath<DualGridTerrainProfile>(profilePath);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<DualGridTerrainProfile>();
                    AssetDatabase.CreateAsset(profile, profilePath);
                }
            }

            profile.ConfigureLayout(artSet.TerrainLayoutSettings);
            foreach (TerrainRenderLayerId layerId in DualGridTerrain.MaterialFamilies)
            {
                profile.ConfigureFamilyTiles(layerId, CopyTiles(artSet.GetLegacyConfiguredDualGridTiles(layerId)));
            }

            profile.ConfigureLegacyTopology(
                CopyTiles(artSet.GetLegacyConfiguredWallContourTiles()),
                CopyTiles(artSet.GetLegacyConfiguredDangerContourTiles()));
            artSet.AssignDualGridTerrainProfile(profile);
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(artSet);
            return profile;
        }

        private static void AppendProfileIssues(DualGridTerrainProfile profile, string label, ICollection<string> messages)
        {
            bool hasIssues = false;
            foreach (string issue in profile.GetValidationIssues())
            {
                messages.Add($"{label}: {issue}");
                hasIssues = true;
            }

            if (!hasIssues)
            {
                messages.Add($"{label}: OK");
            }
        }

        private static Tile[] CopyTiles(TileBase[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<Tile>();
            }

            var copy = new Tile[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                copy[i] = source[i] as Tile;
            }

            return copy;
        }

        private static string BuildProfilePath(MinebotPresentationArtSet artSet)
        {
            string artSetPath = AssetDatabase.GetAssetPath(artSet);
            string directory = Path.GetDirectoryName(artSetPath) ?? "Assets";
            string fileName = $"{Path.GetFileNameWithoutExtension(artSetPath)}_DualGridTerrainProfile.asset";
            string combinedPath = Path.Combine(directory, fileName).Replace('\\', '/');
            return AssetDatabase.GenerateUniqueAssetPath(combinedPath);
        }
    }
}
