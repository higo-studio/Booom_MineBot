## ADDED Requirements

### Requirement: dual-grid 主地形资源必须按 material family 维护 `16-state` atlas
项目 SHALL 为 dual-grid terrain 主渲染提供按 material family 组织的 `16-state` atlas。当前至少包括 `Floor`、`Soil`、`Stone`、`HardRock`、`UltraHard` 和 `Boundary` 六个 family，并继续保留可追溯的资源目录、导入设置和生成记录。

#### Scenario: 新增一批 dual-grid terrain 资源
- **WHEN** 开发者或 Agent 为 Minebot 生成新的 dual-grid terrain 资源
- **THEN** 资源会按 family 和 `16-state` atlas 组织，而不是只产出单格墙体 tile 或单一 contour family

#### Scenario: 替换某个 terrain family
- **WHEN** 美术或开发者只替换 `Stone` 或 `Boundary` 等某个 dual-grid family atlas
- **THEN** 可以通过更新对应 PNG / Tile / art set 配置完成替换，而不需要修改玩法规则代码

### Requirement: dual-grid terrain family 资源必须遵守统一命名
dual-grid terrain family 的 PNG、Tile 和 art set 字段命名 SHALL 反映 family 与 `16-state` index，避免 floor / hardness / boundary atlas 在实现和测试中混名。

#### Scenario: 新增 floor family atlas
- **WHEN** 项目导入一组新的 floor dual-grid 资源
- **THEN** PNG、Tile 和 art set 字段会带有 `DG Floor` 或等价 family 前缀，并可明确对应 `00-15` 的 dual-grid index

#### Scenario: 审查某个 Tile 的来源
- **WHEN** 开发者查看一个 dual-grid terrain Tile
- **THEN** 可以从其名称直接读出它属于哪个 family、对应哪个 atlas index，而不需要再反查临时文档
