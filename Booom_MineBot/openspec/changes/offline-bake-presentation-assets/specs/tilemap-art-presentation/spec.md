## MODIFIED Requirements

### Requirement: Tilemap 表现必须优先使用配置化美术资产
Gameplay 和 DebugSandbox 的地图表现 SHALL 通过可序列化的美术配置资产读取 Tile 与 Sprite。默认表现资源必须来自离线生成的 art set / profile / prefab 资产；运行时 MUST NOT 再用程序化贴图 fallback 避免空画面。

#### Scenario: 场景配置了默认美术资产
- **WHEN** `Gameplay` 或 `DebugSandbox` 进入 Play Mode
- **THEN** 地图、设施、覆盖层和角色表现会使用项目内离线生成的 Tile/Sprite/Prefab 资产

#### Scenario: 默认表现资源缺失
- **WHEN** 场景没有配置 art set，且默认 `Resources/Minebot` 表现资产缺失或关键引用为空
- **THEN** 系统会显式暴露缺失项，而不是自动生成程序化占位纹理继续运行

### Requirement: 视觉升级不得改变玩法权威状态
像素风 Tile、Sprite、Tile Palette 和美术配置 SHALL 只用于表现层，玩法规则仍 SHALL 从 `LogicalGridState` 和运行时服务读取。即使默认资源现在改为强依赖离线资产，这些资源也 MUST NOT 反向成为玩法真相来源。

#### Scenario: 替换离线 dual-grid 资源
- **WHEN** 开发者替换某个 dual-grid terrain/fog tile、glyph atlas 或 overlay 资源
- **THEN** 玩家移动、挖掘、危险区、扫描和建造等规则不会因资源替换而改变
