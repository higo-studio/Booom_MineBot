## Why

当前 `BootstrapConfig` 只是一个聚合入口，但它下面的 `GameBalanceConfig`、`UpgradePoolConfig`、`HazardRules`、`MiningRules`、`WaveConfig` 和 `BuildingDefinition[]` 仍然依赖手动创建资产、手动拖引用。随着配置量增长，这个流程既慢，也容易出现空引用或错引用。

用户希望把这套流程收拢成一个明确的配置编辑器，并满足两个目标：

- 配置列表按“大项配置分类”组织，而不是直接暴露一堆零散 SO 引用。
- 对于应当存在的配置 SO，如果缺失，编辑器自动创建并回填引用，不再要求手动拖拽。

## What Changes

- 新增一个基于 `UIToolkit` 的 `Minebot` 配置编辑器窗口，按启动、地图、成长/经济、挖掘/风险、波次、建筑等大类组织配置内容。
- 将 `BootstrapConfig` 继续作为聚合根，但由编辑器自动托管其必需子资产与建筑定义数组引用。
- 在编辑器侧补一个配置资产托管工具，负责：
  - 自动定位或创建默认 `BootstrapConfig`
  - 自动定位或创建必需的 ScriptableObject 配置资产并写回引用
  - 自动同步 `BuildingDefinition` 列表
  - 在用户需要时一键创建并引用 `MapDefinition`
- 为这套编辑器托管逻辑补充 EditMode 测试。

## Capabilities

### New Capabilities
- `project-foundation`: 项目提供一个统一的 `UIToolkit` 配置编辑器，用于维护 `BootstrapConfig` 及其下游配置资产。

### Modified Capabilities
- `project-foundation`: 默认配置资产的维护方式从“手动创建 + 手动拖引用”调整为“编辑器自动托管 + 分类编辑”。

## Impact

- 影响范围主要在 `Assets/Scripts/Editor`、`Assets/Scripts/Tests/EditMode` 和 `project-foundation` 对应的 OpenSpec 能力说明。
- 运行时规则与数据结构不改变，改的是编辑期配置工作流。
- 现有 `Assets/Settings/Gameplay` 目录下的默认资产会继续兼容，编辑器优先复用已有资产，而不是盲目生成重复文件。
