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
        public const string BuildPanelName = "Build Panel";
        public const string BuildingInteractionPanelName = "Building Interaction Panel";
        public const string RepairStationInteractionButtonName = "Repair Station Interaction Button";
        public const string RobotFactoryInteractionButtonName = "Robot Factory Interaction Button";
        private const string BundledChineseFontRelativePath = "Minebot/Fonts/NotoSansSC-Regular.ttf";

        [SerializeField]
        private bool autoInitializeServices = true;

        [SerializeField]
        private bool enableWaveTick = true;

        [SerializeField]
        private MinebotPresentationArtSet artSet;

        [SerializeField]
        private int repairMetalCost = 2;

        private RuntimeServiceRegistry services;
        private MinebotPresentationAssets assets;
        private TilemapGridPresentation gridPresentation;
        private Transform actorRoot;
        private Transform buildingRoot;
        private SpriteRenderer playerView;
        private FreeformActorController playerFreeform;
        private readonly List<SpriteRenderer> robotViews = new List<SpriteRenderer>();
        private readonly List<GameObject> buildingViews = new List<GameObject>();
        private TMP_Text hudText;
        private TMP_Text interactionText;
        private TMP_Text feedbackText;
        private TMP_Text warningText;
        private TMP_Text gameOverText;
        private static TMP_FontAsset runtimeFontAsset;
        private GameObject upgradePanel;
        private GameObject buildPanel;
        private GameObject buildingInteractionPanel;
        private Button repairStationInteractionButton;
        private Button robotFactoryInteractionButton;
        private readonly List<Button> upgradeButtons = new List<Button>();
        private readonly List<Button> buildButtons = new List<Button>();
        private UpgradeDefinition[] currentCandidates = Array.Empty<UpgradeDefinition>();
        private BuildingDefinition[] availableBuildingDefinitions = Array.Empty<BuildingDefinition>();
        private BuildingDefinition selectedBuildingDefinition;
        private GridPosition repairStationPosition;
        private GridPosition robotFactoryPosition;
        private GridPosition? scanOrigin;
        private GridPosition? buildPreviewOrigin;
        private int lastScanCount;
        private string feedbackMessage = "WASD 自由移动，贴墙自动挖掘。Q 探测，E 标记，R 建筑。";
        private GameplayInteractionMode interactionMode = GameplayInteractionMode.Normal;
        private bool isSubscribed;
        private bool defaultFacilitiesRegistered;

        public TilemapGridPresentation GridPresentation => gridPresentation;
        public GridPosition RepairStationPosition => repairStationPosition;
        public GridPosition RobotFactoryPosition => robotFactoryPosition;
        public string HudSummary => hudText != null ? hudText.text : string.Empty;
        public string FeedbackMessage => feedbackMessage;
        public string WarningSummary => warningText != null ? warningText.text : string.Empty;
        public int ActiveRobotViewCount
        {
            get
            {
                int count = 0;
                foreach (SpriteRenderer view in robotViews)
                {
                    if (view != null && view.gameObject.activeSelf)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool IsUpgradePanelShowing => upgradePanel != null && upgradePanel.activeSelf;
        public bool IsRepairInteractionButtonShowing => repairStationInteractionButton != null && repairStationInteractionButton.gameObject.activeInHierarchy;
        public bool IsRobotFactoryInteractionButtonShowing => robotFactoryInteractionButton != null && robotFactoryInteractionButton.gameObject.activeInHierarchy;
        public bool IsGameOver => services != null && services.Vitals.IsDead;
        public bool IsUsingConfiguredArtSet => assets != null && assets.IsUsingConfiguredArtSet;
        public GameplayInteractionMode InteractionMode => interactionMode;
        public Vector2 PlayerWorldPosition => playerFreeform != null
            ? playerFreeform.WorldPosition
            : (services != null ? (Vector2)GridToWorld(services.PlayerMiningState.Position) : Vector2.zero);
        public IReadOnlyList<BuildingDefinition> AvailableBuildingDefinitions => availableBuildingDefinitions;

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

            if (!services.Session.TickRobots(Time.deltaTime))
            {
                RefreshHud();
            }
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
            RefreshBuildings();
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
            ShowFeedback($"探测 {origin}：周边 8 格炸药 {bombCount} 个，蓝色格为探测中心。");
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

        public bool TryRepairAtStation()
        {
            return TryRepairAtStation(repairMetalCost);
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

        public bool TryPlaceBuildingAt(BuildingDefinition definition, GridPosition origin)
        {
            if (services == null || services.Buildings == null)
            {
                return false;
            }

            if (!services.Buildings.TryPlace(definition, origin, out BuildingInstance instance, out BuildingPlacementFailure failure))
            {
                ShowFeedback($"无法建造：{ToChinesePlacementFailure(failure)}");
                return false;
            }

            ShowFeedback($"已建造 {instance.Definition.DisplayName}。");
            RefreshAll();
            return true;
        }

        public bool CanPlaceBuildingAt(BuildingDefinition definition, GridPosition origin)
        {
            return services != null
                && services.Buildings != null
                && services.Buildings.CanPlace(definition, origin, out _);
        }

        public void SetInteractionMode(GameplayInteractionMode mode)
        {
            interactionMode = mode;
            if (mode != GameplayInteractionMode.Build)
            {
                buildPreviewOrigin = null;
                if (gridPresentation != null)
                {
                    gridPresentation.ShowBuildPreview(null, null, false);
                }
            }

            RefreshHud();
        }

        public void SetSelectedBuilding(BuildingDefinition definition)
        {
            selectedBuildingDefinition = definition;
            if (buildPreviewOrigin.HasValue && gridPresentation != null)
            {
                gridPresentation.ShowBuildPreview(selectedBuildingDefinition, buildPreviewOrigin, CanPlaceBuildingAt(selectedBuildingDefinition, buildPreviewOrigin.Value));
            }

            RefreshHud();
        }

        public BuildingDefinition GetSelectedBuildingOrDefault()
        {
            if (selectedBuildingDefinition != null)
            {
                return selectedBuildingDefinition;
            }

            if (availableBuildingDefinitions.Length > 0)
            {
                selectedBuildingDefinition = availableBuildingDefinitions[0];
            }

            return selectedBuildingDefinition;
        }

        public void SetBuildPreview(GridPosition? origin)
        {
            buildPreviewOrigin = origin;
            BuildingDefinition definition = GetSelectedBuildingOrDefault();
            if (gridPresentation != null)
            {
                bool isValid = origin.HasValue && CanPlaceBuildingAt(definition, origin.Value);
                gridPresentation.ShowBuildPreview(definition, origin, isValid);
                if (services != null)
                {
                    gridPresentation.Refresh(services, repairStationPosition, robotFactoryPosition);
                }
            }

            RefreshHud();
        }

        public bool TryMovePlayerFreeform(Vector2 direction, float deltaTime, out GridPosition contactCell)
        {
            contactCell = services != null ? services.PlayerMiningState.Position : GridPosition.Zero;
            if (playerFreeform == null || services == null)
            {
                return false;
            }

            bool moved = playerFreeform.TryMove(services.Grid, direction, deltaTime, out contactCell);
            if (moved)
            {
                GridPosition currentCell = ActorContactProbe.WorldToGrid(playerFreeform.WorldPosition);
                if (services.Grid.IsInside(currentCell) && services.Grid.GetCell(currentCell).IsPassable)
                {
                    services.PlayerMiningState.Teleport(currentCell);
                }
            }

            return moved;
        }

        public void SnapPlayerToLogicalPosition()
        {
            if (playerFreeform == null || services == null)
            {
                return;
            }

            playerFreeform.SnapTo(services.PlayerMiningState.Position);
        }

        public GridPosition ScreenToGridPosition(Vector2 screenPosition)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return services != null ? services.PlayerMiningState.Position : GridPosition.Zero;
            }

            Vector3 world = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -camera.transform.position.z));
            return ActorContactProbe.WorldToGrid(world);
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
            if (applied && !services.Experience.HasPendingUpgrade && !services.Vitals.IsDead)
            {
                interactionMode = GameplayInteractionMode.Normal;
            }

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
            availableBuildingDefinitions = ResolveBuildingDefinitions();
            selectedBuildingDefinition = availableBuildingDefinitions.Length > 0 ? availableBuildingDefinitions[0] : null;
        }

        private void EnsureSceneInfrastructure()
        {
            assets = assets ?? MinebotPresentationAssets.Create(ResolveArtSet());
            EnsureCamera();
            EnsureLight();

            Transform presentationRoot = EnsureChild(transform, PresentationRootName);
            Transform gridRoot = EnsureChild(presentationRoot, "Grid");
            actorRoot = EnsureChild(presentationRoot, "Actor Root");
            buildingRoot = EnsureChild(presentationRoot, "Building Root");

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
            playerFreeform = EnsureFreeformActor(playerView, services != null ? services.PlayerMiningState.Position : GridPosition.Zero);
            EnsureCircleCollider(playerView.gameObject, 0.34f);
            EnsureDefaultFacilityBuildings();
            EnsureHud();
            EnsureEventSystem();
        }

        private static FreeformActorController EnsureFreeformActor(SpriteRenderer renderer, GridPosition position)
        {
            FreeformActorController controller = renderer.GetComponent<FreeformActorController>();
            if (controller == null)
            {
                controller = renderer.gameObject.AddComponent<FreeformActorController>();
            }

            controller.SnapTo(position);
            return controller;
        }

        private static void EnsureCircleCollider(GameObject target, float radius)
        {
            CircleCollider2D collider = target.GetComponent<CircleCollider2D>();
            if (collider == null)
            {
                collider = target.AddComponent<CircleCollider2D>();
            }

            collider.radius = radius;
        }

        private MinebotPresentationArtSet ResolveArtSet()
        {
            return artSet != null ? artSet : Resources.Load<MinebotPresentationArtSet>("Minebot/MinebotPresentationArtSet_Default");
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
            EnsureBuildPanel(hudRoot);
            EnsureBuildingInteractionPanel(hudRoot);
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

        private void EnsureBuildPanel(Transform hudRoot)
        {
            Transform panel = hudRoot.Find(BuildPanelName);
            if (panel == null)
            {
                panel = new GameObject(BuildPanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform;
                panel.SetParent(hudRoot, false);
            }

            buildPanel = panel.gameObject;
            var panelRect = (RectTransform)panel;
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-24f, -144f);
            int buttonCount = Mathf.Max(2, availableBuildingDefinitions.Length);
            panelRect.sizeDelta = new Vector2(420f, Mathf.Max(190f, 86f + buttonCount * 52f));

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.07f, 0.09f, 0.1f, 0.93f);

            TMP_Text title = EnsureText(panel, "Build Title", new Vector2(16f, -12f), new Vector2(388f, 38f), 20, TextAnchor.UpperLeft);
            title.text = "建筑模式：选择建筑后点击空地";

            for (int i = 0; i < buttonCount; i++)
            {
                Button button = EnsureBuildButton(panel, i);
                if (!buildButtons.Contains(button))
                {
                    buildButtons.Add(button);
                }
            }
        }

        private Button EnsureBuildButton(Transform panel, int index)
        {
            string objectName = $"Build Button {index + 1}";
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
            rect.anchoredPosition = new Vector2(16f, -56f - index * 52f);
            rect.sizeDelta = new Vector2(388f, 44f);

            Image image = child.GetComponent<Image>();
            image.color = new Color(0.18f, 0.22f, 0.2f, 0.96f);

            Button button = child.GetComponent<Button>();
            int capturedIndex = index;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (capturedIndex >= 0 && capturedIndex < availableBuildingDefinitions.Length)
                {
                    SetSelectedBuilding(availableBuildingDefinitions[capturedIndex]);
                    ShowFeedback($"已选择建筑：{availableBuildingDefinitions[capturedIndex].DisplayName}");
                }
            });

            EnsureText(child, "Label", new Vector2(10f, -6f), new Vector2(368f, 34f), 17, TextAnchor.MiddleLeft);
            return button;
        }

        private void EnsureBuildingInteractionPanel(Transform hudRoot)
        {
            Transform panel = hudRoot.Find(BuildingInteractionPanelName);
            if (panel == null)
            {
                panel = new GameObject(BuildingInteractionPanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform;
                panel.SetParent(hudRoot, false);
            }

            buildingInteractionPanel = panel.gameObject;
            var panelRect = (RectTransform)panel;
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 0f);
            panelRect.pivot = new Vector2(0f, 0f);
            panelRect.anchoredPosition = new Vector2(16f, 24f);
            panelRect.sizeDelta = new Vector2(420f, 142f);

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.06f, 0.08f, 0.08f, 0.92f);

            TMP_Text title = EnsureText(panel, "Building Interaction Title", new Vector2(16f, -12f), new Vector2(388f, 34f), 19, TextAnchor.UpperLeft);
            title.text = "建筑交互";

            repairStationInteractionButton = EnsureBuildingInteractionButton(panel, RepairStationInteractionButtonName, 0);
            repairStationInteractionButton.onClick.RemoveAllListeners();
            repairStationInteractionButton.onClick.AddListener(() =>
            {
                if (CanUseBuildingInteractionButtons())
                {
                    TryRepairAtStation();
                }
            });

            robotFactoryInteractionButton = EnsureBuildingInteractionButton(panel, RobotFactoryInteractionButtonName, 1);
            robotFactoryInteractionButton.onClick.RemoveAllListeners();
            robotFactoryInteractionButton.onClick.AddListener(() =>
            {
                if (CanUseBuildingInteractionButtons())
                {
                    TryBuildRobotAtFactory();
                }
            });
        }

        private Button EnsureBuildingInteractionButton(Transform panel, string objectName, int index)
        {
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
            rect.anchoredPosition = new Vector2(16f, -48f - index * 48f);
            rect.sizeDelta = new Vector2(388f, 40f);

            Image image = child.GetComponent<Image>();
            image.color = new Color(0.17f, 0.24f, 0.22f, 0.98f);

            EnsureText(child, "Label", new Vector2(10f, -5f), new Vector2(368f, 30f), 17, TextAnchor.MiddleLeft);
            return child.GetComponent<Button>();
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

            runtimeFontAsset = CreateBundledChineseFontAsset();
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

        private static TMP_FontAsset CreateBundledChineseFontAsset()
        {
            return CreateFontAssetFromFile(Path.Combine(Application.streamingAssetsPath, BundledChineseFontRelativePath));
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
            fontAsset.TryAddCharacters("方向键移动挖掘金属能量等级经验波次当前位置钻头可交互维修站机器人工厂恢复生命生产从属机器人升级可用点击地震倒计时红色区域危险立即避开尚未探测上次任务失败核心机体失效炸药标记取消不足完成应用选择暂停土层石层硬岩极硬已挖开触发目标无效地形阻挡强度未知结果空格当前版本暂未冻结时间周边蓝色中心输入已锁定先选择升级下一波半径已标记格自由贴墙自动建筑模式鼠标空地右键退出占地不可建造按钮执行");
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
            services.Session.RobotAutomationCompleted += OnRobotAutomationCompleted;
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
            services.Session.RobotAutomationCompleted -= OnRobotAutomationCompleted;
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
            feedbackMessage = $"探测完成：周边 8 格炸药 {bombCount} 个。";
        }

        private void OnRobotAutomationCompleted(RobotAutomationResult result)
        {
            switch (result.Kind)
            {
                case RobotAutomationResultKind.Mined:
                    feedbackMessage = $"从属机器人挖掘完成：+{result.Reward.Metal} 金属 / +{result.Reward.Energy} 能量 / +{result.Reward.Experience} 经验";
                    break;
                case RobotAutomationResultKind.Destroyed:
                    feedbackMessage = result.Reward.Metal > 0 || result.Reward.Energy > 0 || result.Reward.Experience > 0
                        ? $"从属机器人损毁，回收 +{result.Reward.Metal} 金属。"
                        : "从属机器人损毁。";
                    break;
                case RobotAutomationResultKind.Idle:
                case RobotAutomationResultKind.Blocked:
                    feedbackMessage = string.IsNullOrEmpty(result.Status) ? "从属机器人待机。" : result.Status;
                    break;
            }
        }

        private void RefreshActors()
        {
            if (playerFreeform == null)
            {
                playerFreeform = EnsureFreeformActor(playerView, services.PlayerMiningState.Position);
            }

            int visibleRobotCount = 0;
            for (int i = 0; i < services.Robots.Count; i++)
            {
                RobotState robot = services.Robots[i];
                if (!robot.IsActive)
                {
                    continue;
                }

                SpriteRenderer robotView = EnsureRobotView(visibleRobotCount);
                Vector3 target = GridToWorld(robot.Position);
                HelperRobotMotionController motion = robotView.GetComponent<HelperRobotMotionController>();
                if (motion != null)
                {
                    if (!robotView.gameObject.activeSelf)
                    {
                        motion.SnapTo(target);
                    }

                    motion.SetTarget(target);
                }
                else
                {
                    robotView.transform.position = target;
                }

                robotView.color = ColorForRobotActivity(robot.Activity);
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
                if (renderer.GetComponent<HelperRobotMotionController>() == null)
                {
                    renderer.gameObject.AddComponent<HelperRobotMotionController>();
                }

                EnsureCircleCollider(renderer.gameObject, 0.28f);
                robotViews.Add(renderer);
            }

            return robotViews[index];
        }

        private void RefreshBuildings()
        {
            if (services == null || services.Buildings == null || buildingRoot == null)
            {
                return;
            }

            IReadOnlyList<BuildingInstance> buildings = services.Buildings.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                GameObject view = EnsureBuildingView(i, buildings[i]);
                view.SetActive(true);
            }

            for (int i = buildings.Count; i < buildingViews.Count; i++)
            {
                buildingViews[i].SetActive(false);
            }
        }

        private GameObject EnsureBuildingView(int index, BuildingInstance instance)
        {
            while (buildingViews.Count <= index)
            {
                int displayIndex = buildingViews.Count + 1;
                var view = new GameObject($"Building View {displayIndex}");
                view.transform.SetParent(buildingRoot, false);
                buildingViews.Add(view);
            }

            GameObject target = buildingViews[index];
            target.name = $"Building View {index + 1} - {instance.Definition.DisplayName}";
            Vector2 footprint = instance.Definition.FootprintSize;
            Vector3 center = new Vector3(instance.Origin.X + footprint.x * 0.5f, instance.Origin.Y + footprint.y * 0.5f, 0f);
            target.transform.position = center;

            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = target.AddComponent<SpriteRenderer>();
            }

            renderer.sortingOrder = 18;
            renderer.color = instance.Id.Contains("factory", StringComparison.OrdinalIgnoreCase)
                ? new Color(1f, 0.55f, 0.18f, 0.9f)
                : new Color(0.25f, 0.63f, 1f, 0.9f);
            renderer.sprite = ResolveBuildingSprite(instance);
            target.transform.localScale = new Vector3(Mathf.Max(0.1f, footprint.x), Mathf.Max(0.1f, footprint.y), 1f);

            BoxCollider2D collider = target.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = target.AddComponent<BoxCollider2D>();
            }

            collider.size = Vector2.one;
            collider.isTrigger = false;
            return target;
        }

        private Sprite ResolveBuildingSprite(BuildingInstance instance)
        {
            if (instance.Definition.Prefab != null)
            {
                SpriteRenderer prefabRenderer = instance.Definition.Prefab.GetComponentInChildren<SpriteRenderer>();
                if (prefabRenderer != null && prefabRenderer.sprite != null)
                {
                    return prefabRenderer.sprite;
                }
            }

            if (instance.Id.Contains("factory", StringComparison.OrdinalIgnoreCase) && assets.RobotFactoryTile != null)
            {
                return assets.RobotFactoryTile.sprite;
            }

            return assets.RepairStationTile != null ? assets.RepairStationTile.sprite : assets.EmptyTile.sprite;
        }

        private void RefreshHud()
        {
            if (services == null || hudText == null)
            {
                return;
            }

            ResourceAmount resources = services.Economy.Resources;
            hudText.text =
                $"生命 {services.Vitals.CurrentHealth}/{services.Vitals.MaxHealth} | 金属 {resources.Metal} | 能量 {resources.Energy}\n" +
                $"等级 {services.Experience.Level} | 经验 {services.Experience.Experience}/{services.Experience.NextThreshold} | 波次 {services.Waves.CurrentWave}\n" +
                $"当前位置 {services.PlayerMiningState.Position} | 钻头 {ToChineseHardnessText(services.PlayerMiningState.DrillTier)}\n" +
                BuildRobotStatusText();

            interactionText.text = BuildInteractionText();
            feedbackText.text = feedbackMessage;
            warningText.text = BuildWarningText();
            warningText.color = PlayerIsInDangerZone() || services.Waves.TimeUntilNextWave <= 5f
                ? new Color(1f, 0.36f, 0.24f, 1f)
                : new Color(1f, 0.91f, 0.58f, 1f);
            gameOverText.gameObject.SetActive(services.Vitals.IsDead);
            gameOverText.text = services.Vitals.IsDead ? "任务失败\n核心机体已失效" : string.Empty;
            RefreshUpgradePanel();
            RefreshBuildPanel();
            RefreshBuildingInteractionPanel();
        }

        private string BuildInteractionText()
        {
            string baseHint = "WASD 自由移动 | 贴墙自动挖掘 | Q 探测 | E 标记模式 | R 建筑模式 | 鼠标点击确认 | 1/2/3 选择升级";
            if (services.Experience.HasPendingUpgrade)
            {
                return "升级待选择：普通操作已暂停。按 1/2/3 或点击升级项继续。\n" + baseHint;
            }

            if (interactionMode == GameplayInteractionMode.Marker)
            {
                return "标记模式：鼠标点击岩壁标记/取消标记，再按 E 或右键/Esc 退出。\n" + baseHint;
            }

            if (interactionMode == GameplayInteractionMode.Build)
            {
                string selected = GetSelectedBuildingOrDefault() != null ? GetSelectedBuildingOrDefault().DisplayName : "未选择";
                string preview = buildPreviewOrigin.HasValue
                    ? (CanPlaceBuildingAt(GetSelectedBuildingOrDefault(), buildPreviewOrigin.Value) ? "当前位置可建造" : "当前位置不可建造")
                    : "移动鼠标选择空地";
                return $"建筑模式：已选择 {selected}，{preview}。再按 R 或右键/Esc 退出。\n" + baseHint;
            }

            if (IsNearRepairStation() && IsNearRobotFactory())
            {
                return baseHint + "\n可交互：点击建筑交互按钮维修或生产从属机器人。";
            }

            if (IsNearRepairStation())
            {
                return baseHint + "\n可交互：点击建筑交互按钮在维修站恢复生命。";
            }

            if (IsNearRobotFactory())
            {
                return baseHint + "\n可交互：点击建筑交互按钮在机器人工厂生产从属机器人。";
            }

            return baseHint;
        }

        private string BuildWarningText()
        {
            string scanLine = scanOrigin.HasValue
                ? $"探测结果：{scanOrigin.Value} 周边 8 格炸药 {lastScanCount} 个"
                : "尚未探测：按 Q 消耗能量显示数字风险";
            string countdown = $"地震波倒计时 {Mathf.Max(0f, services.Waves.TimeUntilNextWave):0.0}s | 下一波半径 {services.Waves.NextDangerRadius}";
            string statusLine = PlayerIsInDangerZone()
                ? "你位于红色危险区，地震结算会失败！"
                : $"红色覆盖为危险区 | 已标记 {CountMarkedCells()} 格";
            if (services.Waves.TimeUntilNextWave <= 5f)
            {
                return $"{countdown}\n红色区域危险，立即避开。\n{statusLine}\n{scanLine}";
            }

            return $"{countdown}\n{statusLine}\n{scanLine}";
        }

        private string BuildRobotStatusText()
        {
            int active = 0;
            int working = 0;
            int idle = 0;
            int blocked = 0;
            foreach (RobotState robot in services.Robots)
            {
                if (!robot.IsActive)
                {
                    continue;
                }

                active++;
                if (robot.Activity == RobotActivity.Moving || robot.Activity == RobotActivity.Mining)
                {
                    working++;
                }
                else if (robot.Activity == RobotActivity.Blocked)
                {
                    blocked++;
                }
                else
                {
                    idle++;
                }
            }

            return $"从属机器人 {active} | 工作 {working} | 待机 {idle} | 受阻 {blocked}";
        }

        private static Color ColorForRobotActivity(RobotActivity activity)
        {
            switch (activity)
            {
                case RobotActivity.Mining:
                    return new Color(1f, 0.92f, 0.45f, 1f);
                case RobotActivity.Blocked:
                    return new Color(1f, 0.45f, 0.36f, 1f);
                case RobotActivity.Idle:
                    return new Color(0.78f, 0.92f, 0.78f, 1f);
                default:
                    return Color.white;
            }
        }

        private BuildingDefinition[] ResolveBuildingDefinitions()
        {
            BuildingDefinition[] defaults = CreateDefaultBuildingDefinitions();
            if (services != null && services.BuildingDefinitions != null && services.BuildingDefinitions.Count > 0)
            {
                var definitions = new List<BuildingDefinition>(defaults);
                foreach (BuildingDefinition definition in services.BuildingDefinitions)
                {
                    if (definition != null && !definitions.Exists(existing => string.Equals(existing.Id, definition.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        definitions.Add(definition);
                    }
                }

                return definitions.ToArray();
            }

            return defaults;
        }

        private BuildingDefinition[] CreateDefaultBuildingDefinitions()
        {
            return new[]
            {
                BuildingDefinition.CreateRuntime(
                    "repair-station",
                    "维修站",
                    new ResourceAmount(2, 0, 0),
                    Vector2Int.one,
                    TerrainKind.Empty,
                    null,
                    Vector2.one),
                BuildingDefinition.CreateRuntime(
                    "robot-factory",
                    "机器人工厂",
                    new ResourceAmount(4, 0, 0),
                    Vector2Int.one,
                    TerrainKind.Empty,
                    null,
                    Vector2.one)
            };
        }

        private void EnsureDefaultFacilityBuildings()
        {
            if (defaultFacilitiesRegistered || services == null || services.Buildings == null || availableBuildingDefinitions.Length < 2)
            {
                return;
            }

            services.Buildings.RegisterInitialBuilding(availableBuildingDefinitions[0], repairStationPosition, out _);
            services.Buildings.RegisterInitialBuilding(availableBuildingDefinitions[1], robotFactoryPosition, out _);
            defaultFacilitiesRegistered = true;
        }

        private static string ToChinesePlacementFailure(BuildingPlacementFailure failure)
        {
            switch (failure)
            {
                case BuildingPlacementFailure.MissingDefinition:
                    return "未选择建筑";
                case BuildingPlacementFailure.OutOfBounds:
                    return "超出地图边界";
                case BuildingPlacementFailure.TerrainBlocked:
                    return "地形不适合";
                case BuildingPlacementFailure.Occupied:
                    return "占地已被占用";
                case BuildingPlacementFailure.InsufficientResources:
                    return "资源不足";
                default:
                    return "未知原因";
            }
        }

        private static string ToChineseHardnessText(HardnessTier hardness)
        {
            switch (hardness)
            {
                case HardnessTier.Soil:
                    return "土层";
                case HardnessTier.Stone:
                    return "石层";
                case HardnessTier.HardRock:
                    return "硬岩";
                case HardnessTier.UltraHard:
                    return "极硬岩";
                default:
                    return "未知";
            }
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
                label.text = $"{i + 1}. {upgrade.displayName} - {DescribeUpgrade(upgrade)}";
            }
        }

        private void RefreshBuildPanel()
        {
            if (buildPanel == null)
            {
                return;
            }

            bool show = interactionMode == GameplayInteractionMode.Build;
            buildPanel.SetActive(show);
            for (int i = 0; i < buildButtons.Count; i++)
            {
                Button button = buildButtons[i];
                bool hasDefinition = i < availableBuildingDefinitions.Length;
                button.gameObject.SetActive(hasDefinition);
                if (!hasDefinition)
                {
                    continue;
                }

                BuildingDefinition definition = availableBuildingDefinitions[i];
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                string selected = definition == selectedBuildingDefinition ? ">" : " ";
                label.text = $"{selected} {definition.DisplayName} | 金属 {definition.Cost.Metal} | {definition.FootprintSize.x}x{definition.FootprintSize.y}";
            }
        }

        private void RefreshBuildingInteractionPanel()
        {
            if (buildingInteractionPanel == null)
            {
                return;
            }

            bool canUse = CanUseBuildingInteractionButtons();
            bool showRepair = canUse && IsNearRepairStation();
            bool showFactory = canUse && IsNearRobotFactory();
            buildingInteractionPanel.SetActive(showRepair || showFactory);
            SetBuildingInteractionButton(repairStationInteractionButton, showRepair, $"维修站：维修（金属 {Mathf.Max(0, repairMetalCost)}）");
            SetBuildingInteractionButton(robotFactoryInteractionButton, showFactory, "机器人工厂：生产从属机器人");
        }

        private bool CanUseBuildingInteractionButtons()
        {
            return services != null
                && !services.Vitals.IsDead
                && !services.Experience.HasPendingUpgrade
                && interactionMode == GameplayInteractionMode.Normal;
        }

        private static void SetBuildingInteractionButton(Button button, bool show, string labelText)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(show);
            button.interactable = show;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = labelText;
            }
        }

        private static string DescribeUpgrade(UpgradeDefinition upgrade)
        {
            if (upgrade.drillTierDelta > 0 && upgrade.maxHealthDelta > 0)
            {
                return $"钻头 +{upgrade.drillTierDelta}，最大生命 +{upgrade.maxHealthDelta}";
            }

            if (upgrade.drillTierDelta > 0)
            {
                return $"钻头 +{upgrade.drillTierDelta}";
            }

            if (upgrade.maxHealthDelta > 0)
            {
                return $"最大生命 +{upgrade.maxHealthDelta}";
            }

            return "立即生效";
        }

        private int CountMarkedCells()
        {
            int count = 0;
            foreach (GridPosition position in services.Grid.Positions())
            {
                if (services.Grid.GetCell(position).IsMarked)
                {
                    count++;
                }
            }

            return count;
        }

        private bool PlayerIsInDangerZone()
        {
            GridPosition position = services.PlayerMiningState.Position;
            return services.Grid.IsInside(position) && services.Grid.GetCell(position).IsDangerZone;
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
