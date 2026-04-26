## MODIFIED Requirements

### Requirement: 危险区必须只显示为 warning base tile
危险区反馈 SHALL 只围绕当前为 `TerrainKind.Empty` 且属于 `LogicalGridState.IsDangerZone` 的区域显示，并 MUST 只使用 world-aligned 的 warning base tile 作为主显示，而不是继续采用逐格内描边或额外的 danger contour。contour 视觉 MUST 保留给岩体本身。

#### Scenario: 贴墙危险带显示危险底图
- **WHEN** 一段空地通道位于当前波次的危险带中，且这些格子与 `MineableWall` 边界连成连续区域
- **THEN** 玩家看到的是危险格上的 warning base tile，而不是每个危险格各自绘制一圈内描边或额外的 danger contour

#### Scenario: 安全空地不显示危险反馈
- **WHEN** 一个空地格不属于 `IsDangerZone`
- **THEN** 该格不会显示 danger base，即使附近存在其它危险带边界

#### Scenario: 更高波次增强危险读感
- **WHEN** 同一段危险边界分别在较早波次和较晚波次被渲染
- **THEN** 较晚波次使用的 warning base 读感不会弱于较早波次

### Requirement: 标记、危险区、建造预览和扫描数字必须拥有独立渲染所有权
标记、危险区、建造预览和扫描数字 SHALL 分别由独立图层或节点根负责渲染。危险区切换为仅保留 `Danger Base` 后，系统刷新这些图层 MUST NOT 清空或替换 marker、build preview 或 scan indicator 的渲染结果，除非对应反馈本身的逻辑状态发生变化。

#### Scenario: 建造预览不会清掉 danger overlay
- **WHEN** 地图上已有 danger base，随后玩家进入建筑模式并刷新建造预览
- **THEN** 建造预览只会更新自己的预览层，不会清空 danger base

#### Scenario: 扫描数字不会清掉 danger overlay
- **WHEN** 地图上已有 danger base，随后玩家执行一次成功探测
- **THEN** 扫描数字只会更新自己的扫描指示器层，danger base 保持原样
