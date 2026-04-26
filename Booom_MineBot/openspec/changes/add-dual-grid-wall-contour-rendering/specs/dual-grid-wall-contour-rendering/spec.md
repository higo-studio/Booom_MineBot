## ADDED Requirements

### Requirement: 可挖岩壁必须支持 dual-grid 轮廓渲染
项目 SHALL 为 `TerrainKind.MineableWall` 提供独立的 dual-grid 墙体轮廓渲染层。该图层 SHALL 基于逻辑网格推导半格偏移的轮廓形状，以更自然地表达空地与矿壁的边界。

#### Scenario: 进入主玩法场景
- **WHEN** `Gameplay` 或 `DebugSandbox` 完成初始化并加载默认地图
- **THEN** 场景中除了 world-aligned 基础地形层外，还会存在一个 dual-grid 墙体轮廓层用于显示可挖岩壁的边缘

#### Scenario: 地图存在连续可挖岩壁
- **WHEN** 若干相邻格为 `MineableWall`
- **THEN** 墙体边缘会以 dual-grid 轮廓显示，而不是只由逐格硬方块边界组成

#### Scenario: 不同硬度岩壁直接相邻
- **WHEN** 两片相邻的 `MineableWall` 分别属于不同 `HardnessTier`
- **THEN** 它们的交界也会显示 contour，帮助玩家直接读出材质分区；同硬度内部连接处则不会重复描边

### Requirement: 单个逻辑格变化必须同步更新相邻 dual-grid 轮廓
当 `MineableWall` 与非 `MineableWall` 状态在某个 world cell 上发生切换，或某个 `MineableWall` 的 `HardnessTier` 发生变化时，系统 SHALL 刷新受影响的 dual-grid 轮廓区域，使周边边界立即与最新逻辑状态一致。

#### Scenario: 挖开一格岩壁
- **WHEN** 玩家成功将一格 `MineableWall` 挖为空地
- **THEN** 该格周围的 dual-grid 轮廓会同步变化，不会保留旧的墙体轮廓残影

#### Scenario: 调试中把空地改回岩壁
- **WHEN** 开发者或测试将某个空地格改回 `MineableWall`
- **THEN** 与该格相邻的 dual-grid 轮廓会重新生成，反映新的矿壁边界

#### Scenario: 调试中修改岩壁硬度
- **WHEN** 开发者或测试把某个 `MineableWall` 从一种 `HardnessTier` 改成另一种
- **THEN** 与该格相邻的 dual-grid 轮廓会重新生成，反映新的材质分区边界

### Requirement: dual-grid 轮廓不得改变玩法真相或碰撞判定
dual-grid 墙体轮廓 SHALL 只用于表现层。玩家移动、碰撞、接触判定、挖掘可达性和其它玩法规则 MUST 继续以 world-grid 的 `LogicalGridState` 为准。

#### Scenario: 玩家靠近圆角墙体
- **WHEN** 墙体视觉轮廓表现为圆角或平滑边缘
- **THEN** 玩家的阻挡与接触仍按逻辑格边界结算，而不是按视觉轮廓几何结算

#### Scenario: 只替换轮廓 atlas 资源
- **WHEN** 开发者替换 dual-grid 墙体轮廓资源但不改逻辑数据
- **THEN** 挖掘、移动、探测、标记和机器人规则结果不会发生变化
