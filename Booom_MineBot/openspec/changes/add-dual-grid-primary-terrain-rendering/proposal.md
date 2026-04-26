## Why

当前地形渲染仍是混合方案：

- `LogicalGridState` 负责玩法真相
- world-grid `Terrain Tilemap` 直接绘制空地 / 岩壁 / 边界
- half-cell offset dual-grid 只负责补充 `Wall Contour`

这条路线已经能改善岩壁外轮廓，但它和当前确认的新目标不是一回事。现在明确的新要求是：

- world grid 继续作为碰撞、挖掘、AI、危险区与测试的真相
- world grid 不再直接参与 terrain 主渲染
- floor 也进入 dual-grid 主显示，而不是只让墙体用 contour
- 硬度必须进入主渲染
- 当前实现接受“按材质 family 分层叠加”的方案
- 同时必须保留将来切到“精确 multistate / 625-state 或 shader/mesh”方案的接口
- `Indestructible` 也进入这套 dual-grid terrain family，而不是留在 world-aligned 边界层外

这意味着已经完成的 `add-dual-grid-wall-contour-rendering` 只能算过渡阶段。它验证了 half-cell offset、2x2 采样和 16-state lookup 的可行性，但其“world-grid terrain 继续直接渲染”的前提已经不再满足当前方向。

## What Changes

- 新增一个独立 change，把 Minebot 的 terrain 主显示从“world-grid base + dual-grid contour overlay”升级为“pure dual-grid primary terrain rendering”。
- 保留 `MapDefinition -> LogicalGridState` 作为唯一玩法真相；玩家碰撞、挖掘、AI、危险区、建筑占位和测试断言继续读取 world-grid 状态。
- terrain 主显示改为同一 `Grid Root` 下的一组 half-cell offset display Tilemap，统一使用 `localPosition = (-0.5, -0.5, 0)`。
- terrain scene graph 使用固定 family 命名与稳定 sorting order，避免实现期在 layer name / 叠放顺序上再次摇摆。
- terrain family 至少包括：`Floor`、`Soil`、`Stone`、`HardRock`、`UltraHard`、`Boundary`。
- 每个 family 当前都采用自己的 `16-state` dual-grid atlas；最终画面由多层 family 叠加得到，而不是继续依赖单一 world-grid terrain tile。
- 引入 `Resolver` 抽象：当前版本用 layered binary resolver，把四角材质样本拆成若干 family mask；未来保留 exact multistate resolver 的可替换接口，但本 change 不实现 exact 方案。resolver 输出顺序必须稳定，方便测试和后续实现替换。
- 一个 world cell 的材质变化只允许脏掉周围 4 个 display cells；初始化和整图替换仍可走全量刷新。
- `Danger`、`Marker`、`BuildPreview`、`Scan`、设施和角色继续保持独立表现层，不被强制并入 dual-grid terrain renderer。
- 扩展美术配置与 fallback 逻辑，使默认资源能提供 `6 x 16` 的 dual-grid terrain families，并保留配置缺失时基于共享 shape mask + family tint 的程序化占位。
- 更新测试基线：从“断言某个 world cell 上的 terrain tile”转为“断言某个 display cell 上的 dual-grid family 叠加结果”和“单格变化只刷新 4 个 display cells”。

## Capabilities

### New Capabilities

- `dual-grid-primary-terrain-rendering`: 使用 half-cell offset dual-grid family layers 作为唯一 terrain 主显示，并从 2x2 world-cell 样本生成 floor / hardness / boundary 的多层合成结果。

### Modified Capabilities

- `tilemap-art-presentation`: 地形表现从“world-grid 单格 terrain tile 直绘”升级为“dual-grid primary terrain families + 独立 overlay”，但仍必须清晰区分空地、不同硬度岩体和不可破坏边界。
- `pixel-art-asset-pipeline`: terrain 资源生产目标从单格墙体 tile / contour family 扩展为多个 dual-grid material family 的 `16-state atlas`。

## Impact

- 影响 `Assets/Scripts/Runtime/Presentation`：需要替换当前 `TerrainTilemap + WallContourTilemaps` 的 terrain 装配方式，引入多 family dual-grid 主渲染层和 resolver 抽象。
- 影响 `MinebotPresentationArtSet` / `MinebotPresentationAssets`：需要从单格地形 tile + wall contour tiles 扩展到多 family 的 dual-grid atlas 配置和 fallback，并为旧字段提供迁移期兼容。
- 影响 `Assets/Scenes/Gameplay.unity`、`Assets/Scenes/DebugSandbox.unity` 的运行时场景结构：terrain 主层将从单一 world-aligned tilemap 迁移到多个 half-cell offset tilemap。
- 影响 `Assets/Scripts/Tests/EditMode` 与 `Assets/Scripts/Tests/PlayMode`：需要新增 resolver 单测、display cell 刷新回归，并重写现有 terrain 贴图断言方式。
- 影响 `Assets/Art/Minebot`：需要规划 floor / 四档硬度 / boundary 的 dual-grid atlas 生产与导入。
- 不影响 `LogicalGridState`、`MapDefinition`、`GridCharacterCollisionWorld`、地震危险区真相和其它规则服务的数据权威。
