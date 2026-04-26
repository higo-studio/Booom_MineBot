## Context

当前 Minebot 的地形表现路径是：

```text
LogicalGridState
  -> TilemapGridPresentation.Refresh(...)
  -> Terrain Tilemap.SetTile(worldCell, tile)
  -> Danger Tilemap.SetTile(worldCell, dangerTile)
```

它的优点是直观、稳定、完全贴合现有 world-grid 碰撞与测试；缺点是岩壁轮廓只能以方格边界显示，而危险区也只能退化成“每个危险格各画一个框”的逐格 overlay。随着像素风资源接入，方格墙体会比当前纯色占位更显生硬；而危险区这种逐格框线在新的矿洞轮廓语言旁边会更像 debug 层，而不是一个可读的危险边界。

本轮探索确认，Minebot 不能把 dual-grid 当作新的 terrain 真相，也不适合直接照搬 Godot 里的“隐藏 world tilemap + display tilemap”结构。项目已经有明确的逻辑真相：

- 编辑期：`Terrain/POI Tilemap + Overlay -> MapDefinition`
- 运行时：`MapDefinition -> LogicalGridState`
- 表现层：只负责从运行时状态生成场景视觉

同时，玩家移动与接触判定仍然是 world-grid 方格碰撞：

- `GridToWorld(position) = (x + 0.5, y + 0.5)`
- `WorldToGrid(world) = floor(world)`
- 墙体阻挡由 `GridCharacterCollisionWorld` 以轴对齐 1x1 cell 计算

另一方面，现有危险区逻辑仍是占位方案：`WaveSurvivalService.EvaluateDangerZones(...)` 把危险区算成若干 origin 的曼哈顿半径整片区域，而 `BuildDangerOrigins()` 甚至仍返回固定角点。这个方案足以先打通波次失败、机器人避险和 HUD 倒计时闭环，但与本轮确认的“危险区出现在平地与岩石边沿”不一致。

因此本设计采用混合方案：terrain / 碰撞仍为 world grid；`MineableWall` 的轮廓由 dual-grid 负责显示；`DangerZone` 的玩法真相则保留在 world-grid 上，但从“半径块”改为“空地-岩壁边沿带”，并只由低透明 `Danger Base` warning tile 可视化。contour 视觉明确保留给岩体，不再给危险区额外描边。

这次额外拉取并审查了参考仓库 `jess-hammer/dual-grid-tilemap-system-unity`（当前 HEAD `c9f948d`）。它给出的有效经验很具体：

- 同一 `Grid` 下同时存在 `placeholderTilemap` 与 `displayTilemap`
- `displayTilemap` 的 Transform 位于 `(-0.5, -0.5, 0)`，以 half-cell offset 承接 dual-grid 结果
- 用项目自管的 16 项 2x2 邻域 lookup table 映射 tile，而不是依赖 `RuleTile`
- 一个 placeholder cell 变化时，只重算周围 4 个 display cells

它也暴露了几个不适合 Minebot 直接照搬的点：

- 把隐藏 tilemap 当真相层会与 `MapDefinition -> LogicalGridState` 形成第二份权威数据
- 初始化通过固定 `-50..50` 全图 brute force 刷新，缺少 bounds 感知
- 示例只覆盖二值地形切换，不包含多层 overlay、危险区真相、建筑预览或测试基线

因此 Minebot 应该借它的“offset 约定 + lookup 拓扑 + 局部刷新思路”，但明确拒绝“placeholder tilemap 重新成为真相层”。

## Goals / Non-Goals

**Goals:**

- 为 `MineableWall` 增加更自然的 dual-grid 轮廓显示，同时保留现有逻辑格真相、Bake 管线和碰撞模型。
- 让 `DangerZone` 的玩法真相围绕“空地与可挖岩壁的边沿带”建立，并只以 `Danger Base` 显示危险格范围，而不是继续沿用 origin + 半径整片覆盖或逐格框线。
- 明确运行时图层职责：什么信息属于 world-aligned base/detail，什么信息属于 half-cell contour。
- 明确 OpenSpec ownership：`tilemap-art-presentation` 在本 change 中只新增 requirement，用来补充 base/detail 与 contour family 的分层职责；归档 spec 里已有的“配置化美术资产”“地形/硬度区分” requirement 不在这里被改写。
- 把 Image2 资产生成目标从“单格墙 tile”改成“wall contour atlas + hardness detail overlays”，并把这批素材重生本身纳入本 change 的正式交付范围。
- 让 `Gameplay` / `DebugSandbox` 的视觉升级不会破坏标记、危险区、建造预览、探测数字和角色碰撞可读性。
- 让玩家失败、机器人避险和建造阻挡直接对齐玩家可见的危险边界，同时保持危险空地仍可通行。
- 保持现有测试可迁移，而不是推翻整个表现层验收基线。

**Non-Goals:**

- 不修改 `LogicalGridState`、`MapDefinition`、`TilemapBakeProfile` 的数据结构。
- 不把 dual-grid 引入编辑期 Bake 真相或关卡 authoring 语义。
- 不把 `Indestructible` 边界第一版纳入 dual-grid 轮廓系统。
- 不重做角色碰撞，使其贴合圆角轮廓；碰撞继续按方格世界求解。
- 不把危险区做成高不透明整片填充、全屏后处理或其它会压过地图语义的强效果。
- 不在本变更内追求 alternate patterns、自动随机变体拼接、动画 shoreline 之类高级 dual-grid 扩展。

## Decisions

### 1. 使用“Base/Detail + Contour Family”表现，而不是纯 dual-grid 主层

在 OpenSpec 映射上，这个决定需要拆成两类：

- `tilemap-art-presentation` 只追加 requirement，补充“基础层与 contour layer 分层职责”“不可破坏边界不进入第一版 dual-grid 墙体系统”等表现约束
- 危险区显示方式与危险区真相的改写继续落在 `layered-grid-feedback-overlays`、`danger-zone-contour-overlay` 与 `automation-and-wave-survival`

这样做的原因是：`tilemap-art-presentation` 归档 spec 已经拥有“配置化资产驱动地形表现”“地形类型与硬度可区分”这些 requirement；本 change 只是继续往上叠加 contour family 约束，而不是重写旧 requirement 的含义
运行时表现拆为三类视觉信号：

```text
Terrain Base Tilemap      world-aligned
Danger Base Tilemap       world-aligned overlay
Wall Contour Tilemap      half-cell offset dual-grid
```

其中：

- `Terrain Base Tilemap` 继续按 world cell 刷新，负责：
  - 空地底图
  - 不可破坏边界
  - MineableWall 的硬度 detail 表达
  - 同类型岩体内部的连续材质铺陈
  - 明确不再承担“每个岩体格子自带完整外框”的旧职责
- `Danger Base Tilemap` 继续按 world cell 刷新，负责：
  - 为每个 `IsDangerZone` 空地格铺设 warning base tile
  - 让危险带厚度超过 1 格时，内部危险格也保持可读
  - 保持低透明度，不压过地形、设施和角色
- `Wall Contour Tilemap` 只负责：
  - 基于 `MineableWall` / `not MineableWall` 的轮廓形状
  - 半格偏移显示
  - 表达更圆润的矿壁边缘
图层装配约定参考上述仓库，但绑定到 Minebot 现有 `Grid Root`：

- `Terrain Base`、`Danger Base`、`Facility`、`Marker`、`BuildPreview` 仍保持 `localPosition = (0, 0, 0)`
- `Wall Contour` 作为同一 `Grid Root` 下的子 Tilemap，`localPosition = (-0.5, -0.5, 0)`
- 不新增隐藏 placeholder Tilemap；wall / danger 的 mask 由 `LogicalGridState` 或其临时派生缓存直接生成

当前版本危险区只采用 `Danger Base` 显示。玩家需要一眼读出危险格覆盖范围，但 contour 视觉不再分配给危险区；base tile 仍必须维持低透明度，不能把地图语义压成整片纯红面。

这意味着 MineableWall 的最终视觉来自两层叠加：

```text
base/detail (对齐 world grid)
        +
contour silhouette (对齐 dual grid)
```

并且这两层必须遵守一个额外约束：

- 相邻的同类型 `MineableWall` 在 base/detail 层里应优先读成同一整块连续岩面
- 明显边界出现在与空地接壤的外缘、转角、洞口、新挖开的破口，以及不同 `HardnessTier` 岩体的交界
- 同类型岩体的内部连接缝不得重复描边，否则会重新退回“每格一块砖”的读感
- 当前如果 base 资源本身在四边都带有完整边缘，这类旧资源必须退役或改造为可无缝拼接的 fill/detail；边缘语义统一迁移到 `Wall Contour`

危险区的最终视觉则来自：

```text
danger truth mask (IsDangerZone)
        +
danger base tile (对齐 world grid)
```

选择理由：

- dual-grid 最擅长表达“空/实”边界，而不是每格玩法语义。
- `HardnessTier` 是玩家决策信息，必须保留 world-grid 可读性。
- 用户当前要求是“同类岩体之间自然连成整片，但不同硬度岩体交界也要直接出描边”，这更适合由 world-grid detail + 按硬度分层的 contour 共同完成，而不是给每个墙格都画完整框线。
- 用户最新要求是取消危险区描边，把显著 contour 语言全部收口给岩体本身，避免同屏出现两套边界语义。
- 可以在不改碰撞模型的前提下提升墙体轮廓观感。

放弃方案：直接让 `Terrain Tilemap` 本身改为 dual-grid 主层。

放弃原因：

- 会打断现有 `Terrain Tilemap` 的测试与资源语义。
- 会放大“圆角视觉 vs 方格碰撞”的错位。
- 会让硬度表达被迫塞进 contour atlas，资源量和复杂度都会上升。
- 也无法为危险区提供与墙体统一但独立的边界语言。

### 2. world-solid dual-grid 只覆盖 `MineableWall`；危险区真相与表现都基于空地-岩壁边沿带

第一版 world-solid contour 解析只把 `TerrainKind.MineableWall` 当作 solid mask；`Empty` 与 `Indestructible` 都不进入可挖轮廓集合。与此同时，`DangerZone` 不再由若干 origin 的曼哈顿半径整片生成，而是按空地一侧的岩壁边沿带生成：

- 危险前沿：当前为 `TerrainKind.Empty`，且至少与一个 4 邻接 `MineableWall` 接壤的格子
- 危险带：从危险前沿出发，仅穿过 `TerrainKind.Empty` 向内扩张 `bandThickness` 格得到的区域；对角仅接触的空地不进入危险带，避免把角点上方或侧方额外膨胀成多余危险块
- 空岛收口：当重算结束后，只保留与出生点所在主空地腔体连通的安全 pocket；其它不属于这块主空地腔体的安全 pocket 会并回危险区，以避免视觉上出现无意义的小安全孔洞，同时避免把同一洞穴里仅仅被危险带切开的安全 pocket 误判成空岛
- `bandThickness` 随波次增长；现有 `DangerRadius` 配置在实现期迁移为“危险带厚度”语义即可
- `TerrainKind.Indestructible` 不参与危险前沿，避免地图外框被误读为可塌方岩层
- 玩家失败、机器人损毁、机器人避险、建造限制继续统一读取 `LogicalGridState.IsDangerZone`
- 玩家移动与角色碰撞不读取 `IsDangerZone`；空地是否可通行仍只由地形和建筑占用决定

表现规则：

- `MineableWall`：base/detail + contour
- `Empty` 且位于危险带中：floor + danger base
- `Empty` 且不在危险带中：floor only
- `Indestructible`：boundary only

选择理由：

- 直接匹配“危险区出现在平地与岩石边沿”的确认图像。
- 让玩家可见的危险底图与致死/避险/建造限制真相保持同形，避免“看起来安全但结算致死”的错位。
- 波次压力会自然转化为“维护通道宽度”，比记忆若干半径块更符合矿洞塌方语义。
- 保持 world-grid 数据模型与 `IsDangerZone` 下游查询接口不变。
- 如果后续要做局部震源、保留通道豁免或特殊地层，只需要在“哪些 `MineableWall` 算不稳定”之前增加筛选步骤；边沿带生成规则本身不必推翻。

### 3. Wall 使用 2x2 mask -> contour index 的显式 lookup 解析框架；Danger 不再消费 contour

设墙体 mask 为 `W(x,y)`，其中 `MineableWall = 1`，其它为 `0`。  
设 contour cell 为 `C(i,j)`，则它采样一个 2x2 mask：

```text
M(i-1, j-1)  M(i, j-1)
M(i-1, j)    M(i, j)
```

从这个 2x2 mask 推导 wall contour atlas index。

实现上采用项目自管的固定 lookup，而不是引入 `RuleTile`：

```text
bit 3 = topLeft
bit 2 = topRight
bit 1 = bottomLeft
bit 0 = bottomRight

index = (tl << 3) | (tr << 2) | (bl << 1) | br
```

`index` 映射到 wall contour tile 数组。  
这样做与参考仓库的 16 项显式字典本质一致，但更适合 Minebot 后续把 lookup 与资源配置分离。

结果：

- 总形态数为 15/16（含 empty）
- 一个 world cell 改变时，墙体 contour 只需重算其周围 4 个 contour cell
- `Wall Contour` 必须提供局部失效入口，避免像参考仓库那样在 Minebot 内退回固定区域全量刷新
- 一次危险带厚度变化，或挖掘 / 爆炸导致局部岩壁边界变化时，第一版继续全量重算 danger truth + danger base 即可

选择理由：

- 这与 dual-grid 的经典 2x2 邻域模型一致。
- 运行时把 contour 语言明确留给岩体，能避免危险区和岩体同时争抢边界强调。
- 后续如果地图扩大，可以从“全量重刷”平滑升级到“脏 contour 区域刷新”。

### 4. Image2 生成目标以 wall contour 与 hardness detail 为主；danger contour 不再是运行时必需品

本变更的美术资产生成规则要从一开始就适配渲染结构。  
Image2 的目标资产拆为两组主资产，外加一组可保留存档的历史 danger contour 素材：

#### A. wall contour atlas

这是主资产。目标是一个 4x4 或等价布局的 contour atlas，包含：

- 全实心
- 四个外角
- 四条边
- 四个内角
- 两个对角分离形态
- empty（可为空白）

语义要求：

- 边界线穿过 tile 中线，供 half-cell offset 后对齐 world grid
- 轮廓尽量圆润，但不能把通道口画得过宽
- 统一明暗和外轮廓语言
- 不按 `HardnessTier` 分四套重复 contour atlas

#### B. danger warning base

这是独立 overlay 资产。目标是一个 world-grid warning base tile。要求：

- danger base tile 需要能单格平铺，透明度足够低，不盖掉地形与设施辨识
- 不直接复用 wall contour atlas 的材质与颜色
- 颜色与明度明确表达“危险区域”，但不再承担“危险边界描边”的职责
- 不再使用当前逐格空心框作为主资源

#### C. world-grid hardness details

这是辅助资产。目标是 1x1 detail tiles 或 overlay tiles，用于区分：

- Soil
- Stone
- HardRock
- UltraHard

语义要求：

- detail 应叠加在 world-grid 基础格上
- 重点表达材料差异、裂纹、颗粒、色带，而不是重新画轮廓
- 相邻同类型岩体拼接后必须优先读成连续纹理面，不能在每个格子的四边重复画一圈深色描边
- 当前旧版那种“单格 base 自带完整边缘”的墙体图不再视为合格候选，除非已经去掉内部四边边框并转成连续 fill/detail
- 必须与 contour family 风格统一

这意味着后续 prompt、筛选和切片都必须优先满足 wall contour、danger base 与 hardness detail 的职责分离，而不是先追求每张单格 tile 自洽。

### 5. `MinebotPresentationArtSet` 需要从单字段升级到 contour family / detail 组合配置

第一版不强行规定最终代码字段名，但配置职责必须支持下列资源族：

```text
MinebotPresentationArtSet
├─ terrain base
│  ├─ floor
│  └─ boundary
├─ wall contour
│  └─ dual-grid contour atlas / 15-16 shapes
├─ wall details
│  ├─ soil
│  ├─ stone
│  ├─ hardRock
│  └─ ultraHard
├─ overlays
│  ├─ marker
│  ├─ build preview valid
│  └─ build preview invalid
├─ facilities
└─ actors
```

选择理由：

- 否则配置层会继续假设“一个语义 = 一个 tile”。
- 只有把 wall contour、danger base 和 detail 作为不同资源职责处理，Image2 资产重生才不会又退回旧模型。
- `Danger` 与 `BuildPreviewInvalid` 必须从资源职责上拆开，避免危险区和非法建造继续使用同一套视觉语言。

### 6. 测试验收要从“单一 terrain tile 正确”升级为“base/detail 与 contour family 共存正确”

现有测试直接从 `Terrain Tilemap` 读取 tile 名称，这对旧方案成立，对新方案不够。

新验收应至少覆盖：

- `Terrain Base Tilemap` 存在
- `Wall Contour Tilemap` 存在
- `Danger Base Tilemap` 存在
- 出生点仍然显示 floor
- 某个 `MineableWall` 格存在对应 hardess detail
- 同一处墙体周围存在 contour tiles
- 连续危险区会显示为 warning base tile，而不是每个格子一个框或额外 contour
- 墙被挖开后：
  - `Terrain Base` 该格切到 floor
  - 周边 wall contour 变化
  - 新暴露或消失的贴墙空地会按当前波次进入或退出 danger band
- 波次危险带厚度变化后，danger base 会同步刷新
- 玩家失败、机器人避险和建造阻挡与玩家可见 danger band 保持一致
- `Marker/DangerBase/BuildPreview/ScanIndicator` 仍不抢占彼此职责

## Risks / Trade-offs

- [Risk] 轮廓画得太圆，会暗示玩家可以擦过角落。  
  Mitigation：保留 world-grid detail / 格子感，不让 contour 吞掉通道判读。

- [Risk] 如果仍按每种硬度各做一整套 contour atlas，资源量会暴涨。  
  Mitigation：共享一套 contour atlas，硬度只做 detail 层。

- [Risk] 如果 detail 资源本身在每格四周都带完整描边，即使有 contour 也会把整片岩体重新切成棋盘格。  
  Mitigation：把“内部连接缝弱化、只在暴露外缘保留明显边界”写入 prompt、筛选标准和验收用例。

- [Risk] `Indestructible` 不进入 contour 后，边界和矿壁的视觉风格差异可能偏大。  
  Mitigation：允许 boundary 在明暗和材质上接近，但轮廓表达仍保持硬边。

- [Risk] 取消 danger contour 后，危险带边界感可能弱于之前方案。  
  Mitigation：保持 danger base 可读、HUD/提示语持续强调危险区语义，并把显著描边集中给岩体边界。

- [Risk] 如果所有贴墙通道都直接进入危险带，早期地图可能显得过于拥挤。  
  Mitigation：第一版将默认危险带厚度锁在 1，并保证出生区和关键通道有足够宽度；压力主要来自后续波次增厚，而不是首波就铺满。

- [Risk] 挖开单格岩壁会立刻改变周围危险带，若刷新节奏不清晰，玩家可能觉得规则在“跳”。  
  Mitigation：`IsDangerZone` 与 danger base 同帧刷新，并在 HUD/提示语里明确危险带代表当前塌方区域，而不是静态标记。

- [Risk] 如果为了“贴近参考仓库”而重新引入隐藏 placeholder Tilemap，Minebot 会出现双份地形真相并增加漂移风险。  
  Mitigation：明确只复用其 offset / lookup / 局部刷新思路，mask 始终直接来自 `LogicalGridState`。

- [Risk] 现有测试过度绑定 tile 名称，会让迁移变得脆弱。  
  Mitigation：把断言改成图层职责与关键位置表现，而不是只断单个 terrain tile 名。

- [Risk] Image2 容易产出“好看但不成套”的墙体图。  
  Mitigation：先以 contour atlas 完整性为第一筛选标准，再看单 tile 细节质量。

## Migration Plan

1. 先在 OpenSpec 中固定“wall contour + danger edge-band truth + hardness detail”的方向。
2. 调整 `WaveSurvivalService` 与危险区入口语义，把 `IsDangerZone` 改为由空地-岩壁边沿带推导，同时保持下游系统继续读取同一字段。
3. 明确 contour tilemap 的 shared Grid 布局、`(-0.5, -0.5)` 偏移和 4-bit lookup 约定，不引入 placeholder truth tilemap。
4. 重写 Image2 prompt 与筛选标准，使输出以 contour family 为核心，并明确旧版“每格自带边缘的 wall base”要迁移为可连续拼接的 fill/detail。
5. 生成并筛选 wall contour / danger contour / detail 资源，整理到 `Sprites/Tiles`，同时淘汰或改造当前带四边边缘的 wall base 资源。
6. 扩展 `MinebotPresentationArtSet` 配置职责。
7. 改造 `TilemapGridPresentation`，让 wall base/detail 只负责连续纹理，边缘统一由 wall contour 表达，并让表现严格跟随新的 danger truth。
8. 更新 `Gameplay` / `DebugSandbox` 装配与 EditMode / PlayMode 验收。

## Open Questions

- 第一版资源是否必须落成 15/16 张独立 tile，还是允许像参考仓库 README 提到的那样由更少的对称母片派生出 16 个逻辑索引。
  当前倾向：运行时始终按 16 个逻辑索引消费；美术资产是否复用对称母片，等首批 Image2 候选出来后再定，不把这个决定写死进实现。

- hardness detail 是画在同一 `Terrain Base Tilemap` 上，还是拆成独立 `Wall Detail Tilemap`。
  当前倾向：优先复用 `Terrain Base Tilemap`；如果 sorting 或资源叠加受限，再拆独立 detail layer。

- danger base 已经确定纳入第一版显示；后续只需要再决定是否要做更强的动画或 shader，而不是再回到 contour-only。
