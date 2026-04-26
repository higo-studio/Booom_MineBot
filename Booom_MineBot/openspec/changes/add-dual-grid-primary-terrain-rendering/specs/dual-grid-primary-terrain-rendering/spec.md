## ADDED Requirements

### Requirement: terrain 主显示必须由 half-cell offset dual-grid 生成
项目 SHALL 使用 half-cell offset 的 display grid 作为 terrain 的唯一主显示层。每个 display cell MUST 从 2x2 world-grid 材质样本生成，而不是继续直接把 world cell 一对一绘制为 terrain tile。

#### Scenario: 进入主玩法场景
- **WHEN** `Gameplay` 或 `DebugSandbox` 初始化默认地图
- **THEN** 场景中的 terrain 主层来自 half-cell offset dual-grid families，而不是 world-aligned 的单一 terrain tilemap

#### Scenario: display grid 的坐标约定
- **WHEN** terrain display layers 被装配到 `Grid Root`
- **THEN** 它们会使用 `(-0.5, -0.5, 0)` 的半格偏移，以与 2x2 world-grid 采样结果对齐

### Requirement: terrain family layers 必须使用固定命名和稳定叠放顺序
dual-grid terrain 主显示 SHALL 由一组具名 family Tilemap 组成，并 MUST 使用稳定的 layer order。当前顺序至少包括 `DG Floor Tilemap`、`DG Soil Tilemap`、`DG Stone Tilemap`、`DG HardRock Tilemap`、`DG UltraHard Tilemap` 和 `DG Boundary Tilemap`。

#### Scenario: 初始化场景图层
- **WHEN** `Gameplay` 或 `DebugSandbox` 完成 terrain scene graph 装配
- **THEN** `Grid Root` 下会存在上述 6 个具名 terrain family Tilemap，而不是只存在一个笼统的 terrain 主层

#### Scenario: mixed-material display cell 叠放
- **WHEN** 同一个 display cell 需要同时绘制多个 material family
- **THEN** family layers 会按固定顺序叠放，使显示结果和测试断言具有确定性

### Requirement: terrain 主显示必须覆盖 floor、不同硬度岩体和 boundary
dual-grid terrain renderer SHALL 覆盖 `Floor`、`Soil`、`Stone`、`HardRock`、`UltraHard` 和 `Boundary` 六类材质，使玩家仍能从画面中区分空地、不同硬度可挖岩体和不可破坏边界。

#### Scenario: 显示默认地图
- **WHEN** 地图包含空地、不同硬度岩壁和不可破坏边界
- **THEN** dual-grid terrain 主层会同时表达这些材质，而不是只给墙体补 contour

#### Scenario: floor 也进入 dual-grid 主显示
- **WHEN** 一段空地与岩体或边界接壤
- **THEN** 玩家看到的 floor 形状同样来自 dual-grid family，而不是从 world-grid floor tile 单独直绘

### Requirement: terrain resolver 必须支持当前分层方案并保留 future exact multistate 接口
系统 SHALL 通过稳定的 resolver 接口从四角材质样本生成 terrain 渲染命令。当前版本 MUST 支持按 material family 分层的 `16-state` 方案；同时 MUST 保留未来切换到 exact multistate resolver 的接口边界，而不要求上层调用者重写采样与刷新逻辑。

#### Scenario: mixed-material display cell
- **WHEN** 一个 display cell 的四角同时包含 floor、soil 或 stone 等不同材质
- **THEN** 当前 resolver 会输出多个 family layer 命令，而不是要求项目立即提供一个精确的全状态 atlas

#### Scenario: 后续切换 exact multistate 方案
- **WHEN** 后续版本引入 exact multistate dual-grid resolver
- **THEN** 上层 world-sample 输入和 display-cell 刷新语义保持不变，只替换 resolver 实现

### Requirement: terrain resolver 输出必须稳定且可测试
对于同一个 `CornerMaterialSample`，resolver MUST 产生稳定顺序的 terrain layer commands。当前 layered 方案中，命令顺序 SHALL 与 terrain family order 保持一致，以便渲染装配和测试断言不依赖偶然顺序。

#### Scenario: 同一输入重复求值
- **WHEN** 同一个 `CornerMaterialSample` 被多次送入 resolver
- **THEN** 输出的 family layer commands 顺序和内容保持一致

#### Scenario: mixed-material sample 输出多层命令
- **WHEN** 一个 sample 同时包含 floor、soil 和 stone 等不同材质
- **THEN** resolver 会按稳定的 family order 输出多条命令，而不是让调用方自行猜测写入顺序

### Requirement: 单个 world cell 的材质变化必须只刷新周围 4 个 display cells
当一个 world cell 的 terrain material 发生变化时，dual-grid terrain renderer SHALL 只刷新受影响的 4 个相邻 display cells，而不是回退到整图 brute-force 重算。

#### Scenario: 挖开一格岩体
- **WHEN** 玩家把一个 `MineableWall` 挖成 `Empty`
- **THEN** 只有与该格相邻的 4 个 display cells 会被重算，并立即反映新的 floor / wall 交界

#### Scenario: 调试中修改岩体硬度
- **WHEN** 开发者把一个 `MineableWall` 从一种 `HardnessTier` 调整为另一种
- **THEN** 只有围绕该格的 4 个 display cells 会更新对应的材质 family 叠加结果

### Requirement: dual-grid terrain 主显示不得改变玩法真相或碰撞判定
dual-grid terrain renderer SHALL 只负责表现层。玩家碰撞、移动、挖掘、AI、危险区、建造占位和任何玩法规则 MUST 继续以 `LogicalGridState` 为准，而不是按 dual-grid 显示几何结算。

#### Scenario: 玩家靠近平滑地形外观
- **WHEN** dual-grid terrain 把空地与岩体显示为较平滑的边界
- **THEN** 玩家阻挡与接触仍按 world-grid 逻辑格边界结算

#### Scenario: 替换 dual-grid atlas 资源
- **WHEN** 开发者替换某个 terrain family 的 dual-grid atlas，但不改变 world-grid 真相
- **THEN** 玩家移动、挖掘、危险区和建造判定不会因显示资源替换而改变
