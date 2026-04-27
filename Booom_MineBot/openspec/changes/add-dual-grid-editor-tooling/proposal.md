## Why

当前 Dual Grid terrain 已经能在运行时驱动 `Gameplay` / `DebugSandbox` 的主显示，但它仍然强耦合在 `TilemapGridPresentation`、`MinebotGameplayPresentation` 和默认 `MinebotPresentationArtSet` 里。结果是 dual-grid 的 family 配置、fallback、图层装配和刷新逻辑只能在 Play Mode 下间接验证，已知配置也分散在多个字段与生成脚本中，后续继续调 atlas、迁移资源或补 editor 工作流的成本都偏高。

现在需要把这套能力从“能跑的运行时实现”推进到“可持续维护的编辑工具链”：统一 dual-grid 配置入口，迁移当前所有已知 family / overlay / fallback 配置，抽象出可复用的渲染核心，并让关卡、美术和程序都能在 Editor Mode 下直接预览与校验结果，而不是每次都进入 Play Mode。

## What Changes

- 新增一个 Dual Grid 编辑工具，用于在 Edit Mode 下装配、预览、刷新和校验 dual-grid terrain family layers。
- 抽象当前 dual-grid 渲染流程，把 2x2 采样、resolver、layer command 和 Tilemap 写入职责从具体 gameplay runtime 组件中拆开，形成可同时被运行时和编辑器复用的渲染核心。
- 引入统一的 dual-grid authoring/config 资产，承接 terrain family atlas、图层命名与排序、offset、fallback 和相关渲染参数，避免继续把配置分散在 `MinebotPresentationArtSet`、生成脚本和场景装配代码里。
- 提供迁移路径，把当前所有已知 dual-grid 相关配置迁移到新配置结构，同时保留对现有默认资源和旧字段的兼容，避免一次性打断 `Gameplay`、`DebugSandbox` 和测试基线。
- 为编辑器预览补充刷新入口、脏区更新、配置校验和一致性测试，确保 Edit Mode 看到的 dual-grid 结果与运行时主渲染结果保持一致。

## Capabilities

### New Capabilities
- `dual-grid-editor-tooling`: 提供 Dual Grid 配置迁移、共享渲染抽象、Edit Mode 预览与校验工作流，使 dual-grid terrain 能以统一配置同时服务编辑器和运行时。

### Modified Capabilities
- 无

## Impact

- 影响 `Assets/Scripts/Runtime/Presentation`：需要重构 `TilemapGridPresentation`、`DualGridTerrain`、`MinebotPresentationAssets` 与 `MinebotGameplayPresentation` 的 dual-grid 组装边界。
- 影响 `Assets/Scripts/Editor`：需要新增或扩展 editor-only 的 dual-grid authoring / preview 工具，并与现有 `MinebotPixelArtAssetPipeline` 协同。
- 影响 `Assets/Resources/Minebot/MinebotPresentationArtSet_Default.asset` 及相关配置资产：需要补齐迁移与兼容路径。
- 影响 `Assets/Scripts/Tests/EditMode` 与部分 PlayMode 测试：需要新增配置迁移、编辑器预览一致性和共享渲染核心回归。
- 不改变 `LogicalGridState`、碰撞、挖掘、危险区和建筑占位等玩法真相；本 change 聚焦 dual-grid 的 authoring、渲染抽象与编辑器工作流。
