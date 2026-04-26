using System;
using System.Collections.Generic;
using Minebot.Automation;
using Minebot.Bootstrap;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.HazardInference;
using Minebot.Progression;
using Minebot.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Minebot.Presentation
{
    [DefaultExecutionOrder(-50)]
    public sealed class MinebotGameplayPresentation : MonoBehaviour
    {
        public const string PresentationRootName = "Presentation Root";
        public const string TerrainTilemapName = "Terrain Tilemap";
        public const string WallContourTilemapName = "Wall Contour Tilemap";
        public const string FacilityTilemapName = "Facility Tilemap";
        public const string MarkerTilemapName = "Marker Tilemap";
        public const string DangerTilemapName = "Danger Tilemap";
        public const string DangerContourTilemapName = "Danger Contour Tilemap";
        public const string BuildPreviewTilemapName = "Build Preview Tilemap";
        public const string ScanIndicatorRootName = "Scan Indicator Root";
        public const string OverlayTilemapName = MarkerTilemapName;
        public const string HintTilemapName = BuildPreviewTilemapName;
        public const string PlayerViewName = "Player View";
        public const string HudRootName = MinebotHudView.RootName;
        public const string UpgradePanelName = MinebotHudView.UpgradePanelName;
        public const string BuildPanelName = MinebotHudView.BuildPanelName;
        public const string BuildingInteractionPanelName = MinebotHudView.BuildingInteractionPanelName;
        public const string RepairStationInteractionButtonName = MinebotHudView.RepairStationInteractionButtonName;
        public const string RobotFactoryInteractionButtonName = MinebotHudView.RobotFactoryInteractionButtonName;

        [SerializeField]
        private bool autoInitializeServices = true;

        [SerializeField]
        private bool enableWaveTick = true;

        [SerializeField]
        private MinebotPresentationArtSet artSet;

        [SerializeField]
        private MinebotHudView hudPrefab;

        [SerializeField]
        private int repairMetalCost = 2;

        private RuntimeServiceRegistry services;
        private MinebotPresentationAssets assets;
        private TilemapGridPresentation gridPresentation;
        private ScanIndicatorPresenter scanIndicatorPresenter;
        private Transform actorRoot;
        private Transform buildingRoot;
        private SpriteRenderer playerView;
        private FreeformActorController playerFreeform;
        private readonly List<SpriteRenderer> robotViews = new List<SpriteRenderer>();
        private readonly List<GameObject> buildingViews = new List<GameObject>();
        private static TMP_FontAsset runtimeFontAsset;
        private MinebotHudView hudView;
        private UpgradeDefinition[] currentCandidates = Array.Empty<UpgradeDefinition>();
        private BuildingDefinition[] availableBuildingDefinitions = Array.Empty<BuildingDefinition>();
        private BuildingDefinition selectedBuildingDefinition;
        private GridPosition repairStationPosition;
        private GridPosition robotFactoryPosition;
        private readonly List<ScanReading> lastScanReadings = new List<ScanReading>();
        private bool hasPerformedScan;
        private GridPosition? buildPreviewOrigin;
        private string feedbackMessage = "WASD 自由移动，贴墙自动挖掘。Q 探测，E 标记，R 建筑。";
        private GameplayInteractionMode interactionMode = GameplayInteractionMode.Normal;
        private bool isSubscribed;
        private bool defaultFacilitiesRegistered;

        public TilemapGridPresentation GridPresentation => gridPresentation;
        public GridPosition RepairStationPosition => repairStationPosition;
        public GridPosition RobotFactoryPosition => robotFactoryPosition;
        public string HudSummary => hudView != null && hudView.StatusPanel != null ? hudView.StatusPanel.Content : string.Empty;
        public string FeedbackMessage => feedbackMessage;
        public string WarningSummary => hudView != null && hudView.WarningPanel != null ? hudView.WarningPanel.Content : string.Empty;
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

        public bool IsUpgradePanelShowing => hudView != null && hudView.UpgradePanel != null && hudView.UpgradePanel.gameObject.activeSelf;
        public bool IsRepairInteractionButtonShowing => hudView != null && hudView.RepairStationInteractionButton != null && hudView.RepairStationInteractionButton.gameObject.activeInHierarchy;
        public bool IsRobotFactoryInteractionButtonShowing => hudView != null && hudView.RobotFactoryInteractionButton != null && hudView.RobotFactoryInteractionButton.gameObject.activeInHierarchy;
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
            scanIndicatorPresenter?.Refresh();
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

        public void RecordScan(IReadOnlyList<ScanReading> readings)
        {
            ApplyScanReadings(readings);
            feedbackMessage = DescribeScanFeedback();
            RefreshAll();
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

        public CharacterMoveResult2D TryMovePlayerFreeform(Vector2 direction, float deltaTime)
        {
            if (playerFreeform == null || services == null)
            {
                Vector2 start = playerFreeform != null ? playerFreeform.WorldPosition : Vector2.zero;
                return new CharacterMoveResult2D(
                    start,
                    start,
                    Vector2.zero,
                    Vector2.zero,
                    CharacterCollisionFlags2D.None,
                    default,
                    false,
                    default,
                    false,
                    0);
            }

            var collisionWorld = new GridCharacterCollisionWorld(services.Grid);
            CharacterMoveResult2D result = playerFreeform.Move(collisionWorld, direction, deltaTime);
            if (result.HasMoved)
            {
                GridPosition currentCell = collisionWorld.ResolveOccupancyCell(
                    result.FinalPosition,
                    services.PlayerMiningState.Position);
                if (services.Grid.IsInside(currentCell) && services.Grid.GetCell(currentCell).IsPassable)
                {
                    services.PlayerMiningState.Teleport(currentCell);
                }
            }

            return result;
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
            Tilemap wallContour = EnsureTilemapLayer(gridRoot, WallContourTilemapName, 2, new Vector3(-0.5f, -0.5f, 0f));
            Tilemap danger = EnsureTilemapLayer(gridRoot, DangerTilemapName, 4);
            Tilemap facility = EnsureTilemapLayer(gridRoot, FacilityTilemapName, 5);
            Tilemap dangerContour = EnsureTilemapLayer(gridRoot, DangerContourTilemapName, 10, new Vector3(-0.5f, -0.5f, 0f));
            Tilemap marker = EnsureTilemapLayer(gridRoot, MarkerTilemapName, 15);
            Tilemap buildPreview = EnsureTilemapLayer(gridRoot, BuildPreviewTilemapName, 20);
            scanIndicatorPresenter = EnsureScanIndicatorPresenter(EnsureChild(gridRoot, ScanIndicatorRootName));
            scanIndicatorPresenter.Configure(assets);

            gridPresentation = gridRoot.GetComponent<TilemapGridPresentation>();
            if (gridPresentation == null)
            {
                gridPresentation = gridRoot.gameObject.AddComponent<TilemapGridPresentation>();
            }

            gridPresentation.Configure(terrain, facility, marker, wallContour, danger, dangerContour, buildPreview, assets);
            playerView = EnsureSpriteRenderer(actorRoot, PlayerViewName, assets.PlayerSprite, 30);
            playerFreeform = EnsureFreeformActor(playerView, services != null ? services.PlayerMiningState.Position : GridPosition.Zero);
            EnsureCircleCollider(playerView.gameObject, assets.PlayerColliderRadius);
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
            FreeformActorController controller = target.GetComponent<FreeformActorController>();
            if (controller != null)
            {
                controller.CollisionRadius = radius;
            }
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

        private static Tilemap EnsureTilemapLayer(Transform gridRoot, string layerName, int sortingOrder, Vector3? localPosition = null)
        {
            Transform layer = EnsureChild(gridRoot, layerName);
            layer.localPosition = localPosition ?? Vector3.zero;
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

        private static ScanIndicatorPresenter EnsureScanIndicatorPresenter(Transform root)
        {
            ScanIndicatorPresenter presenter = root.GetComponent<ScanIndicatorPresenter>();
            if (presenter == null)
            {
                presenter = root.gameObject.AddComponent<ScanIndicatorPresenter>();
            }

            return presenter;
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
            hudView = ResolveHudView();
            hudView.EnsureDefaultStructure(GetDefaultTmpFontAsset(), Mathf.Max(2, availableBuildingDefinitions.Length));

            hudView.BindUpgradeButtons(index => SelectUpgradeIndex(index));
            hudView.BindBuildButtons(SelectBuildButtonIndex);
            hudView.BindBuildingInteractionButtons(
                () =>
                {
                    if (CanUseBuildingInteractionButtons())
                    {
                        TryRepairAtStation();
                    }
                },
                () =>
                {
                    if (CanUseBuildingInteractionButtons())
                    {
                        TryBuildRobotAtFactory();
                    }
                });
        }

        private MinebotHudView ResolveHudView()
        {
            if (hudView != null)
            {
                return hudView;
            }

            Transform hudRoot = transform.Find(HudRootName);
            if (hudRoot != null)
            {
                MinebotHudView existingView = hudRoot.GetComponent<MinebotHudView>();
                return existingView != null ? existingView : hudRoot.gameObject.AddComponent<MinebotHudView>();
            }

            MinebotHudView existingChild = GetComponentInChildren<MinebotHudView>(true);
            if (existingChild != null)
            {
                existingChild.name = HudRootName;
                return existingChild;
            }

            MinebotHudView prefab = hudPrefab != null ? hudPrefab : Resources.Load<MinebotHudView>(MinebotHudView.ResourcePath);
            if (prefab != null)
            {
                MinebotHudView instance = Instantiate(prefab, transform, false);
                instance.name = HudRootName;
                return instance;
            }

            var canvasObject = new GameObject(HudRootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MinebotHudView));
            canvasObject.transform.SetParent(transform, false);
            return canvasObject.GetComponent<MinebotHudView>();
        }

        private void SelectBuildButtonIndex(int index)
        {
            if (index < 0 || index >= availableBuildingDefinitions.Length)
            {
                return;
            }

            SetSelectedBuilding(availableBuildingDefinitions[index]);
            ShowFeedback($"已选择建筑：{availableBuildingDefinitions[index].DisplayName}");
        }

        private static TMP_FontAsset GetDefaultTmpFontAsset()
        {
            runtimeFontAsset = runtimeFontAsset ?? MinebotHudFontUtility.GetDefaultFontAsset();
            return runtimeFontAsset;
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

        private void OnScanCompleted(IReadOnlyList<ScanReading> readings)
        {
            ApplyScanReadings(readings);
            feedbackMessage = DescribeScanFeedback();
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
            if (services == null || hudView == null || hudView.StatusPanel == null)
            {
                return;
            }

            ResourceAmount resources = services.Economy.Resources;
            hudView.StatusPanel.SetText(
                $"生命 {services.Vitals.CurrentHealth}/{services.Vitals.MaxHealth} | 金属 {resources.Metal} | 能量 {resources.Energy}\n" +
                $"等级 {services.Experience.Level} | 经验 {services.Experience.Experience}/{services.Experience.NextThreshold} | 波次 {services.Waves.CurrentWave}\n" +
                $"当前位置 {services.PlayerMiningState.Position} | 钻头 {ToChineseHardnessText(services.PlayerMiningState.DrillTier)}\n" +
                BuildRobotStatusText());

            if (hudView.InteractionPanel != null)
            {
                hudView.InteractionPanel.SetText(BuildInteractionText());
            }

            if (hudView.FeedbackPanel != null)
            {
                hudView.FeedbackPanel.SetText(feedbackMessage);
            }

            if (hudView.WarningPanel != null)
            {
                hudView.WarningPanel.SetText(BuildWarningText());
                hudView.WarningPanel.SetColor(PlayerIsInDangerZone() || services.Waves.TimeUntilNextWave <= 5f
                ? new Color(1f, 0.36f, 0.24f, 1f)
                : new Color(1f, 0.91f, 0.58f, 1f));
            }

            if (hudView.GameOverPanel != null)
            {
                hudView.GameOverPanel.SetVisible(services.Vitals.IsDead);
                hudView.GameOverPanel.SetText(services.Vitals.IsDead ? "任务失败\n核心机体已失效" : string.Empty);
            }

            RefreshUpgradePanel();
            RefreshBuildPanel();
            RefreshBuildingInteractionPanel();
        }

        private string BuildInteractionText()
        {
            string baseHint = "WASD 自由移动 | 贴墙自动挖掘 | Q 探测前沿岩壁 | E 标记模式 | R 建筑模式 | 鼠标点击确认 | 1/2/3 选择升级";
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
            string scanLine = BuildScanSummaryText();
            string countdown = $"地震波倒计时 {Mathf.Max(0f, services.Waves.TimeUntilNextWave):0.0}s | 下一波危险带厚度 {services.Waves.NextDangerRadius}";
            string statusLine = PlayerIsInDangerZone()
                ? "你位于危险区，地震结算会失败！"
                : $"橙红底纹与轮廓为危险区 | 已标记 {CountMarkedCells()} 格";
            if (services.Waves.TimeUntilNextWave <= 5f)
            {
                return $"{countdown}\n危险区正在逼近，立即避开。\n{statusLine}\n{scanLine}";
            }

            return $"{countdown}\n{statusLine}\n{scanLine}";
        }

        private void ApplyScanReadings(IReadOnlyList<ScanReading> readings)
        {
            hasPerformedScan = true;
            lastScanReadings.Clear();
            if (readings != null)
            {
                for (int i = 0; i < readings.Count; i++)
                {
                    lastScanReadings.Add(readings[i]);
                }
            }

            scanIndicatorPresenter?.ShowReadings(lastScanReadings);
        }

        private string DescribeScanFeedback()
        {
            if (lastScanReadings.Count == 0)
            {
                return "探测完成：附近没有可读数的前沿岩壁。";
            }

            if (lastScanReadings.Count == 1)
            {
                ScanReading reading = lastScanReadings[0];
                return $"探测完成：{reading.WallPosition} 上方显示数字 {reading.BombCount}。";
            }

            return $"探测完成：已在 {lastScanReadings.Count} 块前沿岩壁上显示数字。";
        }

        private string BuildScanSummaryText()
        {
            if (!hasPerformedScan)
            {
                return "尚未探测：按 Q 消耗能量显示前沿岩壁数字";
            }

            if (lastScanReadings.Count == 0)
            {
                return "最近探测：附近没有可显示数字的前沿岩壁";
            }

            if (lastScanReadings.Count == 1)
            {
                ScanReading reading = lastScanReadings[0];
                return $"最近探测：{reading.WallPosition} 显示 {reading.BombCount}";
            }

            int highestRisk = 0;
            for (int i = 0; i < lastScanReadings.Count; i++)
            {
                highestRisk = Mathf.Max(highestRisk, lastScanReadings[i].BombCount);
            }

            return $"最近探测：{lastScanReadings.Count} 块前沿岩壁已显示数字，最高风险 {highestRisk}";
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
            if (hudView == null || hudView.UpgradePanel == null)
            {
                return;
            }

            currentCandidates = services.Upgrades.GetCandidates(MinebotHudDefaults.UpgradeButtonCount);
            bool show = currentCandidates.Length > 0;
            hudView.UpgradePanel.SetVisible(show);
            hudView.UpgradePanel.SetTitle(MinebotHudDefaults.UpgradeTitle);

            for (int i = 0; i < MinebotHudDefaults.UpgradeButtonCount; i++)
            {
                bool hasCandidate = i < currentCandidates.Length;
                UpgradeDefinition upgrade = hasCandidate ? currentCandidates[i] : null;
                string label = hasCandidate ? $"{i + 1}. {upgrade.displayName} - {DescribeUpgrade(upgrade)}" : string.Empty;
                hudView.UpgradePanel.SetButton(i, hasCandidate, label);
                if (!hasCandidate)
                {
                    continue;
                }
            }
        }

        private void RefreshBuildPanel()
        {
            if (hudView == null || hudView.BuildPanel == null)
            {
                return;
            }

            bool show = interactionMode == GameplayInteractionMode.Build;
            hudView.BuildPanel.SetVisible(show);
            hudView.BuildPanel.SetTitle(MinebotHudDefaults.BuildTitle);
            for (int i = 0; i < Mathf.Max(MinebotHudDefaults.MinimumBuildButtonCount, availableBuildingDefinitions.Length); i++)
            {
                bool hasDefinition = i < availableBuildingDefinitions.Length;
                string label = string.Empty;
                if (hasDefinition)
                {
                    BuildingDefinition definition = availableBuildingDefinitions[i];
                    string selected = definition == selectedBuildingDefinition ? ">" : " ";
                    label = $"{selected} {definition.DisplayName} | 金属 {definition.Cost.Metal} | {definition.FootprintSize.x}x{definition.FootprintSize.y}";
                }

                hudView.BuildPanel.SetButton(i, hasDefinition, label);
                if (!hasDefinition)
                {
                    continue;
                }
            }
        }

        private void RefreshBuildingInteractionPanel()
        {
            if (hudView == null || hudView.BuildingInteractionPanel == null)
            {
                return;
            }

            bool canUse = CanUseBuildingInteractionButtons();
            bool showRepair = canUse && IsNearRepairStation();
            bool showFactory = canUse && IsNearRobotFactory();
            hudView.BuildingInteractionPanel.SetVisible(showRepair || showFactory);
            hudView.BuildingInteractionPanel.SetTitle(MinebotHudDefaults.BuildingInteractionTitle);
            hudView.BuildingInteractionPanel.SetButton(0, showRepair, $"维修站：维修（金属 {Mathf.Max(0, repairMetalCost)}）");
            hudView.BuildingInteractionPanel.SetButton(1, showFactory, "机器人工厂：生产从属机器人");
        }

        private bool CanUseBuildingInteractionButtons()
        {
            return services != null
                && !services.Vitals.IsDead
                && !services.Experience.HasPendingUpgrade
                && interactionMode == GameplayInteractionMode.Normal;
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
            services.Waves.EvaluateDangerZones();
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
