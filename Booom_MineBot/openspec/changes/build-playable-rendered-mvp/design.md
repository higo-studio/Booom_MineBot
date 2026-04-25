## Context

`bootstrap-minebot-foundation` 已经建立了核心规则服务和最小场景流，但当前 `Gameplay` 场景只有占位相机与 marker。玩家无法看到策划案要求的方格矿洞、主机器人、从属机器人、维修站、机器人工厂、资源状态、探测数字、危险标记、地震波或升级选择，因此现有工程还不能作为“可玩的游戏”进行体验验证。

本变更的目标是补齐 `Gameplay Presentation` 层，把已有规则服务接到 Unity 场景、Input System 和 UGUI。表现层必须继续服从既有架构边界：`LogicalGridState` 和运行时服务是玩法真相，Scene、Prefab、Tilemap、Sprite 和 UI 只负责呈现与输入转发。

## Goals / Non-Goals

**Goals:**

- 让从 `Bootstrap` 启动进入 `Gameplay` 后，玩家能立即看到可读的地底方格地图、主机器人、HUD 和基础操作提示。
- 支持键盘/鼠标或键盘快捷键完成移动、挖掘、探测、标记、维修和造机器人。
- 在视觉上区分空地、岩壁、不可破坏边界、玩家标记、危险区、维修站、机器人工厂、主机器人和从属机器人。
- 提供最小 HUD 与反馈：生命、金属、能量、经验、波次、探测数字、升级弹窗、地震预警和失败状态。
- 为 `Gameplay` 和 `DebugSandbox` 增加可重复的 PlayMode/手动烟雾验证路径。

**Non-Goals:**

- 不制作最终美术资源、动画系统、特效管线或完整音频。
- 不引入 DOTS/ECS、Addressables、第三方 UI 框架、第三方 FSM/行为树或正式存档系统。
- 不把 Tilemap/Scene 作为运行时规则真相；表现层只能读取服务快照并发起命令。
- 不在本变更内重写现有规则服务的核心结算逻辑。

## Decisions

### 1. 新增轻量表现层模块，而不是把逻辑塞进现有服务

新增或扩展 `Minebot.Runtime.UI` / `Minebot.Runtime.Presentation` 方向的 MonoBehaviour 组件：

```text
GameplayController
  -> 读取输入
  -> 调用 RuntimeServiceRegistry
  -> 触发刷新

GridPresentation
  -> 读取 LogicalGridState
  -> 刷新多层 Tilemap

ActorPresentation
  -> 主机器人与从属机器人位置同步

HudPresentation / UpgradePanel
  -> 读取资源、生命、经验、波次
  -> 显示选择与反馈
```

选择理由：

- 规则层已经有明确服务边界，表现层只需要订阅/刷新，不应该反向持有规则决策。
- MonoBehaviour 组件便于直接装配到 `Gameplay` 和 `DebugSandbox`，符合 GameJam MVP 速度。

备选方案：把表现逻辑直接写进 `GameSessionService`。

放弃原因：会破坏“纯规则服务可 EditMode 测试”的边界，也会让后续 UI/美术迭代牵动规则程序集。

### 2. 地图表现明确采用 Tilemap 方案

第一版地图显示直接采用 Unity Tilemap，而不是每格一个 SpriteRenderer GameObject。Tilemap 只作为运行时表现层：它从 `LogicalGridState` 刷新显示，不保存玩法真相，也不参与规则判断。

推荐层级：

```text
Grid
  Terrain Tilemap      空地 / 岩壁 / 不可破坏边界
  Facility Tilemap     维修站 / 机器人工厂
  Overlay Tilemap      玩家标记 / 危险区 / 爆炸影响
  Hint Tilemap         探测数字占位或简单提示符
Actor Root
  Player SpriteRenderer
  Robot SpriteRenderer[]
UI Canvas
  HUD / Upgrade / Warning / GameOver
```

Tile 资产第一版使用程序化占位图或最小 `.asset` Tile，不等待正式美术。主机器人、从属机器人和需要连续移动/动画的实体继续使用 SpriteRenderer 或 prefab；HUD 与升级选择继续使用 UGUI。

建议视觉约定：

- 空地：深色低饱和格
- 岩壁：棕/灰色块，并用硬度调整亮度或边框
- 不可破坏边界：深灰或黑色
- 主机器人：高亮青色/黄色
- 从属机器人：绿色
- 标记疑似炸药：红旗或红色叠层
- 危险区：红色半透明覆盖
- 维修站：蓝色十字或蓝色设施块
- 机器人工厂：橙色设施块

选择理由：

- Tilemap 与策划里的方格矿洞天然匹配，后续接地图编辑和 Bake 管线更顺。
- 地形、设施、危险区和标记都属于格子化信息，用多层 Tilemap 比大量格子 GameObject 更清晰。
- 程序化占位 Tile 可以在仓库内快速生成，不阻塞正式美术输入。

备选方案：每个格子使用 SpriteRenderer GameObject。

放弃原因：实现短期更快，但会偏离既有 Tilemap/Bake 方向；后续替换正式 Tile 和地图编辑工具时需要重写更多表现层。

### 3. 输入层只转发意图，不直接改状态

项目使用 `Assets/InputSystem_Actions.inputactions` 作为唯一输入资产，并启用 Input System 生成的 C# wrapper。wrapper 输出为 `Assets/Scripts/Runtime/Bootstrap/MinebotInputActions.cs`，类名为 `Minebot.Bootstrap.MinebotInputActions`。`GameplayInputController` 通过该 wrapper 读取 `Player` action map，并将输入转换为规则命令：

```text
Move             Value Vector2       WASD / Arrow / Gamepad leftStick 或 dpad
Mine             Button              Space / E / Mouse left / Gamepad south
Scan             Button              Q / Gamepad north
ToggleMarker     Button              F / Mouse right / Gamepad west
Repair           Button              R / Gamepad left shoulder
BuildRobot       Button              B / Gamepad right shoulder
SelectUpgrade1   Button              1 / Gamepad dpad left
SelectUpgrade2   Button              2 / Gamepad dpad up
SelectUpgrade3   Button              3 / Gamepad dpad right
Pause            Button              Escape / Gamepad start
PointerPosition  PassThrough Vector2 Pointer position
```

`Move` 是连续 `Value` 输入，但玩法层必须离散化为四方向网格步进，并记录最后朝向。其它玩法命令均在 `performed` 时结算一次，不使用模板里的 `Attack / Jump / Crouch / Sprint` 语义名。`PointerPosition` 只用于鼠标悬停格或后续选格逻辑，不直接触发规则命令。

`UI` action map 保留 Unity 模板里的 `Navigate / Submit / Cancel / Point / Click / ScrollWheel` 等动作，用于 UGUI 导航和点击。输入层不得直接改 `GridCellState`、资源或生命值，只能调用 `GameSessionService`、`BaseOpsService`、`RobotFactoryService` 等服务。后续实现若需要访问 action，优先使用 `MinebotInputActions.PlayerActions` / `MinebotInputActions.UIActions`，不要用字符串查找 action 名。

### 4. 场景装配优先保持单入口

`Bootstrap` 继续负责服务初始化并加载 `Gameplay`。`Gameplay` 场景内放置表现层根对象，例如：

```text
Gameplay Root
  Presentation Root
    Grid Presentation
    Actor Presentation
    Feedback Presentation
  UI Canvas
    HUD
    Upgrade Panel
    Game Over Panel
```

`DebugSandbox` 可以复用同一套表现组件，但允许挂调试按钮或固定配置，便于验证挖掘/探测/波次。

### 5. 验证以“可看到 + 可操作 + 状态变化”作为最低门槛

测试不要求像素级截图对比，但必须验证：

- `Bootstrap` 进入 `Gameplay` 后存在 Camera、地图可视对象、玩家可视对象和 HUD。
- 执行一次移动/挖掘后，玩家位置和格子表现刷新。
- 经验满后升级 UI 出现并可选择。
- 受伤后维修会刷新生命显示。
- 造机器人后从属机器人可视对象出现。
- 波次/危险区/失败状态至少有可读文本或覆盖层提示。

## Risks / Trade-offs

- [Risk] 程序化 Tile 可能看起来粗糙。→ Mitigation: 先满足可读性和可玩性，后续可替换正式 Tile 资源而不改规则服务。
- [Risk] Tilemap 初始装配比 SpriteRenderer 网格多一些资产和层级配置。→ Mitigation: 先使用程序化 Tile 资产和固定层级，避免依赖 Tile Palette 手工流程。
- [Risk] 表现层刷新如果每次全量重绘 Tilemap，地图变大后可能浪费。→ Mitigation: MVP 地图较小，先做全量刷新；后续再基于事件或脏格优化。
- [Risk] 输入与 UI 快捷键可能和后续正式交互冲突。→ Mitigation: 把按键集中在单一 controller 和 HUD 文案中，后续替换成本低。
- [Risk] 场景对象与规则状态不同步。→ Mitigation: 所有状态变化后统一走刷新入口，PlayMode 测试覆盖关键链路。

## Migration Plan

1. 保留当前服务层和 Bootstrap 流程。
2. 新增 Tilemap 表现层组件、占位 Tile 资产和 `Gameplay` 场景装配。
3. 将现有 PlayMode 烟测扩展为渲染/交互烟测。
4. 通过 `unity.compile`、PlayMode 测试和手动启动 `Bootstrap` 验证。

回退策略：表现层组件与场景对象可以整体移除，现有规则服务和 EditMode 测试不应受影响。

## Open Questions

- 挖掘目标选择采用“面向方向”还是“鼠标悬停格”；第一版可先用方向键最后方向 + Space。
- 探测数字显示是落在格子上，还是先进入 HUD/浮动文本；第一版建议直接显示在格子中心。
- 波次推进是否需要真实时间自动触发，还是先用快捷键触发；第一版建议自动计时并保留调试快捷键。
