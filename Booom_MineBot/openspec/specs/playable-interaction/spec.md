# playable-interaction Specification

## Purpose
TBD - created by archiving change build-playable-rendered-mvp. Update Purpose after archive.
## Requirements
### Requirement: 输入资产必须提供 Minebot 语义动作
项目 SHALL 使用 `Assets/InputSystem_Actions.inputactions` 作为主输入资产，SHALL 启用 Input System 生成的 C# wrapper，并在 `Player` action map 中提供 Minebot 语义动作，而不是依赖 Unity 模板中的 `Attack`、`Jump`、`Crouch`、`Sprint` 等泛用动作名。

#### Scenario: 读取主玩法输入动作
- **WHEN** `GameplayInputController` 初始化输入
- **THEN** 它能够通过 `Minebot.Bootstrap.MinebotInputActions.PlayerActions` 读取 `Move`、`Mine`、`Scan`、`ToggleMarker`、`Repair`、`BuildRobot`、`SelectUpgrade1`、`SelectUpgrade2`、`SelectUpgrade3`、`Pause` 和 `PointerPosition`

#### Scenario: 保留 UI 输入动作
- **WHEN** 升级选择、按钮点击或暂停 UI 需要导航
- **THEN** 系统继续使用同一输入资产中的 `UI` action map 处理 `Navigate`、`Submit`、`Cancel`、`Point` 和 `Click`

### Requirement: 主玩法场景必须支持键盘驱动的网格移动和挖掘
项目 SHALL 在 `Gameplay` 场景中提供可操作输入，使玩家能够移动主机器人并挖掘相邻岩壁格。

#### Scenario: 移动到相邻空地
- **WHEN** 玩家按下移动输入，且目标格为可通行空地
- **THEN** 主机器人规则位置和画面位置都会移动到目标格

#### Scenario: 挖掘相邻岩壁
- **WHEN** 玩家面向或选择一个相邻可挖岩壁格并触发挖掘
- **THEN** 系统会调用规则服务结算挖掘，并刷新地图表现、资源和经验显示

### Requirement: 主玩法场景必须支持探测和标记操作
项目 SHALL 在 `Gameplay` 场景中提供探测与标记输入，并把结果以地图或 HUD 反馈展示给玩家。

#### Scenario: 执行探测
- **WHEN** 玩家能量充足并触发探测
- **THEN** 能量会被扣除，探测数字会在当前区域以可读方式显示

#### Scenario: 标记疑似危险格
- **WHEN** 玩家对候选岩壁格触发标记
- **THEN** 该格会显示标记表现，并被从属机器人安全逻辑避开

### Requirement: 主玩法场景必须支持维修和造机器人
项目 SHALL 允许玩家在主玩法场景中通过输入触发维修站维修和机器人工厂生产机器人。

#### Scenario: 执行维修
- **WHEN** 玩家受伤、资源足够，并在维修站可交互范围内触发维修
- **THEN** 玩家生命值会恢复，HUD 生命显示同步更新

#### Scenario: 生产机器人
- **WHEN** 玩家金属足够，并在机器人工厂可交互范围内触发生产
- **THEN** 金属会被扣除，从属机器人会生成，并在画面中显示

### Requirement: 输入提示必须对玩家可见
项目 SHALL 在主玩法 HUD 中显示当前可用的核心操作按键或交互提示。

#### Scenario: 进入 Gameplay
- **WHEN** 玩家进入 `Gameplay` 场景
- **THEN** HUD 会显示移动、挖掘、探测、标记、维修和造机器人相关的最小操作提示

