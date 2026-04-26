# tilemap-art-presentation Specification

## Purpose
TBD - created by archiving change improve-tilemap-pixel-art-assets. Update Purpose after archive.
## Requirements
### Requirement: Tilemap 表现必须优先使用配置化美术资产
Gameplay 和 DebugSandbox 的地图表现 SHALL 通过可序列化的美术配置资产读取 Tile 与 Sprite，并在配置缺失时保留程序化 fallback 以避免空画面。

#### Scenario: 场景配置了默认美术资产
- **WHEN** `Gameplay` 或 `DebugSandbox` 进入 Play Mode
- **THEN** 地图、设施、覆盖层和角色表现会优先使用项目内配置的像素风 Tile/Sprite 资产

#### Scenario: 美术配置缺失或引用为空
- **WHEN** 场景没有配置美术资产或某个 Tile 引用缺失
- **THEN** 表现层会回退到程序化占位资源，保证场景仍可运行并显示可读地图

### Requirement: 地形 Tile 必须区分地形类型和岩壁硬度
Tilemap 表现 SHALL 按 `TerrainKind` 与 `HardnessTier` 显示不同地形 Tile，使玩家能从画面中区分空地、不同硬度岩壁和不可破坏边界。

#### Scenario: 显示默认地图
- **WHEN** 地图包含空地、可挖岩壁和不可破坏边界
- **THEN** `Terrain Tilemap` 会使用不同像素 Tile 区分这些地形类型

#### Scenario: 显示不同硬度岩壁
- **WHEN** 地图中存在 Soil、Stone、HardRock 或 UltraHard 岩壁
- **THEN** `Terrain Tilemap` 会使用可区分的岩壁 Tile 表示硬度差异，而不是所有岩壁共用一个色块

### Requirement: 设施和实体必须使用像素风可替换资源
维修站、机器人工厂、主机器人和从属机器人 SHALL 使用项目内可替换的像素风资源，并继续根据运行时服务状态同步位置和显隐。

#### Scenario: 进入主玩法场景
- **WHEN** `Gameplay` 场景初始化完成
- **THEN** 玩家能看到像素风维修站、机器人工厂和主机器人资源，而不是纯色临时块

#### Scenario: 生产从属机器人
- **WHEN** 玩家成功生产从属机器人
- **THEN** 新机器人会使用配置化像素风 Sprite 显示在地图对应位置

### Requirement: 风险反馈覆盖层必须保持可读优先级
标记、危险区和探测提示 SHALL 继续使用独立 Tilemap 层展示，并保持玩家标记优先于危险区覆盖，以确保风险判断信息可读。

#### Scenario: 同一格同时处于危险区并被玩家标记
- **WHEN** 某格既是危险区又被玩家标记
- **THEN** `Overlay Tilemap` 会优先显示玩家标记资源，避免标记被危险区覆盖

#### Scenario: 执行探测
- **WHEN** 玩家执行探测并获得数字或中心提示
- **THEN** `Hint Tilemap` 或 HUD 会显示与探测区域对应的像素风提示资源

### Requirement: 视觉升级不得改变玩法权威状态
像素风 Tile、Sprite、Tile Palette 和美术配置 SHALL 只用于表现层，玩法规则仍 SHALL 从 `LogicalGridState` 和运行时服务读取。

#### Scenario: 替换某个 Tile 资源
- **WHEN** 开发者替换空地、岩壁、危险区或设施 Tile
- **THEN** 玩家移动、挖掘、探测、标记、维修、造机器人和地震结算规则不会因资源替换而改变

#### Scenario: PlayMode 烟测运行
- **WHEN** 测试加载 `Bootstrap -> Gameplay`
- **THEN** 测试可以确认地图表现对象存在并刷新，但规则断言仍基于运行时服务状态而不是 Tilemap 当前贴图

