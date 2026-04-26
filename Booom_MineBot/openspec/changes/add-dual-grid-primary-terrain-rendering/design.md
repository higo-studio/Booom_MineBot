## Context

当前 Minebot 的 terrain 表现路径仍然是：

```text
LogicalGridState
  -> world-grid Terrain Tilemap
  -> half-cell Wall Contour Tilemaps
  -> independent overlays
```

这条路径已经验证了两件事：

- world-grid 继续担任玩法真相是正确的
- dual-grid 的 half-cell offset + 2x2 lookup 非常适合地形外观生成

但它仍然保留了一个核心限制：terrain 主显示还在 world grid 上。  
用户现在明确要的是另一种结构：

```text
LogicalGridState / World Grid
= 碰撞 / 挖掘 / AI / 危险区 / 建筑占位 / 测试真相
= 不直接渲染 terrain

Offset Display Grid (-0.5, -0.5)
= 唯一 terrain 主显示
= floor / hardness / boundary 全部进入
= 每个 display cell 读取 2x2 world cells
```

同时，用户已经把多材质问题的边界定死了：

1. floor 也必须进入 dual-grid 主显示  
2. 当前版本接受“分层叠加”，但必须保留 future exact multistate 的接口  
3. `Indestructible` 也进入这套 dual-grid terrain family

这意味着“16 种”不能再被理解为整个 terrain 的全局状态，而只能理解为：

```text
每个 material family 各自维持 16-state
最终画面 = 多个 family layer 的叠加
```

因为如果直接把四个角都当成多值材质状态：

```text
corner state = {Floor, Soil, Stone, HardRock, UltraHard, Boundary}
总组合数 = 6^4 = 1296
```

这不适合当前 Minebot 的 MVP 复杂度。  
因此本 design 选择：

- 当前落 `LayeredBinaryResolver`
- 保留 `ExactMultistateResolver` 接口
- 不让上层调用者依赖“当前一定是分层 16-state”

## Goals / Non-Goals

**Goals**

- world grid 保持唯一玩法真相，但不再直接承担 terrain 主渲染。
- terrain 主显示完全迁移到 half-cell offset dual-grid families。
- floor、四档可挖岩体和 `Indestructible` 都纳入 dual-grid 主显示。
- 以可替换 resolver 接口承接“当前 layered binary / 未来 exact multistate”两种路线。
- 保留当前独立 overlays：danger、marker、build preview、scan、facility、actors。
- 把刷新语义收口为“1 个 world cell 变化 -> 周围 4 个 display cells 脏掉”。
- 让资源、fallback、测试和场景装配都围绕新主渲染结构重组。

**Non-Goals**

- 不改变 `LogicalGridState`、`MapDefinition`、`GridCharacterCollisionWorld` 或其它规则服务的权威。
- 不在本 change 内实现 exact multistate / shader / mesh 版本的 terrain resolver。
- 不在本 change 内重做危险区规则或把 overlays 一起改成 dual-grid。
- 不让碰撞几何跟着 dual-grid 外观变化；碰撞继续按 world-grid 方格结算。
- 不在本 change 内引入自动随机变体拼接、材质动画或高级后处理。

## Decisions

### 1. world grid 只保留真相，不再直接绘制 terrain

`LogicalGridState` 继续是以下系统的唯一输入：

- 玩家碰撞
- 挖掘与钻头门槛
- AI 路径 / 自动化
- 危险区与地震波
- 建筑占位
- 测试规则断言

但 terrain 主显示不再写入 world-grid `Terrain Tilemap`。  
terrain 的可见结果将只由 dual-grid display layers 生成。

选择理由：

- 这正是用户当前明确要求的渲染模型。
- 能把 floor / wall / boundary 统一到同一套 dual-grid 语言里。
- 避免继续维持“world base + contour overlay”两套 terrain 语义。

### 2. terrain family 统一映射为 6 类材质

当前版本定义一组稳定的 display material family：

```text
Floor
Soil
Stone
HardRock
UltraHard
Boundary
```

world truth 到 display material 的映射固定为：

```text
TerrainKind.Empty                     -> Floor
TerrainKind.MineableWall + Soil       -> Soil
TerrainKind.MineableWall + Stone      -> Stone
TerrainKind.MineableWall + HardRock   -> HardRock
TerrainKind.MineableWall + UltraHard  -> UltraHard
TerrainKind.Indestructible            -> Boundary
```

这层映射只属于表现层，不改变玩法数据结构。

### 3. 通过 `CornerMaterialSample -> Resolver -> RenderLayerCommand[]` 隔离当前与未来方案

当前版本引入稳定的采样与解析接口：

```text
CornerMaterialSample
┌────┬────┐
│ TL │ TR │
├────┼────┤
│ BL │ BR │
└────┴────┘

IDualGridTerrainResolver
  Resolve(sample) -> RenderLayerCommand[]
```

其中：

- `CornerMaterialSample` 记录一个 display cell 对应四个 world corners 的完整材质信息
- `RenderLayerCommand` 至少包含：
  - `LayerId`
  - `AtlasIndex`
  - 可选的 `ClearWhenAbsent` / `HasContent` 语义，用于让 renderer 明确某层当前应清空还是保留
- `IDualGridTerrainResolver` 只关心输入样本和输出渲染命令，不关心具体 Tilemap 如何写入

建议最小接口形态：

```text
enum TerrainMaterialId
enum TerrainRenderLayerId

struct CornerMaterialSample
  TL / TR / BL / BR : TerrainMaterialId

struct RenderLayerCommand
  LayerId    : TerrainRenderLayerId
  AtlasIndex : int
  HasContent : bool

interface IDualGridTerrainResolver
  Resolve(sample) -> ordered RenderLayerCommand[]
```

这里的关键约束不是具体语言形态，而是：

- resolver 输入固定为完整四角材质
- resolver 输出必须是稳定排序的 layer commands
- renderer 不直接理解材质规则，只消费 commands
- future exact multistate 版本只替换 resolver，不替换 sampling / dirty-region / Tilemap wiring

当前版本实现：

```text
LayeredBinaryResolver
```

未来预留：

```text
ExactMultistateResolver
```

这样后续如果要切到 1296-state atlas、shader 拼材质、mesh quarter-tile 等更复杂路线，上层采样和刷新逻辑不必重写。

### 4. 当前 terrain 生成策略使用 layered binary families

`LayeredBinaryResolver` 的逻辑是：

1. 读取四角完整材质  
2. 对每个 material family 生成一个独立 4-bit mask  
3. 对非零 mask 计算 dual-grid `16-state` index  
4. 输出多个 `RenderLayerCommand`

例如：

```text
TL = Soil
TR = Stone
BL = Floor
BR = Floor
```

会得到：

```text
Floor mask = 0011 -> Floor_3
Soil  mask = 1000 -> Soil_8
Stone mask = 0100 -> Stone_4
```

最终显示结果：

```text
DG Floor  index 3
+ DG Soil index 8
+ DG Stone index 4
```

这满足两点：

- 当前仍然可以把资源控制在 `每个 family 16 tiles`
- 不会把 exact multistate 的未来路线封死

### 5. terrain scene graph 改为 6 个 half-cell offset Tilemap

terrain 主显示将由以下 Tilemap 组成，全部挂在同一 `Grid Root` 下，并使用：

```text
localPosition = (-0.5, -0.5, 0)
```

建议层级顺序：

```text
DG Floor Tilemap
DG Soil Tilemap
DG Stone Tilemap
DG HardRock Tilemap
DG UltraHard Tilemap
DG Boundary Tilemap
```

建议排序值：

```text
DG Floor Tilemap       0
DG Soil Tilemap        1
DG Stone Tilemap       2
DG HardRock Tilemap    3
DG UltraHard Tilemap   4
DG Boundary Tilemap    5
Danger Tilemap        10
Facility Tilemap      15
Marker Tilemap        20
Build Preview Tilemap 25
Scan Indicator Root   30
Actors                40
```

overlay 与实体层保持独立：

```text
Danger
Facility
Marker
BuildPreview
ScanIndicator
Actors
```

选择理由：

- terrain families 都走同一 offset display 规则，避免部分材质仍然留在 world-grid。
- `Boundary` 放在最上层，能稳定覆盖外框和不可破坏边界语义。
- overlays 不被 terrain renderer 绑死，减少对现有危险区、建造预览和探测系统的改动面。
- 固定 layer name 和 sorting order 后，PlayMode 烟测与人工截图审查会更稳定。

### 6. 刷新语义固定为“1 个 world cell -> 4 个 display cells”

display cell 与 world cell 的邻接关系沿用现有 dual-grid 拓扑：

```text
display(x, y) 采样
  TL = world(x-1, y)
  TR = world(x,   y)
  BL = world(x-1, y-1)
  BR = world(x,   y-1)
```

因此一个 world cell 材质变化，只会影响：

```text
(x, y)
(x+1, y)
(x, y+1)
(x+1, y+1)
```

初始化、地图整体替换或 art set 热切换时可以全量重建。  
日常挖掘、爆炸、调试材质改动都应走局部刷新。

### 7. 资源配置改为“6 个 family × 16-state atlas”

当前版本的运行时资源目标至少为：

```text
Floor dual-grid atlas       16
Soil dual-grid atlas        16
Stone dual-grid atlas       16
HardRock dual-grid atlas    16
UltraHard dual-grid atlas   16
Boundary dual-grid atlas    16
```

即：

```text
96 terrain tiles
```

这不是说美术必须手绘 96 张完全独立图，而是说配置层必须能表达 6 套 family atlas。  
是否通过共享 shape mask、换色、程序化 fallback 或半自动切片来降低生产量，属于实现与资源流程问题，不应反推到运行时接口。

当前建议的 art set 目标结构：

```text
floorDualGridTiles[16]
soilDualGridTiles[16]
stoneDualGridTiles[16]
hardRockDualGridTiles[16]
ultraHardDualGridTiles[16]
boundaryDualGridTiles[16]
```

迁移期兼容策略：

- 现有 `emptyTile`、`soilWallTile`、`stoneWallTile`、`hardRockWallTile`、`ultraHardWallTile`、`boundaryTile`
  不再作为最终 terrain 主渲染输入
- 它们只作为 migration / fallback 输入保留一段时间
- 当前 contour change 留下的 `wallContourTiles` 对这条新主渲染路线不再是主资产
- 当 dual-grid family atlas 缺失时，fallback 应通过共享 shape mask 生成器加 family tint 动态补齐，而不是直接回退成旧的 world-grid terrain 绘制

推荐的资源命名约定：

```text
tile_dg_floor_00.png      ... tile_dg_floor_15.png
tile_dg_soil_00.png       ... tile_dg_soil_15.png
tile_dg_stone_00.png      ... tile_dg_stone_15.png
tile_dg_hardrock_00.png   ... tile_dg_hardrock_15.png
tile_dg_ultrahard_00.png  ... tile_dg_ultrahard_15.png
tile_dg_boundary_00.png   ... tile_dg_boundary_15.png
```

对应 Tile 资产：

```text
Tile_DG_Floor_00.asset
Tile_DG_Soil_00.asset
...
Tile_DG_Boundary_15.asset
```

### 8. 测试基线从 world cell terrain 断言迁移到 resolver / display 断言

当前很多测试默认可以直接断言：

```text
TerrainTilemap.GetTile(worldCell)
```

新基线应拆为两类：

- EditMode resolver tests
  - 输入 `CornerMaterialSample`
  - 断言输出的 `RenderLayerCommand[]`
- PlayMode / integration tests
  - 改一个 world cell
  - 断言只脏掉 4 个 display cells
  - 断言对应 family tilemap 更新
  - 断言 overlays 和规则真相不受影响

此外还应显式验证：

- `Grid Root` 下存在 6 个具名 dual-grid terrain family Tilemap
- 它们都共享 `(-0.5, -0.5, 0)` offset
- mixed-material sample 的命令顺序稳定为 `Floor -> Soil -> Stone -> HardRock -> UltraHard -> Boundary`

## Consequences

正向结果：

- terrain 视觉语言统一为真正的 dual-grid 主显示
- floor、不同硬度岩体、边界都进入同一套显示逻辑
- 当前仍可把资源复杂度控制在 family-level 16-state
- future exact multistate 路线有稳定扩展点

代价：

- 需要重写现有 terrain 表现层装配与测试断言
- 当前 contour change 的 terrain 假设不再是最终架构
- 资源生产目标从 contour family 扩大到多套 dual-grid terrain family

## Open Questions

- `Boundary` family 的最终美术语言是否需要比其它 terrain family 更规则、更机械化？
- 是否保留一个仅供调试的 world-grid terrain truth 可视化开关？如果保留，它必须是 debug-only，不得参与正常主渲染。
