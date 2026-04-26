## MODIFIED Requirements

### Requirement: 地形 Tile 必须区分基础语义与墙体轮廓职责
Tilemap 表现 SHALL 将 world-aligned 的基础地形表达与 dual-grid 的墙体轮廓表达拆分为不同职责层。空地、不可破坏边界和硬度细节 MUST 保持 world-grid 对齐；可挖岩壁边缘 SHALL 由独立 contour layer 补充表达。

#### Scenario: 显示默认地图
- **WHEN** 地图包含空地、可挖岩壁和不可破坏边界
- **THEN** 场景会同时显示 world-aligned 基础地形信息与可挖岩壁的 dual-grid 轮廓，而不是只依赖单一 terrain tile 表达全部信息

#### Scenario: 显示不同硬度岩壁
- **WHEN** 地图中存在 Soil、Stone、HardRock 或 UltraHard 岩壁
- **THEN** 玩家仍能通过 world-aligned 的 detail 资源区分硬度差异，而 dual-grid 轮廓不会把四种硬度强制拆成四套重复轮廓资源

### Requirement: 不可破坏边界必须保持独立于可挖墙轮廓系统
`TerrainKind.Indestructible` MUST 使用独立的 world-aligned 边界表现，而不是与 `MineableWall` 共用第一版 dual-grid 轮廓系统。

#### Scenario: 地图外框显示
- **WHEN** 默认地图包含不可破坏外边界
- **THEN** 边界会以独立边界资源显示，而不会被渲染成与可挖矿壁同语义的圆角 contour

#### Scenario: 地图同时存在边界和可挖墙
- **WHEN** 玩家在靠近地图边缘处观察画面
- **THEN** 可以从表现上区分“不可进入边界”和“可挖矿壁”这两类地形
