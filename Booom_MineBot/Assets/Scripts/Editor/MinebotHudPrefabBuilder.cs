using System.IO;
using Minebot.Presentation;
using Minebot.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Minebot.Editor
{
    public static class MinebotHudPrefabBuilder
    {
        private const string MenuPath = "Minebot/界面/重建界面预制体";
        private const string DefaultArtSetPath = "Assets/Resources/Minebot/MinebotPresentationArtSet_Default.asset";
        private const string HudTemplateAssetPath = "Assets/Resources/Minebot/UI/HUD.prefab";
        private const string HudBackgroundPath = "Assets/Art/Minebot/Sprites/UI/HUD/hud_panel_background.png";
        private const string HudStatusIconPath = "Assets/Art/Minebot/Sprites/UI/HUD/hud_icon_status.png";
        private const string HudInteractionIconPath = "Assets/Art/Minebot/Sprites/UI/HUD/hud_icon_interaction.png";
        private const string HudFeedbackIconPath = "Assets/Art/Minebot/Sprites/UI/HUD/hud_icon_feedback.png";
        private const string HudWarningIconPath = "Assets/Art/Minebot/Sprites/UI/HUD/hud_icon_warning.png";
        private const string HudUpgradeIconPath = "Assets/Art/Minebot/Sprites/UI/HUD/hud_icon_upgrade.png";
        private const string HudBuildIconPath = "Assets/Art/Minebot/Sprites/UI/HUD/hud_icon_build.png";
        private const string HudBuildingInteractionIconPath = "Assets/Art/Minebot/Sprites/UI/HUD/hud_icon_building_interaction.png";

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
            CreateMinimapPanelPrefab(MinebotHudDefaults.MinimapPanelAssetPath, MinebotHudDefaults.MinimapPanelObjectName, MinebotHudDefaults.MinimapPanel);

            CreateOptionPanelPrefab(MinebotHudDefaults.UpgradePanelAssetPath, MinebotHudDefaults.UpgradePanelObjectName, MinebotHudDefaults.UpgradeOptions, MinebotHudDefaults.UpgradeButtonCount, MinebotHudDefaults.UpgradeTitle);
            CreateOptionPanelPrefab(MinebotHudDefaults.BuildPanelAssetPath, MinebotHudDefaults.BuildPanelObjectName, MinebotHudDefaults.BuildOptions, MinebotHudDefaults.MinimumBuildButtonCount, MinebotHudDefaults.BuildTitle);
            CreateOptionPanelPrefab(MinebotHudDefaults.BuildingInteractionPanelAssetPath, MinebotHudDefaults.BuildingInteractionPanelObjectName, MinebotHudDefaults.BuildingInteractionOptions, MinebotHudDefaults.BuildingInteractionButtonCount, MinebotHudDefaults.BuildingInteractionTitle);

            CreateMainUiPrefab();
            UpdateDefaultHudReference();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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

        private static void CreateMinimapPanelPrefab(string assetPath, string objectName, MinebotHudDefaults.MinimapPanelLayout layout)
        {
            GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(MinebotHudMinimapPanelView));
            try
            {
                MinebotHudMinimapPanelView view = root.GetComponent<MinebotHudMinimapPanelView>();
                view.EnsureDefaultStructure(MinebotHudFontUtility.GetDefaultFontAsset(), layout);
                PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateMainUiPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HudTemplateAssetPath) != null)
            {
                GameObject templateRoot = PrefabUtility.LoadPrefabContents(HudTemplateAssetPath);
                try
                {
                    templateRoot.name = MinebotHudView.RootName;
                    GetOrAdd<MinebotHudView>(templateRoot);
                    ConfigureCanvasRoot(templateRoot);
                    ApplyRootRect(templateRoot.GetComponent<RectTransform>());
                    PrefabUtility.SaveAsPrefabAsset(templateRoot, MinebotHudView.PrefabAssetPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(templateRoot);
                }

                return;
            }

            GameObject root = new GameObject(MinebotHudView.RootName, typeof(RectTransform));
            try
            {
                GetOrAdd<MinebotHudView>(root);
                ConfigureCanvasRoot(root);
                ApplyRootRect(root.GetComponent<RectTransform>());
                PrefabUtility.SaveAsPrefabAsset(root, MinebotHudView.PrefabAssetPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void UpdateDefaultHudReference()
        {
            MinebotPresentationArtSet artSet = AssetDatabase.LoadAssetAtPath<MinebotPresentationArtSet>(DefaultArtSetPath);
            if (artSet == null)
            {
                return;
            }

            artSet.ConfigureHudPresentation(
                AssetDatabase.LoadAssetAtPath<MinebotHudView>(MinebotHudView.PrefabAssetPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(HudBackgroundPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(HudStatusIconPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(HudInteractionIconPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(HudFeedbackIconPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(HudWarningIconPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(HudUpgradeIconPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(HudBuildIconPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(HudBuildingInteractionIconPath));
            EditorUtility.SetDirty(artSet);
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

        private static void ConfigureCanvasRoot(GameObject root)
        {
            Canvas canvas = GetOrAdd<Canvas>(root);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = GetOrAdd<CanvasScaler>(root);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            GetOrAdd<GraphicRaycaster>(root);
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
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
