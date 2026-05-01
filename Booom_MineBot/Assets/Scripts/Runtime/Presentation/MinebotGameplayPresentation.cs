using System;
using System.Collections.Generic;
using Minebot.Automation;
using Minebot.Bootstrap;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.HazardInference;
using Minebot.Progression;
using Minebot.UI;
using Minebot.WaveSurvival;
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
        public static readonly string DgFloorTilemapName = DualGridTerrain.GetTilemapName(TerrainRenderLayerId.Floor);
        public static readonly string DgWallTilemapName = DualGridTerrain.GetTilemapName(TerrainRenderLayerId.Soil);
        public static readonly string DgBoundaryTilemapName = DualGridTerrain.GetTilemapName(TerrainRenderLayerId.Boundary);
        public const string FogDeepTilemapName = "DG Fog Deep Tilemap";
        public const string FogNearTilemapName = "DG Fog Near Tilemap";
        public static readonly string TerrainTilemapName = DgFloorTilemapName;
        public const string FacilityTilemapName = "Facility Tilemap";
        public const string MarkerTilemapName = "Marker Tilemap";
        public const string DangerTilemapName = "Danger Tilemap";
        public const string BuildPreviewTilemapName = "Build Preview Tilemap";
        public const string ScanIndicatorRootName = "Scan Indicator Root";
        public const string PickupRootName = "Pickup Root";
        public const string CellFxRootName = "Cell FX Root";
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
        private Transform pickupRoot;
        private Transform cellFxRoot;
        private Transform buildingRoot;
        private MinebotActorView playerActorView;
        private SpriteRenderer playerView;
        private FreeformActorController playerFreeform;
        private readonly List<MinebotActorView> robotViews = new List<MinebotActorView>();
        private readonly Dictionary<int, MinebotPickupView> pickupViews = new Dictionary<int, MinebotPickupView>();
        private readonly Dictionary<GridPosition, MinebotCellFxView> miningCrackViews = new Dictionary<GridPosition, MinebotCellFxView>();
        private readonly Dictionary<RobotState, float> destroyedRobotVisualExpiry = new Dictionary<RobotState, float>();
        private readonly List<GameObject> buildingViews = new List<GameObject>();
        private static TMP_FontAsset runtimeFontAsset;
        private Texture2D hudMinimapTexture;
        private MinebotHudView hudView;
        private UpgradeDefinition[] currentCandidates = Array.Empty<UpgradeDefinition>();
        private BuildingDefinition[] availableBuildingDefinitions = Array.Empty<BuildingDefinition>();
        private BuildingDefinition selectedBuildingDefinition;
        private GridPosition repairStationPosition;
        private GridPosition robotFactoryPosition;
        private readonly List<ScanReading> lastHazardSenseReadings = new List<ScanReading>();
        private bool hasHazardSenseSnapshot;
        private GridPosition? buildPreviewOrigin;
        private string feedbackMessage = "准备就绪 | 朝岩壁推进即可自动挖掘";
        private GameplayInteractionMode interactionMode = GameplayInteractionMode.Normal;
        private PresentationActorState playerVisualState = PresentationActorState.Idle;
        private float playerVisualHoldRemaining;
        private bool isSubscribed;
        private bool defaultFacilitiesRegistered;

        public TilemapGridPresentation GridPresentation => gridPresentation;
        public GridPosition RepairStationPosition => repairStationPosition;
        public GridPosition RobotFactoryPosition => robotFactoryPosition;
        public string HudSummary => services != null ? BuildLegacyHudSummary() : string.Empty;
        public string FeedbackMessage => feedbackMessage;
        public string WarningSummary => services != null ? BuildWarningText() : string.Empty;
        public int ActiveRobotViewCount
        {
            get
            {
                int count = 0;
                foreach (MinebotActorView view in robotViews)
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

        private void OnDestroy()
        {
            ReleaseHudMinimapTexture();
        }

        private void Update()
        {
            if (services == null)
            {
                return;
            }

            UpdatePlayerVisualState(Time.deltaTime);
            UpdateCameraFraming();

            if (enableWaveTick && !services.Vitals.IsDead && services.Waves.Tick(Time.deltaTime))
            {
                ResolveWave();
            }

            bool hazardSenseUpdated = services.Session.TickPassiveHazardSense(Time.deltaTime);
            bool robotsChanged = services.Session.TickRobots(Time.deltaTime);
            bool pickupRewardsGranted = services.Session.TickWorldPickups(Time.deltaTime, PlayerWorldPosition);
            if (!hazardSenseUpdated && !robotsChanged && !pickupRewardsGranted)
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
            RefreshPickups();
            RefreshBuildings();
            RefreshHud();
        }

        public void RefreshDangerZoneOnly()
        {
            if (services == null)
            {
                return;
            }

            Debug.Log($"[DangerZone] RefreshDangerZoneOnly called");
            EvaluateDangerZones();
            gridPresentation.Refresh(services, repairStationPosition, robotFactoryPosition);
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
            pickupRoot = EnsureChild(presentationRoot, PickupRootName);
            cellFxRoot = EnsureChild(presentationRoot, CellFxRootName);
            buildingRoot = EnsureChild(presentationRoot, "Building Root");

            var unityGrid = gridRoot.GetComponent<UnityEngine.Grid>();
            if (unityGrid == null)
            {
                unityGrid = gridRoot.gameObject.AddComponent<UnityEngine.Grid>();
            }

            unityGrid.cellSize = Vector3.one;
            unityGrid.cellGap = Vector3.zero;

            Tilemap[] terrainFamilies = EnsureTerrainFamilyLayers(gridRoot);
            Vector3 fogOffset = assets != null ? assets.TerrainLayoutSettings.DisplayOffset : DualGridFog.DisplayOffset;
            Tilemap fogDeep = EnsureTilemapLayer(gridRoot, FogDeepTilemapName, 8, fogOffset);
            Tilemap fogNear = EnsureTilemapLayer(gridRoot, FogNearTilemapName, 9, fogOffset);
            Tilemap danger = EnsureTilemapLayer(gridRoot, DangerTilemapName, 10);
            Tilemap facility = EnsureTilemapLayer(gridRoot, FacilityTilemapName, 15);
            Tilemap marker = EnsureTilemapLayer(gridRoot, MarkerTilemapName, 20);
            Tilemap buildPreview = EnsureTilemapLayer(gridRoot, BuildPreviewTilemapName, 25);
            scanIndicatorPresenter = EnsureScanIndicatorPresenter(EnsureChild(gridRoot, ScanIndicatorRootName));
            scanIndicatorPresenter.Configure(assets);

            gridPresentation = gridRoot.GetComponent<TilemapGridPresentation>();
            if (gridPresentation == null)
            {
                gridPresentation = gridRoot.gameObject.AddComponent<TilemapGridPresentation>();
            }

            gridPresentation.Configure(terrainFamilies, fogNear, fogDeep, facility, marker, danger, buildPreview, assets);
            playerActorView = EnsureActorView(actorRoot, PlayerViewName, assets.PlayerActorPrefab, assets.PlayerSprite, 40);
            playerView = playerActorView.BodyRenderer;
            playerFreeform = EnsureFreeformActor(playerActorView.gameObject, services != null ? services.PlayerMiningState.Position : GridPosition.Zero);
            EnsureCircleCollider(playerActorView.gameObject, assets.PlayerColliderRadius);
            EnsureDefaultFacilityBuildings();
            EnsureHud();
            EnsureEventSystem();
        }

        private static FreeformActorController EnsureFreeformActor(GameObject target, GridPosition position)
        {
            FreeformActorController controller = target.GetComponent<FreeformActorController>();
            if (controller == null)
            {
                controller = target.AddComponent<FreeformActorController>();
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
            UpdateCameraFraming(camera);
        }

        private void UpdateCameraFraming()
        {
            UpdateCameraFraming(Camera.main);
        }

        private void UpdateCameraFraming(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            if (services == null)
            {
                camera.transform.position = new Vector3(6f, 6f, -10f);
                camera.orthographicSize = 6.5f;
                return;
            }

            Vector2 size = services.Grid.Size;
            float boardInset = size.x > 4f && size.y > 4f ? 1f : 0f;
            Vector2 interiorMin = new Vector2(boardInset, boardInset);
            Vector2 interiorMax = new Vector2(
                Mathf.Max(interiorMin.x + 1f, size.x - boardInset),
                Mathf.Max(interiorMin.y + 1f, size.y - boardInset));
            Vector2 interiorCenter = (interiorMin + interiorMax) * 0.5f;
            Vector2 playerFocus = PlayerWorldPosition;
            Vector2 focusCenter = Vector2.Lerp(interiorCenter, playerFocus, 0.18f);
            float halfHeight = Mathf.Max(2.5f, (interiorMax.y - interiorMin.y) * 0.5f + 0.2f);
            float halfWidth = Mathf.Max(2.5f, (interiorMax.x - interiorMin.x) * 0.5f + 0.2f);
            float aspect = Mathf.Max(1f, camera.aspect);
            camera.orthographicSize = Mathf.Clamp(Mathf.Max(halfHeight, halfWidth / aspect), 4.6f, 7f);
            float visibleHalfHeight = camera.orthographicSize;
            float visibleHalfWidth = visibleHalfHeight * aspect;
            float minCenterX = visibleHalfWidth;
            float maxCenterX = size.x - visibleHalfWidth;
            float minCenterY = visibleHalfHeight;
            float maxCenterY = size.y - visibleHalfHeight;
            focusCenter.x = minCenterX <= maxCenterX ? Mathf.Clamp(focusCenter.x, minCenterX, maxCenterX) : size.x * 0.5f;
            focusCenter.y = minCenterY <= maxCenterY ? Mathf.Clamp(focusCenter.y, minCenterY, maxCenterY) : size.y * 0.5f;
            camera.transform.position = new Vector3(focusCenter.x, focusCenter.y, -10f);
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

        private Tilemap[] EnsureTerrainFamilyLayers(Transform gridRoot)
        {
            DualGridTerrainLayoutSettings layoutSettings = assets != null
                ? assets.TerrainLayoutSettings
                : DualGridTerrainLayoutSettings.CreateDefault();
            TerrainRenderLayerId[] orderedLayers = DualGridTerrainLayout.OrderedLayers;
            var terrainTilemaps = new Tilemap[orderedLayers.Length];
            for (int i = 0; i < orderedLayers.Length; i++)
            {
                TerrainRenderLayerId layerId = orderedLayers[i];
                terrainTilemaps[i] = EnsureTilemapLayer(
                    gridRoot,
                    DualGridTerrainLayout.GetTilemapName(layerId),
                    DualGridTerrainLayout.GetSortingOrder(layerId, layoutSettings),
                    layoutSettings.DisplayOffset);
            }

            return terrainTilemaps;
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

        private static MinebotActorView EnsureActorView(Transform parent, string objectName, GameObject prefab, Sprite fallbackSprite, int sortingOrder)
        {
            Transform existing = parent.Find(objectName);
            MinebotActorView view = existing != null ? existing.GetComponent<MinebotActorView>() : null;
            if (view == null)
            {
                if (existing != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(existing.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(existing.gameObject);
                    }
                }

                GameObject instance = prefab != null ? Instantiate(prefab, parent, false) : new GameObject(objectName, typeof(MinebotActorView));
                instance.name = objectName;
                view = instance.GetComponent<MinebotActorView>();
                if (view == null)
                {
                    view = instance.AddComponent<MinebotActorView>();
                }
            }

            view.EnsureDefaultStructure(fallbackSprite, sortingOrder);
            return view;
        }

        private void EnsureHud()
        {
            hudView = ResolveHudView();
            hudView.EnsureDefaultStructure(GetDefaultTmpFontAsset(), Mathf.Max(2, availableBuildingDefinitions.Length));
            hudView.ApplyGraphics(
                assets != null ? assets.HudPanelBackground : null,
                assets != null ? assets.HudStatusIcon : null,
                assets != null ? assets.HudInteractionIcon : null,
                assets != null ? assets.HudFeedbackIcon : null,
                assets != null ? assets.HudWarningIcon : null,
                assets != null ? assets.HudUpgradeIcon : null,
                assets != null ? assets.HudBuildIcon : null,
                assets != null ? assets.HudBuildingInteractionIcon : null);

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

            MinebotHudView prefab = assets != null && assets.HudPrefab != null
                ? assets.HudPrefab
                : (hudPrefab != null ? hudPrefab : Resources.Load<MinebotHudView>(MinebotHudView.ResourcePath));
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
            if (services == null || services.Vitals.IsDead || services.Experience.HasPendingUpgrade)
            {
                return;
            }

            if (index < 0)
            {
                return;
            }

            if (index == 2)
            {
                GameplayInputController input = ResolveGameplayInputController();
                input?.ToggleMarkerMode();
                return;
            }

            if (index == 3)
            {
                ShowFeedback("周边危险值会自动刷新，无需手动探测。");
                return;
            }

            if (index >= availableBuildingDefinitions.Length)
            {
                return;
            }

            SetSelectedBuilding(availableBuildingDefinitions[index]);
            if (interactionMode != GameplayInteractionMode.Build)
            {
                SetInteractionMode(GameplayInteractionMode.Build);
            }

            ShowFeedback($"已选择建筑：{availableBuildingDefinitions[index].DisplayName}，点击空地建造。");
        }

        private GameplayInputController ResolveGameplayInputController()
        {
            GameplayInputController controller = GetComponent<GameplayInputController>();
            return controller != null ? controller : FindAnyObjectByType<GameplayInputController>();
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

            services.Session.StateChanged += RefreshDangerZoneOnly;
            services.Session.RewardGranted += OnRewardGranted;
            services.Session.PassiveHazardSenseUpdated += OnPassiveHazardSenseUpdated;
            services.Session.RobotAutomationCompleted += OnRobotAutomationCompleted;
            if (services.WorldPickups != null)
            {
                services.WorldPickups.PickupAbsorbed += OnPickupAbsorbed;
            }

            services.Session.RefreshPassiveHazardSense();
            isSubscribed = true;
        }

        private void UnsubscribeFromServices()
        {
            if (!isSubscribed || services == null)
            {
                return;
            }

            services.Session.StateChanged -= RefreshDangerZoneOnly;
            services.Session.RewardGranted -= OnRewardGranted;
            services.Session.PassiveHazardSenseUpdated -= OnPassiveHazardSenseUpdated;
            services.Session.RobotAutomationCompleted -= OnRobotAutomationCompleted;
            if (services.WorldPickups != null)
            {
                services.WorldPickups.PickupAbsorbed -= OnPickupAbsorbed;
            }
            isSubscribed = false;
        }

        private void OnRewardGranted(ResourceAmount reward)
        {
            if (reward.Metal > 0 || reward.Energy > 0 || reward.Experience > 0)
            {
                feedbackMessage = $"+{reward.Metal} 金属 / +{reward.Energy} 能量 / +{reward.Experience} 经验";
            }
        }

        private void OnPassiveHazardSenseUpdated(IReadOnlyList<ScanReading> readings)
        {
            ApplyHazardSenseReadings(readings);
            scanIndicatorPresenter?.Refresh();
            RefreshHud();
        }

        private void OnRobotAutomationCompleted(RobotAutomationResult result)
        {
            switch (result.Kind)
            {
                case RobotAutomationResultKind.Mined:
                    feedbackMessage = $"从属机器人挖掘完成：+{result.Reward.Metal} 金属 / +{result.Reward.Energy} 能量 / +{result.Reward.Experience} 经验";
                    if (result.ClearedCells.Count > 0)
                    {
                        for (int i = 0; i < result.ClearedCells.Count; i++)
                        {
                            PlayWallBreakFx(result.ClearedCells[i].Position, triggerExplosion: false);
                        }
                    }
                    else
                    {
                        PlayWallBreakFx(result.Target, triggerExplosion: false);
                    }
                    break;
                case RobotAutomationResultKind.Destroyed:
                    destroyedRobotVisualExpiry[result.Robot] = Time.time + 0.35f;
                    if (!result.Target.Equals(GridPosition.Zero) && !string.IsNullOrEmpty(result.Status) && result.Status.Contains("炸药", StringComparison.Ordinal))
                    {
                        PlayWallBreakFx(result.Target, triggerExplosion: true);
                    }

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

        private void OnPickupAbsorbed(WorldPickupAbsorption absorption)
        {
            if (pickupViews.TryGetValue(absorption.Pickup.Id, out MinebotPickupView view) && view != null)
            {
                view.BeginAbsorb(PlayerWorldPosition);
            }
        }

        public void NotifyPlayerMoved()
        {
            SetPlayerVisualState(PresentationActorState.Moving, 0.12f);
        }

        public void NotifyPlayerBlocked()
        {
            SetPlayerVisualState(PresentationActorState.Blocked, 0.18f);
        }

        public void NotifyPlayerMiningContact(GridPosition target)
        {
            SetPlayerVisualState(PresentationActorState.Mining, 0.24f);
            RefreshMiningCrack(target);
        }

        public void NotifyPlayerMineResolved(GridPosition target, MineInteractionResult result)
        {
            SetPlayerVisualState(PresentationActorState.Mining, 0.24f);
            RefreshMiningCrack(target);
            if (result == MineInteractionResult.Mined || result == MineInteractionResult.TriggeredBomb)
            {
                if (result == MineInteractionResult.Mined && services != null)
                {
                    IReadOnlyList<MineClearedCell> clearedCells = services.Session.LastMineResolution.ClearedCells;
                    for (int i = 0; i < clearedCells.Count; i++)
                    {
                        PlayWallBreakFx(clearedCells[i].Position, triggerExplosion: false);
                    }
                }
                else
                {
                    PlayWallBreakFx(target, result == MineInteractionResult.TriggeredBomb);
                }
            }
            else
            {
                NotifyPlayerBlocked();
            }
        }

        private void RefreshActors()
        {
            if (playerFreeform == null)
            {
                playerFreeform = EnsureFreeformActor(playerActorView.gameObject, services.PlayerMiningState.Position);
            }

            PresentationActorState playerState = services.Vitals.IsDead ? PresentationActorState.Destroyed : playerVisualState;
            playerActorView.ApplyState(assets.PlayerActorStates, playerState, assets.PlayerSprite, Color.white);

            int visibleRobotCount = 0;
            for (int i = 0; i < services.Robots.Count; i++)
            {
                RobotState robot = services.Robots[i];
                bool showDestroyedVisual = !robot.IsActive
                    && destroyedRobotVisualExpiry.TryGetValue(robot, out float visibleUntil)
                    && Time.time <= visibleUntil;
                if (!robot.IsActive && !showDestroyedVisual)
                {
                    continue;
                }

                MinebotActorView robotView = EnsureRobotView(visibleRobotCount);
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

                PresentationActorState actorState = !robot.IsActive ? PresentationActorState.Destroyed : ToPresentationActorState(robot.Activity);
                robotView.ApplyState(assets.HelperRobotStates, actorState, assets.RobotSprite, ColorForRobotActivity(robot.Activity));
                robotView.gameObject.SetActive(true);
                visibleRobotCount++;
            }

            for (int i = visibleRobotCount; i < robotViews.Count; i++)
            {
                robotViews[i].gameObject.SetActive(false);
            }
        }

        private MinebotActorView EnsureRobotView(int index)
        {
            while (robotViews.Count <= index)
            {
                int displayIndex = robotViews.Count + 1;
                MinebotActorView view = EnsureActorView(actorRoot, $"Robot View {displayIndex}", assets.HelperRobotPrefab, assets.RobotSprite, 40);
                if (view.GetComponent<HelperRobotMotionController>() == null)
                {
                    view.gameObject.AddComponent<HelperRobotMotionController>();
                }

                EnsureCircleCollider(view.gameObject, 0.28f);
                view.gameObject.SetActive(false);
                robotViews.Add(view);
            }

            return robotViews[index];
        }

        private void RefreshPickups()
        {
            if (services == null || services.WorldPickups == null || pickupRoot == null)
            {
                return;
            }

            var activeIds = new HashSet<int>();
            IReadOnlyList<WorldPickupState> activePickups = services.WorldPickups.ActivePickups;
            for (int i = 0; i < activePickups.Count; i++)
            {
                WorldPickupState pickup = activePickups[i];
                activeIds.Add(pickup.Id);
                MinebotPickupView view = EnsurePickupView(pickup);
                view.Bind(pickup, assets.PickupIconFor(pickup.Type), GridToWorld(pickup.Origin), 34);
            }

            var staleIds = new List<int>();
            foreach (KeyValuePair<int, MinebotPickupView> pair in pickupViews)
            {
                if (pair.Value == null)
                {
                    staleIds.Add(pair.Key);
                    continue;
                }

                if (!activeIds.Contains(pair.Key) && !pair.Value.IsAbsorbingVisual)
                {
                    Destroy(pair.Value.gameObject);
                    staleIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < staleIds.Count; i++)
            {
                pickupViews.Remove(staleIds[i]);
            }
        }

        private MinebotPickupView EnsurePickupView(WorldPickupState pickup)
        {
            if (pickupViews.TryGetValue(pickup.Id, out MinebotPickupView existing) && existing != null)
            {
                return existing;
            }

            GameObject prefab = assets.PickupPrefabFor(pickup.Type);
            GameObject instance = prefab != null ? Instantiate(prefab, pickupRoot, false) : new GameObject($"Pickup {pickup.Id}", typeof(MinebotPickupView));
            instance.name = $"Pickup {pickup.Id} - {pickup.Type}";
            instance.transform.SetParent(pickupRoot, false);
            MinebotPickupView view = instance.GetComponent<MinebotPickupView>();
            if (view == null)
            {
                view = instance.AddComponent<MinebotPickupView>();
            }

            pickupViews[pickup.Id] = view;
            return view;
        }

        private void RefreshMiningCrack(GridPosition target)
        {
            if (cellFxRoot == null)
            {
                return;
            }

            if (!miningCrackViews.TryGetValue(target, out MinebotCellFxView view) || view == null)
            {
                GameObject prefab = assets.MiningCrackPrefab;
                GameObject instance = prefab != null ? Instantiate(prefab, cellFxRoot, false) : new GameObject($"Mining Crack {target}", typeof(MinebotCellFxView));
                instance.name = $"Mining Crack {target}";
                instance.transform.SetParent(cellFxRoot, false);
                view = instance.GetComponent<MinebotCellFxView>();
                if (view == null)
                {
                    view = instance.AddComponent<MinebotCellFxView>();
                }

                miningCrackViews[target] = view;
            }

            view.RefreshPersistent(assets.MiningCrackSequence, GridToWorld(target), 36);
        }

        private void PlayWallBreakFx(GridPosition target, bool triggerExplosion)
        {
            if (cellFxRoot == null)
            {
                return;
            }

            GameObject prefab = triggerExplosion && assets.ExplosionPrefab != null
                ? assets.ExplosionPrefab
                : assets.WallBreakPrefab;
            GameObject instance = prefab != null ? Instantiate(prefab, cellFxRoot, false) : new GameObject($"Cell Fx {target}", typeof(MinebotCellFxView));
            instance.name = triggerExplosion ? $"Wall Break + Explosion {target}" : $"Wall Break {target}";
            instance.transform.SetParent(cellFxRoot, false);
            MinebotCellFxView view = instance.GetComponent<MinebotCellFxView>();
            if (view == null)
            {
                view = instance.AddComponent<MinebotCellFxView>();
            }

            view.PlayOneShot(
                assets.WallBreakSequence,
                GridToWorld(target),
                37,
                triggerExplosion ? assets.ExplosionSequence : null,
                0.08f);
        }

        private void UpdatePlayerVisualState(float deltaTime)
        {
            if (services != null && services.Vitals.IsDead)
            {
                playerVisualState = PresentationActorState.Destroyed;
                playerVisualHoldRemaining = 0f;
                return;
            }

            if (playerVisualState == PresentationActorState.Destroyed)
            {
                playerVisualState = PresentationActorState.Idle;
            }

            playerVisualHoldRemaining = Mathf.Max(0f, playerVisualHoldRemaining - Mathf.Max(0f, deltaTime));
            if (playerVisualHoldRemaining <= 0f)
            {
                playerVisualState = PresentationActorState.Idle;
            }
        }

        private void SetPlayerVisualState(PresentationActorState state, float holdDuration)
        {
            if (services != null && services.Vitals.IsDead)
            {
                playerVisualState = PresentationActorState.Destroyed;
                playerVisualHoldRemaining = 0f;
                return;
            }

            playerVisualState = state;
            playerVisualHoldRemaining = Mathf.Max(playerVisualHoldRemaining, Mathf.Max(0.02f, holdDuration));
        }

        private static PresentationActorState ToPresentationActorState(RobotActivity activity)
        {
            switch (activity)
            {
                case RobotActivity.Moving:
                    return PresentationActorState.Moving;
                case RobotActivity.Mining:
                    return PresentationActorState.Mining;
                case RobotActivity.Blocked:
                    return PresentationActorState.Blocked;
                case RobotActivity.Destroyed:
                    return PresentationActorState.Destroyed;
                default:
                    return PresentationActorState.Idle;
            }
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
                return (assets.RobotFactoryTile as Tile)?.sprite ?? (assets.EmptyTile as Tile)?.sprite;
            }

            Tile repairStationTile = assets.RepairStationTile as Tile;
            Tile emptyTile = assets.EmptyTile as Tile;
            return repairStationTile != null ? repairStationTile.sprite : emptyTile?.sprite;
        }

        private void RefreshHud()
        {
            if (services == null || hudView == null)
            {
                return;
            }

            ResourceAmount resources = services.Economy.Resources;
            CountRobotStates(out _, out int working, out int waiting, out _);
            if (hudView.StatusPanel != null)
            {
                hudView.StatusPanel.SetText(BuildRobotStatusPanelText());
            }
            hudView.UpdateTemplateRobotStatus(working, waiting);
            hudView.UpdateTemplateResources(
                resources.Metal,
                resources.Energy,
                services.Vitals.CurrentHealth,
                services.Vitals.MaxHealth,
                services.Experience.Experience,
                services.Experience.NextThreshold);

            if (hudView.InteractionPanel != null)
            {
                hudView.InteractionPanel.SetText(BuildResourcePanelText(resources));
            }

            if (hudView.FeedbackPanel != null)
            {
                hudView.FeedbackPanel.SetText(BuildActionTagText());
                hudView.FeedbackPanel.SetColor(BuildActionTagColor());
            }

            if (hudView.WarningPanel != null)
            {
                hudView.WarningPanel.SetText(BuildWaveHeaderText());
                hudView.WarningPanel.SetColor(services.Waves.TimeUntilNextWave <= WaveSurvivalService.DangerWarningLeadTime
                    ? new Color(1f, 0.2f, 0.58f, 1f)
                    : new Color(1f, 0.96f, 0.22f, 1f));
            }
            hudView.UpdateTemplateWaveStatus(
                Mathf.Max(1, services.Waves.CurrentWave + 1),
                services.Waves.TimeUntilNextWave,
                services.Waves.WaveInterval);

            if (hudView.GameOverPanel != null)
            {
                hudView.GameOverPanel.SetVisible(services.Vitals.IsDead);
                hudView.GameOverPanel.SetText(services.Vitals.IsDead ? "任务失败\n核心机体已失效" : string.Empty);
            }

            RefreshMinimapPanel();
            RefreshUpgradePanel();
            RefreshBuildPanel();
            RefreshBuildingInteractionPanel();
        }

        private string BuildInteractionText()
        {
            const string baseHint = "WASD 移动  贴墙即挖掘  自动感知周边风险  E 标记  点击底栏选建筑  R 开关建造  1-3 升级";
            if (services.Experience.HasPendingUpgrade)
            {
                return "升级可用：按 1/2/3 或点击右下升级卡片";
            }

            if (interactionMode == GameplayInteractionMode.Marker)
            {
                return "标记模式：点击岩壁切换标记，E / 右键 / Esc 退出";
            }

            if (interactionMode == GameplayInteractionMode.Build)
            {
                string selected = GetSelectedBuildingOrDefault() != null ? GetSelectedBuildingOrDefault().DisplayName : "未选择";
                string preview = buildPreviewOrigin.HasValue
                    ? (CanPlaceBuildingAt(GetSelectedBuildingOrDefault(), buildPreviewOrigin.Value) ? "当前位置可建" : "当前位置不可建")
                    : "移动鼠标选空地";
                return $"建筑模式：{selected} | {preview} | 点击底栏切换，R / 右键 / Esc 退出";
            }

            if (IsNearRepairStation() && IsNearRobotFactory())
            {
                return "可交互：右侧卡片可维修或生产从属机器人";
            }

            if (IsNearRepairStation())
            {
                return "可交互：右侧卡片可在维修站恢复生命";
            }

            if (IsNearRobotFactory())
            {
                return "可交互：右侧卡片可生产从属机器人";
            }

            return baseHint;
        }

        private string BuildLegacyHudSummary()
        {
            ResourceAmount resources = services.Economy.Resources;
            return
                $"HP {services.Vitals.CurrentHealth}/{services.Vitals.MaxHealth} | 金属 {resources.Metal} | 能量 {resources.Energy} | 波次 {services.Waves.CurrentWave}\n" +
                $"Lv {services.Experience.Level} | XP {services.Experience.Experience}/{services.Experience.NextThreshold} | 钻头 {ToChineseHardnessText(services.PlayerMiningState.DrillTier)}\n" +
                $"坐标 {services.PlayerMiningState.Position} | {BuildRobotStatusText()}";
        }

        private string BuildRobotStatusPanelText()
        {
            CountRobotStates(out int active, out int working, out int waiting, out _);
            return
                $"<color=#D9FF1A>工作中 x{working}</color>\n" +
                $"<color=#FF1787>待机中 x{waiting}</color>\n" +
                $"<color=#FFFFFF>在线 {active}</color>";
        }

        private static string BuildResourcePanelText(ResourceAmount resources)
        {
            return
                $"<color=#18F0FF>金属 {resources.Metal}</color>\n" +
                $"<color=#18F0FF>能量 {resources.Energy}</color>";
        }

        private string BuildWaveHeaderText()
        {
            int displayWave = Mathf.Max(1, services.Waves.CurrentWave + 1);
            return
                $"WAVE {displayWave}\n" +
                $"{Mathf.Max(0f, services.Waves.TimeUntilNextWave):00.0}s | 厚度 {services.Waves.NextDangerRadius}";
        }

        private string BuildActionTagText()
        {
            if (services.Experience.HasPendingUpgrade)
            {
                return "升级待选";
            }

            if (interactionMode == GameplayInteractionMode.Build)
            {
                return "建造";
            }

            if (interactionMode == GameplayInteractionMode.Marker)
            {
                return "标记";
            }

            if (PlayerIsInDangerZone())
            {
                return "撤离";
            }

            if (string.IsNullOrEmpty(feedbackMessage))
            {
                return "自动感知";
            }

            return feedbackMessage.Length <= 10 ? feedbackMessage : feedbackMessage.Substring(0, 10);
        }

        private Color BuildActionTagColor()
        {
            if (services.Experience.HasPendingUpgrade)
            {
                return new Color(1f, 0.2f, 0.58f, 1f);
            }

            if (PlayerIsInDangerZone())
            {
                return new Color(1f, 0.76f, 0.16f, 1f);
            }

            return new Color(0.16f, 0.94f, 1f, 1f);
        }

        private string BuildWarningText()
        {
            string scanLine = BuildHazardSenseSummaryText();
            string countdown = $"地震波 {Mathf.Max(0f, services.Waves.TimeUntilNextWave):0.0}s | 下一波厚度 {services.Waves.NextDangerRadius}";
            if (services.Waves.TimeUntilNextWave <= WaveSurvivalService.DangerWarningLeadTime)
            {
                return $"{countdown}\n危险区逼近，立即避开 | {scanLine}";
            }

            string statusLine = PlayerIsInDangerZone()
                ? "你在危险区内，立即撤离"
                : $"危险区红色轮廓 | 已标记 {CountMarkedCells()} 格";
            return $"{countdown}\n{statusLine} | {scanLine}";
        }

        private void ApplyHazardSenseReadings(IReadOnlyList<ScanReading> readings)
        {
            hasHazardSenseSnapshot = true;
            lastHazardSenseReadings.Clear();
            if (readings != null)
            {
                for (int i = 0; i < readings.Count; i++)
                {
                    lastHazardSenseReadings.Add(readings[i]);
                }
            }

            scanIndicatorPresenter?.ShowReadings(lastHazardSenseReadings);
        }

        private string BuildHazardSenseSummaryText()
        {
            if (!hasHazardSenseSnapshot)
            {
                return "周边感知启动中";
            }

            if (lastHazardSenseReadings.Count == 0)
            {
                return "周边感知：附近无可读前沿岩壁";
            }

            if (lastHazardSenseReadings.Count == 1)
            {
                ScanReading reading = lastHazardSenseReadings[0];
                return $"周边感知：{reading.WallPosition} = {reading.BombCount}";
            }

            int highestRisk = 0;
            for (int i = 0; i < lastHazardSenseReadings.Count; i++)
            {
                highestRisk = Mathf.Max(highestRisk, lastHazardSenseReadings[i].BombCount);
            }

            return $"周边感知：{lastHazardSenseReadings.Count} 处，最高风险 {highestRisk}";
        }

        private string BuildRobotStatusText()
        {
            CountRobotStates(out int active, out int working, out int waiting, out int blocked);
            return $"从属机器人 {active} | 工作 {working} 待命 {waiting} 阻塞 {blocked}";
        }

        private void CountRobotStates(out int active, out int working, out int waiting, out int blocked)
        {
            active = 0;
            working = 0;
            waiting = 0;
            blocked = 0;
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
                    waiting++;
                }
                else
                {
                    waiting++;
                }
            }
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
            if (hudView == null)
            {
                return;
            }

            bool show = services != null
                && !services.Vitals.IsDead
                && !services.Experience.HasPendingUpgrade;
            if (hudView.BuildPanel != null)
            {
                hudView.BuildPanel.SetVisible(show);
            }
            if (!show)
            {
                for (int i = 0; i < MinebotHudDefaults.MinimumBuildButtonCount; i++)
                {
                    hudView.SetTemplateBuildButton(i, false, string.Empty, false);
                }
                return;
            }

            if (hudView.BuildPanel != null)
            {
                hudView.BuildPanel.SetTitle("主操作");
            }
            BuildingDefinition selectedDefinition = GetSelectedBuildingOrDefault();
            for (int i = 0; i < MinebotHudDefaults.MinimumBuildButtonCount; i++)
            {
                bool visible = true;
                string label;
                bool selected;
                if (i < availableBuildingDefinitions.Length)
                {
                    BuildingDefinition definition = availableBuildingDefinitions[i];
                    selected = definition == selectedDefinition;
                    label = $"{i + 1}\n{definition.DisplayName}\n{definition.Cost.Metal}金";
                }
                else if (i == 2)
                {
                    selected = interactionMode == GameplayInteractionMode.Marker;
                    label = selected ? "E\n退出\n标记" : "E\n标记\n风险";
                }
                else if (i == 3)
                {
                    selected = false;
                    label = "AUTO\n周边\n感知";
                }
                else
                {
                    visible = false;
                    selected = false;
                    label = string.Empty;
                }

                if (hudView.BuildPanel != null)
                {
                    hudView.BuildPanel.SetButton(i, visible, label, selected);
                }
                hudView.SetTemplateBuildButton(i, visible, label, selected);
            }
        }

        private void RefreshMinimapPanel()
        {
            if (hudView == null || hudView.MinimapPanel == null)
            {
                return;
            }

            if (services == null)
            {
                hudView.MinimapPanel.SetVisible(false);
                return;
            }

            Vector2Int size = services.Grid.Size;
            if (size.x <= 0 || size.y <= 0)
            {
                hudView.MinimapPanel.SetVisible(false);
                return;
            }

            if (hudMinimapTexture == null || hudMinimapTexture.width != size.x || hudMinimapTexture.height != size.y)
            {
                ReleaseHudMinimapTexture();
                hudMinimapTexture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false)
                {
                    name = "HUD Minimap",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            foreach (GridPosition position in services.Grid.Positions())
            {
                GridCellState cell = services.Grid.GetCell(position);
                hudMinimapTexture.SetPixel(position.X, size.y - 1 - position.Y, ColorForMinimapCell(cell));
            }

            if (services.Grid.IsInside(repairStationPosition))
            {
                hudMinimapTexture.SetPixel(repairStationPosition.X, size.y - 1 - repairStationPosition.Y, new Color32(72, 220, 255, 255));
            }

            if (services.Grid.IsInside(robotFactoryPosition))
            {
                hudMinimapTexture.SetPixel(robotFactoryPosition.X, size.y - 1 - robotFactoryPosition.Y, new Color32(255, 142, 64, 255));
            }

            foreach (RobotState robot in services.Robots)
            {
                if (robot.IsActive && services.Grid.IsInside(robot.Position))
                {
                    hudMinimapTexture.SetPixel(robot.Position.X, size.y - 1 - robot.Position.Y, new Color32(112, 255, 148, 255));
                }
            }

            GridPosition playerPosition = services.PlayerMiningState.Position;
            if (services.Grid.IsInside(playerPosition))
            {
                hudMinimapTexture.SetPixel(playerPosition.X, size.y - 1 - playerPosition.Y, new Color32(40, 168, 255, 255));
            }

            hudMinimapTexture.Apply(false, false);
            hudView.MinimapPanel.SetTexture(hudMinimapTexture);
            hudView.MinimapPanel.SetSummary($"已标记 {CountMarkedCells()} | 危险 {CountDangerZoneCells()}");
            hudView.MinimapPanel.SetVisible(true);
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

        private int CountDangerZoneCells()
        {
            int count = 0;
            foreach (GridPosition position in services.Grid.Positions())
            {
                GridCellState cell = services.Grid.GetCell(position);
                if (cell.TerrainKind == TerrainKind.Empty && cell.IsDangerZone)
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
                services.Session.SpawnWorldPickupReward(services.PlayerMiningState.Position, resolution.DroppedResources, WorldPickupSource.WaveRecycle);
            }

            EvaluateDangerZones();
            feedbackMessage = resolution.PlayerKilled
                ? "地震波吞没了主机器人。"
                : $"地震波结算：存活到第 {resolution.SurvivedWave} 波，损失机器人 {resolution.RobotsDestroyed}。";
            RefreshAll();
        }

        private void ReleaseHudMinimapTexture()
        {
            if (hudMinimapTexture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(hudMinimapTexture);
            }
            else
            {
                DestroyImmediate(hudMinimapTexture);
            }

            hudMinimapTexture = null;
        }

        private static Color32 ColorForMinimapCell(GridCellState cell)
        {
            if (DualGridFog.IsSolid(cell))
            {
                return new Color32(10, 14, 18, 255);
            }

            if (cell.IsMarked)
            {
                return new Color32(32, 232, 220, 255);
            }

            if (cell.IsOccupiedByBuilding)
            {
                return new Color32(196, 206, 212, 255);
            }

            if (cell.TerrainKind == TerrainKind.Empty)
            {
                return cell.IsDangerZone
                    ? new Color32(196, 64, 58, 255)
                    : new Color32(128, 92, 58, 255);
            }

            if (cell.TerrainKind == TerrainKind.Indestructible)
            {
                return new Color32(28, 34, 40, 255);
            }

            switch (cell.HardnessTier)
            {
                case HardnessTier.Stone:
                    return new Color32(116, 122, 128, 255);
                case HardnessTier.HardRock:
                    return new Color32(82, 90, 102, 255);
                case HardnessTier.UltraHard:
                    return new Color32(56, 64, 78, 255);
                default:
                    return new Color32(150, 128, 92, 255);
            }
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
