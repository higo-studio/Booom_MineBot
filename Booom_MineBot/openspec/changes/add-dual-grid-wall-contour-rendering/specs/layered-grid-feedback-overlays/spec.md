## MODIFIED Requirements

### Requirement: 危险区必须显示为 warning base tile + 连续 contour 边界，并保留波次增强读感
危险区反馈 SHALL 只围绕当前为 `TerrainKind.Empty` 且属于 `LogicalGridState.IsDangerZone` 的区域显示，并 MUST 以 world-aligned 的 warning base tile 配合连续 contour 边界作为主显示，而不是继续采用逐格内描边。danger contour 的视觉强调 MUST 随波次提升保持不减，可通过线宽、亮度、颜色档位或等价方式体现更高压力。

#### Scenario: 贴墙危险带显示连续边界
- **WHEN** 一段空地通道位于当前波次的危险带中，且这些格子与 `MineableWall` 边界连成连续区域
- **THEN** 玩家看到的是危险格上的 warning base tile，以及沿危险带外缘连续分布的 danger contour，而不是每个危险格各自绘制一圈内描边

#### Scenario: 安全空地不显示危险 contour
- **WHEN** 一个空地格不属于 `IsDangerZone`
- **THEN** 该格不会显示 danger base 或 danger contour，即使附近存在其它危险带边界

#### Scenario: 更高波次增强危险读感
- **WHEN** 同一段危险边界分别在较早波次和较晚波次被渲染
- **THEN** 较晚波次使用的 danger contour 视觉强调不会弱于较早波次

### Requirement: 标记、危险区、建造预览和扫描数字必须拥有独立渲染所有权
标记、危险区、建造预览和扫描数字 SHALL 分别由独立图层或节点根负责渲染。危险区切换为 `Danger Base` + `Danger Contour` 后，系统刷新这些图层 MUST NOT 清空或替换 marker、build preview 或 scan indicator 的渲染结果，除非对应反馈本身的逻辑状态发生变化。

#### Scenario: 建造预览不会清掉 danger overlay
- **WHEN** 地图上已有 danger base 与 danger contour，随后玩家进入建筑模式并刷新建造预览
- **THEN** 建造预览只会更新自己的预览层，不会清空 danger overlay

#### Scenario: 扫描数字不会清掉 danger overlay
- **WHEN** 地图上已有 danger base 与 danger contour，随后玩家执行一次成功探测
- **THEN** 扫描数字只会更新自己的扫描指示器层，danger overlay 保持原样
