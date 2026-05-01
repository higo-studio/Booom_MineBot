using Minebot.Bootstrap;
using Minebot.Common;
using Minebot.GridMining;
using Minebot.Progression;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Minebot.Presentation
{
    public sealed class GameplayInputController : MonoBehaviour
    {
        [SerializeField]
        private int repairMetalCost = 2;

        private MinebotInputActions inputActions;
        private MinebotGameplayPresentation presentation;
        private RuntimeServiceRegistry services;
        private GridPosition lastDirection = GridPosition.Up;
        private Vector2 currentMoveInput;
        private Vector2 pointerPosition;
        private AutoMineContactState autoMineState = AutoMineContactState.None;

        [SerializeField]
        private float freeMoveStepSeconds = 0.25f;

        [SerializeField]
        private float autoMineInterval = 0.18f;

        private void Awake()
        {
            presentation = GetComponent<MinebotGameplayPresentation>();
            if (presentation == null)
            {
                presentation = FindAnyObjectByType<MinebotGameplayPresentation>();
            }
        }

        private void OnEnable()
        {
            services = MinebotServices.Current;
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
            if (currentMoveInput.sqrMagnitude > 0.0001f)
            {
                MoveFreeform(currentMoveInput, Time.deltaTime);
            }
        }

        public bool Move(GridPosition direction)
        {
            lastDirection = direction;
            return MoveFreeform(ToVector2(direction), Mathf.Max(0.02f, freeMoveStepSeconds));
        }

        public bool MoveFreeform(Vector2 direction, float deltaTime)
        {
            if (!CanAcceptGameplayInput() || presentation.InteractionMode != GameplayInteractionMode.Normal)
            {
                return false;
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                ResetAutoMineState();
                return false;
            }

            lastDirection = QuantizeDirection(direction);
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

        public bool MineFacingCell()
        {
            if (!CanAcceptGameplayInput())
            {
                return false;
            }

            GridPosition target = services.PlayerMiningState.Position + lastDirection;
            return MineTarget(target);
        }

        public bool ToggleMarkerFacingCell()
        {
            if (!CanAcceptGameplayInput())
            {
                return false;
            }

            GridPosition target = services.PlayerMiningState.Position + lastDirection;
            bool marked = services.Session.ToggleMarker(target);
            presentation.RefreshAfterMarkerChanged();
            presentation.ShowFeedback(marked ? $"已标记 {target}，机器人会避开该格。" : $"已取消或无法标记 {target}。");
            return marked;
        }

        public bool ToggleMarkerMode()
        {
            if (!CanChangeMode())
            {
                return false;
            }

            bool entering = presentation.InteractionMode != GameplayInteractionMode.Marker;
            presentation.SetInteractionMode(entering ? GameplayInteractionMode.Marker : GameplayInteractionMode.Normal);
            presentation.ShowFeedback(entering ? "已进入标记模式：点击岩壁标记。" : "已退出标记模式。");
            return true;
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

            presentation.ShowFeedback(entering ? "已进入建筑模式：选择建筑后点击空地。" : "已退出建筑模式。");
            return true;
        }

        public bool ClickGridCell(GridPosition target)
        {
            if (!EnsureServices())
            {
                return false;
            }

            if (services.Vitals.IsDead || services.Experience.HasPendingUpgrade || presentation.IsUpgradePanelShowing)
            {
                presentation.ShowFeedback("输入已锁定，先处理升级或失败状态。");
                return false;
            }

            if (presentation.InteractionMode == GameplayInteractionMode.Marker)
            {
                bool marked = services.Session.ToggleMarker(target);
                presentation.RefreshAfterMarkerChanged();
                presentation.ShowFeedback(marked ? $"已标记 {target}，机器人会避开该格。" : $"已取消或无法标记 {target}。");
                return true;
            }

            if (presentation.InteractionMode == GameplayInteractionMode.Build)
            {
                BuildingDefinition definition = presentation.GetSelectedBuildingOrDefault();
                return presentation.TryPlaceBuildingAt(definition, target);
            }

            return false;
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
            if (selected && !services.Experience.HasPendingUpgrade && !services.Vitals.IsDead)
            {
                presentation.SetInteractionMode(GameplayInteractionMode.Normal);
            }

            return selected;
        }

        private bool CanAcceptGameplayInput()
        {
            if (!EnsureServices())
            {
                return false;
            }

            if (services.Vitals.IsDead)
            {
                presentation.SetInteractionMode(GameplayInteractionMode.GameOver);
                return false;
            }

            if (services.Experience.HasPendingUpgrade || presentation.IsUpgradePanelShowing)
            {
                presentation.SetInteractionMode(GameplayInteractionMode.UpgradeLocked);
                presentation.ShowFeedback("升级待选择，普通操作已暂停。请按 1/2/3 或点击升级项。");
                return false;
            }

            return true;
        }

        private bool CanChangeMode()
        {
            if (!EnsureServices())
            {
                return false;
            }

            if (services.Vitals.IsDead)
            {
                presentation.SetInteractionMode(GameplayInteractionMode.GameOver);
                return false;
            }

            if (services.Experience.HasPendingUpgrade || presentation.IsUpgradePanelShowing)
            {
                presentation.SetInteractionMode(GameplayInteractionMode.UpgradeLocked);
                presentation.ShowFeedback("升级待选择，模式操作已暂停。请按 1/2/3 或点击升级项。");
                return false;
            }

            return true;
        }

        private bool EnsureServices()
        {
            if (services == null && MinebotServices.IsInitialized)
            {
                services = MinebotServices.Current;
            }

            if (presentation == null)
            {
                presentation = FindAnyObjectByType<MinebotGameplayPresentation>();
            }

            return services != null && presentation != null;
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            currentMoveInput = context.ReadValue<Vector2>();
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            currentMoveInput = Vector2.zero;
            ResetAutoMineState();
        }

        private void OnToggleMarkerModePerformed(InputAction.CallbackContext context)
        {
            ToggleMarkerMode();
        }

        private void OnToggleBuildModePerformed(InputAction.CallbackContext context)
        {
            ToggleBuildMode();
        }

        private void OnPointerPositionPerformed(InputAction.CallbackContext context)
        {
            pointerPosition = context.ReadValue<Vector2>();
            if (presentation != null && presentation.InteractionMode == GameplayInteractionMode.Build)
            {
                presentation.SetBuildPreview(presentation.ScreenToGridPosition(pointerPosition));
            }
        }

        private void OnPointerClickPerformed(InputAction.CallbackContext context)
        {
            ClickGridCell(presentation.ScreenToGridPosition(pointerPosition));
        }

        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            if (!EnsureServices())
            {
                return;
            }

            if (presentation.InteractionMode == GameplayInteractionMode.Marker || presentation.InteractionMode == GameplayInteractionMode.Build)
            {
                presentation.SetInteractionMode(GameplayInteractionMode.Normal);
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
            AutoMineContactDecision decision = AutoMineContactResolver.Advance(
                autoMineState,
                moveResult,
                services.Grid,
                services.PlayerMiningState.Position,
                lastDirection,
                deltaTime,
                autoMineInterval);

            autoMineState = decision.NextState;
            if (decision.ShouldShowFeedback)
            {
                presentation.NotifyPlayerMiningContact(decision.TargetCell);
                presentation.ShowFeedback($"正在挖掘 {decision.TargetCell}...");
            }

            if (!decision.ShouldMine)
            {
                if (moveResult.WasBlocked && !decision.ShouldShowFeedback)
                {
                    presentation.NotifyPlayerBlocked();
                }

                return false;
            }

            ResetAutoMineState();
            return MineTarget(decision.TargetCell);
        }

        private void ResetAutoMineState()
        {
            autoMineState = AutoMineContactState.None;
        }

        private bool MineTarget(GridPosition target)
        {
            MineInteractionResult result = services.Session.Mine(target);
            presentation.NotifyPlayerMineResolved(target, result);
            if (result == MineInteractionResult.Mined || result == MineInteractionResult.TriggeredBomb)
            {
                presentation.RefreshAfterTerrainChanged();
            }
            presentation.ShowFeedback(result == MineInteractionResult.Mined || result == MineInteractionResult.TriggeredBomb
                ? $"自动挖掘 {target}：{ToChineseResultText(result)}"
                : $"无法挖掘 {target}：{ToChineseResultText(result)}");
            return result == MineInteractionResult.Mined || result == MineInteractionResult.TriggeredBomb;
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
                    return "钻头强度不足";
                case MineInteractionResult.Mined:
                    return "已挖开";
                case MineInteractionResult.TriggeredBomb:
                    return "触发炸药";
                default:
                    return "未知结果";
            }
        }
    }
}
