using System.Collections.Generic;
using Minebot.Presentation;
using UnityEditor;
using UnityEngine;

namespace Minebot.Editor
{
    public static class DualGridMigrationTools
    {
        [MenuItem("Minebot/双网格/迁移并清理默认配置")]
        public static void NormalizeDefaultConfiguration()
        {
            MinebotPresentationArtSet artSet = MinebotPixelArtAssetPipeline.EnsureDefaultDualGridConfiguration();
            if (artSet == null)
            {
                Debug.LogWarning("迁移默认双网格配置失败。");
                return;
            }

            bool changed = MinebotConfigAssetUtility.NormalizeDualGridConfiguration(artSet);
            changed |= DeleteDefaultLegacyProfileAsset();

            Debug.Log(changed
                ? $"已迁移并清理默认双网格配置：'{AssetDatabase.GetAssetPath(artSet)}'。"
                : "默认双网格配置已经是清理后的内联结构，无需再次迁移。");
        }

        [MenuItem("Minebot/双网格/迁移并清理所选资源", true)]
        private static bool CanMergeSelectedArtSets()
        {
            return Selection.GetFiltered<MinebotPresentationArtSet>(SelectionMode.Assets).Length > 0
                || Selection.GetFiltered<DualGridTerrainProfile>(SelectionMode.Assets).Length > 0;
        }

        [MenuItem("Minebot/双网格/迁移并清理所选资源")]
        public static void NormalizeSelectedAssets()
        {
            MinebotPresentationArtSet[] artSets = Selection.GetFiltered<MinebotPresentationArtSet>(SelectionMode.Assets);
            DualGridTerrainProfile[] profiles = Selection.GetFiltered<DualGridTerrainProfile>(SelectionMode.Assets);
            if (artSets.Length == 0 && profiles.Length == 0)
            {
                Debug.LogWarning("请至少选择一个双网格相关资源再执行迁移。");
                return;
            }

            var changedAssets = new List<string>(artSets.Length + profiles.Length);
            for (int i = 0; i < artSets.Length; i++)
            {
                MinebotPresentationArtSet artSet = artSets[i];
                if (artSet != null && artSet.NormalizeDualGridConfiguration())
                {
                    EditorUtility.SetDirty(artSet);
                    changedAssets.Add(artSet.name);
                }
            }

            for (int i = 0; i < profiles.Length; i++)
            {
                DualGridTerrainProfile profile = profiles[i];
                if (profile != null && profile.NormalizeLegacyConfiguration())
                {
                    EditorUtility.SetDirty(profile);
                    changedAssets.Add(profile.name);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log(changedAssets.Count > 0
                ? $"已迁移并清理 {changedAssets.Count} 个双网格资源：\n{string.Join("\n", changedAssets)}"
                : "所选资源都已经是清理后的结构，无需再次迁移。");
        }

        [MenuItem("Minebot/双网格/校验所选资源", true)]
        private static bool CanValidateSelectedAssets()
        {
            return Selection.GetFiltered<MinebotPresentationArtSet>(SelectionMode.Assets).Length > 0
                || Selection.GetFiltered<DualGridTerrainProfile>(SelectionMode.Assets).Length > 0;
        }

        [MenuItem("Minebot/双网格/校验所选资源")]
        public static void ValidateSelectedAssets()
        {
            var messages = new List<string>();
            MinebotPresentationArtSet[] artSets = Selection.GetFiltered<MinebotPresentationArtSet>(SelectionMode.Assets);
            DualGridTerrainProfile[] profiles = Selection.GetFiltered<DualGridTerrainProfile>(SelectionMode.Assets);

            foreach (MinebotPresentationArtSet artSet in artSets)
            {
                AppendIssues(artSet.GetDualGridValidationIssues(), artSet.name, messages);
            }

            foreach (DualGridTerrainProfile profile in profiles)
            {
                AppendIssues(profile.GetValidationIssues(), profile.name, messages);
            }

            if (messages.Count == 0)
            {
                Debug.Log("所选双网格资源校验通过，没有发现问题。");
                return;
            }

            Debug.LogWarning($"所选双网格资源发现 {messages.Count} 个问题：\n{string.Join("\n", messages)}");
        }

        private static void AppendIssues(IEnumerable<string> issues, string label, ICollection<string> messages)
        {
            bool hasIssues = false;
            foreach (string issue in issues)
            {
                messages.Add($"{label}：{issue}");
                hasIssues = true;
            }

            if (!hasIssues)
            {
                messages.Add($"{label}：正常");
            }
        }

        private static bool DeleteDefaultLegacyProfileAsset()
        {
            const string defaultProfilePath = "Assets/Resources/Minebot/MinebotDualGridTerrainProfile_Default.asset";
            if (AssetDatabase.LoadAssetAtPath<DualGridTerrainProfile>(defaultProfilePath) == null)
            {
                return false;
            }

            return AssetDatabase.DeleteAsset(defaultProfilePath);
        }
    }
}
