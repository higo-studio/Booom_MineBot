## ADDED Requirements

### Requirement: 项目必须提供统一的 Bootstrap 配置编辑器
项目 SHALL 提供一个基于 `UIToolkit` 的配置编辑器，用于维护 `BootstrapConfig` 及其主配置资产。编辑器的配置列表 SHALL 按玩法大类组织，而不是直接暴露零散的 SO 引用字段。

#### Scenario: 浏览主配置分类
- **WHEN** 开发者打开 Minebot 配置编辑器
- **THEN** 编辑器会按启动与场景、地图、成长与经济、挖掘与风险、波次、建筑等大类展示配置入口

#### Scenario: 在分类下编辑现有配置
- **WHEN** 开发者切换到某个配置大类
- **THEN** 编辑器会展示该分类对应的 `BootstrapConfig` 字段或下游 ScriptableObject 配置内容，并允许直接修改

### Requirement: 必需的配置资产引用必须支持自动创建与回填
对于 `BootstrapConfig` 运行时主链路依赖的必需配置资产，项目 SHALL 在编辑器内支持自动定位、自动创建和自动回填引用，而不是要求开发者手动拖拽 SO 文件。

#### Scenario: 打开编辑器时发现主配置引用缺失
- **WHEN** 开发者打开配置编辑器，且 `BootstrapConfig` 缺少必需子配置引用
- **THEN** 编辑器会优先复用同目录已有资产；若仍不存在，则自动创建对应资产并回填引用

#### Scenario: authored 地图配置按需创建
- **WHEN** 开发者在地图分类里明确请求创建或引用 `MapDefinition`
- **THEN** 编辑器会自动创建或绑定地图资产，并写回 `BootstrapConfig`

### Requirement: 建筑定义列表必须支持自动同步
项目 SHALL 支持从 `BootstrapConfig` 所在目录树自动收集 `BuildingDefinition` 资产，并同步回 `buildingDefinitions` 列表，避免依赖手动维护数组引用。

#### Scenario: 新增建筑配置
- **WHEN** 开发者在配置编辑器中新增一份建筑定义
- **THEN** 编辑器会创建新的 `BuildingDefinition` 资产，并自动将其同步到 `BootstrapConfig.buildingDefinitions`

#### Scenario: 已有建筑资产尚未进入引用数组
- **WHEN** `BootstrapConfig` 目录树下已经存在有效的 `BuildingDefinition` 资产，但数组中尚未引用
- **THEN** 编辑器会在同步时自动把这些资产加入 `buildingDefinitions`
