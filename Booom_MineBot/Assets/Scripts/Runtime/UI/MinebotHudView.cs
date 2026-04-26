using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minebot.UI
{
    public sealed class MinebotHudView : MonoBehaviour
    {
        public const string RootName = "Minebot HUD";
        public const string ResourcePath = MinebotHudDefaults.RootResourcePath;
        public const string PrefabAssetPath = MinebotHudDefaults.RootAssetPath;
        public const string UpgradePanelName = MinebotHudDefaults.UpgradePanelObjectName;
        public const string BuildPanelName = MinebotHudDefaults.BuildPanelObjectName;
        public const string BuildingInteractionPanelName = MinebotHudDefaults.BuildingInteractionPanelObjectName;
        public const string RepairStationInteractionButtonName = "Repair Station Interaction Button";
        public const string RobotFactoryInteractionButtonName = "Robot Factory Interaction Button";

        public const string StatusSlotName = "Status Panel Slot";
        public const string InteractionSlotName = "Interaction Panel Slot";
        public const string FeedbackSlotName = "Feedback Panel Slot";
        public const string WarningSlotName = "Warning Panel Slot";
        public const string GameOverSlotName = "Game Over Panel Slot";
        public const string UpgradeSlotName = "Upgrade Panel Slot";
        public const string BuildSlotName = "Build Panel Slot";
        public const string BuildingInteractionSlotName = "Building Interaction Panel Slot";

        [SerializeField]
        private RectTransform statusSlot;

        [SerializeField]
        private RectTransform interactionSlot;

        [SerializeField]
        private RectTransform feedbackSlot;

        [SerializeField]
        private RectTransform warningSlot;

        [SerializeField]
        private RectTransform gameOverSlot;

        [SerializeField]
        private RectTransform upgradeSlot;

        [SerializeField]
        private RectTransform buildSlot;

        [SerializeField]
        private RectTransform buildingInteractionSlot;

        [SerializeField]
        private MinebotHudTextPanelView statusPanel;

        [SerializeField]
        private MinebotHudTextPanelView interactionPanel;

        [SerializeField]
        private MinebotHudTextPanelView feedbackPanel;

        [SerializeField]
        private MinebotHudTextPanelView warningPanel;

        [SerializeField]
        private MinebotHudTextPanelView gameOverPanel;

        [SerializeField]
        private MinebotHudOptionPanelView upgradePanel;

        [SerializeField]
        private MinebotHudOptionPanelView buildPanel;

        [SerializeField]
        private MinebotHudOptionPanelView buildingInteractionPanel;

        public MinebotHudTextPanelView StatusPanel => statusPanel;
        public MinebotHudTextPanelView InteractionPanel => interactionPanel;
        public MinebotHudTextPanelView FeedbackPanel => feedbackPanel;
        public MinebotHudTextPanelView WarningPanel => warningPanel;
        public MinebotHudTextPanelView GameOverPanel => gameOverPanel;
        public MinebotHudOptionPanelView UpgradePanel => upgradePanel;
        public MinebotHudOptionPanelView BuildPanel => buildPanel;
        public MinebotHudOptionPanelView BuildingInteractionPanel => buildingInteractionPanel;

        public Button RepairStationInteractionButton => buildingInteractionPanel != null ? buildingInteractionPanel.GetButton(0) : null;
        public Button RobotFactoryInteractionButton => buildingInteractionPanel != null ? buildingInteractionPanel.GetButton(1) : null;

        public void EnsureShell(int buildButtonCount)
        {
            MinebotHudUiFactory.ConfigureCanvasRoot(gameObject);
            statusSlot = MinebotHudUiFactory.EnsureSlot(ref statusSlot, transform, StatusSlotName, MinebotHudDefaults.StatusSlot);
            interactionSlot = MinebotHudUiFactory.EnsureSlot(ref interactionSlot, transform, InteractionSlotName, MinebotHudDefaults.InteractionSlot);
            feedbackSlot = MinebotHudUiFactory.EnsureSlot(ref feedbackSlot, transform, FeedbackSlotName, MinebotHudDefaults.FeedbackSlot);
            warningSlot = MinebotHudUiFactory.EnsureSlot(ref warningSlot, transform, WarningSlotName, MinebotHudDefaults.WarningSlot);
            gameOverSlot = MinebotHudUiFactory.EnsureSlot(ref gameOverSlot, transform, GameOverSlotName, MinebotHudDefaults.GameOverSlot);
            upgradeSlot = MinebotHudUiFactory.EnsureSlot(ref upgradeSlot, transform, UpgradeSlotName, MinebotHudDefaults.UpgradeSlot);
            buildSlot = MinebotHudUiFactory.EnsureSlot(ref buildSlot, transform, BuildSlotName, MinebotHudDefaults.BuildSlot(buildButtonCount));
            buildingInteractionSlot = MinebotHudUiFactory.EnsureSlot(ref buildingInteractionSlot, transform, BuildingInteractionSlotName, MinebotHudDefaults.BuildingInteractionSlot);
        }

        public void EnsureDefaultStructure(TMP_FontAsset runtimeFontAsset, int buildButtonCount)
        {
            EnsureShell(buildButtonCount);

            statusPanel = EnsureTextPanel(statusPanel, statusSlot, MinebotHudDefaults.StatusPanelObjectName, MinebotHudDefaults.StatusPanelResourcePath, runtimeFontAsset, MinebotHudDefaults.StatusText);
            interactionPanel = EnsureTextPanel(interactionPanel, interactionSlot, MinebotHudDefaults.InteractionPanelObjectName, MinebotHudDefaults.InteractionPanelResourcePath, runtimeFontAsset, MinebotHudDefaults.InteractionText);
            feedbackPanel = EnsureTextPanel(feedbackPanel, feedbackSlot, MinebotHudDefaults.FeedbackPanelObjectName, MinebotHudDefaults.FeedbackPanelResourcePath, runtimeFontAsset, MinebotHudDefaults.FeedbackText);
            warningPanel = EnsureTextPanel(warningPanel, warningSlot, MinebotHudDefaults.WarningPanelObjectName, MinebotHudDefaults.WarningPanelResourcePath, runtimeFontAsset, MinebotHudDefaults.WarningText);
            gameOverPanel = EnsureTextPanel(gameOverPanel, gameOverSlot, MinebotHudDefaults.GameOverPanelObjectName, MinebotHudDefaults.GameOverPanelResourcePath, runtimeFontAsset, MinebotHudDefaults.GameOverText);

            upgradePanel = EnsureOptionPanel(upgradePanel, upgradeSlot, MinebotHudDefaults.UpgradePanelObjectName, MinebotHudDefaults.UpgradePanelResourcePath, runtimeFontAsset, MinebotHudDefaults.UpgradeButtonCount, MinebotHudDefaults.UpgradeOptions, MinebotHudDefaults.UpgradeTitle);
            buildPanel = EnsureOptionPanel(buildPanel, buildSlot, MinebotHudDefaults.BuildPanelObjectName, MinebotHudDefaults.BuildPanelResourcePath, runtimeFontAsset, Mathf.Max(MinebotHudDefaults.MinimumBuildButtonCount, buildButtonCount), MinebotHudDefaults.BuildOptions, MinebotHudDefaults.BuildTitle);
            buildingInteractionPanel = EnsureOptionPanel(buildingInteractionPanel, buildingInteractionSlot, MinebotHudDefaults.BuildingInteractionPanelObjectName, MinebotHudDefaults.BuildingInteractionPanelResourcePath, runtimeFontAsset, MinebotHudDefaults.BuildingInteractionButtonCount, MinebotHudDefaults.BuildingInteractionOptions, MinebotHudDefaults.BuildingInteractionTitle);
            RenameButton(buildingInteractionPanel, 0, RepairStationInteractionButtonName);
            RenameButton(buildingInteractionPanel, 1, RobotFactoryInteractionButtonName);
        }

        public void BindUpgradeButtons(Action<int> onClick)
        {
            if (upgradePanel != null)
            {
                upgradePanel.BindButtons(onClick);
            }
        }

        public void BindBuildButtons(Action<int> onClick)
        {
            if (buildPanel != null)
            {
                buildPanel.BindButtons(onClick);
            }
        }

        public void BindBuildingInteractionButtons(Action repair, Action produceRobot)
        {
            if (buildingInteractionPanel == null)
            {
                return;
            }

            buildingInteractionPanel.BindButton(0, repair);
            buildingInteractionPanel.BindButton(1, produceRobot);
        }

        private static MinebotHudTextPanelView EnsureTextPanel(MinebotHudTextPanelView current, RectTransform slot, string objectName, string resourcePath, TMP_FontAsset runtimeFontAsset, MinebotHudDefaults.TextPanelLayout layout)
        {
            MinebotHudTextPanelView panel = ResolvePanel(current, slot, objectName, resourcePath);
            panel.EnsureDefaultStructure(runtimeFontAsset, layout);
            return panel;
        }

        private static MinebotHudOptionPanelView EnsureOptionPanel(MinebotHudOptionPanelView current, RectTransform slot, string objectName, string resourcePath, TMP_FontAsset runtimeFontAsset, int buttonCount, MinebotHudDefaults.OptionPanelLayout layout, string defaultTitle)
        {
            MinebotHudOptionPanelView panel = ResolvePanel(current, slot, objectName, resourcePath);
            panel.EnsureDefaultStructure(runtimeFontAsset, buttonCount, layout);
            panel.SetTitle(defaultTitle);
            return panel;
        }

        private static void RenameButton(MinebotHudOptionPanelView panel, int index, string objectName)
        {
            Button button = panel != null ? panel.GetButton(index) : null;
            if (button != null)
            {
                button.name = objectName;
            }
        }

        private static T ResolvePanel<T>(T current, RectTransform slot, string objectName, string resourcePath) where T : Component
        {
            if (current != null)
            {
                return current;
            }

            T existing = slot.GetComponentInChildren<T>(true);
            if (existing != null)
            {
                existing.name = objectName;
                return existing;
            }

            T prefab = Resources.Load<T>(resourcePath);
            if (prefab != null)
            {
                T instance = Instantiate(prefab, slot, false);
                instance.name = objectName;
                return instance;
            }

            var fallback = new GameObject(objectName, typeof(RectTransform), typeof(T)).GetComponent<T>();
            fallback.transform.SetParent(slot, false);
            return fallback;
        }

    }
}
