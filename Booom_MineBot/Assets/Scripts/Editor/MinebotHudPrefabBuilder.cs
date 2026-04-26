using System.IO;
using Minebot.UI;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UI;

namespace Minebot.Editor
{
    [InitializeOnLoad]
    public static class MinebotHudPrefabBuilder
    {
        private const string MenuPath = "Minebot/UI/Rebuild HUD Prefabs";

        static MinebotHudPrefabBuilder()
        {
            QueueEnsurePrefabs();
        }

        [DidReloadScripts]
        private static void EnsurePrefabsAfterScriptsReload()
        {
            QueueEnsurePrefabs();
        }

        [MenuItem(MenuPath)]
        public static void CreateOrUpdatePrefabs()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Minebot");
            EnsureFolder("Assets/Resources/Minebot/UI");
            EnsureFolder(MinebotHudDefaults.PanelFolderAssetPath);

            CreateTextPanelPrefab(MinebotHudDefaults.StatusPanelAssetPath, MinebotHudDefaults.StatusPanelObjectName, MinebotHudDefaults.StatusText);
            CreateTextPanelPrefab(MinebotHudDefaults.InteractionPanelAssetPath, MinebotHudDefaults.InteractionPanelObjectName, MinebotHudDefaults.InteractionText);
            CreateTextPanelPrefab(MinebotHudDefaults.FeedbackPanelAssetPath, MinebotHudDefaults.FeedbackPanelObjectName, MinebotHudDefaults.FeedbackText);
            CreateTextPanelPrefab(MinebotHudDefaults.WarningPanelAssetPath, MinebotHudDefaults.WarningPanelObjectName, MinebotHudDefaults.WarningText);
            CreateTextPanelPrefab(MinebotHudDefaults.GameOverPanelAssetPath, MinebotHudDefaults.GameOverPanelObjectName, MinebotHudDefaults.GameOverText);

            CreateOptionPanelPrefab(MinebotHudDefaults.UpgradePanelAssetPath, MinebotHudDefaults.UpgradePanelObjectName, MinebotHudDefaults.UpgradeOptions, MinebotHudDefaults.UpgradeButtonCount, MinebotHudDefaults.UpgradeTitle);
            CreateOptionPanelPrefab(MinebotHudDefaults.BuildPanelAssetPath, MinebotHudDefaults.BuildPanelObjectName, MinebotHudDefaults.BuildOptions, MinebotHudDefaults.MinimumBuildButtonCount, MinebotHudDefaults.BuildTitle);
            CreateOptionPanelPrefab(MinebotHudDefaults.BuildingInteractionPanelAssetPath, MinebotHudDefaults.BuildingInteractionPanelObjectName, MinebotHudDefaults.BuildingInteractionOptions, MinebotHudDefaults.BuildingInteractionButtonCount, MinebotHudDefaults.BuildingInteractionTitle);

            CreateHudShellPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void QueueEnsurePrefabs()
        {
            EditorApplication.delayCall -= EnsurePrefabsExist;
            EditorApplication.delayCall += EnsurePrefabsExist;
        }

        private static void EnsurePrefabsExist()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                QueueEnsurePrefabs();
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudView.PrefabAssetPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudDefaults.StatusPanelAssetPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(MinebotHudDefaults.UpgradePanelAssetPath) != null)
            {
                return;
            }

            CreateOrUpdatePrefabs();
        }

        private static void CreateTextPanelPrefab(string assetPath, string objectName, MinebotHudDefaults.TextPanelLayout layout)
        {
            GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(MinebotHudTextPanelView));
            try
            {
                MinebotHudTextPanelView view = root.GetComponent<MinebotHudTextPanelView>();
                view.EnsureDefaultStructure(MinebotHudFontUtility.GetDefaultFontAsset(), layout);
                PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateOptionPanelPrefab(string assetPath, string objectName, MinebotHudDefaults.OptionPanelLayout layout, int buttonCount, string defaultTitle)
        {
            GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(MinebotHudOptionPanelView));
            try
            {
                MinebotHudOptionPanelView view = root.GetComponent<MinebotHudOptionPanelView>();
                view.EnsureDefaultStructure(MinebotHudFontUtility.GetDefaultFontAsset(), buttonCount, layout);
                view.SetTitle(defaultTitle);
                PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateHudShellPrefab()
        {
            GameObject root = new GameObject(MinebotHudView.RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MinebotHudView));
            try
            {
                MinebotHudView view = root.GetComponent<MinebotHudView>();
                view.EnsureShell(MinebotHudDefaults.MinimumBuildButtonCount);
                ApplyRootRect(root.GetComponent<RectTransform>());
                PrefabUtility.SaveAsPrefabAsset(root, MinebotHudView.PrefabAssetPath);
                NormalizeSavedPrefabRoot();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void NormalizeSavedPrefabRoot()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MinebotHudView.PrefabAssetPath);
            try
            {
                ApplyRootRect(prefabRoot.GetComponent<RectTransform>());
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, MinebotHudView.PrefabAssetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void ApplyRootRect(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
            EditorUtility.SetDirty(rect);
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(assetFolder)?.Replace("\\", "/");
            string name = Path.GetFileName(assetFolder);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
