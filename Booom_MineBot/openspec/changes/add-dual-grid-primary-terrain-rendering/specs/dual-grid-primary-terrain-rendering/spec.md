## ADDED Requirements

### Requirement: terrain 主显示必须由 half-cell offset dual-grid 生成
项目 SHALL 使用 half-cell offset 的 display grid 作为 terrain 的唯一主显示层。每个 display cell MUST 从 2x2 world-grid 材质样本生成，而不是继续直接把 world cell 一对一绘制为 terrain tile。

#### Scenario: 进入主玩法场景
- **WHEN** `Gameplay` 或 `DebugSandbox` 初始化默认地图
- **THEN** 场景中的 terrain 主层来自 half-cell offset dual-grid display layers，而不是 world-aligned 的单一 terrain tilemap

#### Scenario: display grid 的坐标约定
- **WHEN** terrain display layers 被装配到 `Grid Root`
- **THEN** 它们会使用 `(-0.5, -0.5, 0)` 的半格偏移，以与 2x2 world-grid 采样结果对齐

### Requirement: terrain 显示层必须使用固定命名和稳定叠放顺序
dual-grid terrain 主显示 SHALL 由一组具名 display Tilemap 组成，并 MUST 使用稳定的 layer order。当前顺序包括 `DG Floor Tilemap`、`DG Wall Tilemap` 和 `DG Boundary Tilemap`。

#### Scenario: 初始化场景图层
- **WHEN** `Gameplay` 或 `DebugSandbox` 完成 terrain scene graph 装配
- **THEN** `Grid Root` 下会存在上述 3 个具名 terrain display Tilemap，而不是只存在一个笼统的 terrain 主层

#### Scenario: wall layer 固定在中间层
- **WHEN** 场景完成 terrain display layer 装配
- **THEN** `DG Wall Tilemap` 会稳定处于 `Floor` 与 `Boundary` 之间，使显示结果和测试断言具有确定性

### Requirement: terrain 主显示必须覆盖 floor、不同硬度岩体和 boundary
dual-grid terrain renderer SHALL 覆盖 `Floor`、`Soil`、`Stone`、`HardRock`、`UltraHard` 和 `Boundary` 六类材质，使玩家仍能从画面中区分空地、不同硬度可挖岩体和不可破坏边界。

#### Scenario: 显示默认地图
- **WHEN** 地图包含空地、不同硬度岩壁和不可破坏边界
- **THEN** dual-grid terrain 主层会同时表达这些材质，而不是只给墙体补 contour

#### Scenario: floor 也进入 dual-grid 主显示
- **WHEN** 一段空地与岩体或边界接壤
- **THEN** 玩家看到的 floor 形状同样来自 dual-grid family，而不是从 world-grid floor tile 单独直绘

### Requirement: wall display layer 必须在单层内表达多种岩体
系统 SHALL 通过稳定的 resolver 接口从四角材质样本生成 terrain 渲染命令。当前版本 MUST 支持单一 `Wall` display layer 内按 display cell 选择 `Soil / Stone / HardRock / UltraHard` family；同时 MUST 保留未来切换到 exact multistate resolver 的接口边界，而不要求上层调用者重写采样与刷新逻辑。

#### Scenario: mixed-material display cell
- **WHEN** 一个 display cell 的四角同时包含多种 wall material
- **THEN** 当前 resolver 会为 `DG Wall Tilemap` 输出单条 wall 命令，并为该 cell 选择一个确定的 wall family tile，而不是拆成多个岩体层叠加

#### Scenario: 后续切换 exact multistate 方案
- **WHEN** 后续版本引入 exact multistate dual-grid resolver
- **THEN** 上层 world-sample 输入和 display-cell 刷新语义保持不变，只替换 resolver 实现

### Requirement: terrain resolver 输出必须稳定且可测试
对于同一个 `CornerMaterialSample`，resolver MUST 产生稳定顺序的 terrain layer commands。当前方案中，命令顺序 SHALL 与 display layer order 保持一致，以便渲染装配和测试断言不依赖偶然顺序。

#### Scenario: 同一输入重复求值
- **WHEN** 同一个 `CornerMaterialSample` 被多次送入 resolver
- **THEN** 输出的 display layer commands 顺序和内容保持一致

#### Scenario: mixed-material wall sample 输出单个 wall 命令
- **WHEN** 一个 sample 同时包含 soil、stone 等不同 wall material
- **THEN** resolver 会在 wall slot 中输出单个稳定命令，而不是让调用方自行猜测写入顺序或再叠 contour/detail layer

### Requirement: 相邻岩体之间不得产生额外 contour split
当相邻的四角样本都属于可挖岩体时，dual-grid contour MUST 只由 `isWall` 决定，而不是因为 `Soil / Stone / HardRock / UltraHard` 的差异额外拆出内部边界。

#### Scenario: soil 与 stone 共享同一墙体块
- **WHEN** 一个 display cell 的四角都属于可挖岩体，但包含 soil 和 stone 两种硬度
- **THEN** `DG Wall Tilemap` 在该格会显示单个 wall tile，atlas index 仍按 full wall mass 计算，而不会同时出现 soil/stone 两条内部 contour

#### Scenario: wall 与 floor 接壤
- **WHEN** 一个 display cell 的四角同时包含 wall 和 floor
- **THEN** `DG Wall Tilemap` 会直接渲染该格的 wall edge，而不是依赖额外的 contour overlay layer

### Requirement: 单个 world cell 的材质变化必须只刷新周围 4 个 display cells
当一个 world cell 的 terrain material 发生变化时，dual-grid terrain renderer SHALL 只刷新受影响的 4 个相邻 display cells，而不是回退到整图 brute-force 重算。

#### Scenario: 挖开一格岩体
- **WHEN** 玩家把一个 `MineableWall` 挖成 `Empty`
- **THEN** 只有与该格相邻的 4 个 display cells 会被重算，并立即反映新的 floor / wall 交界

#### Scenario: 调试中修改岩体硬度
- **WHEN** 开发者把一个 `MineableWall` 从一种 `HardnessTier` 调整为另一种
- **THEN** 只有围绕该格的 4 个 display cells 会更新对应的 wall family 选择结果

### Requirement: dual-grid terrain 主显示不得改变玩法真相或碰撞判定
dual-grid terrain renderer SHALL 只负责表现层。玩家碰撞、移动、挖掘、AI、危险区、建造占位和任何玩法规则 MUST 继续以 `LogicalGridState` 为准，而不是按 dual-grid 显示几何结算。

#### Scenario: 玩家靠近平滑地形外观
- **WHEN** dual-grid terrain 把空地与岩体显示为较平滑的边界
- **THEN** 玩家阻挡与接触仍按 world-grid 逻辑格边界结算

#### Scenario: 替换 dual-grid atlas 资源
- **WHEN** 开发者替换某个 terrain family 的 dual-grid atlas，但不改变 world-grid 真相
- **THEN** 玩家移动、挖掘、危险区和建造判定不会因显示资源替换而改变
