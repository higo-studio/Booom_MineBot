## MODIFIED Requirements

### Requirement: 地震波必须沿空地与可挖岩壁的边沿形成危险带
游戏 SHALL 将地震波危险区评估为当前可站立空地中、与 `TerrainKind.MineableWall` 以 4 邻接接壤并按当前波次向内扩张的带状区域。系统 MUST NOT 再把若干 danger origin 的曼哈顿半径整片空地直接视为最终危险区。

#### Scenario: 贴墙单格通道在早期波次变为危险
- **WHEN** 一段空地通道直接贴着 `MineableWall`，且其到最近岩壁边沿的距离不超过当前危险带厚度
- **THEN** 这些空地格会被标记为 `IsDangerZone`

#### Scenario: 对角仅接触岩壁的空地保持安全
- **WHEN** 一块空地只通过对角与 `MineableWall` 接壤，且没有任何 4 邻接岩壁
- **THEN** 该格不会仅因这份对角接触而被标记为 `IsDangerZone`

#### Scenario: 不属于出生主空地腔体的空岛会被危险区吞并
- **WHEN** 一片非危险空地不属于出生点所在的主空地腔体，只形成被危险带包围或被墙体完全隔开的孤立安全 pocket
- **THEN** 该空岛会被并回 `IsDangerZone`，而不是继续保留为独立的小安全孔洞

#### Scenario: 开阔空地中心仍保持安全
- **WHEN** 一块空地虽然位于矿洞内部，但与最近 `MineableWall` 的距离大于当前危险带厚度
- **THEN** 该格不会被标记为 `IsDangerZone`

### Requirement: 危险区真相必须随地形边界与波次同步刷新
当地图中的 `MineableWall` 被挖开、爆炸清除，或当前波次导致危险带厚度增长时，系统 SHALL 重新评估受影响区域的 `IsDangerZone`，使危险边沿立即反映最新地图状态。

#### Scenario: 挖开一块岩壁后暴露新的危险边沿
- **WHEN** 一块 `MineableWall` 被挖开并变为 `TerrainKind.Empty`
- **THEN** 与剩余岩壁重新接壤的空地会按当前波次重新计算 `IsDangerZone`，而不是继续沿用旧的危险区结果

#### Scenario: 存活到更高波次后危险带增厚
- **WHEN** 单局进入更高波次阶段
- **THEN** 地震系统会扩大危险带厚度或至少维持当前覆盖，而不是回退到更弱的基线

### Requirement: 波次致死与自动避险必须继续读取统一的危险区真相
玩家失败判定、从属机器人损毁与自动避险 MUST 继续统一读取 `LogicalGridState.IsDangerZone`。新的边沿带规则 MUST 直接驱动这些结果，而不是另外维护一份与可见危险边界不同形的隐藏危险判定。

#### Scenario: 玩家在危险边沿带中遭遇地震结算
- **WHEN** 玩家在地震结算时处于一个属于边沿带的 `IsDangerZone` 格
- **THEN** 本局会立即以失败结束

#### Scenario: 从属机器人避开危险边沿带目标
- **WHEN** 一个可挖目标或其相邻落脚格处于新的边沿带 `IsDangerZone`
- **THEN** 自动模式会继续将该目标视为不安全并选择避开

### Requirement: 危险区不得把空地额外变成碰撞阻挡
`LogicalGridState.IsDangerZone` SHALL 只表达波次危险语义，不得把 `TerrainKind.Empty` 额外改成角色碰撞阻挡。玩家与其它角色仍可以进入危险空地；失败、损毁、避险和建造限制继续由危险区规则单独处理。

#### Scenario: 玩家穿过危险空地
- **WHEN** 一个空地格被标记为 `IsDangerZone`，但该格既不是墙体也没有建筑占用
- **THEN** 玩家仍可移动进入该格，而不会被当作地形碰撞阻挡
