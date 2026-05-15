using Minebot.Bootstrap;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.Progression;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Minebot.Presentation
{
    [MinebotRuntimeTag(MinebotRuntimeTag.Consumer)]
    public sealed class GameplayInputController : MonoBehaviour
    {
        private static readonly ProfilerMarker UpdateProfilerMarker = new("Minebot.GameplayInputController.Update");
        private static readonly ProfilerMarker UpdateMoveProfilerMarker = new("Minebot.GameplayInputController.Update.MoveInput");
        private static readonly ProfilerMarker MoveFreeformProfilerMarker = new("Minebot.GameplayInputController.MoveFreeform");
        private static readonly ProfilerMarker ClickGridCellProfilerMarker = new("Minebot.GameplayInputController.ClickGridCell");
        private static readonly ProfilerMarker PointerPositionProfilerMarker = new("Minebot.GameplayInputController.PointerPosition");
        private static readonly ProfilerMarker PointerClickProfilerMarker = new("Minebot.GameplayInputController.PointerClick");
        private static readonly ProfilerMarker AutoMineProfilerMarker = new("Minebot.GameplayInputController.AutoMineContact");
        private static readonly ProfilerMarker MineTargetProfilerMarker = new("Minebot.GameplayInputController.MineTarget");

        [SerializeField]
        private int repairMetalCost = 2;

        private MinebotInputActions inputActions;
        private MinebotGameplayPresentation presentation;
        private LogicalGridState grid;
        private PlayerMiningState playerMiningState;
        private GameSessionService session;
        private PlayerVitals vitals;
        private ExperienceService experience;
        private GridPosition lastDirection = GridPosition.Up;
        private Vector2 currentMoveInput;
        private Vector2 pointerPosition;
        private AutoMineContactState autoMineState = AutoMineContactState.None;

        [SerializeField]
        private float freeMoveStepSeconds = 0.25f;

        [SerializeField]
        private float autoMineInterval = 0.2f;

        public float AutoMineInterval => autoMineInterval;

        private void Awake()
        {
            presentation = GetComponent<MinebotGameplayPresentation>();
            if (presentation == null)
            {
                presentation = FindAnyObjectByType<MinebotGameplayPresentation>();
            }

            EnsureServices();
        }

        private void OnEnable()
        {
            EnsureServices();
            inputActions = new MinebotInputActions();

            MinebotInputActions.PlayerActions player = inputActions.Player;
            player.Move.performed += OnMovePerformed;
            player.Move.canceled += OnMoveCanceled;
            player.ToggleMarkerMode.performed += OnToggleMarkerModePerformed;
            player.ToggleBuildMode.performed += OnToggleBuildModePerformed;
            player.PointerPosition.performed += OnPointerPositionPerformed;
            player.PointerClick.performed += OnPointerClickPerformed;
            player.Cancel.performed += OnCancelPerformed;
            player.SelectUpgrade1.performed += OnSelectUpgrade1Performed;
            player.SelectUpgrade2.performed += OnSelectUpgrade2Performed;
            player.SelectUpgrade3.performed += OnSelectUpgrade3Performed;
            player.Pause.performed += OnPausePerformed;
            player.Enable();
            inputActions.UI.Enable();
        }

        private void OnDisable()
        {
            if (inputActions == null)
            {
                return;
            }

            MinebotInputActions.PlayerActions player = inputActions.Player;
            player.Disable();
            inputActions.UI.Disable();
            player.Move.performed -= OnMovePerformed;
            player.Move.canceled -= OnMoveCanceled;
            player.ToggleMarkerMode.performed -= OnToggleMarkerModePerformed;
            player.ToggleBuildMode.performed -= OnToggleBuildModePerformed;
            player.PointerPosition.performed -= OnPointerPositionPerformed;
            player.PointerClick.performed -= OnPointerClickPerformed;
            player.Cancel.performed -= OnCancelPerformed;
            player.SelectUpgrade1.performed -= OnSelectUpgrade1Performed;
            player.SelectUpgrade2.performed -= OnSelectUpgrade2Performed;
            player.SelectUpgrade3.performed -= OnSelectUpgrade3Performed;
            player.Pause.performed -= OnPausePerformed;
            inputActions.Dispose();
            inputActions = null;
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.f6Key.wasPressedThisFrame)
            {
                return;
            }

            ToggleGmBombReveal();
        }

        private void FixedUpdate()
        {
            using (UpdateProfilerMarker.Auto())
            {
                if (currentMoveInput.sqrMagnitude > 0.0001f)
                {
                    using (UpdateMoveProfilerMarker.Auto())
                    {
                        MoveFreeform(currentMoveInput, Time.fixedDeltaTime);
                    }
                }
            }
        }

        public bool Move(GridPosition direction)
        {
            lastDirection = direction;
            presentation?.SetPlayerFacingDirection(direction);
            return MoveFreeform(ToVector2(direction), Mathf.Max(0.02f, freeMoveStepSeconds));
        }

        public bool MoveFreeform(Vector2 direction, float deltaTime)
        {
            using (MoveFreeformProfilerMarker.Auto())
            {
                if (!CanAcceptGameplayInput(suppressWaveLockFeedback: true) || presentation.InteractionMode != GameplayInteractionMode.Normal)
                {
                    return false;
                }

                if (direction.sqrMagnitude < 0.0001f)
                {
                    ResetAutoMineState();
                    return false;
                }

                lastDirection = QuantizeDirection(direction);
                presentation?.SetPlayerFacingDirection(lastDirection);
                CharacterMoveResult2D moveResult = presentation.TryMovePlayerFreeform(direction, deltaTime);
                if (moveResult.HasMoved)
                {
                    ResetAutoMineState();
                    presentation.NotifyPlayerMoved();
                    presentation.RefreshAfterPlayerMoved();
                    return true;
                }

                return TryAutoMineContact(moveResult, deltaTime);
            }
        }

        public bool MineFacingCell()
        {
            if (!CanAcceptGameplayInput())
            {
                return false;
            }

            GridPosition target = playerMiningState.Position + lastDirection;
            return MineTarget(target);
        }

        public bool ToggleMarkerFacingCell()
        {
            if (!CanAcceptGameplayInput())
            {
                return false;
            }

            GridPosition target = playerMiningState.Position + lastDirection;
            bool canToggle = CanToggleMarker(target, out bool wasMarked);
            bool marked = session.ToggleMarker(target);
            presentation.RefreshAfterMarkerChanged(target);
            if (marked)
            {
                presentation.AudioController?.PlayMarkerSet();
            }
            else if (canToggle && wasMarked)
            {
                presentation.AudioController?.PlayMarkerClear();
            }
            else
            {
                presentation.AudioController?.PlayActionDenied();
            }

            bool succeeded = marked || (canToggle && wasMarked);
            presentation.ShowFeedback(marked
                ? $"已标记 {target}，机器人会避开该格。"
                : canToggle && wasMarked
                    ? $"已取消标记 {target}。"
                    : $"无法标记 {target}。");
            return succeeded;
        }

        public bool ToggleMarkerMode()
        {
            return ToggleMarkerFacingCell();
        }

        public bool ToggleBuildMode()
        {
            if (!CanChangeMode())
            {
                return false;
            }

            bool entering = presentation.InteractionMode != GameplayInteractionMode.Build;
            presentation.SetInteractionMode(entering ? GameplayInteractionMode.Build : GameplayInteractionMode.Normal);
            if (entering)
            {
                presentation.SetSelectedBuilding(presentation.GetSelectedBuildingOrDefault());
            }

            presentation.AudioController?.PlayBuildModeToggle();
            presentation.ShowFeedback(entering ? "已进入建筑模式：选择建筑后点击空地。" : "已退出建筑模式。");
            return true;
        }

        public bool ToggleGmBombReveal()
        {
            if (presentation == null && !EnsureServices())
            {
                return false;
            }

            bool enabled = presentation.ToggleGmBombReveal();
            presentation.ShowFeedback(enabled ? "GM炸药 ON" : "GM炸药 OFF");
            return true;
        }

        public bool ClickGridCell(GridPosition target)
        {
            using (ClickGridCellProfilerMarker.Auto())
            {
                if (!EnsureServices())
                {
                    return false;
                }

                if (session != null && session.IsWaveResolutionActive)
                {
                    presentation.AudioController?.PlayActionDenied();
                    presentation.ShowFeedback("地震结算中，动作已暂停。");
                    return false;
                }

                if (vitals.IsDead || experience.HasPendingUpgrade || presentation.IsUpgradePanelShowing)
                {
                    presentation.AudioController?.PlayActionDenied();
                    presentation.ShowFeedback("输入已锁定，先处理升级或失败状态。");
                    return false;
                }

                if (presentation.InteractionMode == GameplayInteractionMode.Build)
                {
                    BuildingDefinition definition = presentation.GetSelectedBuildingOrDefault();
                    return presentation.TryPlaceBuildingAt(definition, target);
                }

                bool canToggle = CanToggleMarker(target, out bool wasMarked);
                if (canToggle)
                {
                    bool marked = session.ToggleMarker(target);
                    presentation.RefreshAfterMarkerChanged(target);
                    if (marked)
                    {
                        presentation.AudioController?.PlayMarkerSet();
                    }
                    else if (wasMarked)
                    {
                        presentation.AudioController?.PlayMarkerClear();
                    }
                    else
                    {
                        presentation.AudioController?.PlayActionDenied();
                    }

                    presentation.ShowFeedback(marked
                        ? $"已标记 {target}，机器人会避开该格。"
                        : wasMarked
                            ? $"已取消标记 {target}。"
                            : $"无法标记 {target}。");
                    return marked || wasMarked;
                }

                return false;
            }
        }

        public bool Repair()
        {
            if (!CanAcceptGameplayInput())
            {
                return false;
            }

            return presentation.TryRepairAtStation(repairMetalCost);
        }

        public bool BuildRobot()
        {
            if (!CanAcceptGameplayInput())
            {
                return false;
            }

            return presentation.TryBuildRobotAtFactory();
        }

        public bool SelectUpgrade(int index)
        {
            if (!EnsureServices())
            {
                return false;
            }

            bool selected = presentation.SelectUpgradeIndex(index);
            if (selected && !experience.HasPendingUpgrade && !vitals.IsDead)
            {
                presentation.SetInteractionMode(GameplayInteractionMode.Normal);
            }

            return selected;
        }

        public void InjectServices(
            LogicalGridState injectedGrid,
            PlayerMiningState injectedPlayerMiningState,
            GameSessionService injectedSession,
            PlayerVitals injectedVitals,
            ExperienceService injectedExperience,
            BootstrapConfig config)
        {
            grid = injectedGrid;
            playerMiningState = injectedPlayerMiningState;
            session = injectedSession;
            vitals = injectedVitals;
            experience = injectedExperience;
        }

        private bool CanAcceptGameplayInput(bool suppressWaveLockFeedback = false)
        {
            if (!EnsureServices())
            {
                return false;
            }

            if (session != null && session.IsWaveResolutionActive)
            {
                if (!suppressWaveLockFeedback)
                {
                    presentation.AudioController?.PlayActionDenied();
                    presentation.ShowFeedback("地震结算中，动作已暂停。");
                }

                return false;
            }

            if (vitals.IsDead)
            {
                presentation.SetInteractionMode(GameplayInteractionMode.GameOver);
                return false;
            }

            if (experience.HasPendingUpgrade || presentation.IsUpgradePanelShowing)
            {
                presentation.SetInteractionMode(GameplayInteractionMode.UpgradeLocked);
                presentation.AudioController?.PlayActionDenied();
                presentation.ShowFeedback("升级待选择，普通操作已暂停。请按 1/2 或点击升级项。");
                return false;
            }

            return true;
        }

        private bool CanChangeMode(bool suppressWaveLockFeedback = false)
        {
            if (!EnsureServices())
            {
                return false;
            }

            if (session != null && session.IsWaveResolutionActive)
            {
                if (!suppressWaveLockFeedback)
                {
                    presentation.AudioController?.PlayActionDenied();
                    presentation.ShowFeedback("地震结算中，动作已暂停。");
                }

                return false;
            }

            if (vitals.IsDead)
            {
                presentation.SetInteractionMode(GameplayInteractionMode.GameOver);
                return false;
            }

            if (experience.HasPendingUpgrade || presentation.IsUpgradePanelShowing)
            {
                presentation.SetInteractionMode(GameplayInteractionMode.UpgradeLocked);
                presentation.AudioController?.PlayActionDenied();
                presentation.ShowFeedback("升级待选择，模式操作已暂停。请按 1/2 或点击升级项。");
                return false;
            }

            return true;
        }

        private bool EnsureServices()
        {
            if (presentation == null)
            {
                presentation = FindAnyObjectByType<MinebotGameplayPresentation>();
            }

            if (HasServices)
            {
                return presentation != null;
            }

            if (MinebotRuntimeDiscovery.TryInjectInto(this))
            {
                return HasServices && presentation != null;
            }

            if (presentation != null && presentation.Services != null)
            {
                AdoptRegistry(presentation.Services);
            }

            return HasServices && presentation != null;
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            currentMoveInput = context.ReadValue<Vector2>();
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            Debug.Log("移动输入取消");
            currentMoveInput = Vector2.zero;
            ResetAutoMineState();
            presentation?.AudioController?.StopPlayerMoveLoop();
            // 先设置朝向，再重置视觉状态，确保 Idle 显示正确方向
            presentation?.SetPlayerFacingDirection(lastDirection);
            presentation?.SetPlayerVisualStateIdle();
        }

        private void OnToggleMarkerModePerformed(InputAction.CallbackContext context)
        {
            ToggleMarkerFacingCell();
        }

        private void OnToggleBuildModePerformed(InputAction.CallbackContext context)
        {
            ToggleBuildMode();
        }

        private void OnPointerPositionPerformed(InputAction.CallbackContext context)
        {
            using (PointerPositionProfilerMarker.Auto())
            {
                pointerPosition = context.ReadValue<Vector2>();
                if (presentation != null
                    && presentation.InteractionMode == GameplayInteractionMode.Build
                    && (session == null || !session.IsWaveResolutionActive))
                {
                    presentation.SetBuildPreview(presentation.ScreenToGridPosition(pointerPosition));
                }
            }
        }

        private void OnPointerClickPerformed(InputAction.CallbackContext context)
        {
            using (PointerClickProfilerMarker.Auto())
            {
                ClickGridCell(presentation.ScreenToGridPosition(pointerPosition));
            }
        }

        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            if (!EnsureServices())
            {
                return;
            }

            if (session != null && session.IsWaveResolutionActive)
            {
                presentation.AudioController?.PlayActionDenied();
                presentation.ShowFeedback("地震结算中，动作已暂停。");
                return;
            }

            if (presentation.InteractionMode == GameplayInteractionMode.Build)
            {
                presentation.SetInteractionMode(GameplayInteractionMode.Normal);
                presentation.AudioController?.PlayBuildModeToggle();
                presentation.ShowFeedback("已退出当前模式。");
            }
        }

        private void OnSelectUpgrade1Performed(InputAction.CallbackContext context)
        {
            SelectUpgrade(0);
        }

        private void OnSelectUpgrade2Performed(InputAction.CallbackContext context)
        {
            SelectUpgrade(1);
        }

        private void OnSelectUpgrade3Performed(InputAction.CallbackContext context)
        {
            SelectUpgrade(2);
        }

        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            if (EnsureServices())
            {
                presentation.ShowFeedback("暂停输入已收到；当前版本暂未冻结时间。");
            }
        }

        private static GridPosition QuantizeDirection(Vector2 value)
        {
            if (value.sqrMagnitude < 0.18f)
            {
                return GridPosition.Zero;
            }

            if (Mathf.Abs(value.x) > Mathf.Abs(value.y))
            {
                return value.x > 0f ? GridPosition.Right : GridPosition.Left;
            }

            return value.y > 0f ? GridPosition.Up : GridPosition.Down;
        }

        private bool TryAutoMineContact(CharacterMoveResult2D moveResult, float deltaTime)
        {
            using (AutoMineProfilerMarker.Auto())
            {
                AutoMineContactDecision decision = AutoMineContactResolver.Advance(
                    autoMineState,
                    moveResult,
                    grid,
                    playerMiningState.Position,
                    lastDirection,
                    deltaTime,
                    GetAutoMineInterval());

                autoMineState = decision.NextState;
                if (decision.ShouldShowFeedback)
                {
                    presentation.NotifyPlayerMiningContact(decision.TargetCell);
                    presentation.ShowFeedback($"正在挖掘 {decision.TargetCell}...");
                }

                if (!decision.ShouldMine)
                {
                    if (moveResult.WasBlocked && !decision.ShouldShowFeedback && !decision.NextState.HasContact)
                    {
                        presentation.NotifyPlayerBlocked();
                    }

                    return false;
                }

                return MineTarget(decision.TargetCell);
            }
        }

        private void ResetAutoMineState()
        {
            autoMineState = AutoMineContactState.None;
            presentation?.AudioController?.StopPlayerMiningLoop();
            //presentation?.AudioController?.StopPlayerMoveLoop();
        }

        private bool MineTarget(GridPosition target)
        {
            using (MineTargetProfilerMarker.Auto())
            {
                MineInteractionResult result = session.Mine(target);
                presentation.NotifyPlayerMineResolved(target, result);
                if (result == MineInteractionResult.Mined || result == MineInteractionResult.TriggeredBomb)
                {
                    presentation.RefreshAfterTerrainChanged(session.LastMineResolution.ClearedCells);
                }

                bool isAdvancingMine = IsAdvancingMineResult(result);
                string prefix = isAdvancingMine ? $"自动挖掘 {target}" : $"无法挖掘 {target}";
                presentation.ShowFeedback($"{prefix}：{ToChineseResultText(result)}");
                return isAdvancingMine;
            }
        }

        private static Vector2 ToVector2(GridPosition direction)
        {
            return new Vector2(direction.X, direction.Y);
        }

        private static string ToChineseResultText(MineInteractionResult result)
        {
            switch (result)
            {
                case MineInteractionResult.InvalidTarget:
                    return "目标无效";
                case MineInteractionResult.Moved:
                    return "移动成功";
                case MineInteractionResult.BlockedByTerrain:
                    return "被地形阻挡";
                case MineInteractionResult.DrillTooWeak:
                    return "攻击不足";
                case MineInteractionResult.MiningInProgress:
                    return "正在挖掘";
                case MineInteractionResult.Mined:
                    return "已挖开";
                case MineInteractionResult.TriggeredBomb:
                    return "触发炸药";
                default:
                    return "未知结果";
            }
        }

        private float GetAutoMineInterval()
        {
            return session != null
                ? session.PlayerMiningTickIntervalSeconds
                : Mathf.Max(0.02f, autoMineInterval);
        }

        private static bool IsAdvancingMineResult(MineInteractionResult result)
        {
            return result == MineInteractionResult.MiningInProgress
                || result == MineInteractionResult.Mined
                || result == MineInteractionResult.TriggeredBomb;
        }

        private bool CanToggleMarker(GridPosition target, out bool wasMarked)
        {
            wasMarked = false;
            if (grid == null || !grid.IsInside(target))
            {
                return false;
            }

            GridCellState cell = grid.GetCell(target);
            wasMarked = cell.IsMarked;
            return cell.IsMineable && !cell.IsRevealed;
        }

        private bool HasServices => grid != null
            && playerMiningState != null
            && session != null
            && vitals != null
            && experience != null;

        private void AdoptRegistry(RuntimeServiceRegistry registry)
        {
            if (registry == null)
            {
                return;
            }

            grid = registry.Grid;
            playerMiningState = registry.PlayerMiningState;
            session = registry.Session;
            vitals = registry.Vitals;
            experience = registry.Experience;
        }
    }
}
