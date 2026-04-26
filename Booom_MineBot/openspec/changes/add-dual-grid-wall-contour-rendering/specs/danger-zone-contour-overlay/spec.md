## ADDED Requirements

### Requirement: 危险区必须以边沿带真相驱动独立的 contour overlay 渲染
项目 SHALL 先将危险区定义为“空地一侧、与 `MineableWall` 接壤并按当前波次向内扩张的边沿带”，再为这份 `IsDangerZone` 区域提供独立的 half-cell contour overlay。该图层 SHALL 以连续危险边界为主显示，而不是继续使用“每个危险格一个框”的逐格 overlay 语言。

#### Scenario: 进入主玩法场景
- **WHEN** `Gameplay` 或 `DebugSandbox` 完成初始化并存在危险区
- **THEN** 场景中会存在独立的 `Danger Contour Tilemap`（或等价图层）用于显示危险边界

#### Scenario: 贴墙通道落入危险边沿带
- **WHEN** 一条空地通道紧邻 `MineableWall`，且这些空地格位于当前波次的危险带厚度内
- **THEN** 玩家看到的是沿岩壁连续分布的 danger contour，而不是把整片开阔空地都视为危险区

#### Scenario: 开阔空地中心远离岩壁
- **WHEN** 某个空地格与最近 `MineableWall` 的距离超过当前危险带厚度
- **THEN** 该格不会进入 `IsDangerZone`，也不会显示 danger contour

### Requirement: 危险区 contour 必须随边沿带真相的变化同步刷新
当 `IsDangerZone` 因波次变化或局部岩壁边界变化而发生更新时，系统 SHALL 更新受影响的 danger contour，使其立即反映最新的危险边界。

#### Scenario: 下一波危险带增厚
- **WHEN** `EvaluateDangerZones(...)` 根据新的波次厚度重算 `IsDangerZone`
- **THEN** danger contour 会同步变化，与新的危险边界保持一致

#### Scenario: 某段岩壁被清空后危险边沿后退
- **WHEN** 一组与岩壁接壤的空地因挖掘或爆炸导致边界关系变化，从危险区恢复为安全区
- **THEN** 该区域对应的 danger contour 会被移除或后退，不会保留旧边界残影

### Requirement: 危险区 contour 必须与墙体 contour 和建造预览保持语义独立
danger contour SHALL 可以与墙体 contour 共享 2x2 mask -> contour index 的解析框架，但 MUST 使用独立的资源族、独立图层职责和独立视觉语义。非法建造预览 MUST NOT 再通过 danger contour 或逐格 danger tile 冒充危险区。

#### Scenario: 危险边界靠近可挖岩壁
- **WHEN** `DangerZone` 与 `MineableWall` 在空间上相邻
- **THEN** 玩家可以同时区分墙体 contour 和危险区 contour，而不会把二者误读为同一种边界

#### Scenario: 非法建造预览与危险区同时出现
- **WHEN** 玩家处于 Build 模式并查看一个非法建造位置，同时附近存在 `DangerZone`
- **THEN** 非法建造预览与危险区边界仍使用不同的视觉语言，不会共用同一 danger 资源语义

### Requirement: 危险区 contour 必须与危险区玩法真相保持同形
danger contour SHALL 忠实反映边沿带规则生成的 `LogicalGridState.IsDangerZone`。玩家死亡判定、机器人损毁、建造限制和任何其它危险区相关规则 MUST 与玩家可见 danger contour 指向同一批危险格，而不是分别使用不同形状。

#### Scenario: 玩家站在可见危险边沿上
- **WHEN** 玩家在地震结算时站在一个显示了 danger contour 的空地格上
- **THEN** 该格会按同一份 `IsDangerZone` 真相参与失败结算

#### Scenario: 建筑预览落在可见危险边沿上
- **WHEN** 玩家进入 Build 模式，并将建筑预览放在一个显示了 danger contour 的空地格上
- **THEN** 该位置会被视为危险区阻挡，而不是出现“看得见红边但仍可建造”的分叉
