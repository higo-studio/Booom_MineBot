# gameplay-presentation Specification

## Purpose
TBD - created by archiving change build-playable-rendered-mvp. Update Purpose after archive.
## Requirements
### Requirement: 主玩法场景必须渲染可读方格地图
项目 SHALL 在 `Gameplay` 场景中将当前 `LogicalGridState` 渲染为可读的 2D 方格地图，并至少区分空地、可挖岩壁、不可破坏边界、危险区和玩家标记格。

#### Scenario: 从 Bootstrap 进入 Gameplay
- **WHEN** 玩家从 `Bootstrap` 场景启动并进入 `Gameplay`
- **THEN** 画面中会出现可见的方格地图，而不是只有空背景或单个占位块

#### Scenario: 地形状态发生变化
- **WHEN** 玩家成功挖开一格岩壁
- **THEN** 该格的表现会从岩壁更新为空地，并允许玩家后续看到当前位置变化

### Requirement: 主机器人和从属机器人必须有运行时表现
项目 SHALL 为主机器人和本局生产出的从属机器人提供可见表现，并根据规则状态同步它们在方格地图上的位置与存活状态。

#### Scenario: 显示主机器人
- **WHEN** `Gameplay` 场景完成运行时初始化
- **THEN** 主机器人会显示在玩家出生格对应的地图位置

#### Scenario: 生产从属机器人
- **WHEN** 玩家在机器人工厂成功生产一个从属机器人
- **THEN** 画面中会出现一个从属机器人表现对象，并显示在对应出生或工厂位置

### Requirement: 据点设施必须可视化
项目 SHALL 在地图中显示维修站和机器人工厂，使玩家能够识别在哪里执行维修和生产机器人相关操作。

#### Scenario: 进入主玩法场景
- **WHEN** `Gameplay` 场景显示默认地图
- **THEN** 玩家能够区分维修站、机器人工厂和普通地形格

#### Scenario: 玩家靠近设施
- **WHEN** 玩家移动到维修站或机器人工厂附近
- **THEN** 场景表现或 HUD 会提供可执行操作的提示

### Requirement: DebugSandbox 必须复用基础表现层
项目 SHALL 让 `DebugSandbox` 复用主玩法的地图、角色、设施和基础反馈表现组件，以便快速验证规则变化。

#### Scenario: 打开 DebugSandbox
- **WHEN** 开发者打开并运行 `DebugSandbox`
- **THEN** 场景会显示可读的调试地图、主机器人和基础 HUD，而不是空场景

