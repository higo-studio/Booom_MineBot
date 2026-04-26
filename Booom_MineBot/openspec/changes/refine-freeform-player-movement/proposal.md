## Why

当前自由移动采用逐轴裁剪的简化接触解算，持续顶墙时容易出现高频抖动，贴近角落时也会卡住而不是顺滑滑行。这已经直接影响玩家对“移动、贴墙、自动挖掘”这条核心操作链路的手感，需要在继续迭代其它玩法前先把角色移动基础打稳。

## What Changes

- 将玩家自由移动从“逐轴裁剪 + 接触格推断”重构为“kinematic + sweep-and-move”解算，支持在墙面与角落旁保持稳定滑行。
- 明确区分“移动被阻挡”和“开始自动挖掘”两个状态，避免持续顶墙时每帧进入抖动式反馈循环。
- 稳定墙面接触结果与接触格锁存，降低角落附近 `contactCell` 抖动导致的误判。
- 调整玩家世界坐标与逻辑格同步策略，使自由移动表现继续服务于逻辑网格真相，而不是反向夺走规则权威。
- 为新的移动解算补齐 EditMode 行为测试，并补充必要的 PlayMode 烟雾验证口径。

## Capabilities

### New Capabilities
- `freeform-player-movement`: 定义玩家自由移动在贴墙、滑角、阻挡接触、自动挖掘触发与逻辑格同步上的可验证行为。

### Modified Capabilities
无。

## Impact

- 受影响代码主要位于 `Assets/Scripts/Runtime/Presentation/ActorContactProbe.cs`、`FreeformActorController.cs`、`GameplayInputController.cs`、`MinebotGameplayPresentation.cs`。
- 受影响系统包括玩家移动、接触判定、自动挖掘触发、反馈刷新节流，以及对应的 EditMode / PlayMode 测试。
- 不引入 `CharacterController` 或以 Scene Collider 为真相的物理架构迁移，继续保持逻辑网格作为玩法权威数据源。
