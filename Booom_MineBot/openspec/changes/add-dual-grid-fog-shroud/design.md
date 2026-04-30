## Context

当前 Minebot 的运行时已经具备：

- `LogicalGridState` 中的 `GridCellState.IsRevealed` 真相字段
- dual-grid terrain 主显示
- 独立的 marker / danger / build preview / scan feedback 所有权
- 全图直出的 minimap

但 `IsRevealed` 目前只被挖掘、爆炸、塌方逻辑维护，没有真正进入表现层。因此玩家虽然在规则上只逐步打开矿洞，画面上却始终能直接看到整片未开采岩层。这个 change 需要在不把需求扩张成完整视野系统的前提下，把“未知区域”明确压回表现层，并且显式保证“一格亮、两格外全黑”的分层读感。

目录与模块边界保持现状：

- `Assets/Scripts/Runtime/Presentation`
  运行时 fog band 渲染、tilemap 装配、minimap 同步
- `Assets/Scripts/Editor`
  默认资源生成与 art set 回填
- `Assets/Scripts/Tests/EditMode` / `PlayMode`
  fog band、dirty refresh、默认资源与场景回归

asmdef 策略保持不变：

- 继续复用 `Minebot.Runtime.Presentation`
- 继续复用 `Minebot.Editor`
- 不新增 fog 专属 asmdef

启动场景流程也保持不变：

`Bootstrap -> MinebotServices.Initialize -> MinebotGameplayPresentation.EnsureSceneInfrastructure -> TilemapGridPresentation.Refresh`

## Goals / Non-Goals

**Goals:**

- 把 `!IsRevealed && TerrainKind != Empty` 渲染成两层独立的 dual-grid fog shroud：前沿 1 格亮边带 + 两格外全黑深层带。
- 复用现有 half-cell offset，并在单格变化时正确前推 / 回收 1 格宽的亮边带。
- 保持 marker、scan label、build preview 等反馈层的独立渲染所有权与可读性。
- 同步 minimap，避免 HUD 继续泄露未揭示的非空地信息。
- 为默认 art set 与无资源场景提供程序化 fallback，保证场景可运行。

**Non-Goals:**

- 不新增 `IsExplored`、`IsVisibleNow`、FOV 半径或离开后重新变暗的规则。
- 不改变 `MapDefinition`、`LogicalGridState`、`GameSessionService` 的权威边界。
- 不把 fog 并入现有 `DualGridTerrainProfile` 的 terrain family 结构。
- 不在本阶段做 editor fog authoring 工具或新的 Fog EditorWindow。
- 不重新设计 marker / scan / danger 的规则真相，只调整它们与 fog 的层级关系。

## Decisions

### 1. 直接复用 `IsRevealed`，不新增新的探索真相字段

fog truth 固定定义为：

```text
FogSolid = !cell.IsRevealed && cell.TerrainKind != Empty
```

也就是：

- 未揭示的 `MineableWall` 进入 fog
- 未揭示的 `Indestructible` 也进入 fog
- `Empty` 永远不进入 fog

选择理由：

- 当前 `IsRevealed` 已经被挖掘、爆炸、塌方路径维护，接入成本最低。
- 这次需求是“未挖开的外描边 + 以外整体迷雾”，不是“玩家移动视野”。
- 不引入额外真相字段可以避免把表现需求扩成完整探索系统。

### 2. 使用双带 fog mask：前沿 1 格亮带 + 深层全黑带

运行时不再只维护单一 fog mask，而是把 `FogSolid` 拆成两个 band：

```text
FogNear = FogSolid && 距任一已揭示格的 Chebyshev 距离 <= 1
FogDeep = FogSolid && !FogNear
```

也就是：

- 未揭示非空地格里，紧贴已揭示区域的一圈 world-cell 必须走 `FogNear`
- 两格外及更深的连续未揭示区域必须走 `FogDeep`
- 两层都继续用 16-state dual-grid 渲染，但 tile style 分开

选择理由：

- 仅靠单 atlas 的边缘视觉无法保证严格的“一格亮边、两格外纯黑”。
- 亮边带是玩法读感要求，应该由运行时 band 逻辑保证，而不是交给美术错觉。

代价：

- 一个 cell 的 reveal 状态变化，可能连带改变周围一圈 world-cell 的 near/deep 分类。
- dirty refresh 不再是固定 4 个 display cells，而是“受 band 变化影响的 world-cell union”。

### 3. fog 使用独立 runtime renderer，不并入 terrain family profile

fog 不是 terrain material family，而是基于 reveal truth 的独立 overlay。  
因此实现上保持：

- `MinebotPresentationArtSet` / `MinebotPresentationAssets` 增加 `FogNearDualGridTiles` 与 `FogDeepDualGridTiles`
- `MinebotPixelArtAssetPipeline` 生成默认 near/deep fog 资源
- `TilemapGridPresentation` 持有独立 `FogNearTilemap` / `FogDeepTilemap`
- runtime 使用专门的 dual-grid fog band renderer / helper

而不是把 fog 塞进现有：

- `DualGridTerrainProfile.Families`
- `TerrainRenderLayerId`
- terrain 6 family Tilemap 顺序

### 4. `DG Fog Deep Tilemap` / `DG Fog Near Tilemap` 进入现有 Grid Root

Scene graph 增量如下：

```text
Grid Root
├─ DG Floor Tilemap
├─ DG Soil Tilemap
├─ DG Stone Tilemap
├─ DG HardRock Tilemap
├─ DG UltraHard Tilemap
├─ DG Boundary Tilemap
├─ DG Fog Deep Tilemap
├─ DG Fog Near Tilemap
├─ Danger Tilemap
├─ Facility Tilemap
├─ Marker Tilemap
├─ Build Preview Tilemap
└─ Scan Indicator Root
```

排序方向：

- `DG Fog Deep` / `DG Fog Near` 都要压住 terrain，且 `Near` 高于 `Deep`
- `Marker`、`Build Preview`、`Scan Indicator` 要继续高于 fog
- `Danger` 是否压 fog 以“已揭示空地危险区必须可读”为准，第一版优先保证 danger 在 fog 之上

### 5. minimap 必须消费同一套 fog truth

当前 minimap 是全图直出。  
本 change 中，minimap 必须与 world fog truth 对齐：

- `!IsRevealed && TerrainKind != Empty` 统一显示为 fog 色
- 只在已揭示格上展示硬度/边界差异
- 设施、玩家、机器人仍可继续作为顶层像素覆盖

### 6. 默认资源通过程序化 fallback 与默认 pipeline 双路径接入

数据配置方式保持与现有 dual-grid terrain 相同的双路径：

- 默认资源：`MinebotPixelArtAssetPipeline` 生成 near/deep 各 16 张 fog sprite + tile asset，并回填默认 art set
- fallback：运行时可在资源缺失时程序化生成 near/deep 两套 fog tiles

开发顺序明确为：

1. 新增 near/deep fog fallback 纹理/Tile 生成
2. 接入 art set / assets / default pipeline
3. 在 scene graph 中新增 `DG Fog Deep Tilemap` / `DG Fog Near Tilemap`
4. 实现 near/deep fog refresh 与 dirty cache
5. 同步 minimap
6. 补齐 EditMode / PlayMode 回归

## Risks / Trade-offs

- [边界墙也进入 fog，可能让地图外框读感更弱] → 先按 `TerrainKind != Empty` 实现；如果试玩反馈要求边界常显，再单独把 boundary 从 mask 中排除。
- [marker / scan 压在 fog 之上可能泄露更深层信息] → 第一版优先保证反馈可用；若实测泄露感过强，再把 marker 限制到前沿墙，而不是先牺牲反馈层可读性。
- [单格 reveal 会引发周围一圈 band 状态变化，dirty refresh 范围扩大] → 用 near/deep cache 精确记录变更 world-cell，再只刷新受影响的 dual-grid display cells。
- [新增双 fog tilemap 后会调整既有排序] → 用 PlayMode 场景回归覆盖 terrain / danger / marker / build preview / scan 的共存顺序。

## Migration Plan

1. 生成默认 near/deep fog sprite 与 tile 资源。
2. 给默认 `MinebotPresentationArtSet` 回填 near/deep fog dual-grid tiles。
3. 在 runtime scene 装配里新增 `DG Fog Deep Tilemap` / `DG Fog Near Tilemap`，并在 `TilemapGridPresentation` 接入 fog band 刷新。
4. 更新 minimap 逻辑。
5. 跑 Unity 编译、目标 EditMode 与 PlayMode 测试。

如果需要回滚：

- 可以移除 `DG Fog Deep Tilemap` / `DG Fog Near Tilemap` 装配
- `MinebotPresentationAssets` 回退到没有 fog tiles 的旧路径
- 迷雾资源缺失时运行时保持无 fog，但 terrain/overlay 不应被破坏

## Open Questions

- 第一版是否需要让 `Danger Tilemap` 明确高于 fog，还是仅对已揭示空地可见的 danger 进行绘制。这一项在实现时以现有危险区可读性优先。
