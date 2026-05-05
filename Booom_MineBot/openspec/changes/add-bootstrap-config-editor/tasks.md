## 1. 配置资产托管底座

- [x] 1.1 新增 Editor 侧配置资产托管工具，负责定位或创建默认 `BootstrapConfig`。
- [x] 1.2 为 `BootstrapConfig` 的必需子配置补自动回填逻辑：`inputActions`、`GameBalanceConfig`、`UpgradePoolConfig`、`HazardRules`、`MiningRules`、`WaveConfig`。
- [x] 1.3 为 `BuildingDefinition[]` 补目录扫描与自动同步逻辑，并支持缺失时自动创建默认建筑资产。
- [x] 1.4 提供 authored `MapDefinition` 的按需创建并引用入口，但不改变“空引用走程序地图”的默认行为。

## 2. UIToolkit 配置编辑器

- [x] 2.1 新增 `UIToolkit` 配置编辑器窗口，并加入 `Minebot` 菜单入口。
- [x] 2.2 将配置列表按启动与场景、地图、成长与经济、挖掘与风险、波次、建筑等大类组织。
- [x] 2.3 在每个大类中展示对应资产的可编辑内容，并提供定位资源、补齐引用、新增建筑配置等操作入口。

## 3. 验证

- [x] 3.1 为自动托管逻辑补 EditMode 测试，覆盖必需资产自动创建/回填。
- [x] 3.2 为地图按需创建与建筑配置同步补 EditMode 测试。
- [x] 3.3 使用 UnityMCP 执行 `unity.compile`，并运行本次相关 EditMode 测试。
- [x] 3.4 运行 `openspec validate add-bootstrap-config-editor`，确认 proposal、design、specs 和 tasks 结构完整。
