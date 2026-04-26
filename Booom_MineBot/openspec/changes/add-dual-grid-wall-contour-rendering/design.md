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

因此本设计采用混合方案：terrain / 碰撞仍为 world grid；`MineableWall` 的轮廓由 dual-grid 负责显示；`DangerZone` 的玩法真相则保留在 world-grid 上，但从“半径块”改为“空地-岩壁边沿带”，并由 `Danger Contour` 直接可视化。

## Goals / Non-Goals

**Goals:**

- 为 `MineableWall` 增加更自然的 dual-grid 轮廓显示，同时保留现有逻辑格真相、Bake 管线和碰撞模型。
- 让 `DangerZone` 的玩法真相与视觉反馈都围绕“空地与可挖岩壁的边沿带”建立，而不是继续沿用 origin + 半径整片覆盖或逐格框线。
- 明确运行时图层职责：什么信息属于 world-aligned base/detail，什么信息属于 half-cell contour。
- 把 Image2 资产生成目标从“单格墙 tile”改成“wall contour atlas + danger contour overlay + hardness detail overlays”，并把这批素材重生本身纳入本 change 的正式交付范围。
- 让 `Gameplay` / `DebugSandbox` 的视觉升级不会破坏标记、危险区、建造预览、探测数字和角色碰撞可读性。
- 让玩家失败、机器人避险和建造阻挡直接对齐玩家可见的危险边界。
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

运行时表现拆为三类视觉信号：

```text
Terrain Base Tilemap      world-aligned
Wall Contour Tilemap      half-cell offset dual-grid
Danger Contour Tilemap    half-cell offset overlay contour
```

其中：

- `Terrain Base Tilemap` 继续按 world cell 刷新，负责：
  - 空地底图
  - 不可破坏边界
  - MineableWall 的硬度 detail 表达
- `Wall Contour Tilemap` 只负责：
  - 基于 `MineableWall` / `not MineableWall` 的轮廓形状
  - 半格偏移显示
  - 表达更圆润的矿壁边缘
- `Danger Contour Tilemap` 只负责：
  - 基于 `IsDangerZone` mask / `not danger` 的危险边界
  - 半格偏移显示
  - 表达连续的波次危险区域轮廓，而不是每格一个框

第一版危险区不再保留“逐格框线”作为主显示，也不要求额外的内部 fill。玩家要读的是危险边界，而不是危险格的调试栅格。

这意味着 MineableWall 的最终视觉来自两层叠加：

```text
base/detail (对齐 world grid)
        +
contour silhouette (对齐 dual grid)
```

危险区的最终视觉则来自：

```text
danger truth mask (IsDangerZone)
        +
danger contour overlay (对齐 dual grid)
```

选择理由：

- dual-grid 最擅长表达“空/实”边界，而不是每格玩法语义。
- `HardnessTier` 是玩家决策信息，必须保留 world-grid 可读性。
- 危险区玩家真正需要读的是“危险边界”，而不是“危险格子清单”。
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
- 危险带：从危险前沿出发，仅穿过 `TerrainKind.Empty` 向内扩张 `bandThickness` 格得到的区域
- `bandThickness` 随波次增长；现有 `DangerRadius` 配置在实现期迁移为“危险带厚度”语义即可
- `TerrainKind.Indestructible` 不参与危险前沿，避免地图外框被误读为可塌方岩层
- 玩家失败、机器人损毁、机器人避险、建造限制继续统一读取 `LogicalGridState.IsDangerZone`

表现规则：

- `MineableWall`：base/detail + contour
- `Empty` 且位于危险带中：floor + danger contour
- `Empty` 且不在危险带中：floor only
- `Indestructible`：boundary only

选择理由：

- 直接匹配“危险区出现在平地与岩石边沿”的确认图像。
- 让玩家可见的红色边沿与致死/阻挡真相保持同形，避免“看起来安全但结算致死”的错位。
- 波次压力会自然转化为“维护通道宽度”，比记忆若干半径块更符合矿洞塌方语义。
- 保持 world-grid 数据模型与 `IsDangerZone` 下游查询接口不变。
- 如果后续要做局部震源、保留通道豁免或特殊地层，只需要在“哪些 `MineableWall` 算不稳定”之前增加筛选步骤；边沿带生成规则本身不必推翻。

### 3. Wall 与 Danger 共用 2x2 mask -> contour index 的解析框架，但使用独立 tilemap 与独立资源族

设墙体 mask 为 `W(x,y)`，其中 `MineableWall = 1`，其它为 `0`。  
设危险区 mask 为 `D(x,y)`，其中 `IsDangerZone = 1`，其它为 `0`。  
设 contour cell 为 `C(i,j)`，则它采样一个 2x2 mask：

```text
M(i-1, j-1)  M(i, j-1)
M(i-1, j)    M(i, j)
```

其中 `M` 可以是 `W` 或 `D`。从这个 2x2 mask 推导 contour atlas index。

结果：

- 总形态数为 15/16（含 empty）
- 一个 world cell 改变时，墙体 contour 只需重算其周围 4 个 contour cell
- 一次危险带厚度变化，或挖掘 / 爆炸导致局部岩壁边界变化时，可以先全量重算 danger truth + danger contour；后续如有需要，再升级为受影响空地区域的脏刷新

选择理由：

- 这与 dual-grid 的经典 2x2 邻域模型一致。
- 墙体与危险区共享同一套拓扑规则，能让整个项目形成统一的 contour visual language。
- 后续如果地图扩大，可以从“全量重刷”平滑升级到“脏 contour 区域刷新”。

### 4. Image2 生成目标必须同时覆盖 wall contour 与 danger contour，而不是继续生产“完整墙 tile”

本变更的美术资产生成规则要从一开始就适配渲染结构。  
Image2 的目标资产拆为三组：

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

#### B. danger contour overlay

这是独立 overlay 资产。目标是与 wall contour 共享拓扑语言、但明显区分语义的 danger 边界资源。要求：

- 仍然支持 15/16 形态或与之等价的 half-cell contour 组合
- 不直接复用 wall contour atlas 的材质与颜色
- 颜色、线宽、发光感应明确表达“危险边界”，而不是“岩壁”
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
- 必须与 contour family 风格统一

这意味着后续 prompt、筛选和切片都必须优先满足 wall / danger contour 的成套完整性与语义分离，而不是先追求每张单格 tile 自洽。

### 5. `MinebotPresentationArtSet` 需要从单字段升级到 contour family / detail 组合配置

第一版不强行规定最终代码字段名，但配置职责必须支持下列资源族：

```text
MinebotPresentationArtSet
├─ terrain base
│  ├─ floor
│  └─ boundary
├─ wall contour
│  └─ dual-grid contour atlas / 15-16 shapes
├─ danger contour
│  └─ contour overlay atlas / 15-16 shapes
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
- 只有把 wall contour、danger contour 和 detail 作为不同资源族处理，Image2 资产重生才不会又退回旧模型。
- `Danger` 与 `BuildPreviewInvalid` 必须从资源职责上拆开，避免危险区和非法建造继续使用同一套视觉语言。

### 6. 测试验收要从“单一 terrain tile 正确”升级为“base/detail 与 contour family 共存正确”

现有测试直接从 `Terrain Tilemap` 读取 tile 名称，这对旧方案成立，对新方案不够。

新验收应至少覆盖：

- `Terrain Base Tilemap` 存在
- `Wall Contour Tilemap` 存在
- `Danger Contour Tilemap` 存在
- 出生点仍然显示 floor
- 某个 `MineableWall` 格存在对应 hardess detail
- 同一处墙体周围存在 contour tiles
- 连续危险区会显示为连续 contour 边界，而不是每个格子一个框
- 墙被挖开后：
  - `Terrain Base` 该格切到 floor
  - 周边 wall contour 变化
  - 新暴露或消失的贴墙空地会按当前波次进入或退出 danger band
- 波次危险带厚度变化后，danger contour 会同步刷新
- 玩家失败、机器人避险和建造阻挡与玩家可见 danger band 保持一致
- `Marker/DangerContour/BuildPreview/ScanIndicator` 仍不抢占彼此职责

## Risks / Trade-offs

- [Risk] 轮廓画得太圆，会暗示玩家可以擦过角落。  
  Mitigation：保留 world-grid detail / 格子感，不让 contour 吞掉通道判读。

- [Risk] 如果仍按每种硬度各做一整套 contour atlas，资源量会暴涨。  
  Mitigation：共享一套 contour atlas，硬度只做 detail 层。

- [Risk] `Indestructible` 不进入 contour 后，边界和矿壁的视觉风格差异可能偏大。  
  Mitigation：允许 boundary 在明暗和材质上接近，但轮廓表达仍保持硬边。

- [Risk] 墙体 contour 与 danger contour 在同一区域相邻时可能产生边界噪音。  
  Mitigation：保持共享解析语言，但在颜色、材质、线宽和层级上清晰区分二者。

- [Risk] 如果所有贴墙通道都直接进入危险带，早期地图可能显得过于拥挤。  
  Mitigation：第一版将默认危险带厚度锁在 1，并保证出生区和关键通道有足够宽度；压力主要来自后续波次增厚，而不是首波就铺满。

- [Risk] 挖开单格岩壁会立刻改变周围危险带，若刷新节奏不清晰，玩家可能觉得规则在“跳”。  
  Mitigation：`IsDangerZone` 与 danger contour 同帧刷新，并在 HUD/提示语里明确危险带代表当前塌方边界，而不是静态标记。

- [Risk] 现有测试过度绑定 tile 名称，会让迁移变得脆弱。  
  Mitigation：把断言改成图层职责与关键位置表现，而不是只断单个 terrain tile 名。

- [Risk] Image2 容易产出“好看但不成套”的墙体图。  
  Mitigation：先以 contour atlas 完整性为第一筛选标准，再看单 tile 细节质量。

## Migration Plan

1. 先在 OpenSpec 中固定“wall contour + danger edge-band truth + hardness detail”的方向。
2. 调整 `WaveSurvivalService` 与危险区入口语义，把 `IsDangerZone` 改为由空地-岩壁边沿带推导，同时保持下游系统继续读取同一字段。
3. 重写 Image2 prompt 与筛选标准，使输出以 contour family 为核心。
4. 生成并筛选 wall contour / danger contour / detail 资源，整理到 `Sprites/Tiles`。
5. 扩展 `MinebotPresentationArtSet` 配置职责。
6. 改造 `TilemapGridPresentation`，增加 wall contour 与 danger contour layer 刷新，并让表现严格跟随新的 danger truth。
7. 更新 `Gameplay` / `DebugSandbox` 装配与 EditMode / PlayMode 验收。

## Open Questions

- 第一版 contour atlas 是否直接要求完整 16 形态，还是允许先以 6 个对称基础形态生成后再人工扩展。
  当前倾向：先直接收完整 15/16 形态，避免实现期再做额外拼装规则。

- hardness detail 是画在同一 `Terrain Base Tilemap` 上，还是拆成独立 `Wall Detail Tilemap`。
  当前倾向：优先复用 `Terrain Base Tilemap`；如果 sorting 或资源叠加受限，再拆独立 detail layer。

- danger contour 第一版是否需要额外的低透明度内部 fill。
  当前倾向：不需要；第一版只保留连续边界，把危险区主信号集中在 contour 上。
