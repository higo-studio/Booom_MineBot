using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minebot.UI
{
    public sealed class MinebotHudView : MonoBehaviour
    {
        private const string TemplateUpperLeftPath = "Upper Left";
        private const string TemplateUpperCenterPath = "Upper Center";
        private const string TemplateLowerLeftPath = "Lower Left";
        private const string TemplateLowerRightPath = "Lower Right";
        private const string TemplateWorkingCountPath = "Upper Left/BotState/BotOnWorking/Count";
        private const string TemplateWaitingCountPath = "Upper Left/BotState/BotAtRest/Count";
        private const string TemplateWaveTextPath = "Upper Center/WaveText";
        private const string TemplateWaveTimerPath = "Upper Center/TimeText";
        private const string TemplateWaveFillPath = "Upper Center/ProcessFill";
        private const string TemplateEnergyCountPath = "Lower Left/Resources/Power/Count";
        private const string TemplateMetalCountPath = "Lower Left/Resources/Metal/Count";
        private const string TemplateExpFillPath = "Lower Left/ExpFill";
        private const string TemplateHealth0Path = "Lower Left/HPLayout/HP";
        private const string TemplateHealth1Path = "Lower Left/HPLayout/HP (1)";
        private const string TemplateHealth2Path = "Lower Left/HPLayout/HP (2)";
        private const string TemplateBuildButtonPath = "Lower Right/Layout/Building";
        private const string TemplateBotButtonPath = "Lower Right/Layout/Bot";
        private const string TemplateMarkerButtonPath = "Lower Right/Layout/Mark";
        private const string TemplateScanButtonPath = "Lower Right/Layout/Radar";
        private static readonly Color TemplateButtonNormalColor = new(0f, 1f, 0.98f, 1f);
        private static readonly Color TemplateButtonSelectedColor = new(1f, 0.93f, 0.17f, 1f);
        private static readonly Color TemplateHealthInactiveColor = new(0.2f, 0.23f, 0.24f, 0.42f);

        public const string RootName = "MainUI";
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
        public const string MinimapSlotName = "Minimap Panel Slot";
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
        private RectTransform minimapSlot;

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
        private MinebotHudMinimapPanelView minimapPanel;

        [SerializeField]
        private MinebotHudOptionPanelView upgradePanel;

        [SerializeField]
        private MinebotHudOptionPanelView buildPanel;

        [SerializeField]
        private MinebotHudOptionPanelView buildingInteractionPanel;

        private bool usingTemplateHud;
        private TMP_Text templateWorkingCountText;
        private TMP_Text templateWaitingCountText;
        private TMP_Text templateWaveText;
        private TMP_Text templateWaveTimerText;
        private TMP_Text templateEnergyCountText;
        private TMP_Text templateMetalCountText;
        private Image templateWaveFillImage;
        private Image templateExpFillImage;
        private Image[] templateHealthImages = Array.Empty<Image>();
        private Button[] templateActionButtons = Array.Empty<Button>();
        private TMP_Text[] templateActionNameTexts = Array.Empty<TMP_Text>();
        private TMP_Text[] templateActionKeyTexts = Array.Empty<TMP_Text>();
        private Image[] templateActionKeyImages = Array.Empty<Image>();

        public MinebotHudTextPanelView StatusPanel => statusPanel;
        public MinebotHudTextPanelView InteractionPanel => interactionPanel;
        public MinebotHudTextPanelView FeedbackPanel => feedbackPanel;
        public MinebotHudTextPanelView WarningPanel => warningPanel;
        public MinebotHudTextPanelView GameOverPanel => gameOverPanel;
        public MinebotHudMinimapPanelView MinimapPanel => minimapPanel;
        public MinebotHudOptionPanelView UpgradePanel => upgradePanel;
        public MinebotHudOptionPanelView BuildPanel => buildPanel;
        public MinebotHudOptionPanelView BuildingInteractionPanel => buildingInteractionPanel;
        public bool UsesTemplateHud => usingTemplateHud;

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
            minimapSlot = MinebotHudUiFactory.EnsureSlot(ref minimapSlot, transform, MinimapSlotName, MinebotHudDefaults.MinimapSlot);
            upgradeSlot = MinebotHudUiFactory.EnsureSlot(ref upgradeSlot, transform, UpgradeSlotName, MinebotHudDefaults.UpgradeSlot);
            buildSlot = MinebotHudUiFactory.EnsureSlot(ref buildSlot, transform, BuildSlotName, MinebotHudDefaults.BuildSlot(buildButtonCount));
            buildingInteractionSlot = MinebotHudUiFactory.EnsureSlot(ref buildingInteractionSlot, transform, BuildingInteractionSlotName, MinebotHudDefaults.BuildingInteractionSlot);
        }

        public void EnsureDefaultStructure(TMP_FontAsset runtimeFontAsset, int buildButtonCount)
        {
            MinebotHudUiFactory.ConfigureCanvasRoot(gameObject);
            usingTemplateHud = BindTemplateHud();
            if (usingTemplateHud)
            {
                ApplyTemplateFont(runtimeFontAsset);
                EnsureTemplateOverlayStructure(runtimeFontAsset);
                HideLegacyPanel(statusPanel);
                HideLegacyPanel(interactionPanel);
                HideLegacyPanel(feedbackPanel);
                HideLegacyPanel(warningPanel);
                HideLegacyPanel(minimapPanel);
                HideLegacyPanel(buildPanel);
                return;
            }

            EnsureShell(buildButtonCount);

            statusPanel = EnsureTextPanel(statusPanel, statusSlot, MinebotHudDefaults.StatusPanelObjectName, MinebotHudDefaults.StatusPanelResourcePath, runtimeFontAsset, MinebotHudDefaults.StatusText);
            interactionPanel = EnsureTextPanel(interactionPanel, interactionSlot, MinebotHudDefaults.InteractionPanelObjectName, MinebotHudDefaults.InteractionPanelResourcePath, runtimeFontAsset, MinebotHudDefaults.InteractionText);
            feedbackPanel = EnsureTextPanel(feedbackPanel, feedbackSlot, MinebotHudDefaults.FeedbackPanelObjectName, MinebotHudDefaults.FeedbackPanelResourcePath, runtimeFontAsset, MinebotHudDefaults.FeedbackText);
            warningPanel = EnsureTextPanel(warningPanel, warningSlot, MinebotHudDefaults.WarningPanelObjectName, MinebotHudDefaults.WarningPanelResourcePath, runtimeFontAsset, MinebotHudDefaults.WarningText);
            gameOverPanel = EnsureTextPanel(gameOverPanel, gameOverSlot, MinebotHudDefaults.GameOverPanelObjectName, MinebotHudDefaults.GameOverPanelResourcePath, runtimeFontAsset, MinebotHudDefaults.GameOverText);
            minimapPanel = EnsureMinimapPanel(minimapPanel, minimapSlot, MinebotHudDefaults.MinimapPanelObjectName, MinebotHudDefaults.MinimapPanelResourcePath, runtimeFontAsset, MinebotHudDefaults.MinimapPanel);

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
            if (usingTemplateHud)
            {
                for (int i = 0; i < templateActionButtons.Length; i++)
                {
                    Button button = templateActionButtons[i];
                    if (button == null)
                    {
                        continue;
                    }

                    int capturedIndex = i;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => onClick?.Invoke(capturedIndex));
                }
            }

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

        public void ApplyGraphics(
            Sprite panelBackground,
            Sprite statusIcon,
            Sprite interactionIcon,
            Sprite feedbackIcon,
            Sprite warningIcon,
            Sprite upgradeIcon,
            Sprite buildIcon,
            Sprite buildingInteractionIcon)
        {
            statusPanel?.ApplyGraphics(panelBackground, statusIcon);
            interactionPanel?.ApplyGraphics(panelBackground, interactionIcon);
            feedbackPanel?.ApplyGraphics(panelBackground, feedbackIcon);
            warningPanel?.ApplyGraphics(panelBackground, warningIcon);
            gameOverPanel?.ApplyGraphics(panelBackground, warningIcon);
            minimapPanel?.ApplyGraphics(panelBackground);
            upgradePanel?.ApplyGraphics(panelBackground, upgradeIcon);
            buildPanel?.ApplyGraphics(panelBackground, buildIcon);
            buildingInteractionPanel?.ApplyGraphics(panelBackground, buildingInteractionIcon);
        }

        public void UpdateTemplateRobotStatus(int working, int waiting)
        {
            if (!usingTemplateHud)
            {
                return;
            }

            SetText(templateWorkingCountText, $"{Mathf.Max(0, working)}");
            SetText(templateWaitingCountText, $"{Mathf.Max(0, waiting)}");
        }

        public void UpdateTemplateResources(int metal, int energy, int currentHealth, int maxHealth, int experience, int nextThreshold)
        {
            if (!usingTemplateHud)
            {
                return;
            }

            SetText(templateEnergyCountText, Mathf.Max(0, energy).ToString("000"));
            SetText(templateMetalCountText, Mathf.Max(0, metal).ToString("000"));

            int maxHearts = Mathf.Clamp(maxHealth, 0, templateHealthImages.Length);
            int activeHearts = Mathf.Clamp(currentHealth, 0, maxHearts);
            for (int i = 0; i < templateHealthImages.Length; i++)
            {
                if (templateHealthImages[i] != null)
                {
                    templateHealthImages[i].enabled = i < maxHearts;
                    templateHealthImages[i].color = i < activeHearts ? Color.white : TemplateHealthInactiveColor;
                }
            }

            if (templateExpFillImage != null)
            {
                float fill = nextThreshold > 0 ? Mathf.Clamp01((float)Mathf.Max(0, experience) / nextThreshold) : 0f;
                templateExpFillImage.fillAmount = fill;
            }
        }

        public void UpdateTemplateWaveStatus(int displayWave, float timeUntilNextWave, float waveInterval)
        {
            if (!usingTemplateHud)
            {
                return;
            }

            SetText(templateWaveText, $"WAVE {Mathf.Max(1, displayWave)}");
            SetText(templateWaveTimerText, FormatWaveTimer(timeUntilNextWave));
            if (templateWaveFillImage != null)
            {
                float interval = waveInterval > 0.01f ? waveInterval : 1f;
                templateWaveFillImage.fillAmount = Mathf.Clamp01(1f - Mathf.Clamp(timeUntilNextWave, 0f, interval) / interval);
                templateWaveFillImage.color = Color.white;
            }
        }

        public void SetTemplateBuildButton(int index, bool visible, string label, bool selected)
        {
            if (!usingTemplateHud || index < 0 || index >= templateActionButtons.Length)
            {
                return;
            }

            Button button = templateActionButtons[index];
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            ParseTemplateButtonLabel(label, index, out string keyText, out string nameText);
            SetText(templateActionKeyTexts[index], keyText);
            SetText(templateActionNameTexts[index], nameText);

            Color textColor = selected ? TemplateButtonSelectedColor : TemplateButtonNormalColor;
            SetColor(templateActionKeyTexts[index], textColor);
            SetColor(templateActionNameTexts[index], textColor);
            if (templateActionKeyImages[index] != null)
            {
                templateActionKeyImages[index].color = selected ? new Color(1f, 0.95f, 0.34f, 1f) : Color.white;
            }
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

        private static MinebotHudMinimapPanelView EnsureMinimapPanel(MinebotHudMinimapPanelView current, RectTransform slot, string objectName, string resourcePath, TMP_FontAsset runtimeFontAsset, MinebotHudDefaults.MinimapPanelLayout layout)
        {
            MinebotHudMinimapPanelView panel = ResolvePanel(current, slot, objectName, resourcePath);
            panel.EnsureDefaultStructure(runtimeFontAsset, layout);
            return panel;
        }

        private void EnsureTemplateOverlayStructure(TMP_FontAsset runtimeFontAsset)
        {
            gameOverSlot = MinebotHudUiFactory.EnsureSlot(ref gameOverSlot, transform, GameOverSlotName, MinebotHudDefaults.GameOverSlot);
            upgradeSlot = MinebotHudUiFactory.EnsureSlot(ref upgradeSlot, transform, UpgradeSlotName, MinebotHudDefaults.UpgradeSlot);
            buildingInteractionSlot = MinebotHudUiFactory.EnsureSlot(ref buildingInteractionSlot, transform, BuildingInteractionSlotName, MinebotHudDefaults.BuildingInteractionSlot);

            gameOverPanel = EnsureTextPanel(gameOverPanel, gameOverSlot, MinebotHudDefaults.GameOverPanelObjectName, MinebotHudDefaults.GameOverPanelResourcePath, runtimeFontAsset, MinebotHudDefaults.GameOverText);
            upgradePanel = EnsureOptionPanel(upgradePanel, upgradeSlot, MinebotHudDefaults.UpgradePanelObjectName, MinebotHudDefaults.UpgradePanelResourcePath, runtimeFontAsset, MinebotHudDefaults.UpgradeButtonCount, MinebotHudDefaults.UpgradeOptions, MinebotHudDefaults.UpgradeTitle);
            buildingInteractionPanel = EnsureOptionPanel(buildingInteractionPanel, buildingInteractionSlot, MinebotHudDefaults.BuildingInteractionPanelObjectName, MinebotHudDefaults.BuildingInteractionPanelResourcePath, runtimeFontAsset, MinebotHudDefaults.BuildingInteractionButtonCount, MinebotHudDefaults.BuildingInteractionOptions, MinebotHudDefaults.BuildingInteractionTitle);
            RenameButton(buildingInteractionPanel, 0, RepairStationInteractionButtonName);
            RenameButton(buildingInteractionPanel, 1, RobotFactoryInteractionButtonName);
        }

        private bool BindTemplateHud()
        {
            Transform upperLeft = transform.Find(TemplateUpperLeftPath);
            Transform upperCenter = transform.Find(TemplateUpperCenterPath);
            Transform lowerLeft = transform.Find(TemplateLowerLeftPath);
            Transform lowerRight = transform.Find(TemplateLowerRightPath);
            if (upperLeft == null || upperCenter == null || lowerLeft == null || lowerRight == null)
            {
                templateActionButtons = Array.Empty<Button>();
                templateActionNameTexts = Array.Empty<TMP_Text>();
                templateActionKeyTexts = Array.Empty<TMP_Text>();
                templateActionKeyImages = Array.Empty<Image>();
                templateHealthImages = Array.Empty<Image>();
                return false;
            }

            templateWorkingCountText = FindComponent<TMP_Text>(TemplateWorkingCountPath);
            templateWaitingCountText = FindComponent<TMP_Text>(TemplateWaitingCountPath);
            templateWaveText = FindComponent<TMP_Text>(TemplateWaveTextPath);
            templateWaveTimerText = FindComponent<TMP_Text>(TemplateWaveTimerPath);
            templateWaveFillImage = FindComponent<Image>(TemplateWaveFillPath);
            templateEnergyCountText = FindComponent<TMP_Text>(TemplateEnergyCountPath);
            templateMetalCountText = FindComponent<TMP_Text>(TemplateMetalCountPath);
            templateExpFillImage = FindComponent<Image>(TemplateExpFillPath);
            templateHealthImages = new[]
            {
                FindComponent<Image>(TemplateHealth0Path),
                FindComponent<Image>(TemplateHealth1Path),
                FindComponent<Image>(TemplateHealth2Path)
            };

            templateActionButtons = new[]
            {
                EnsureTemplateButton(TemplateBuildButtonPath),
                EnsureTemplateButton(TemplateBotButtonPath),
                EnsureTemplateButton(TemplateMarkerButtonPath),
                EnsureTemplateButton(TemplateScanButtonPath)
            };
            templateActionNameTexts = new[]
            {
                FindComponent<TMP_Text>($"{TemplateBuildButtonPath}/Name"),
                FindComponent<TMP_Text>($"{TemplateBotButtonPath}/Name"),
                FindComponent<TMP_Text>($"{TemplateMarkerButtonPath}/Name"),
                FindComponent<TMP_Text>($"{TemplateScanButtonPath}/Name")
            };
            templateActionKeyTexts = new[]
            {
                FindComponent<TMP_Text>($"{TemplateBuildButtonPath}/Key/Text (TMP)"),
                FindComponent<TMP_Text>($"{TemplateBotButtonPath}/Key/Text (TMP)"),
                FindComponent<TMP_Text>($"{TemplateMarkerButtonPath}/Key/Text (TMP)"),
                FindComponent<TMP_Text>($"{TemplateScanButtonPath}/Key/Text (TMP)")
            };
            templateActionKeyImages = new[]
            {
                FindComponent<Image>($"{TemplateBuildButtonPath}/Key"),
                FindComponent<Image>($"{TemplateBotButtonPath}/Key"),
                FindComponent<Image>($"{TemplateMarkerButtonPath}/Key"),
                FindComponent<Image>($"{TemplateScanButtonPath}/Key")
            };

            return templateActionButtons.Length == MinebotHudDefaults.MinimumBuildButtonCount;
        }

        private void ApplyTemplateFont(TMP_FontAsset runtimeFontAsset)
        {
            if (runtimeFontAsset == null)
            {
                return;
            }

            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null)
                {
                    continue;
                }

                text.font = runtimeFontAsset;
                if (runtimeFontAsset.material != null)
                {
                    text.fontSharedMaterial = runtimeFontAsset.material;
                }
            }
        }

        private Button EnsureTemplateButton(string path)
        {
            Transform target = transform.Find(path);
            if (target == null)
            {
                return null;
            }

            Image hitImage = MinebotHudUiFactory.GetOrAdd<Image>(target.gameObject);
            hitImage.color = new Color(1f, 1f, 1f, 0f);
            hitImage.raycastTarget = true;

            Button button = MinebotHudUiFactory.GetOrAdd<Button>(target.gameObject);
            button.targetGraphic = hitImage;
            button.transition = Selectable.Transition.None;
            return button;
        }

        private T FindComponent<T>(string path) where T : Component
        {
            Transform target = transform.Find(path);
            return target != null ? target.GetComponent<T>() : null;
        }

        private static void HideLegacyPanel(Component panel)
        {
            if (panel == null)
            {
                return;
            }

            CanvasGroup canvasGroup = MinebotHudUiFactory.GetOrAdd<CanvasGroup>(panel.gameObject);
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        private static void ParseTemplateButtonLabel(string label, int index, out string keyText, out string nameText)
        {
            string fallback = index switch
            {
                0 => "建筑",
                1 => "从机",
                2 => "标记",
                3 => "感知",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(label))
            {
                keyText = string.Empty;
                nameText = fallback;
                return;
            }

            string[] lines = label.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            keyText = lines.Length > 0 ? lines[0].Trim() : string.Empty;
            nameText = lines.Length > 1
                ? string.Join("\n", lines, 1, lines.Length - 1).Trim()
                : fallback;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private static void SetColor(TMP_Text target, Color value)
        {
            if (target != null)
            {
                target.color = value;
            }
        }

        private static string FormatWaveTimer(float timeUntilNextWave)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(timeUntilNextWave));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
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
