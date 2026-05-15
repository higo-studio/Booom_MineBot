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
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Minebot.Presentation
{
    [DefaultExecutionOrder(-50)]
    [MinebotRuntimeTag(MinebotRuntimeTag.Consumer)]
    public sealed class MinebotGameplayPresentation : MonoBehaviour
    {
        private static readonly ProfilerMarker RefreshAfterTerrainChangedProfilerMarker = new("Minebot.GameplayPresentation.RefreshAfterTerrainChanged");
        private static readonly ProfilerMarker RefreshAfterTerrainChangedDangerProfilerMarker = new("Minebot.GameplayPresentation.RefreshAfterTerrainChanged.DangerZones");
        private static readonly ProfilerMarker RefreshAfterTerrainChangedGridProfilerMarker = new("Minebot.GameplayPresentation.RefreshAfterTerrainChanged.GridRefresh");
        private static readonly ProfilerMarker RefreshAfterTerrainChangedViewProfilerMarker = new("Minebot.GameplayPresentation.RefreshAfterTerrainChanged.Views");

        public const string PresentationRootName = "Presentation Root";
        public static readonly string DgFloorTilemapName = DualGridTerrain.GetTilemapName(TerrainRenderLayerId.Floor);
        public static readonly string DgWallTilemapName = DualGridTerrain.GetTilemapName(TerrainRenderLayerId.Soil);
        public static readonly string DgBoundaryTilemapName = DualGridTerrain.GetTilemapName(TerrainRenderLayerId.Boundary);
        public const string FogDeepTilemapName = "DG Fog Deep Tilemap";
        public const string FogNearTilemapName = "DG Fog Near Tilemap";
        public static readonly string TerrainTilemapName = DgFloorTilemapName;
        public const string FacilityTilemapName = "Facility Tilemap";
        public const string MarkerTilemapName = "Marker Tilemap";
        public const string GmBombTilemapName = "GM Bomb Tilemap";
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
        private const string RepairStationBuildingId = "repair-station";
        private const string RobotFactoryBuildingId = "robot-factory";

        [SerializeField]
        private bool autoInitializeServices = true;

        [SerializeField]
        private bool enableWaveTick = true;

        [SerializeField]
        [InspectorLabel("启动配置（留空则自动查找）")]
        private BootstrapConfig bootstrapConfig;

        [SerializeField]
        private MinebotPresentationArtSet artSet;

        [SerializeField]
        private MinebotHudView hudPrefab;

        [SerializeField]
        private int repairMetalCost = 2;

        private MinebotContainer serviceContainer;
        private RuntimeServiceRegistry services;
        private MinebotPresentationAssets assets;
        private TilemapGridPresentation gridPresentation;
        private ScanIndicatorPresenter scanIndicatorPresenter;
        private Transform actorRoot;
        private Transform pickupRoot;
        private Transform cellFxRoot;
        private Transform cameraRig;
        private Transform buildingRoot;
        private MinebotPickupRenderer pickupRenderer;
        private MinebotActorView playerActorView;
        private SpriteRenderer playerView;
        private FreeformActorController playerFreeform;
        private readonly List<MinebotActorView> robotViews = new List<MinebotActorView>();
        private readonly Dictionary<GridPosition, MinebotCellFxView> miningCrackViews = new Dictionary<GridPosition, MinebotCellFxView>();
        private readonly Dictionary<RobotState, float> destroyedRobotVisualExpiry = new Dictionary<RobotState, float>();
        private readonly List<GameObject> buildingViews = new List<GameObject>();
        private MinebotHudView hudView;
        private MinebotGameplayAudioController audioController;
        private UpgradeDefinition[] currentCandidates = Array.Empty<UpgradeDefinition>();
        private BuildingDefinition[] availableBuildingDefinitions = Array.Empty<BuildingDefinition>();
        private BuildingDefinition selectedBuildingDefinition;
        private GridPosition repairStationPosition;
        private GridPosition robotFactoryPosition;
        private readonly List<ScanReading> lastHazardSenseReadings = new List<ScanReading>();
        private bool hasHazardSenseSnapshot;
        private BuildingDefinition buildPreviewDefinition;
        private GridPosition? buildPreviewOrigin;
        private bool buildPreviewIsValid;
        private bool gmBombRevealEnabled;
        private string feedbackMessage = "准备就绪 | 朝岩壁推进即可自动挖掘";
        private GameplayInteractionMode interactionMode = GameplayInteractionMode.Normal;
        private PresentationActorState playerVisualState = PresentationActorState.Idle;
        private float playerVisualHoldRemaining;
        private ActorFacingDirection currentPlayerFacingDirection = ActorFacingDirection.Front;
        private bool currentPlayerFacingLeft = false;
        private bool isSubscribed;
        private bool defaultFacilitiesRegistered;
        private string leaderboardNameInput = "PLAYER";
        private bool leaderboardSubmitted;
        private MinebotRankPageView rankPageView;
        private int rankPageShownFrame = -1;

        public TilemapGridPresentation GridPresentation => gridPresentation;
        public MinebotPickupRenderer PickupRenderer => pickupRenderer;
        public GridPosition RepairStationPosition => ResolveFacilityPosition(RepairStationBuildingId, repairStationPosition);
        public GridPosition RobotFactoryPosition => ResolveFacilityPosition(RobotFactoryBuildingId, robotFactoryPosition);
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

        public bool IsUpgradePanelShowing => hudView != null && hudView.UpgradePanel != null && hudView.UpgradePanel.IsVisible;
        public bool IsRepairInteractionButtonShowing => hudView != null && hudView.RepairStationInteractionButton != null && hudView.RepairStationInteractionButton.gameObject.activeInHierarchy;
        public bool IsRobotFactoryInteractionButtonShowing => hudView != null && hudView.RobotFactoryInteractionButton != null && hudView.RobotFactoryInteractionButton.gameObject.activeInHierarchy;
        public bool IsGameOver => services != null && services.Vitals.IsDead;
        public bool IsGmBombRevealEnabled => gmBombRevealEnabled;
        public bool IsUsingConfiguredArtSet => assets != null && assets.IsUsingConfiguredArtSet;
        public GameplayInteractionMode InteractionMode => interactionMode;
        public RuntimeServiceRegistry Services => services;
        public BootstrapConfig ActiveBootstrapConfig => bootstrapConfig;
        internal MinebotGameplayAudioController AudioController => audioController;
        public Vector2 PlayerWorldPosition => playerFreeform != null
            ? playerFreeform.WorldPosition
            : (services != null ? (Vector2)GridToWorld(services.PlayerMiningState.Position) : Vector2.zero);
        public IReadOnlyList<BuildingDefinition> AvailableBuildingDefinitions => availableBuildingDefinitions;

        /// <summary>
        /// 设置玩家朝向方向（由输入控制器调用）
        /// </summary>
        public void SetPlayerFacingDirection(GridPosition direction)
        {
            if (direction.X > 0)
            {
                currentPlayerFacingDirection = ActorFacingDirection.Side;
                currentPlayerFacingLeft = false;
            }
            else if (direction.X < 0)
            {
                currentPlayerFacingDirection = ActorFacingDirection.Side;
                currentPlayerFacingLeft = true;
            }
            else if (direction.Y > 0)
            {
                currentPlayerFacingDirection = ActorFacingDirection.Back;
                currentPlayerFacingLeft = false;
            }
            else
            {
                currentPlayerFacingDirection = ActorFacingDirection.Front;
                currentPlayerFacingLeft = false;
            }
        }

        public void InjectServices(
            LogicalGridState grid,
            PlayerMiningState playerMiningState,
            MiningService mining,
            HazardService hazards,
            GameSessionService session,
            UpgradeSelectionService upgrades,
            ScoreService scores,
            PlayerEconomy economy,
            PlayerVitals vitals,
            ExperienceService experience,
            WorldPickupService worldPickups,
            BaseOpsService baseOps,
            BuildingPlacementService buildings,
            IReadOnlyList<BuildingDefinition> buildingDefinitions,
            RobotAutomationService robotAutomation,
            RobotFactoryService robotFactory,
            IReadOnlyList<RobotState> robots,
            WaveSurvivalService waves,
            BootstrapConfig injectedConfig)
        {
            if (grid == null
                || playerMiningState == null
                || mining == null
                || hazards == null
                || session == null
                || upgrades == null
                || economy == null
                || vitals == null
                || experience == null
                || worldPickups == null
                || baseOps == null
                || buildings == null
                || robotAutomation == null
                || robotFactory == null
                || robots == null
                || waves == null)
            {
                return;
            }

            if (serviceContainer == null && MinebotRuntimeDiscovery.TryResolveContainer(out MinebotContainer runtimeContainer))
            {
                serviceContainer = runtimeContainer;
            }

            var injectedServices = new RuntimeServiceRegistry(
                grid,
                playerMiningState,
                mining,
                hazards,
                session,
                upgrades,
                scores,
                economy,
                vitals,
                experience,
                worldPickups,
                baseOps,
                buildings,
                buildingDefinitions,
                robotAutomation,
                robotFactory,
                robots,
                waves);

            if (services != null && ReferenceEquals(services.Session, injectedServices.Session))
            {
                if (bootstrapConfig == null && injectedConfig != null)
                {
                    bootstrapConfig = injectedConfig;
                }

                DiscoverAndInjectTaggedConsumers();
                return;
            }

            if (isSubscribed)
            {
                UnsubscribeFromServices();
            }

            services = injectedServices;
            if (injectedConfig != null)
            {
                bootstrapConfig = injectedConfig;
            }

            InitializeServiceBackedState();
            DiscoverAndInjectTaggedConsumers();

            if (isActiveAndEnabled)
            {
                SubscribeToServices();
            }

            if (gridPresentation != null)
            {
                RefreshAll();
            }
        }

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
            audioController?.StopTransientLoops();
            UnsubscribeFromServices();
        }

        private void OnDestroy()
        {
        }

        private void Update()
        {
            if (services == null)
            {
                return;
            }

            if (!services.Vitals.IsDead)
            {
                leaderboardSubmitted = false;
            }

            UpdatePlayerVisualState(Time.deltaTime);
            UpdateCameraFraming();
            audioController?.SyncState(services, playerActorView != null ? playerActorView.transform : transform, GetFirstRobotMiningAnchor());
            bool waveResolutionActive = services.Session != null && services.Session.IsWaveResolutionActive;
            bool pauseSimulation = IsSimulationPausedByUpgradePanel();
            gridPresentation?.SetWaveCountdownPaused(pauseSimulation || waveResolutionActive);

            // 处理排行榜页面点击返回启动页
            if (rankPageView != null && rankPageView.IsVisible && Time.frameCount > rankPageShownFrame)
            {
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
                {
                    rankPageView.SetVisible(false);
                    LoadBootstrapScene();
                }
            }
        }

        private void FixedUpdate()
        {
            if (services == null)
            {
                return;
            }

            TickSimulation(Time.fixedDeltaTime);
        }

        private void TickSimulation(float deltaTime)
        {
            bool waveResolutionActive = services.Session != null && services.Session.IsWaveResolutionActive;
            bool pauseSimulation = IsSimulationPausedByUpgradePanel();
            gridPresentation?.SetWaveCountdownPaused(pauseSimulation || waveResolutionActive);

            if (waveResolutionActive)
            {
                bool waveResolutionChanged = services.Session.TickWaveResolution(deltaTime);
                if (waveResolutionChanged)
                {
                    RefreshFromCurrentGridState();
                }
                else
                {
                    RefreshHud();
                }

                return;
            }

            if (pauseSimulation)
            {
                RefreshHud();
                return;
            }

            if (enableWaveTick && !services.Vitals.IsDead && services.Waves.Tick(deltaTime))
            {
                if (services.Session.BeginWaveResolution())
                {
                    bool waveResolutionChanged = services.Session.TickWaveResolution(0f);
                    if (waveResolutionChanged)
                    {
                        RefreshFromCurrentGridState();
                    }
                    else
                    {
                        RefreshHud();
                    }

                    return;
                }
            }

            bool miningRecoveryChanged = services.Session.TickMiningRecovery(deltaTime);
            bool hazardSenseUpdated = services.Session.TickPassiveHazardSense(deltaTime);
            bool robotsChanged = services.Session.TickRobots(deltaTime);
            bool pickupRewardsGranted = services.Session.TickWorldPickups(deltaTime, PlayerWorldPosition);
            if (robotsChanged)
            {
                RefreshAfterRobotAutomationTick();
            }

            if (pickupRewardsGranted)
            {
                RefreshAfterPickupCollection();
            }

            if (!miningRecoveryChanged && !hazardSenseUpdated && !robotsChanged && !pickupRewardsGranted)
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

            if (services.Session == null || !services.Session.IsWaveResolutionActive)
            {
                EvaluateDangerZones();
            }

            RefreshFromCurrentGridState();
        }

        public void RefreshDangerZoneOnly()
        {
            if (services == null)
            {
                return;
            }

            Debug.Log($"[DangerZone] RefreshDangerZoneOnly called");
            if (services.Session == null || !services.Session.IsWaveResolutionActive)
            {
                EvaluateDangerZones();
            }

            RefreshFromCurrentGridState();
        }

        public void ShowFeedback(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                feedbackMessage = message;
            }

            if (services != null)
            {
                RefreshActors();
            }

            RefreshHud();
        }

        public void RefreshAfterPlayerMoved()
        {
            if (services == null)
            {
                return;
            }

            RefreshActors();
            RefreshHud();
        }

        public void RefreshAfterMarkerChanged(GridPosition changedCell)
        {
            if (services == null)
            {
                return;
            }

            gridPresentation.RefreshMarkerCellsOnly(services.Grid, new[] { changedCell });
            RefreshHud();
        }

        public bool ToggleGmBombReveal()
        {
            gmBombRevealEnabled = !gmBombRevealEnabled;
            gridPresentation?.SetGmBombRevealEnabled(gmBombRevealEnabled);
            if (services != null)
            {
                RefreshFromCurrentGridState();
            }

            return gmBombRevealEnabled;
        }

        public void RefreshAfterTerrainChanged(IReadOnlyList<MineClearedCell> clearedCells)
        {
            using (RefreshAfterTerrainChangedProfilerMarker.Auto())
            {
                if (services == null)
                {
                    return;
                }

                using (RefreshAfterTerrainChangedDangerProfilerMarker.Auto())
                {
                    EvaluateDangerZones();
                }

                using (RefreshAfterTerrainChangedGridProfilerMarker.Auto())
                {
                    HashSet<GridPosition> changedCells = CollectClearedPositions(clearedCells);
                    if (changedCells.Count > 0 && !gmBombRevealEnabled)
                    {
                        gridPresentation.RefreshLocalTerrainChange(services, repairStationPosition, robotFactoryPosition, changedCells);
                        gridPresentation.RefreshDangerOverlayOnly(services);
                    }
                    else
                    {
                        gridPresentation.Refresh(services, repairStationPosition, robotFactoryPosition);
                    }
                }

                using (RefreshAfterTerrainChangedViewProfilerMarker.Auto())
                {
                    RefreshActors();
                    RefreshPickups();
                    RefreshHud();
                }
            }
        }

        private void RefreshFromCurrentGridState()
        {
            if (services == null)
            {
                return;
            }

            gridPresentation.Refresh(services, repairStationPosition, robotFactoryPosition);
            scanIndicatorPresenter?.Refresh();
            RefreshActors();
            RefreshPickups();
            RefreshBuildings();
            RefreshHud();
            SyncMiningCracks(services.Session.ActiveMiningProgressSnapshots);
        }

        public bool TryRepairAtStation(int metalCost)
        {
            if (!IsNearRepairStation())
            {
                audioController?.PlayActionDenied();
                ShowFeedback("需要靠近蓝色维修站才能维修。");
                return false;
            }

            bool repaired = services.BaseOps.TryRepair(new ResourceAmount(Mathf.Max(0, metalCost), 0, 0));
            if (repaired)
            {
                audioController?.PlayRepairSuccess();
            }
            else
            {
                audioController?.PlayActionDenied();
            }
            ShowFeedback(repaired ? "维修完成，生命已恢复。" : "金属不足，无法维修。");
            return repaired;
        }

        public bool TryRepairAtStation()
        {
            return TryRepairAtStation(repairMetalCost);
        }

        public bool TryBuildRobotAtFactory()
        {
            if (!TryGetNearbyBuildingOrigin(RobotFactoryBuildingId, out GridPosition factoryOrigin))
            {
                audioController?.PlayActionDenied();
                ShowFeedback("需要靠近橙色机器人工厂才能生产机器人。");
                return false;
            }

            bool produced = services.RobotFactory.TryProduce(factoryOrigin, out RobotState robot);
            if (produced && robot != null)
            {
                RefreshActors();
                RefreshHud();
            }

            if (produced && robot != null)
            {
                audioController?.PlayRobotBuildSuccess();
            }
            else
            {
                audioController?.PlayActionDenied();
            }

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
                UpdateBuildPreviewState(definition, origin);
                audioController?.PlayActionDenied();
                ShowFeedback($"无法建造：{ToChinesePlacementFailure(failure)}");
                return false;
            }

            RefreshBuildings();
            UpdateBuildPreviewState(definition, origin);
            services.Scores?.AddBuildingConstructed(instance.Definition.Id);
            audioController?.PlayBuildPlaceSuccess();
            ShowFeedback($"已建造 {instance.Definition.DisplayName}。");
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
                UpdateBuildPreviewState(null, null);
            }

            RefreshHud();
        }

        public void SetSelectedBuilding(BuildingDefinition definition)
        {
            if (selectedBuildingDefinition == definition)
            {
                return;
            }

            selectedBuildingDefinition = definition;
            UpdateBuildPreviewState(selectedBuildingDefinition, buildPreviewOrigin);
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
            BuildingDefinition definition = GetSelectedBuildingOrDefault();
            if (!UpdateBuildPreviewState(definition, origin))
            {
                return;
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
                audioController?.PlayActionDenied();
                ShowFeedback("当前没有可选升级。");
                return false;
            }

            UpgradeDefinition selected = currentCandidates[index];
            bool applied = services.Upgrades.Select(selected);
            if (applied && !services.Experience.HasPendingUpgrade && !services.Vitals.IsDead)
            {
                interactionMode = GameplayInteractionMode.Normal;
            }

            if (applied)
            {
                audioController?.PlayUpgradeApply();
            }
            else
            {
                audioController?.PlayActionDenied();
            }

            ShowFeedback(applied ? $"升级已应用：{selected.displayName}" : "升级选择失败。");
            return applied;
        }

        public bool IsNearRepairStation()
        {
            return TryGetNearbyBuildingOrigin(RepairStationBuildingId, out _);
        }

        public bool IsNearRobotFactory()
        {
            return TryGetNearbyBuildingOrigin(RobotFactoryBuildingId, out _);
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

            if (TryAdoptRuntimeContext())
            {
                return;
            }

            if (autoInitializeServices)
            {
                BootstrapConfig config = ResolveBootstrapConfig();
                Debug.Log($"[MinebotGameplayPresentation] 自动初始化服务，使用配置: {(config != null ? config.name : "null")}");
                serviceContainer = RuntimeServiceFactory.CreateContainer(config);
                MinebotServices.SetCurrentContainer(serviceContainer);
                MinebotRuntimeDiscovery.TryInjectInto(this, serviceContainer);
            }
        }

        private bool TryAdoptRuntimeContext()
        {
            if (!MinebotRuntimeDiscovery.TryResolveContainer(out MinebotContainer runtimeContainer))
            {
                return false;
            }

            serviceContainer = runtimeContainer;
            MinebotRuntimeDiscovery.TryInjectInto(this, runtimeContainer);
            return services != null;
        }

        private void InitializeServiceBackedState()
        {
            repairStationPosition = PickFacilityPosition(GridPosition.Left);
            robotFactoryPosition = PickFacilityPosition(GridPosition.Right);
            availableBuildingDefinitions = ResolveBuildingDefinitions();
            if (selectedBuildingDefinition == null && availableBuildingDefinitions.Length > 0)
            {
                selectedBuildingDefinition = availableBuildingDefinitions[0];
            }

            EnsureAudioController();
        }

        private void DiscoverAndInjectTaggedConsumers()
        {
            if (services == null)
            {
                return;
            }

            if (serviceContainer == null && MinebotRuntimeDiscovery.TryResolveContainer(out MinebotContainer runtimeContainer))
            {
                serviceContainer = runtimeContainer;
            }

            if (serviceContainer == null)
            {
                return;
            }

            MinebotRuntimeDiscovery.InjectIntoHierarchy(gameObject, serviceContainer, this);
        }

        private BootstrapConfig ResolveBootstrapConfig()
        {
            if (bootstrapConfig != null)
            {
                Debug.Log("[MinebotGameplayPresentation] 使用序列化的配置");
                return bootstrapConfig;
            }

            if (MinebotRuntimeDiscovery.TryResolveBootstrapConfig(out BootstrapConfig runtimeConfig) && runtimeConfig != null)
            {
                Debug.Log("[MinebotGameplayPresentation] 使用运行时上下文配置");
                return runtimeConfig;
            }

            Debug.LogWarning("[MinebotGameplayPresentation] 未能找到 BootstrapConfig，将使用默认配置");
            return null;
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
            pickupRenderer = EnsurePickupRenderer(pickupRoot);
            pickupRenderer.Configure(assets);

            var unityGrid = gridRoot.GetComponent<UnityEngine.Grid>();
            if (unityGrid == null)
            {
                unityGrid = gridRoot.gameObject.AddComponent<UnityEngine.Grid>();
            }

            unityGrid.cellSize = Vector3.one;
            unityGrid.cellGap = Vector3.zero;

            Tilemap[] terrainFamilies = EnsureTerrainFamilyLayers(gridRoot);
            Vector3 fogOffset = assets != null ? assets.TerrainLayoutSettings.DisplayOffset : DualGridFog.DisplayOffset;
            Tilemap fogDeep = EnsureTilemapLayer(gridRoot, FogDeepTilemapName, assets?.FogDeepSortingOrder ?? 8, fogOffset);
            Tilemap fogNear = EnsureTilemapLayer(gridRoot, FogNearTilemapName, assets?.FogNearSortingOrder ?? 9, fogOffset);
            Tilemap danger = EnsureTilemapLayer(gridRoot, DangerTilemapName, assets?.DangerSortingOrder ?? 10);
            Tilemap facility = EnsureTilemapLayer(gridRoot, FacilityTilemapName, assets?.FacilitySortingOrder ?? 15);
            int fogTopSortingOrder = Mathf.Max(assets?.FogNearSortingOrder ?? 9, assets?.FogDeepSortingOrder ?? 8);
            int markerSortingOrder = Mathf.Max(assets?.MarkerSortingOrder ?? 20, fogTopSortingOrder + 1);
            Tilemap marker = EnsureTilemapLayer(gridRoot, MarkerTilemapName, markerSortingOrder);
            marker.tileAnchor = new Vector3(marker.tileAnchor.x, 0.9f, marker.tileAnchor.z);
            Tilemap gmBomb = EnsureTilemapLayer(gridRoot, GmBombTilemapName, markerSortingOrder + 1);
            Tilemap buildPreview = EnsureTilemapLayer(gridRoot, BuildPreviewTilemapName, assets?.BuildPreviewSortingOrder ?? 25);
            scanIndicatorPresenter = EnsureScanIndicatorPresenter(EnsureChild(gridRoot, ScanIndicatorRootName));
            scanIndicatorPresenter.Configure(assets);

            gridPresentation = gridRoot.GetComponent<TilemapGridPresentation>();
            if (gridPresentation == null)
            {
                gridPresentation = gridRoot.gameObject.AddComponent<TilemapGridPresentation>();
            }

            gridPresentation.Configure(terrainFamilies, fogNear, fogDeep, facility, marker, gmBomb, danger, buildPreview, assets);
            gridPresentation.SetGmBombRevealEnabled(gmBombRevealEnabled);
            playerActorView = EnsureActorView(actorRoot, PlayerViewName, assets.PlayerActorPrefab, assets.PlayerSprite, assets.PlayerSortingOrder);
            playerView = playerActorView.BodyRenderer;
            playerFreeform = EnsureFreeformActor(playerActorView.gameObject, services != null ? services.PlayerMiningState.Position : GridPosition.Zero);
            EnsureCircleCollider(playerActorView.gameObject, assets.PlayerColliderRadius);
            // EnsureDefaultFacilityBuildings(); // 已屏蔽开局建筑生成
            EnsureHud();
            EnsureEventSystem();
            DiscoverAndInjectTaggedConsumers();
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
            return artSet != null ? artSet : MinebotPresentationAssets.LoadDefaultArtSet();
        }

        private void EnsureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                // 创建相机Rig父物体
                var rigObject = new GameObject("Camera Rig");
                cameraRig = rigObject.transform;
                
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(cameraRig, false);
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }
            else
            {
                // 如果相机已存在，确保它有父物体Rig
                if (camera.transform.parent == null || camera.transform.parent.name != "Camera Rig")
                {
                    var rigObject = new GameObject("Camera Rig");
                    cameraRig = rigObject.transform;
                    camera.transform.SetParent(cameraRig, false);
                }
                else
                {
                    cameraRig = camera.transform.parent;
                }
            }

            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.07f, 0.08f, 1f);
            // 确保相机本地位置为(0,0,0)，震动效果会在localPosition上叠加
            camera.transform.localPosition = Vector3.zero;

            // 确保后处理开启 (URP)
            EnableCameraPostProcessing(camera);

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

            // 获取相机Rig，如果不存在则使用相机本身
            Transform rig = cameraRig;
            if (rig == null)
            {
                rig = camera.transform.parent != null ? camera.transform.parent : camera.transform;
            }

            if (services == null)
            {
                rig.position = new Vector3(6f, 6f, -10f);
                camera.orthographicSize = 6.5f;
                return;
            }

            Vector2 size = services.Grid.Size;
            float boardInset = size.x > 4f && size.y > 4f ? 1f : 0f;
            Vector2 interiorMin = new Vector2(boardInset, boardInset);
            Vector2 interiorMax = new Vector2(
                Mathf.Max(interiorMin.x + 1f, size.x - boardInset),
                Mathf.Max(interiorMin.y + 1f, size.y - boardInset));
            Vector2 playerFocus = PlayerWorldPosition;
            Vector2 focusCenter = playerFocus;
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
            // 设置Rig的位置，震动效果在相机的localPosition上叠加
            rig.position = new Vector3(focusCenter.x, focusCenter.y, -10f);
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

            renderer.mode = TilemapRenderer.Mode.Individual;
            renderer.sortOrder = TilemapRenderer.SortOrder.TopLeft;
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
                int sortingOrder = ResolveTerrainSortingOrder(layerId, layoutSettings);
                Debug.Log($"[SortOrder] Layer {layerId} -> SortingOrder {sortingOrder} (Floor={assets?.FloorSortingOrder}, Wall={assets?.WallSortingOrder}, Boundary={assets?.BoundarySortingOrder})");
                Vector3 displayOffset = (layerId == TerrainRenderLayerId.Floor && assets != null)
                    ? layoutSettings.DisplayOffset + assets.FloorDisplayOffset
                    : layoutSettings.DisplayOffset;
                Debug.Log($"[DisplayOffset] Layer {layerId} -> Offset {displayOffset} (Base={layoutSettings.DisplayOffset}, FloorExtra={assets?.FloorDisplayOffset})");
                terrainTilemaps[i] = EnsureTilemapLayer(
                    gridRoot,
                    DualGridTerrainLayout.GetTilemapName(layerId),
                    sortingOrder,
                    displayOffset);
            }

            return terrainTilemaps;
        }

        private int ResolveTerrainSortingOrder(TerrainRenderLayerId layerId, DualGridTerrainLayoutSettings settings)
        {
            // Always use ArtSet direct sorting order fields if available
            if (assets != null)
            {
                switch (layerId)
                {
                    case TerrainRenderLayerId.Floor:
                        return assets.FloorSortingOrder;
                    case TerrainRenderLayerId.Soil:
                    case TerrainRenderLayerId.Stone:
                    case TerrainRenderLayerId.HardRock:
                    case TerrainRenderLayerId.UltraHard:
                        return assets.WallSortingOrder;
                    case TerrainRenderLayerId.Boundary:
                        return assets.BoundarySortingOrder;
                }
            }

            // Fall back to DualGridTerrainLayoutSettings
            if (settings.UseManualSortingOrders)
            {
                return settings.GetManualSortingOrder(layerId);
            }

            return DualGridTerrainLayout.GetSortingOrder(layerId, settings);
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

        private static MinebotPickupRenderer EnsurePickupRenderer(Transform root)
        {
            MinebotPickupRenderer renderer = root.GetComponent<MinebotPickupRenderer>();
            if (renderer == null)
            {
                renderer = root.gameObject.AddComponent<MinebotPickupRenderer>();
            }

            return renderer;
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
            hudView.EnsureDefaultStructure(Mathf.Max(2, availableBuildingDefinitions.Length));
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

            if (index >= availableBuildingDefinitions.Length)
            {
                return;
            }

            SetSelectedBuilding(availableBuildingDefinitions[index]);
            audioController?.PlayBuildingSelect();
            if (interactionMode != GameplayInteractionMode.Build)
            {
                SetInteractionMode(GameplayInteractionMode.Build);
            }

            ShowFeedback($"已选择建筑：{availableBuildingDefinitions[index].DisplayName}，点击空地建造。");
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

            services.Session.RewardGranted += OnRewardGranted;
            services.Session.MiningProgressUpdated += OnMiningProgressUpdated;
            services.Session.PassiveHazardSenseUpdated += OnPassiveHazardSenseUpdated;
            services.Session.RobotAutomationCompleted += OnRobotAutomationCompleted;
            services.Session.BombsExploded += OnBombsExploded;
            if (services.WorldPickups != null)
            {
                services.WorldPickups.PickupAbsorbed += OnPickupAbsorbed;
            }

            services.Session.RefreshPassiveHazardSense();
            OnMiningProgressUpdated(services.Session.ActiveMiningProgressSnapshots);
            isSubscribed = true;
        }

        private void UnsubscribeFromServices()
        {
            if (!isSubscribed || services == null)
            {
                return;
            }

            services.Session.RewardGranted -= OnRewardGranted;
            services.Session.MiningProgressUpdated -= OnMiningProgressUpdated;
            services.Session.PassiveHazardSenseUpdated -= OnPassiveHazardSenseUpdated;
            services.Session.RobotAutomationCompleted -= OnRobotAutomationCompleted;
            services.Session.BombsExploded -= OnBombsExploded;
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

        private void OnMiningProgressUpdated(IReadOnlyList<MiningProgressSnapshot> snapshots)
        {
            SyncMiningCracks(snapshots);
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
                            audioController?.PlayRobotWallBreak(GridToWorld(result.ClearedCells[i].Position));
                        }
                    }
                    else
                    {
                        PlayWallBreakFx(result.Target, triggerExplosion: false);
                        audioController?.PlayRobotWallBreak(GridToWorld(result.Target));
                    }
                    break;
                case RobotAutomationResultKind.TriggeredBomb:
                    destroyedRobotVisualExpiry[result.Robot] = Time.time + 0.35f;
                    audioController?.PlayRobotDestroyed(GridToWorld(result.Target));
                    feedbackMessage = result.Reward.Metal > 0 || result.Reward.Energy > 0 || result.Reward.Experience > 0
                        ? $"从属机器人误挖炸药损毁，回收 +{result.Reward.Metal} 金属。"
                        : "从属机器人误挖炸药并损毁。";
                    break;
                case RobotAutomationResultKind.MiningProgressed:
                    break;
                case RobotAutomationResultKind.Destroyed:
                    destroyedRobotVisualExpiry[result.Robot] = Time.time + 0.35f;
                    if (!result.Target.Equals(GridPosition.Zero))
                    {
                        audioController?.PlayRobotDestroyed(GridToWorld(result.Target));
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

        private void OnBombsExploded(IReadOnlyList<GridPosition> origins)
        {
            if (origins == null)
            {
                return;
            }

            for (int i = 0; i < origins.Count; i++)
            {
                PlayWallBreakFx(origins[i], triggerExplosion: true);
                audioController?.PlayExplosion(GridToWorld(origins[i]));
            }
        }

        private void OnPickupAbsorbed(WorldPickupAbsorption absorption)
        {
            pickupRenderer?.BeginAbsorb(absorption.Pickup, PlayerWorldPosition);
            audioController?.PlayPickupAbsorb(absorption.Pickup.Type);
        }

        private void RefreshAfterRobotAutomationTick()
        {
            if (services == null)
            {
                return;
            }

            RobotAutomationResultKind kind = services.Session.LastRobotAutomationResult.Kind;
            if (kind == RobotAutomationResultKind.Mined || kind == RobotAutomationResultKind.TriggeredBomb)
            {
                RefreshAfterTerrainChanged(services.Session.LastRobotAutomationResult.ClearedCells);
                return;
            }

            RefreshActors();
            RefreshPickups();
            RefreshHud();
        }

        private void RefreshAfterPickupCollection()
        {
            if (services == null)
            {
                return;
            }

            RefreshPickups();
            RefreshHud();
        }

        public void NotifyPlayerMoved()
        {
            SetPlayerVisualState(PresentationActorState.Moving, 0.12f);
            audioController?.StopPlayerMiningLoop();
            audioController?.PlayPlayerMove();
        }

        public void NotifyPlayerBlocked()
        {
            SetPlayerVisualState(PresentationActorState.Blocked, 0.18f);
            audioController?.StopPlayerMiningLoop();
            audioController?.PlayPlayerBlock();
        }

        public void NotifyPlayerMiningContact(GridPosition target)
        {
            SetPlayerVisualState(PresentationActorState.Mining, 0.24f);
            audioController?.StartPlayerMiningLoop(playerActorView != null ? playerActorView.transform : transform);
            audioController?.StopPlayerMoveLoop();
        }

        public void NotifyPlayerMineResolved(GridPosition target, MineInteractionResult result)
        {
            SetPlayerVisualState(PresentationActorState.Mining, 0.24f);
            switch (result)
            {
                case MineInteractionResult.MiningInProgress:
                    return;
                case MineInteractionResult.Mined:
                    audioController?.StopPlayerMiningLoop();
                    if (services != null)
                    {
                        IReadOnlyList<MineClearedCell> clearedCells = services.Session.LastMineResolution.ClearedCells;
                        if (clearedCells.Count == 0)
                        {
                            audioController?.PlayTerrainWallBreak(GridToWorld(target));
                        }

                        for (int i = 0; i < clearedCells.Count; i++)
                        {
                            PlayWallBreakFx(clearedCells[i].Position, triggerExplosion: false);
                            audioController?.PlayTerrainWallBreak(GridToWorld(clearedCells[i].Position));
                        }
                    }
                    else
                    {
                        audioController?.PlayTerrainWallBreak(GridToWorld(target));
                    }
                    return;
                case MineInteractionResult.TriggeredBomb:
                    audioController?.StopPlayerMiningLoop();
                    audioController?.PlayPlayerDamage();
                    return;
                case MineInteractionResult.DrillTooWeak:
                    audioController?.StopPlayerMiningLoop();
                    audioController?.PlayPlayerMiningWeak();
                    return;
                default:
                    NotifyPlayerBlocked();
                    return;
            }
        }

        private void RefreshActors()
        {
            if (playerFreeform == null)
            {
                playerFreeform = EnsureFreeformActor(playerActorView.gameObject, services.PlayerMiningState.Position);
            }

            playerFreeform.MoveSpeedMultiplier = Mathf.Max(0.1f, services.PlayerMiningState.MoveSpeedMultiplier);

            PresentationActorState playerState = services.Vitals.IsDead ? PresentationActorState.Destroyed : playerVisualState;
            playerActorView.ApplyState(assets.PlayerActorStates, playerState, currentPlayerFacingDirection, assets.PlayerSprite, Color.white, currentPlayerFacingLeft);

            bool waveResolutionActive = services.Session != null && services.Session.IsWaveResolutionActive;
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
                    if (!robotView.gameObject.activeSelf || waveResolutionActive)
                    {
                        motion.SnapTo(target);
                    }
                    else
                    {
                        motion.SetTarget(target);
                    }
                }
                else
                {
                    robotView.transform.position = target;
                }

                PresentationActorState actorState = !robot.IsActive ? PresentationActorState.Destroyed : ToPresentationActorState(robot.Activity);
                robotView.ApplyState(assets.HelperRobotStates, actorState, ActorFacingDirection.Front, assets.RobotSprite, ColorForRobotActivity(robot.Activity), false);
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
                MinebotActorView view = EnsureActorView(actorRoot, $"Robot View {displayIndex}", assets.HelperRobotPrefab, assets.RobotSprite, assets.RobotSortingOrder);
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
            if (pickupRenderer == null)
            {
                return;
            }

            if (services == null || services.WorldPickups == null || pickupRoot == null)
            {
                pickupRenderer.ClearAll();
                return;
            }

            pickupRenderer.SyncActivePickups(services.WorldPickups.ActivePickups, GridToWorld);
        }

        private void SyncMiningCracks(IReadOnlyList<MiningProgressSnapshot> snapshots)
        {
            if (cellFxRoot == null)
            {
                return;
            }

            var activeTargets = new HashSet<GridPosition>();
            if (snapshots != null)
            {
                for (int i = 0; i < snapshots.Count; i++)
                {
                    MiningProgressSnapshot snapshot = snapshots[i];
                    if (!snapshot.IsValid || snapshot.CurrentHealth >= snapshot.MaxHealth)
                    {
                        continue;
                    }

                    activeTargets.Add(snapshot.Position);
                    ShowMiningCrack(snapshot);
                }
            }

            ClearInactiveMiningCracks(activeTargets);
        }

        private void PlayWallBreakFx(GridPosition target, bool triggerExplosion)
        {
            if (cellFxRoot == null)
            {
                return;
            }

            ClearMiningCrack(target);
            Vector3 worldPosition = GridToWorld(target);

            // 实例化序列帧动画预制体
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
                worldPosition,
                37,
                triggerExplosion ? assets.ExplosionSequence : null,
                0.08f);

            // 实例化粒子特效预制体
            if (assets.WallBreakParticlePrefab != null)
            {
                GameObject particleInstance = Instantiate(assets.WallBreakParticlePrefab, cellFxRoot, false);
                particleInstance.name = $"Wall Break Particle {target}";
                particleInstance.transform.position = worldPosition;
            }
            
            // 爆炸时实例化爆炸粒子特效
            if (triggerExplosion && assets.ExplosionParticlePrefab != null)
            {
                GameObject explosionParticleInstance = Instantiate(assets.ExplosionParticlePrefab, cellFxRoot, false);
                explosionParticleInstance.name = $"Explosion Particle {target}";
                explosionParticleInstance.transform.position = worldPosition;
                
                // 爆炸时触发屏幕震动
                TriggerExplosionScreenShake();
            }
        }

        private void TriggerExplosionScreenShake()
        {
            // 使用简单的相机震动效果
            Camera camera = Camera.main;
            if (camera != null)
            {
                StartCoroutine(SimpleScreenShake(camera));
            }
        }

        private System.Collections.IEnumerator SimpleScreenShake(Camera camera)
        {
            Vector3 originalPos = camera.transform.localPosition;
            float shakeDuration = 0.2f;
            float shakeIntensity = 0.15f;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * shakeIntensity;
                float y = UnityEngine.Random.Range(-1f, 1f) * shakeIntensity;
                camera.transform.localPosition = originalPos + new Vector3(x, y, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            camera.transform.localPosition = originalPos;
        }

        private void EnsureAudioController()
        {
            if (audioController != null)
            {
                return;
            }

            BootstrapConfig config = ResolveBootstrapConfig();
            if (config?.AudioConfig == null)
            {
                return;
            }

            audioController = new MinebotGameplayAudioController(config.AudioConfig);
        }

        private Transform GetFirstRobotMiningAnchor()
        {
            if (services == null || robotViews.Count == 0)
            {
                return null;
            }

            int count = Mathf.Min(services.Robots.Count, robotViews.Count);
            for (int i = 0; i < count; i++)
            {
                RobotState robot = services.Robots[i];
                MinebotActorView view = robotViews[i];
                if (robot != null
                    && robot.IsActive
                    && robot.Activity == RobotActivity.Mining
                    && view != null
                    && view.gameObject.activeInHierarchy)
                {
                    return view.transform;
                }
            }

            return null;
        }

        private void ShowMiningCrack(MiningProgressSnapshot snapshot)
        {
            if (!miningCrackViews.TryGetValue(snapshot.Position, out MinebotCellFxView view) || view == null)
            {
                GameObject prefab = assets.MiningCrackPrefab;
                GameObject instance = prefab != null ? Instantiate(prefab, cellFxRoot, false) : new GameObject($"Mining Crack {snapshot.Position}", typeof(MinebotCellFxView));
                instance.name = $"Mining Crack {snapshot.Position}";
                instance.transform.SetParent(cellFxRoot, false);
                view = instance.GetComponent<MinebotCellFxView>();
                if (view == null)
                {
                    view = instance.AddComponent<MinebotCellFxView>();
                }

                miningCrackViews[snapshot.Position] = view;
            }

            int frameIndex = GetMiningCrackFrameIndex(snapshot);
            Vector3 crackWorldPosition = GridToWorld(snapshot.Position) + (Vector3)assets.MiningCrackOffset;
            view.ShowPersistentFrame(assets.MiningCrackSequence, frameIndex, crackWorldPosition, assets.MiningCrackSortingOrder);
        }

        private void ClearInactiveMiningCracks(HashSet<GridPosition> activeTargets)
        {
            if (miningCrackViews.Count == 0)
            {
                return;
            }

            var staleTargets = new List<GridPosition>();
            foreach (KeyValuePair<GridPosition, MinebotCellFxView> pair in miningCrackViews)
            {
                if (!activeTargets.Contains(pair.Key))
                {
                    staleTargets.Add(pair.Key);
                }
            }

            for (int i = 0; i < staleTargets.Count; i++)
            {
                ClearMiningCrack(staleTargets[i]);
            }
        }

        private void ClearMiningCrack(GridPosition target)
        {
            if (!miningCrackViews.TryGetValue(target, out MinebotCellFxView view))
            {
                return;
            }

            miningCrackViews.Remove(target);
            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }

        private int GetMiningCrackFrameIndex(MiningProgressSnapshot snapshot)
        {
            SpriteSequenceAsset sequence = assets != null ? assets.MiningCrackSequence : null;
            if (sequence == null || sequence.Frames == null || sequence.Frames.Length == 0)
            {
                return 0;
            }

            int frameCount = sequence.Frames.Length;
            return Mathf.Clamp(Mathf.FloorToInt(snapshot.DamageNormalized * frameCount), 0, frameCount - 1);
        }

        private void UpdatePlayerVisualState(float deltaTime)
        {
            if (services != null && services.Vitals.IsDead)
            {
                playerVisualState = PresentationActorState.Destroyed;
                playerVisualHoldRemaining = 0f;
                RefreshActors();
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

            RefreshActors();
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
                return (assets.RobotFactoryTile as Tile)?.sprite;
            }

            Tile repairStationTile = assets.RepairStationTile as Tile;
            return repairStationTile != null ? repairStationTile.sprite : null;
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
                hudView.WarningPanel.SetColor(services.Session != null && services.Session.IsWaveResolutionActive
                    ? new Color(1f, 0.38f, 0.18f, 1f)
                    : services.Waves.TimeUntilNextWave <= WaveSurvivalService.DangerWarningLeadTime
                        ? new Color(1f, 0.2f, 0.58f, 1f)
                        : new Color(1f, 0.96f, 0.22f, 1f));
            }

            if (services.Session != null && services.Session.IsWaveResolutionActive)
            {
                WaveResolutionState state = services.Session.CurrentWaveResolutionState;
                float phaseHoldSeconds = GetWaveResolutionPhaseHoldSeconds(state.Phase);
                float fillAmount = phaseHoldSeconds > 0.0001f
                    ? Mathf.Clamp01(state.PhaseElapsedSeconds / phaseHoldSeconds)
                    : 1f;
                hudView.UpdateTemplateWaveStatus(
                    $"WAVE {Mathf.Max(1, state.TargetWave)}",
                    $"{ToChineseWaveResolutionPhase(state)} | 暂停",
                    fillAmount,
                    new Color(1f, 0.38f, 0.18f, 1f));
            }
            else
            {
                hudView.UpdateTemplateWaveStatus(
                    Mathf.Max(1, services.Waves.CurrentWave + 1),
                    services.Waves.TimeUntilNextWave,
                    services.Waves.WaveInterval);
            }

            RefreshScorePanel();

            DisableMinimapPanel();
            RefreshUpgradePanel();
            RefreshBuildPanel();
            RefreshBuildingInteractionPanel();
        }

        private void DisableMinimapPanel()
        {
            if (hudView == null || hudView.MinimapPanel == null)
            {
                return;
            }

            hudView.MinimapPanel.SetTexture(null);
            hudView.MinimapPanel.SetSummary(string.Empty);
            hudView.MinimapPanel.SetVisible(false);
        }

        private void RefreshScorePanel()
        {
            if (hudView == null || hudView.ScorePanel == null)
            {
                return;
            }

            bool isDead = services != null && services.Vitals.IsDead;
            MinebotScorePageView panel = hudView.ScorePanel;
            panel.SetVisible(isDead);
            if (!isDead)
            {
                panel.SetScore(0);
                panel.SetSubmissionEnabled(false);
                return;
            }

            panel.BindNameChanged(value => leaderboardNameInput = value);
            panel.BindSubmit(HandleLeaderboardSubmit);
            panel.SetScore(services.Scores?.CurrentScore ?? 0);
            panel.SetNameInput(leaderboardNameInput);
            panel.SetSubmissionEnabled(!leaderboardSubmitted);
        }

        private void HandleLeaderboardSubmit(string rawName)
        {
            if (services == null || !services.Vitals.IsDead || leaderboardSubmitted)
            {
                return;
            }

            leaderboardNameInput = rawName;
            LocalLeaderboardService.TryAddEntry(
                leaderboardNameInput,
                services.Scores?.CurrentScore ?? 0,
                services.Waves.BestSurvivedWave,
                out _);
            leaderboardSubmitted = true;
            ShowRankPageAfterSubmit();
            RefreshHud();
        }

        private void ShowRankPageAfterSubmit()
        {
            EnsureRankPageView();
            if (rankPageView == null)
            {
                return;
            }

            rankPageView.SetEntries(LocalLeaderboardService.GetEntries());
            rankPageView.SetVisible(true);
            rankPageShownFrame = Time.frameCount;
        }

        private void EnsureRankPageView()
        {
            if (rankPageView != null)
            {
                return;
            }

            // 创建 Canvas 根节点
            var root = new GameObject("RankPageRoot", typeof(RectTransform));
            root.layer = 5;
            root.transform.SetParent(transform, false);

            // 配置 Canvas
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            // 设置 RectTransform 填满父物体
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;

            // 加载预制体
            const string RankPageResourcePath = "Prefabs/Rank Panel ";
            GameObject prefab = Resources.Load<GameObject>(RankPageResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"MinebotGameplayPresentation 缺少排行榜页面资源：{RankPageResourcePath}");
                return;
            }

            GameObject instance = Instantiate(prefab, rect, false);
            instance.name = "Rank Panel";
            if (instance.transform is RectTransform instanceRect)
            {
                instanceRect.anchorMin = Vector2.zero;
                instanceRect.anchorMax = Vector2.one;
                instanceRect.pivot = new Vector2(0.5f, 0.5f);
                instanceRect.anchoredPosition = Vector2.zero;
                instanceRect.sizeDelta = Vector2.zero;
                instanceRect.localScale = Vector3.one;
            }

            rankPageView = new MinebotRankPageView(instance);
            if (!rankPageView.HasRequiredBindings(out string missingBindings))
            {
                Debug.LogError($"Rank Panel.prefab 缺少必需引用：{missingBindings}");
            }

            rankPageView.SetVisible(false);
        }

        private void LoadBootstrapScene()
        {
            MinebotServices.ResetForTests();
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Bootstrap", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        private string BuildInteractionText()
        {
            const string baseHint = "WASD 移动  贴墙即挖掘  点击墙体标记风险  点击底栏选建筑  R 开关建造  1-2 升级";
            if (services.Session != null && services.Session.IsWaveResolutionActive)
            {
                return AppendGmBombHint("地震结算中：动作已暂停，等待外围炸弹、危险区重算和塌方完成");
            }

            if (services.Experience.HasPendingUpgrade)
            {
                return AppendGmBombHint("升级可用：按 1/2 或点击右下升级卡片");
            }

            if (interactionMode == GameplayInteractionMode.Build)
            {
                string selected = GetSelectedBuildingOrDefault() != null ? GetSelectedBuildingOrDefault().DisplayName : "未选择";
                string preview = buildPreviewOrigin.HasValue
                    ? (buildPreviewIsValid ? "当前位置可建" : "当前位置不可建")
                    : "移动鼠标选空地";
                return AppendGmBombHint($"建筑模式：{selected} | {preview} | 点击底栏切换，R / 右键 / Esc 退出");
            }

            if (IsNearRepairStation() && IsNearRobotFactory())
            {
                return AppendGmBombHint("可交互：右侧卡片可维修或生产从属机器人");
            }

            if (IsNearRepairStation())
            {
                return AppendGmBombHint("可交互：右侧卡片可在维修站恢复生命");
            }

            if (IsNearRobotFactory())
            {
                return AppendGmBombHint("可交互：右侧卡片可生产从属机器人");
            }

            return AppendGmBombHint(baseHint);
        }

        private string BuildLegacyHudSummary()
        {
            ResourceAmount resources = services.Economy.Resources;
            return
                $"HP {services.Vitals.CurrentHealth}/{services.Vitals.MaxHealth} | 金属 {resources.Metal} | 能量 {resources.Energy} | 波次 {services.Waves.CurrentWave}\n" +
                $"Lv {services.Experience.Level} | XP {services.Experience.Experience}/{services.Experience.NextThreshold} | 钻头 {ToChineseHardnessText(services.PlayerMiningState.DrillTier)}\n" +
                $"分数 {services.Scores?.CurrentScore ?? 0} | 坐标 {services.PlayerMiningState.Position} | {BuildRobotStatusText()} | GM炸药 {(gmBombRevealEnabled ? "ON" : "OFF")}";
        }

        private string AppendGmBombHint(string text)
        {
            string suffix = gmBombRevealEnabled ? "GM炸药 ON" : "F6 GM炸药";
            return string.IsNullOrEmpty(text) ? suffix : $"{text} | {suffix}";
        }

        private string BuildRobotStatusPanelText()
        {
            CountRobotStates(out int active, out int working, out int waiting, out _);
            return
                $"<color=#D9FF1A>工作中 x{working}</color>\n" +
                $"<color=#FF1787>待机中 x{waiting}</color>\n" +
                $"<color=#FFFFFF>在线 {active}</color>";
        }

        private string BuildResourcePanelText(ResourceAmount resources)
        {
            return
                $"<color=#18F0FF>金属 {resources.Metal}</color>\n" +
                $"<color=#18F0FF>能量 {resources.Energy}</color>\n" +
                $"<color=#FFD928>分数 {services.Scores?.CurrentScore ?? 0}</color>";
        }

        private string BuildWaveHeaderText()
        {
            if (services.Session != null && services.Session.IsWaveResolutionActive)
            {
                WaveResolutionState state = services.Session.CurrentWaveResolutionState;
                return
                    $"WAVE {Mathf.Max(1, state.TargetWave)}\n" +
                    $"{ToChineseWaveResolutionPhase(state)} | 动作暂停";
            }

            int displayWave = Mathf.Max(1, services.Waves.CurrentWave + 1);
            return
                $"WAVE {displayWave}\n" +
                $"{Mathf.Max(0f, services.Waves.TimeUntilNextWave):00.0}s | 厚度 {services.Waves.NextDangerRadius}";
        }

        private string BuildActionTagText()
        {
            if (services.Session != null && services.Session.IsWaveResolutionActive)
            {
                return "结算中";
            }

            if (services.Experience.HasPendingUpgrade)
            {
                return "升级待选";
            }

            if (interactionMode == GameplayInteractionMode.Build)
            {
                return "建造";
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
            if (services.Session != null && services.Session.IsWaveResolutionActive)
            {
                return new Color(1f, 0.48f, 0.18f, 1f);
            }

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
            if (services.Session != null && services.Session.IsWaveResolutionActive)
            {
                WaveResolutionState state = services.Session.CurrentWaveResolutionState;
                return $"地震结算中：{ToChineseWaveResolutionPhase(state)} | 动作已暂停";
            }

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
                return "周边感知：九宫格内没有贴岩体的空地";
            }

            if (lastHazardSenseReadings.Count == 1)
            {
                ScanReading reading = lastHazardSenseReadings[0];
                return $"周边感知：{reading.CellPosition} = {reading.BombCount}";
            }

            int highestRisk = 0;
            for (int i = 0; i < lastHazardSenseReadings.Count; i++)
            {
                highestRisk = Mathf.Max(highestRisk, lastHazardSenseReadings[i].BombCount);
            }

            return $"周边感知：{lastHazardSenseReadings.Count} 处，最高风险 {highestRisk}";
        }

        private static string ToChineseWaveResolutionPhase(WaveResolutionState state)
        {
            return state.Phase switch
            {
                WaveResolutionPhase.DetonatePerimeterBombs => state.TotalPerimeterBombs > 0
                    ? $"外围炸弹 {state.DetonatedPerimeterBombs}/{state.TotalPerimeterBombs}"
                    : "外围炸弹",
                WaveResolutionPhase.ReevaluateDangerZones => "危险区重算",
                WaveResolutionPhase.CollapseDangerZones => "塌方回填",
                _ => "地震结算"
            };
        }

        private float GetWaveResolutionPhaseHoldSeconds(WaveResolutionPhase phase)
        {
            if (services == null || services.Waves == null)
            {
                return 0f;
            }

            return phase switch
            {
                WaveResolutionPhase.DetonatePerimeterBombs => services.Waves.PerimeterBombPhaseHoldSeconds,
                WaveResolutionPhase.ReevaluateDangerZones => services.Waves.DangerRefreshPhaseHoldSeconds,
                WaveResolutionPhase.CollapseDangerZones => services.Waves.CollapsePhaseHoldSeconds,
                _ => 0f
            };
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
            hudView.UpgradePanel.SetCancelVisible(false);

            for (int i = 0; i < MinebotHudDefaults.UpgradeButtonCount; i++)
            {
                bool hasCandidate = i < currentCandidates.Length;
                UpgradeDefinition upgrade = hasCandidate ? currentCandidates[i] : null;
                hudView.UpgradePanel.SetOption(
                    i,
                    hasCandidate,
                    hasCandidate ? upgrade.displayName : string.Empty,
                    hasCandidate ? DescribeUpgrade(upgrade) : string.Empty,
                    hasCandidate ? BuildUpgradeHint(i) : string.Empty);
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
                && !services.Experience.HasPendingUpgrade
                && (services.Session == null || !services.Session.IsWaveResolutionActive);
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

        private bool IsSimulationPausedByUpgradePanel()
        {
            return services != null
                && !services.Vitals.IsDead
                && (services.Experience.HasPendingUpgrade || IsUpgradePanelShowing);
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
                && (services.Session == null || !services.Session.IsWaveResolutionActive)
                && interactionMode == GameplayInteractionMode.Normal;
        }

        private static string DescribeUpgrade(UpgradeDefinition upgrade)
        {
            if (upgrade == null)
            {
                return string.Empty;
            }

            var parts = new List<string>(4);
            if (upgrade.maxHealthDelta > 0)
            {
                parts.Add($"最大生命 +{upgrade.maxHealthDelta}");
            }

            int drillBonus = Mathf.Max(0, upgrade.drillTierDelta) + Mathf.Max(0, upgrade.miningDamageDelta);
            if (drillBonus > 0)
            {
                parts.Add($"钻头 +{drillBonus}");
            }

            if (upgrade.moveSpeedMultiplierDelta > 0f)
            {
                parts.Add($"移速 +{Mathf.RoundToInt(upgrade.moveSpeedMultiplierDelta * 100f)}%");
            }

            if (upgrade.markerCapacityDelta > 0)
            {
                parts.Add($"标记 +{upgrade.markerCapacityDelta}");
            }

            return parts.Count > 0 ? string.Join("，", parts) : "立即生效";
        }

        private static string BuildUpgradeHint(int index)
        {
            return $"按 {index + 1} 键选择";
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

        private bool UpdateBuildPreviewState(BuildingDefinition definition, GridPosition? origin)
        {
            bool isValid = definition != null
                && origin.HasValue
                && CanPlaceBuildingAt(definition, origin.Value);
            bool changed = !Nullable.Equals(buildPreviewOrigin, origin)
                || buildPreviewIsValid != isValid
                || !ReferenceEquals(buildPreviewDefinition, definition);

            buildPreviewDefinition = definition;
            buildPreviewOrigin = origin;
            buildPreviewIsValid = isValid;

            if (gridPresentation != null)
            {
                gridPresentation.ShowBuildPreview(definition, origin, isValid);
                if (services != null)
                {
                    gridPresentation.RefreshBuildPreviewOnly(services.Grid);
                }
            }

            return changed;
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

        private static HashSet<GridPosition> CollectClearedPositions(IReadOnlyList<MineClearedCell> clearedCells)
        {
            var positions = new HashSet<GridPosition>();
            if (clearedCells == null)
            {
                return positions;
            }

            for (int i = 0; i < clearedCells.Count; i++)
            {
                positions.Add(clearedCells[i].Position);
            }

            return positions;
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

        private GridPosition ResolveFacilityPosition(string buildingId, GridPosition fallback)
        {
            if (services?.Buildings == null)
            {
                return fallback;
            }

            IReadOnlyList<BuildingInstance> buildings = services.Buildings.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                BuildingInstance instance = buildings[i];
                if (HasBuildingId(instance, buildingId))
                {
                    return instance.Origin;
                }
            }

            return fallback;
        }

        private bool TryGetNearbyBuildingOrigin(string buildingId, out GridPosition origin)
        {
            origin = GridPosition.Zero;
            if (services?.Buildings == null)
            {
                return false;
            }

            int bestDistance = int.MaxValue;
            GridPosition playerPosition = services.PlayerMiningState.Position;
            IReadOnlyList<BuildingInstance> buildings = services.Buildings.Buildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                BuildingInstance instance = buildings[i];
                if (!HasBuildingId(instance, buildingId))
                {
                    continue;
                }

                int distance = DistanceToBuilding(playerPosition, instance);
                if (distance <= 1 && distance < bestDistance)
                {
                    bestDistance = distance;
                    origin = instance.Origin;
                }
            }

            return bestDistance != int.MaxValue;
        }

        private int DistanceToBuilding(GridPosition playerPosition, BuildingInstance instance)
        {
            int bestDistance = int.MaxValue;
            foreach (GridPosition footprintCell in services.Buildings.FootprintCells(instance.Definition, instance.Origin))
            {
                bestDistance = Mathf.Min(bestDistance, playerPosition.ManhattanDistance(footprintCell));
            }

            return bestDistance;
        }

        private static bool HasBuildingId(BuildingInstance instance, string buildingId)
        {
            return instance != null
                && instance.Definition != null
                && string.Equals(instance.Definition.Id, buildingId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 重置玩家视觉状态为待机（由输入控制器调用）
        /// </summary>
        public void SetPlayerVisualStateIdle()
        {
            playerVisualState = PresentationActorState.Idle;
            playerVisualHoldRemaining = 0f;
            // 不调用 RefreshActors，让 Update 中的 UpdatePlayerVisualState 处理刷新
        }

        private static void EnableCameraPostProcessing(Camera camera)
        {
            // 通过反射添加 UniversalAdditionalCameraData 并设置 renderPostProcessing
            var assembly = System.Reflection.Assembly.Load("Unity.RenderPipelines.Universal.Runtime");
            var cameraDataType = assembly.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            if (cameraDataType == null)
            {
                Debug.LogWarning("[MinebotGameplayPresentation] 无法找到 UniversalAdditionalCameraData 类型，跳过后处理设置");
                return;
            }

            var cameraData = camera.gameObject.AddComponent(cameraDataType);
            var property = cameraDataType.GetProperty("renderPostProcessing");
            property?.SetValue(cameraData, true);
        }
    }
}
