## ADDED Requirements

### Requirement: 未揭示的非空地格必须以 dual-grid 亮边带与深层迷雾显示
系统 SHALL 基于 `!IsRevealed && TerrainKind != Empty` 生成 fog truth，并在 `Gameplay` 与 `DebugSandbox` 中把未揭示的非空地格拆成“前沿 1 格亮边带”和“两格外全黑深层带”两种 dual-grid 迷雾显示，而不是继续让整片未开采区域直接完整可见。

#### Scenario: 默认地图进入主玩法场景
- **WHEN** 玩家从 `Bootstrap` 进入 `Gameplay`
- **THEN** 已揭示空地保持可见，而未揭示的非空地格通过 near/deep 两层 dual-grid fog 显示

#### Scenario: 新挖开一格岩壁
- **WHEN** 玩家成功挖开一格此前未揭示的岩壁
- **THEN** 该格对应的 fog 会被清除，且周围一圈未揭示格会按“亮边带 / 深层带”重新分类并刷新受影响的 dual-grid display cells

### Requirement: 迷雾层必须显式保证一格亮边与两格外全黑
系统 SHALL 在运行时显式区分 `FogNear` 与 `FogDeep` 两类 mask，其中 `FogNear` 表示距离任一已揭示格 Chebyshev 距离不超过 1 的未揭示非空地格，`FogDeep` 表示其余未揭示非空地格；两类 mask MUST 使用独立 dual-grid tile style 渲染。

#### Scenario: 前沿墙与更深墙体同时存在
- **WHEN** 一块未揭示岩壁紧贴已揭示空地，而更深处还有连续未揭示岩壁
- **THEN** 紧贴破口的一圈 world-cell 显示为更亮的 near band，而再外侧的连续未揭示区域直接显示为更黑的 deep band

#### Scenario: 深层墙体被打开后前沿带前推一格
- **WHEN** 玩家或爆炸打开一格深层未揭示岩壁
- **THEN** 新暴露边界外侧的一圈 world-cell 会重新变成 near band，而更深区域仍保持 deep band

### Requirement: 迷雾不得破坏既有反馈层独立所有权
系统 SHALL 让 dual-grid fog tilemap 压在 terrain 之上，但 MUST NOT 清空、替换或遮蔽 marker、build preview、scan indicator 等独立反馈层的刷新所有权，除非对应反馈自身的逻辑状态发生变化。

#### Scenario: 地图上已有标记与迷雾
- **WHEN** 一格未揭示岩壁已被玩家标记，且 fog 正常渲染
- **THEN** 标记层仍保持可见，fog 刷新不会把标记层清空或替换成 terrain/fog 贴图

#### Scenario: 成功探测前沿岩壁
- **WHEN** 玩家执行一次成功探测，并在前沿岩壁上生成扫描数字
- **THEN** 扫描数字仍然显示在对应岩壁上方，fog 刷新不会吃掉扫描数字

### Requirement: minimap 必须与迷雾真相保持一致
HUD minimap SHALL 使用与主场景相同的 fog truth；对于 `!IsRevealed && TerrainKind != Empty` 的格子，HUD MUST 不再直接泄露其完整地形类别与硬度信息。

#### Scenario: 地图中存在未揭示岩层
- **WHEN** HUD minimap 刷新当前地图
- **THEN** 未揭示的非空地格会显示为 fog/未知色，而不是继续按 Soil、Stone、HardRock、UltraHard 或 Boundary 直接着色

#### Scenario: 地震塌方把空地回填成未揭示岩壁
- **WHEN** 地震波结算后一片已揭示空地塌回普通岩壁并重新变为未揭示
- **THEN** 主场景 fog 与 minimap 都会再次把这些格子显示为未知区域
