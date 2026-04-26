# mode-based-interaction Specification

## Purpose
TBD - created by archiving change apply-first-round-gameplay-feedback. Update Purpose after archive.
## Requirements
### Requirement: 主玩法输入必须匹配第一轮反馈按键
项目 SHALL 将主玩法输入调整为第一轮反馈语义：WASD 控制自由移动，`Q` 触发探测，`E` 进入或退出标记模式，`R` 进入或退出建筑模式，鼠标点击用于当前模式的地图目标确认。

#### Scenario: 初始化主玩法输入
- **WHEN** `GameplayInputController` 初始化 Player action map
- **THEN** 它能够读取 `Move`、`Scan`、`ToggleMarkerMode`、`ToggleBuildMode`、`PointerPosition`、`PointerClick`、`Cancel`、`Pause` 和升级选择动作

#### Scenario: 正常模式移动
- **WHEN** 当前交互模式为 Normal 且玩家按下 WASD
- **THEN** 输入会驱动主机器人自由移动，并允许接触岩壁时自动挖掘

### Requirement: 探测必须由 Q 键触发并保持数字风险反馈
玩家 SHALL 通过 `Q` 触发探测功能。探测仍 SHALL 消耗能量并显示与当前探测区域对应的数字风险反馈。

#### Scenario: 能量充足时探测
- **WHEN** 玩家在可探测状态下按下 `Q` 且能量充足
- **THEN** 系统会扣除能量并显示探测数字或中心提示

#### Scenario: 能量不足时探测
- **WHEN** 玩家按下 `Q` 但能量不足
- **THEN** 系统不会执行探测结算，并会显示能量不足反馈

### Requirement: 标记模式必须通过 E 键和鼠标点击操作
玩家 SHALL 通过 `E` 进入或退出标记模式。标记模式下，鼠标位置 SHALL 决定候选格或镜头关注区域，点击可标记或取消标记岩壁。

#### Scenario: 进入标记模式
- **WHEN** 玩家在 Normal 模式按下 `E`
- **THEN** 系统会进入 Marker 模式，HUD 或光标反馈会显示当前鼠标指向的候选岩壁

#### Scenario: 点击岩壁执行标记
- **WHEN** 玩家在 Marker 模式点击一块可标记岩壁
- **THEN** 该岩壁的标记状态会切换，地图表现会刷新，并被从属机器人避险逻辑读取

#### Scenario: 退出标记模式
- **WHEN** 玩家在 Marker 模式再次按下 `E` 或执行取消输入
- **THEN** 系统会退出 Marker 模式并回到 Normal 模式

### Requirement: 建筑模式必须通过 R 键、建筑菜单和鼠标选址操作
玩家 SHALL 通过 `R` 进入建筑模式。建筑模式 SHALL 弹出建筑菜单，允许玩家选择建筑，并在地图空地上预览和确认放置。

#### Scenario: 进入建筑模式
- **WHEN** 玩家在 Normal 模式按下 `R`
- **THEN** 系统会进入 Build 模式并显示可建造建筑菜单

#### Scenario: 选择建筑后预览放置
- **WHEN** 玩家在 Build 模式选择一个建筑定义并移动鼠标到地图上
- **THEN** 系统会显示该建筑 footprint 的合法或非法预览

#### Scenario: 确认合法建筑位置
- **WHEN** 玩家在 Build 模式点击一个合法放置位置
- **THEN** 系统会提交建造请求、扣除资源、生成建筑实例，并退出或保持在可继续建造的状态

#### Scenario: 退出建筑模式
- **WHEN** 玩家在 Build 模式再次按下 `R` 或执行取消输入
- **THEN** 系统会关闭建筑菜单和预览，并回到 Normal 模式

### Requirement: 高优先级 UI 状态必须锁定地图交互
升级选择、暂停和失败状态 SHALL 阻止标记模式、建筑模式和自动挖掘继续提交规则命令，避免 UI 操作与地图操作冲突。

#### Scenario: 升级面板打开时点击地图
- **WHEN** 升级选择 UI 处于打开状态且玩家点击地图
- **THEN** 系统不会执行标记、建造或挖掘命令，点击只会按 UI 规则处理或被忽略

#### Scenario: 游戏失败后输入
- **WHEN** 游戏处于 GameOver 状态且玩家继续按下移动、`Q`、`E` 或 `R`
- **THEN** 系统不会提交新的移动、探测、标记或建造命令

