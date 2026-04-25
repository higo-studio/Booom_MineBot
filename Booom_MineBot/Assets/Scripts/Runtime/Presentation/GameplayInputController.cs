using Minebot.Bootstrap;
using Minebot.Common;
using Minebot.GridMining;
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
            player.Mine.performed += OnMinePerformed;
            player.Scan.performed += OnScanPerformed;
            player.ToggleMarker.performed += OnToggleMarkerPerformed;
            player.Repair.performed += OnRepairPerformed;
            player.BuildRobot.performed += OnBuildRobotPerformed;
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
            player.Mine.performed -= OnMinePerformed;
            player.Scan.performed -= OnScanPerformed;
            player.ToggleMarker.performed -= OnToggleMarkerPerformed;
            player.Repair.performed -= OnRepairPerformed;
            player.BuildRobot.performed -= OnBuildRobotPerformed;
            player.SelectUpgrade1.performed -= OnSelectUpgrade1Performed;
            player.SelectUpgrade2.performed -= OnSelectUpgrade2Performed;
            player.SelectUpgrade3.performed -= OnSelectUpgrade3Performed;
            player.Pause.performed -= OnPausePerformed;
            inputActions.Dispose();
            inputActions = null;
        }

        public bool Move(GridPosition direction)
        {
            if (!CanAcceptGameplayInput())
            {
                return false;
            }

            lastDirection = direction;
            MineInteractionResult result = services.Session.Move(direction);
            if (result == MineInteractionResult.Moved)
            {
                presentation.ShowFeedback($"移动到 {services.PlayerMiningState.Position}");
                return true;
            }

            presentation.ShowFeedback($"移动受阻：{result}");
            return false;
        }

        public bool MineFacingCell()
        {
            if (!CanAcceptGameplayInput())
            {
                return false;
            }

            GridPosition target = services.PlayerMiningState.Position + lastDirection;
            MineInteractionResult result = services.Session.Mine(target);
            presentation.ShowFeedback(result == MineInteractionResult.Mined || result == MineInteractionResult.TriggeredBomb
                ? $"挖掘 {target}: {result}"
                : $"无法挖掘 {target}: {result}");
            return result == MineInteractionResult.Mined || result == MineInteractionResult.TriggeredBomb;
        }

        public bool ScanCurrentCell()
        {
            if (!CanAcceptGameplayInput())
            {
                return false;
            }

            GridPosition origin = services.PlayerMiningState.Position;
            ScanResult result = services.Session.Scan(origin);
            if (result.Success)
            {
                presentation.RecordScan(origin, result.BombCount);
                return true;
            }

            presentation.ShowFeedback("能量不足，无法探测。");
            return false;
        }

        public bool ToggleMarkerFacingCell()
        {
            if (!CanAcceptGameplayInput())
            {
                return false;
            }

            GridPosition target = services.PlayerMiningState.Position + lastDirection;
            bool marked = services.Session.ToggleMarker(target);
            presentation.ShowFeedback(marked ? $"已标记 {target}，机器人会避开该格。" : $"已取消或无法标记 {target}。");
            return marked;
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

            return presentation.SelectUpgradeIndex(index);
        }

        private bool CanAcceptGameplayInput()
        {
            return EnsureServices() && !services.Vitals.IsDead;
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
            Vector2 value = context.ReadValue<Vector2>();
            GridPosition direction = QuantizeDirection(value);
            if (!direction.Equals(GridPosition.Zero))
            {
                Move(direction);
            }
        }

        private void OnMinePerformed(InputAction.CallbackContext context)
        {
            MineFacingCell();
        }

        private void OnScanPerformed(InputAction.CallbackContext context)
        {
            ScanCurrentCell();
        }

        private void OnToggleMarkerPerformed(InputAction.CallbackContext context)
        {
            ToggleMarkerFacingCell();
        }

        private void OnRepairPerformed(InputAction.CallbackContext context)
        {
            Repair();
        }

        private void OnBuildRobotPerformed(InputAction.CallbackContext context)
        {
            BuildRobot();
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
                presentation.ShowFeedback("暂停输入已收到；MVP 暂未冻结时间。");
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
    }
}
