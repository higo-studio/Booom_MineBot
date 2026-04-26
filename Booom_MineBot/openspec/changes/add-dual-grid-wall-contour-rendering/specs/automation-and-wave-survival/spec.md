## MODIFIED Requirements

### Requirement: 地震波必须沿空地与可挖岩壁的边沿形成危险带
游戏 SHALL 将地震波危险区评估为当前可站立空地中、与 `TerrainKind.MineableWall` 接壤并按当前波次向内扩张的带状区域。系统 MUST NOT 再把若干 danger origin 的曼哈顿半径整片空地直接视为最终危险区。

#### Scenario: 贴墙单格通道在早期波次变为危险
- **WHEN** 一段空地通道直接贴着 `MineableWall`，且其到最近岩壁边沿的距离不超过当前危险带厚度
- **THEN** 这些空地格会被标记为 `IsDangerZone`

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
