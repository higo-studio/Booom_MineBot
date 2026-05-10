# Context

当前炸药真相保存在 `LogicalGridState` 的 `GridCellState.HasBomb` 中，规则层与表现层都已经依赖这份权威数据。正常玩法要求炸药在开局时隐藏，直到挖掘、爆炸或扫描反馈间接揭示风险，因此本次不能改动规则真相，只能在表现层额外挂一层“只读的调试可视化”。

现有表现层已经有独立 overlay tilemap：

- `Marker Tilemap`
- `Danger Tilemap`
- `Build Preview Tilemap`

因此新增 GM 显雷最稳的路径，是继续走 overlay tilemap，而不是把炸药真相写进地形 tile 或角色反馈中。

# Decision

## 1. GM 显雷只做表现层开关，不进入规则服务

- 默认关闭。
- 开启后，从 `LogicalGridState` 扫描当前仍然满足 `IsMineable && HasBomb` 的格子。
- 使用单独的 `GM Bomb Tilemap` 绘制 overlay。
- 关闭后立即清空该 overlay。

这样可以保证：

- 炸药仍然是“隐藏真相”，只是被 GM 可视化读出来。
- 不会污染 `HazardService`、`RobotAutomationService` 或 `GameSessionService` 的行为边界。
- 不需要为炸药再维护第二份运行时显隐状态。

## 2. 复用现有 hologram tile 作为显雷图标

本轮不新增专门的炸弹美术资源，优先复用 `ScanHintTile`。如果 art set 缺少 `ScanHintTile`，则回退到 `DangerTile`，再回退到 `MarkerTile`。

放弃新增专用 bomb tile 的原因：

- 这次目标是快速提供稳定的 GM 调试能力，而不是扩展一套新的 UI / 美术资源链。
- 现有 hologram tile 已经足够表达“这是额外调试信息”。

## 3. 输入入口先走运行时调试快捷键

当前生成的 `MinebotInputActions` 没有预留 GM 动作。为了把改动收敛在最小范围，先在 `GameplayInputController` 中增加一个运行时快捷键入口，并提供可测试的公开方法 `ToggleGmBombReveal()`。

本轮采用：

- `F6` 作为运行时 GM 显雷切换键
- HUD / feedback 文本同步显示 `GM炸药 ON/OFF`

# Risks

- GM overlay 可能遮挡玩家标记。Mitigation：单独使用一层 tilemap，并复用较轻量的 hologram tile，而不是直接改 marker 层。
- Terrain 局部刷新后若不额外处理，overlay 可能残留。Mitigation：在 GM 开启时，地形变化直接走整张 grid refresh；局部刷新链路也补齐单格 overlay 清理。
- 调试快捷键若写进普通模式输入逻辑，可能影响正式交互。Mitigation：该开关只切换表现层显示，不改变任何规则结算与交互 mode。
