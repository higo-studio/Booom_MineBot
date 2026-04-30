## Why

当前 Minebot 已经具备 dual-grid terrain 主显示和 `IsRevealed` 运行时状态，但未揭示岩壁仍然直接完整显示，导致“已挖开空腔 vs 更深未开采区域”的读感过于平铺。现在需要把未揭示的非空地格渲染成一层独立的 dual-grid 迷雾，使玩家能稳定读出前沿破口、一格宽的亮边带，以及两格外直接变成全黑的更深未知区，同时不把需求扩张成完整视野系统。

## What Changes

- 新增基于 `!cell.IsRevealed && cell.TerrainKind != Empty` 的双带 fog 分类：前沿 1 格亮边带 + 两格外全黑深层带，并继续使用 dual-grid 渲染。
- 在 `Gameplay` / `DebugSandbox` 运行时场景中新增独立 `DG Fog Near Tilemap` / `DG Fog Deep Tilemap`，沿用现有 half-cell offset，但允许单格 reveal 连带刷新周围一圈 fog band。
- 为默认美术配置与程序化 fallback 增加 near/deep 两套 fog dual-grid Tile 资源，并接入默认 art set / asset pipeline。
- 更新 minimap 表现，使未揭示非空地格不会继续泄露完整地形信息。
- 补充 EditMode / PlayMode 回归，覆盖挖掘、爆炸、塌方和带宽分层刷新时的 fog 结果。

## Capabilities

### New Capabilities
- `dual-grid-fog-shroud`: 基于 `IsRevealed` 的 dual-grid 亮边带 / 深层迷雾、默认资源接入、minimap 同步与运行时刷新。

### Modified Capabilities
- 无

## Impact

- 受影响代码主要在 `Assets/Scripts/Runtime/Presentation`、`Assets/Scripts/Editor/MinebotPixelArtAssetPipeline.cs`、`Assets/Scripts/Runtime/GridMining` 现有 reveal 消费路径，以及 `Assets/Scripts/Tests/EditMode` / `PlayMode`。
- 不新增外部依赖，不改变 `MapDefinition`、`LogicalGridState` 的权威边界，不引入完整视野/FOV 系统。
