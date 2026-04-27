## ADDED Requirements

### Requirement: 编辑器和运行时必须复用同一套 dual-grid 渲染核心
系统 SHALL 通过共享的 dual-grid 渲染核心，从统一的材质采样、resolver 和 layer command 规则生成 terrain family 显示结果。`Gameplay` / `DebugSandbox` 的运行时渲染与 Edit Mode 预览 MUST 使用同一套 family order、display offset 和 atlas 选择语义，而不是维护两套彼此独立的 dual-grid 实现。

#### Scenario: 运行时刷新 dual-grid terrain
- **WHEN** `Gameplay` 或 `DebugSandbox` 刷新 terrain 主显示
- **THEN** 系统会通过共享 dual-grid 渲染核心生成 `DG Floor` 到 `DG Boundary` 的 family layer 结果，而不是在 runtime 组件内维护一条 preview 不可复用的专有路径

#### Scenario: 编辑器刷新 dual-grid 预览
- **WHEN** 开发者在 Edit Mode 触发 dual-grid 预览刷新
- **THEN** 编辑器工具会调用与运行时相同的 dual-grid 渲染核心和 layer 规则，而不是维护一套单独的 editor-only lookup 逻辑

### Requirement: 当前所有已知 dual-grid 配置必须可迁移到统一 authoring 资产
系统 SHALL 提供一个统一的 dual-grid authoring/config 资产，用来承接当前所有已知的 dual-grid 相关配置，包括 terrain family atlas、layer 命名与顺序、display offset、fallback 所需参数，以及仍需保留来源记录或兼容读取的 legacy dual-grid 配置。工具 MUST 支持把默认 art set 和现有生成资源中的这些已知配置迁移到新结构，同时保留兼容路径，避免出现空引用黑屏。

#### Scenario: 迁移默认 dual-grid 配置
- **WHEN** 开发者对默认 `MinebotPresentationArtSet` 或对应默认资源执行 dual-grid 配置迁移
- **THEN** 系统会创建或更新统一的 dual-grid 配置资产，并把当前已知 dual-grid 引用映射到新结构，而不是要求手工逐字段重填

#### Scenario: 迁移后存在缺失字段
- **WHEN** 新配置资产中的某个 dual-grid 字段缺失、为空或尚未迁移
- **THEN** 运行时和编辑器预览仍会通过 legacy 字段或程序化 fallback 保持可显示结果，而不是直接出现空白 terrain layer

### Requirement: Dual Grid 工具必须支持 Edit Mode 预览与刷新
系统 SHALL 允许开发者在不进入 Play Mode 的情况下，基于当前 authoring tilemap、对应的 bake profile 和 dual-grid 配置，生成并刷新 dual-grid terrain family 预览。该预览 MUST 使用与运行时一致的 Tilemap 命名、sorting order 和 `(-0.5, -0.5, 0)` 半格偏移约定。

#### Scenario: 在编辑器中创建或刷新 dual-grid family 预览
- **WHEN** 开发者在场景中配置 dual-grid preview tool 并执行一次刷新
- **THEN** 场景会存在 `DG Floor Tilemap`、`DG Soil Tilemap`、`DG Stone Tilemap`、`DG HardRock Tilemap`、`DG UltraHard Tilemap` 和 `DG Boundary Tilemap`，并显示当前 authoring 地形对应的 dual-grid 结果

#### Scenario: 编辑单个地形格后重新预览
- **WHEN** 开发者修改一个 source terrain cell 并再次触发 dual-grid 预览刷新
- **THEN** 预览结果会更新该格对应的 dual-grid 显示，并保持与运行时相同的局部刷新语义，而不要求先进入 Play Mode 才能验证效果

### Requirement: 编辑器预览必须保持表现层属性，不得取代 bake 或运行时真相
Edit Mode 下的 dual-grid 预览 SHALL 只生成表现层结果。工具 MUST NOT 在预览刷新时隐式改写 `MapDefinition`、运行时服务或其它玩法真相数据；只有开发者显式执行 bake 或保存迁移资产时，才允许更新对应的 authoring 输出。

#### Scenario: 仅刷新 dual-grid 预览
- **WHEN** 开发者在 Edit Mode 只执行 dual-grid preview refresh
- **THEN** 系统只更新 preview tilemap 或其目标表现层，不会隐式重写 `MapDefinition`、初始化 runtime services 或改变玩法规则状态

#### Scenario: 校验 dual-grid 预览配置
- **WHEN** dual-grid 工具发现 source tilemap、bake profile、target layer 或配置资产存在缺失
- **THEN** 工具会给出可审查的校验结果，并阻止错误配置静默产出不可信的预览
