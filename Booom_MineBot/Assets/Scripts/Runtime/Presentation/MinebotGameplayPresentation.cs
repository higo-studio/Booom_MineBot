using System;
using System.Collections.Generic;
using System.IO;
using Minebot.Automation;
using Minebot.Bootstrap;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Minebot.Presentation
{
    [DefaultExecutionOrder(-50)]
    public sealed class MinebotGameplayPresentation : MonoBehaviour
    {
        public const string PresentationRootName = "Presentation Root";
        public const string TerrainTilemapName = "Terrain Tilemap";
        public const string FacilityTilemapName = "Facility Tilemap";
        public const string OverlayTilemapName = "Overlay Tilemap";
        public const string HintTilemapName = "Hint Tilemap";
        public const string PlayerViewName = "Player View";
        public const string HudRootName = "Minebot HUD";
        public const string UpgradePanelName = "Upgrade Panel";

        [SerializeField]
        private bool autoInitializeServices = true;

        [SerializeField]
        private bool enableWaveTick = true;

        private RuntimeServiceRegistry services;
        private MinebotPresentationAssets assets;
        private TilemapGridPresentation gridPresentation;
        private Transform actorRoot;
        private SpriteRenderer playerView;
        private readonly List<SpriteRenderer> robotViews = new List<SpriteRenderer>();
        private TMP_Text hudText;
        private TMP_Text interactionText;
        private TMP_Text feedbackText;
        private TMP_Text warningText;
        private TMP_Text gameOverText;
        private static TMP_FontAsset runtimeFontAsset;
        private GameObject upgradePanel;
        private readonly List<Button> upgradeButtons = new List<Button>();
        private UpgradeDefinition[] currentCandidates = Array.Empty<UpgradeDefinition>();
        private GridPosition repairStationPosition;
        private GridPosition robotFactoryPosition;
        private GridPosition? scanOrigin;
        private int lastScanCount;
        private string feedbackMessage = "WASD/方向键移动，Space/E 挖掘。";
        private bool isSubscribed;

        public TilemapGridPresentation GridPresentation => gridPresentation;
        public GridPosition RepairStationPosition => repairStationPosition;
        public GridPosition RobotFactoryPosition => robotFactoryPosition;
        public string HudSummary => hudText != null ? hudText.text : string.Empty;
        public string FeedbackMessage => feedbackMessage;
        public int ActiveRobotViewCount => robotViews.Count;
        public bool IsUpgradePanelShowing => upgradePanel != null && upgradePanel.activeSelf;
        public bool IsGameOver => services != null && services.Vitals.IsDead;

        private void Awake()
        {
            EnsureInitialized();
            EnsureSceneInfrastructure();
            RefreshAll();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            SubscribeToServices();
            RefreshAll();
        }

        private void OnDisable()
        {
            UnsubscribeFromServices();
        }

        private void Update()
        {
            if (services == null)
            {
                return;
            }

            if (enableWaveTick && !services.Vitals.IsDead && services.Waves.Tick(Time.deltaTime))
            {
                ResolveWave();
            }

            RefreshHud();
        }

        public void RefreshAll()
        {
            if (services == null)
            {
                EnsureInitialized();
            }

            if (services == null)
            {
                return;
            }

            EvaluateDangerZones();
            gridPresentation.Refresh(services, repairStationPosition, robotFactoryPosition);
            RefreshActors();
            RefreshHud();
        }

        public void ShowFeedback(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                feedbackMessage = message;
            }

            RefreshAll();
        }

        public void RecordScan(GridPosition origin, int bombCount)
        {
            scanOrigin = origin;
            lastScanCount = bombCount;
            gridPresentation.ShowScanAt(origin);
            ShowFeedback($"探测 {origin}: 周围炸药数 {bombCount}");
        }

        public bool TryRepairAtStation(int metalCost)
        {
            if (!IsNearRepairStation())
            {
                ShowFeedback("需要靠近蓝色维修站才能维修。");
                return false;
            }

            bool repaired = services.BaseOps.TryRepair(new ResourceAmount(Mathf.Max(0, metalCost), 0, 0));
            ShowFeedback(repaired ? "维修完成，生命已恢复。" : "金属不足，无法维修。");
            return repaired;
        }

        public bool TryBuildRobotAtFactory()
        {
            if (!IsNearRobotFactory())
            {
                ShowFeedback("需要靠近橙色机器人工厂才能生产机器人。");
                return false;
            }

            bool produced = services.RobotFactory.TryProduce(robotFactoryPosition, out RobotState robot);
            ShowFeedback(produced && robot != null ? "已生产从属机器人。" : "金属不足，无法生产机器人。");
            return produced;
        }

        public bool SelectUpgradeIndex(int index)
        {
            RefreshUpgradePanel();
            if (index < 0 || index >= currentCandidates.Length)
            {
                ShowFeedback("当前没有可选升级。");
                return false;
            }

            UpgradeDefinition selected = currentCandidates[index];
            bool applied = services.Upgrades.Select(selected);
            ShowFeedback(applied ? $"升级已应用：{selected.displayName}" : "升级选择失败。");
            return applied;
        }

        public bool IsNearRepairStation()
        {
            return services != null && services.PlayerMiningState.Position.ManhattanDistance(repairStationPosition) <= 1;
        }

        public bool IsNearRobotFactory()
        {
            return services != null && services.PlayerMiningState.Position.ManhattanDistance(robotFactoryPosition) <= 1;
        }

        public Vector3 GridToWorld(GridPosition position)
        {
            return new Vector3(position.X + 0.5f, position.Y + 0.5f, 0f);
        }

        private void EnsureInitialized()
        {
            if (services != null)
            {
                return;
            }

            if (!MinebotServices.IsInitialized && autoInitializeServices)
            {
                MinebotServices.Initialize(null);
            }

            if (!MinebotServices.IsInitialized)
            {
                return;
            }

            services = MinebotServices.Current;
            repairStationPosition = PickFacilityPosition(GridPosition.Left);
            robotFactoryPosition = PickFacilityPosition(GridPosition.Right);
        }

        private void EnsureSceneInfrastructure()
        {
            assets = assets ?? MinebotPresentationAssets.Create();
            EnsureCamera();
            EnsureLight();

            Transform presentationRoot = EnsureChild(transform, PresentationRootName);
            Transform gridRoot = EnsureChild(presentationRoot, "Grid");
            actorRoot = EnsureChild(presentationRoot, "Actor Root");

            var unityGrid = gridRoot.GetComponent<UnityEngine.Grid>();
            if (unityGrid == null)
            {
                unityGrid = gridRoot.gameObject.AddComponent<UnityEngine.Grid>();
            }

            unityGrid.cellSize = Vector3.one;
            unityGrid.cellGap = Vector3.zero;

            Tilemap terrain = EnsureTilemapLayer(gridRoot, TerrainTilemapName, 0);
            Tilemap facility = EnsureTilemapLayer(gridRoot, FacilityTilemapName, 5);
            Tilemap overlay = EnsureTilemapLayer(gridRoot, OverlayTilemapName, 10);
            Tilemap hint = EnsureTilemapLayer(gridRoot, HintTilemapName, 15);

            gridPresentation = gridRoot.GetComponent<TilemapGridPresentation>();
            if (gridPresentation == null)
            {
                gridPresentation = gridRoot.gameObject.AddComponent<TilemapGridPresentation>();
            }

            gridPresentation.Configure(terrain, facility, overlay, hint, assets);
            playerView = EnsureSpriteRenderer(actorRoot, PlayerViewName, assets.PlayerSprite, 30);
            EnsureHud();
            EnsureEventSystem();
        }

        private void EnsureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.07f, 0.08f, 1f);

            if (services != null)
            {
                Vector2 size = services.Grid.Size;
                camera.transform.position = new Vector3(size.x * 0.5f, size.y * 0.5f, -10f);
                camera.orthographicSize = Mathf.Max(size.x, size.y) * 0.58f;
            }
            else
            {
                camera.transform.position = new Vector3(6f, 6f, -10f);
                camera.orthographicSize = 8f;
            }
        }

        private static void EnsureLight()
        {
            if (FindAnyObjectByType<Light>() != null)
            {
                return;
            }

            var lightObject = new GameObject("Presentation Key Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.rotation = Quaternion.Euler(45f, 30f, 0f);
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            var childObject = new GameObject(childName);
            childObject.transform.SetParent(parent, false);
            return childObject.transform;
        }

        private static Tilemap EnsureTilemapLayer(Transform gridRoot, string layerName, int sortingOrder)
        {
            Transform layer = EnsureChild(gridRoot, layerName);
            Tilemap tilemap = layer.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                tilemap = layer.gameObject.AddComponent<Tilemap>();
            }

            TilemapRenderer renderer = layer.GetComponent<TilemapRenderer>();
            if (renderer == null)
            {
                renderer = layer.gameObject.AddComponent<TilemapRenderer>();
            }

            renderer.sortingOrder = sortingOrder;
            return tilemap;
        }

        private static SpriteRenderer EnsureSpriteRenderer(Transform parent, string objectName, Sprite sprite, int sortingOrder)
        {
            Transform child = parent.Find(objectName);
            if (child == null)
            {
                child = new GameObject(objectName).transform;
                child.SetParent(parent, false);
            }

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = child.gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.transform.localScale = new Vector3(0.82f, 0.82f, 1f);
            return renderer;
        }

        private void EnsureHud()
        {
            Transform hudRoot = transform.Find(HudRootName);
            if (hudRoot == null)
            {
                var canvasObject = new GameObject(HudRootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                hudRoot = canvasObject.transform;
                hudRoot.SetParent(transform, false);
            }

            Canvas canvas = hudRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = hudRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            hudText = EnsureText(hudRoot, "Status Text", new Vector2(16f, -16f), new Vector2(520f, 128f), 22, TextAnchor.UpperLeft);
            interactionText = EnsureText(hudRoot, "Interaction Text", new Vector2(16f, -136f), new Vector2(760f, 110f), 18, TextAnchor.UpperLeft);
            feedbackText = EnsureText(hudRoot, "Feedback Text", new Vector2(16f, -250f), new Vector2(720f, 96f), 20, TextAnchor.UpperLeft);
            warningText = EnsureText(hudRoot, "Wave Warning Text", new Vector2(-16f, -16f), new Vector2(420f, 110f), 22, TextAnchor.UpperRight);
            warningText.rectTransform.anchorMin = new Vector2(1f, 1f);
            warningText.rectTransform.anchorMax = new Vector2(1f, 1f);
            warningText.rectTransform.pivot = new Vector2(1f, 1f);
            gameOverText = EnsureText(hudRoot, "Game Over Text", Vector2.zero, new Vector2(760f, 160f), 42, TextAnchor.MiddleCenter);
            gameOverText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            gameOverText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            gameOverText.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            EnsureUpgradePanel(hudRoot);
        }

        private void EnsureUpgradePanel(Transform hudRoot)
        {
            Transform panel = hudRoot.Find(UpgradePanelName);
            if (panel == null)
            {
                panel = new GameObject(UpgradePanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform;
                panel.SetParent(hudRoot, false);
            }

            upgradePanel = panel.gameObject;
            var panelRect = (RectTransform)panel;
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.anchoredPosition = new Vector2(-24f, 24f);
            panelRect.sizeDelta = new Vector2(420f, 230f);

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.05f, 0.07f, 0.08f, 0.92f);

            TMP_Text title = EnsureText(panel, "Upgrade Title", new Vector2(16f, -16f), new Vector2(388f, 42f), 20, TextAnchor.UpperLeft);
            title.text = "升级可用：按 1/2/3 或点击";

            for (int i = 0; i < 3; i++)
            {
                Button button = EnsureUpgradeButton(panel, i);
                if (!upgradeButtons.Contains(button))
                {
                    upgradeButtons.Add(button);
                }
            }
        }

        private Button EnsureUpgradeButton(Transform panel, int index)
        {
            string objectName = $"Upgrade Button {index + 1}";
            Transform child = panel.Find(objectName);
            if (child == null)
            {
                child = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)).transform;
                child.SetParent(panel, false);
            }

            var rect = (RectTransform)child;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(16f, -64f - index * 52f);
            rect.sizeDelta = new Vector2(388f, 44f);

            Image image = child.GetComponent<Image>();
            image.color = new Color(0.16f, 0.23f, 0.24f, 0.96f);

            Button button = child.GetComponent<Button>();
            int capturedIndex = index;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectUpgradeIndex(capturedIndex));

            EnsureText(child, "Label", new Vector2(10f, -6f), new Vector2(368f, 34f), 17, TextAnchor.MiddleLeft);
            return button;
        }

        private static TMP_Text EnsureText(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
        {
            Transform child = parent.Find(objectName);
            if (child == null)
            {
                child = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).transform;
                child.SetParent(parent, false);
            }

            var rect = (RectTransform)child;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text legacyText = child.GetComponent<Text>();
            if (legacyText != null)
            {
                Destroy(legacyText);
            }

            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text == null)
            {
                text = child.gameObject.AddComponent<TextMeshProUGUI>();
            }

            text.font = GetDefaultTmpFontAsset();
            text.fontSize = fontSize;
            text.alignment = ToTmpAlignment(alignment);
            text.color = Color.white;
            text.raycastTarget = false;
            text.enableAutoSizing = false;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private static TMP_FontAsset GetDefaultTmpFontAsset()
        {
            if (runtimeFontAsset != null)
            {
                return runtimeFontAsset;
            }

            runtimeFontAsset = CreateRuntimeChineseFontAsset();
            if (runtimeFontAsset != null)
            {
                return runtimeFontAsset;
            }

            runtimeFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            return runtimeFontAsset;
        }

        private static TMP_FontAsset CreateRuntimeChineseFontAsset()
        {
            string[] preferredFontFiles =
            {
                "/System/Library/Fonts/Supplemental/Arial Unicode.ttf",
                "/Library/Fonts/Arial Unicode.ttf",
                "/System/Library/Fonts/STHeiti Medium.ttc",
                "/System/Library/Fonts/Hiragino Sans GB.ttc"
            };

            foreach (string fontPath in preferredFontFiles)
            {
                TMP_FontAsset fontAsset = CreateFontAssetFromFile(fontPath);
                if (fontAsset != null)
                {
                    return fontAsset;
                }
            }

            string[] preferredFonts =
            {
                "PingFang SC",
                "Hiragino Sans GB",
                "Heiti SC",
                "STHeiti",
                "Noto Sans CJK SC",
                "Noto Sans CJK",
                "Microsoft YaHei",
                "SimHei",
                "Arial Unicode MS"
            };

            foreach (string fontName in preferredFonts)
            {
                if (!IsOsFontInstalled(fontName))
                {
                    continue;
                }

                Font font = Font.CreateDynamicFontFromOSFont(fontName, 90);
                if (font == null)
                {
                    continue;
                }

                TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                if (fontAsset == null)
                {
                    continue;
                }

                fontAsset.name = $"Minebot TMP {fontName}";
                WarmupMinebotGlyphs(fontAsset);
                return fontAsset;
            }

            return null;
        }

        private static TMP_FontAsset CreateFontAssetFromFile(string fontPath)
        {
            if (!File.Exists(fontPath))
            {
                return null;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(fontPath, 0, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048);
            if (fontAsset == null)
            {
                return null;
            }

            fontAsset.name = $"Minebot TMP {Path.GetFileNameWithoutExtension(fontPath)}";
            WarmupMinebotGlyphs(fontAsset);
            return fontAsset;
        }

        private static void WarmupMinebotGlyphs(TMP_FontAsset fontAsset)
        {
            fontAsset.TryAddCharacters("方向键移动挖掘金属能量等级经验波次当前位置钻头可交互维修站机器人工厂恢复生命生产从属机器人升级可用点击地震倒计时红色区域危险立即避开尚未探测上次任务失败核心机体失效炸药标记取消不足完成应用选择暂停");
        }

        private static bool IsOsFontInstalled(string fontName)
        {
            string[] installedFonts = Font.GetOSInstalledFontNames();
            for (int i = 0; i < installedFonts.Length; i++)
            {
                if (string.Equals(installedFonts[i], fontName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static TextAlignmentOptions ToTmpAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft:
                    return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter:
                    return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight:
                    return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft:
                    return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleCenter:
                    return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight:
                    return TextAlignmentOptions.MidlineRight;
                case TextAnchor.LowerLeft:
                    return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter:
                    return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight:
                    return TextAlignmentOptions.BottomRight;
                default:
                    return TextAlignmentOptions.TopLeft;
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private void SubscribeToServices()
        {
            if (isSubscribed || services == null)
            {
                return;
            }

            services.Session.StateChanged += RefreshAll;
            services.Session.RewardGranted += OnRewardGranted;
            services.Session.ScanCompleted += OnScanCompleted;
            isSubscribed = true;
        }

        private void UnsubscribeFromServices()
        {
            if (!isSubscribed || services == null)
            {
                return;
            }

            services.Session.StateChanged -= RefreshAll;
            services.Session.RewardGranted -= OnRewardGranted;
            services.Session.ScanCompleted -= OnScanCompleted;
            isSubscribed = false;
        }

        private void OnRewardGranted(ResourceAmount reward)
        {
            if (reward.Metal > 0 || reward.Energy > 0 || reward.Experience > 0)
            {
                feedbackMessage = $"+{reward.Metal} 金属 / +{reward.Energy} 能量 / +{reward.Experience} 经验";
            }
        }

        private void OnScanCompleted(int bombCount)
        {
            lastScanCount = bombCount;
            scanOrigin = services.PlayerMiningState.Position;
            gridPresentation.ShowScanAt(scanOrigin.Value);
            feedbackMessage = $"探测完成：周围炸药数 {bombCount}";
        }

        private void RefreshActors()
        {
            playerView.transform.position = GridToWorld(services.PlayerMiningState.Position);

            int visibleRobotCount = 0;
            for (int i = 0; i < services.Robots.Count; i++)
            {
                RobotState robot = services.Robots[i];
                if (!robot.IsActive)
                {
                    continue;
                }

                SpriteRenderer robotView = EnsureRobotView(visibleRobotCount);
                robotView.transform.position = GridToWorld(robot.Position);
                robotView.gameObject.SetActive(true);
                visibleRobotCount++;
            }

            for (int i = visibleRobotCount; i < robotViews.Count; i++)
            {
                robotViews[i].gameObject.SetActive(false);
            }
        }

        private SpriteRenderer EnsureRobotView(int index)
        {
            while (robotViews.Count <= index)
            {
                int displayIndex = robotViews.Count + 1;
                SpriteRenderer renderer = EnsureSpriteRenderer(actorRoot, $"Robot View {displayIndex}", assets.RobotSprite, 25);
                robotViews.Add(renderer);
            }

            return robotViews[index];
        }

        private void RefreshHud()
        {
            if (services == null || hudText == null)
            {
                return;
            }

            ResourceAmount resources = services.Economy.Resources;
            hudText.text =
                $"HP {services.Vitals.CurrentHealth}/{services.Vitals.MaxHealth} | 金属 {resources.Metal} | 能量 {resources.Energy}\n" +
                $"等级 {services.Experience.Level} | 经验 {services.Experience.Experience}/{services.Experience.NextThreshold} | 波次 {services.Waves.CurrentWave}\n" +
                $"当前位置 {services.PlayerMiningState.Position} | 钻头 {services.PlayerMiningState.DrillTier}";

            interactionText.text = BuildInteractionText();
            feedbackText.text = feedbackMessage;
            warningText.text = BuildWarningText();
            gameOverText.gameObject.SetActive(services.Vitals.IsDead);
            gameOverText.text = services.Vitals.IsDead ? "任务失败\n核心机体已失效" : string.Empty;
            RefreshUpgradePanel();
        }

        private string BuildInteractionText()
        {
            string baseHint = "WASD/方向键 移动 | Space/E 挖掘 | Q 探测 | F 标记 | R 维修 | B 造机器人 | 1/2/3 选择升级";
            if (IsNearRepairStation() && IsNearRobotFactory())
            {
                return baseHint + "\n可交互：维修站、机器人工厂";
            }

            if (IsNearRepairStation())
            {
                return baseHint + "\n可交互：按 R 在维修站恢复生命。";
            }

            if (IsNearRobotFactory())
            {
                return baseHint + "\n可交互：按 B 在机器人工厂生产从属机器人。";
            }

            return baseHint;
        }

        private string BuildWarningText()
        {
            string scanLine = scanOrigin.HasValue ? $"上次探测 {scanOrigin.Value}: {lastScanCount}" : "尚未探测";
            string countdown = $"地震波倒计时 {Mathf.Max(0f, services.Waves.TimeUntilNextWave):0.0}s";
            if (services.Waves.TimeUntilNextWave <= 5f)
            {
                return $"{countdown}\n红色区域危险，立即避开。\n{scanLine}";
            }

            return $"{countdown}\n红色覆盖为危险区。\n{scanLine}";
        }

        private void RefreshUpgradePanel()
        {
            currentCandidates = services.Upgrades.GetCandidates(3);
            bool show = currentCandidates.Length > 0;
            upgradePanel.SetActive(show);

            for (int i = 0; i < upgradeButtons.Count; i++)
            {
                Button button = upgradeButtons[i];
                bool hasCandidate = i < currentCandidates.Length;
                button.gameObject.SetActive(hasCandidate);
                if (!hasCandidate)
                {
                    continue;
                }

                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                UpgradeDefinition upgrade = currentCandidates[i];
                label.text = $"{i + 1}. {upgrade.displayName}";
            }
        }

        private void EvaluateDangerZones()
        {
            services.Waves.EvaluateDangerZones(BuildDangerOrigins());
        }

        private IEnumerable<GridPosition> BuildDangerOrigins()
        {
            Vector2Int size = services.Grid.Size;
            yield return new GridPosition(1, 1);
            yield return new GridPosition(Mathf.Max(1, size.x - 2), Mathf.Max(1, size.y - 2));
        }

        private void ResolveWave()
        {
            IList<RobotState> mutableRobots = services.Robots as IList<RobotState>;
            var resolution = services.Waves.ResolveWave(services.PlayerMiningState.Position, services.Vitals, mutableRobots);
            if (resolution.DroppedResources.Metal > 0 || resolution.DroppedResources.Energy > 0 || resolution.DroppedResources.Experience > 0)
            {
                services.Economy.Add(resolution.DroppedResources);
            }

            EvaluateDangerZones();
            feedbackMessage = resolution.PlayerKilled
                ? "地震波吞没了主机器人。"
                : $"地震波结算：存活到第 {resolution.SurvivedWave} 波，损失机器人 {resolution.RobotsDestroyed}。";
            RefreshAll();
        }

        private GridPosition PickFacilityPosition(GridPosition direction)
        {
            if (services == null)
            {
                return GridPosition.Zero;
            }

            GridPosition candidate = services.Grid.PlayerSpawn + direction;
            if (services.Grid.IsInside(candidate) && services.Grid.GetCell(candidate).TerrainKind == TerrainKind.Empty)
            {
                return candidate;
            }

            foreach (GridPosition position in services.Grid.Positions())
            {
                if (services.Grid.GetCell(position).TerrainKind == TerrainKind.Empty)
                {
                    return position;
                }
            }

            return services.Grid.PlayerSpawn;
        }
    }
}
