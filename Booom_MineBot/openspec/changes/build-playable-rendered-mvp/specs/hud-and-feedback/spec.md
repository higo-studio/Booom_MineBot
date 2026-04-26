## ADDED Requirements

### Requirement: HUD 必须显示核心生存和经济状态
项目 SHALL 在主玩法场景中显示玩家生命值、金属、能量、经验进度、当前波次和基础操作提示。

#### Scenario: 进入 Gameplay
- **WHEN** `Gameplay` 场景初始化完成
- **THEN** HUD 会显示生命、金属、能量、经验和波次状态

#### Scenario: 状态发生变化
- **WHEN** 玩家挖掘、探测、维修、升级或生产机器人导致状态变化
- **THEN** HUD 会在同一轮刷新中显示最新数值

### Requirement: HUD UI 必须以独立 prefab 资产迭代
项目 SHALL 将主玩法 HUD 的 UGUI 层级保存为可复用 prefab 资产，使 UI 布局、面板和按钮可以脱离场景脚本单独更新。

#### Scenario: 进入 Gameplay
- **WHEN** `Gameplay` 或 `DebugSandbox` 场景初始化表现层
- **THEN** 表现层会实例化或复用 HUD prefab，并通过绑定脚本刷新文本、面板和按钮状态

#### Scenario: prefab 缺少局部节点
- **WHEN** HUD prefab 在迭代中暂时缺少某个必需文本、面板或按钮节点
- **THEN** 绑定脚本会补齐最小默认节点，保证运行时 HUD 不会因空引用失效

#### Scenario: 按功能拆分面板 prefab
- **WHEN** HUD 包含状态、提示、升级、建筑或建筑交互等不同功能面板
- **THEN** 每个功能面板都应拥有独立 prefab 和面板绑定脚本，HUD root 只负责 slot 布局与装配，不直接硬编码所有子节点结构

### Requirement: 升级选择必须以可交互 UI 呈现
项目 SHALL 在经验达到阈值时显示可交互升级选择 UI，并允许玩家选择升级后返回玩法循环。

#### Scenario: 经验达到阈值
- **WHEN** 玩家通过挖掘获得足够经验
- **THEN** 游戏会显示升级选择 UI，并让玩家看到至少一个可选升级项

#### Scenario: 选择升级项
- **WHEN** 玩家选择一个升级项
- **THEN** 升级效果会应用，升级 UI 会关闭，玩家能继续操作主机器人

### Requirement: 风险反馈必须可读
项目 SHALL 为探测数字、玩家标记、爆炸结果和危险区提供可读反馈，使玩家能理解风险判断结果。

#### Scenario: 探测返回数字
- **WHEN** 玩家执行探测并获得数字结果
- **THEN** 数字会显示在地图格或 HUD 中，且能和被探测区域建立清晰对应

#### Scenario: 爆炸结算
- **WHEN** 玩家挖到炸药并触发爆炸
- **THEN** 画面会显示受影响区域变化，并让玩家看到生命值损失

### Requirement: 地震波和失败状态必须有明确提示
项目 SHALL 在地震波倒计时、危险区结算和失败发生时提供明确的 HUD 或场景反馈。

#### Scenario: 地震波即将到来
- **WHEN** 当前波次倒计时接近结算
- **THEN** HUD 会显示地震预警或倒计时提示

#### Scenario: 玩家失败
- **WHEN** 玩家生命值归零或地震结算时处于致死危险区
- **THEN** 游戏会显示失败提示，并阻止玩家继续正常操作
