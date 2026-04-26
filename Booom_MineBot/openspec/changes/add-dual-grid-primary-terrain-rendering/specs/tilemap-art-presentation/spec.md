## MODIFIED Requirements

### Requirement: 地形 Tile 必须区分地形类型和岩壁硬度
Tilemap 表现 SHALL 按 `TerrainKind` 与 `HardnessTier` 清晰区分空地、不同硬度可挖岩壁和不可破坏边界；当前版本允许这些差异通过 dual-grid terrain family layers 共同表达，而不再要求继续由 world-grid `Terrain Tilemap` 对每个逻辑格一对一直绘。

#### Scenario: 显示默认地图
- **WHEN** 地图包含空地、可挖岩壁和不可破坏边界
- **THEN** terrain 主显示会通过 dual-grid family layers 区分这些地形类型，而不是必须存在一个 world-grid `Terrain Tilemap` 逐格承担全部 terrain 语义

#### Scenario: 显示不同硬度岩壁
- **WHEN** 地图中存在 Soil、Stone、HardRock 或 UltraHard 岩壁
- **THEN** 玩家仍能从 terrain 主显示中读出硬度差异，即使这些差异是由多层 dual-grid family 叠加表达，而不是由单一岩壁色块表示

### Requirement: 视觉升级不得改变玩法权威状态
像素风 Tile、Sprite、dual-grid atlas、resolver 和美术配置 SHALL 只用于表现层。即使 terrain 主显示不再由 world-grid `Terrain Tilemap` 直接绘制，玩法规则也仍 SHALL 从 `LogicalGridState` 和运行时服务读取。

#### Scenario: 替换 dual-grid terrain family 资源
- **WHEN** 开发者替换 floor、岩壁或 boundary 的 dual-grid atlas
- **THEN** 玩家移动、挖掘、探测、标记、维修、造机器人和地震结算规则不会因显示资源替换而改变

#### Scenario: PlayMode 烟测运行
- **WHEN** 测试加载 `Bootstrap -> Gameplay`
- **THEN** 测试可以确认 dual-grid terrain display layers 存在并刷新，但规则断言仍基于运行时服务状态而不是当前贴图外观
