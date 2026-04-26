## Why

当前 `Gameplay` / `DebugSandbox` 的地形表现仍然采用“一个逻辑格直接选择一个地形 Tile”的渲染方式。这个方案与现有 `LogicalGridState`、碰撞和 Bake 管线兼容，但墙体轮廓始终是严格方格状，空地与岩壁的交界会显得生硬，也限制了后续像素风矿洞的整体观感。

本轮探索确认，Minebot 不适合直接照搬 Godot 原型里的“隐藏 world tilemap + 偏移 display tilemap”结构，因为项目已经有更高优先级的世界真相：`MapDefinition` / `LogicalGridState`。同时，玩家移动、接触判定和测试验收都仍然建立在 world-grid 方格边界上，如果把整套地形直接改成纯 dual-grid 主层，会放大“圆角视觉 vs 方格碰撞”的错位感，并打断现有测试与资源语义。

因此更合适的方案是：保留 terrain world-grid、Bake 流程与基础地形表达不变，但把危险区从当前占位性的“origin + 半径整片覆盖”改为“空地与可挖岩壁交界处的边沿带”真相。`MineableWall` 使用 `Wall Contour` 表达更自然的矿洞边界；危险区则以同一份边沿带真相同时驱动 `LogicalGridState.IsDangerZone` 与独立的 `Danger Contour` overlay，让玩家看到的红边就是会致死、会阻止建造、会让机器人避让的那一圈区域。两者共享 contour 解析语言，但不共享资源职责；同时重做一批配套像素风素材，让资源组织与新图层职责对齐。

## What Changes

- 本 change 明确同时包含两部分交付：contour family 渲染接入，以及与之配套的 Image2 像素风素材重生与资产落盘；它不是只改代码结构、不改美术资源的技术重构。
- 新增一套“terrain 仍保持 world-grid 真相、危险区改为边沿带真相、contour 负责把这份真相清晰画出来”的运行时地形/覆盖层方案。
- 在现有 `Grid Root` 下新增 `Wall Contour Tilemap`（或等价命名）的偏移渲染层，位置相对 world grid 偏移半格，用 dual-grid 规则从 `LogicalGridState` 推导 `MineableWall` 的轮廓。
- 将波次危险区真相从“若干 danger origin 的曼哈顿半径整片空地”改为“空地一侧、与 `MineableWall` 接壤并按当前波次向内扩张的边沿带”，继续写回 `LogicalGridState.IsDangerZone`。
- 同时新增 `Danger Contour Tilemap`（或等价命名）的偏移渲染层，基于新的 `IsDangerZone` 边沿带 mask 提取连续危险边界，取代当前“每个危险格一个框”的主显示方式。
- 保留 world-aligned 的基础地形层，继续负责空地、不可破坏边界和硬度细节表达；`Wall Contour` 只承载 `MineableWall` 的轮廓，`Danger Contour` 则只消费 `IsDangerZone` mask，不再把危险区作为逐格 overlay 语义硬塞进地形层。
- 第一版 dual-grid 只覆盖 `TerrainKind.MineableWall` 的轮廓；`TerrainKind.Indestructible` 仍保持 world-aligned 边界表现，避免地图外框被误读为可挖的自然圆角矿壁。
- 改造 `MinebotPresentationArtSet` / Tile 资源组织方式，从“每种地形或覆盖语义一个单格 tile”扩展为“基础地形/detail 资源 + wall contour 资源 + danger contour 资源”的组合式配置。
- 重新规划并生成一批适配新渲染结构的 Image2 像素风源图。生成目标不再是“每种硬度各一张完整墙体单格 tile”作为主资产，而是优先围绕 contour atlas 组织，并将最终项目消费资源切分为：
  - 空地底图
  - `MineableWall` 的 dual-grid 轮廓 tileset（15/16 形态 atlas，供 `Wall Contour Tilemap` 使用）
  - `DangerZone` 的 contour overlay 资源（与墙体共享解析框架，但保持独立视觉语义）
  - Soil / Stone / HardRock / UltraHard 的硬度 detail 纹理或叠层 tile
  - Boundary、Facility、Marker、BuildPreview、Actor 资源
- 危险区与非法建造预览不再共用同一套 `DangerTile` 语义；`BuildPreviewInvalid` 必须拥有独立视觉语言。
- 更新 `Gameplay` / `DebugSandbox` 的渲染装配与测试，使其验证：
  - `Wall Contour` 与 `Danger Contour` 图层存在且可刷新
  - 墙体被挖开后，周围墙体轮廓与危险边沿都会同步变化
  - 危险区会显示为连续 contour 边界，而不是每格一个框
  - 玩家失败、机器人避险和建造阻挡读取的危险区，与玩家可见的红色边沿带保持同形
  - 标记/危险区/建造预览仍保有各自独立层级和视觉语义
  - 玩家碰撞、接触和移动规则仍以 world-grid 方格为准
- 不修改 `LogicalGridState`、`MapDefinition`、`TilemapBakeProfile` 的数据结构，也不修改碰撞体求解或自由移动规则；本变更会调整波次危险区求值语义，并同步升级表现层与美术资源管线。

## Capabilities

### New Capabilities

- `dual-grid-wall-contour-rendering`: 从逻辑网格生成半格偏移的墙体轮廓层，以更自然地表达空地与可挖矿壁的边界。
- `danger-zone-contour-overlay`: 从“空地与可挖岩壁交界的危险边沿带”真相生成 `IsDangerZone` mask 与连续 contour overlay，以边界而非逐格框线表达波次危险区域。

### Modified Capabilities

- `automation-and-wave-survival`: 地震波危险区从占位性的 origin + 半径整片覆盖，调整为沿 `MineableWall` 边沿向空地内侧扩张的危险带；玩家失败、机器人避险与其它危险区查询继续统一读取 `IsDangerZone`。
- `tilemap-art-presentation`: 地形与关键覆盖层表现从“单格语义 tile”升级为“world-aligned 基础层 + contour family overlay”的组合式渲染。
- `pixel-art-asset-pipeline`: 像素风资源生产目标从单格墙体 tile 扩展为 wall contour atlas、danger contour overlay、硬度 detail 资源与配套导入配置。
- `gameplay-presentation`: `Gameplay` / `DebugSandbox` 需要装配新的 contour layer，并在刷新路径中维护其与现有反馈层的关系。

## Impact

- 影响 `Assets/Scripts/Runtime/WaveSurvival` 与危险区求值入口：需要把 `IsDangerZone` 的生成从“origin + 半径整片覆盖”改为“空地-岩壁边沿带”，并重新定义波次增长对危险带厚度的作用。
- 影响 `Assets/Scripts/Runtime/Presentation`：需要为地形渲染和危险区 overlay 新增 contour 解析与单独图层装配，同时移除当前对 placeholder danger origin 的表现层依赖。
- 影响 `Assets/Scripts/Runtime/Automation`、`Assets/Scripts/Runtime/Progression` 及相关测试：机器人避险、建筑阻挡等继续读取 `IsDangerZone`，但预期结果将跟随新的边沿带真相变化。
- 影响 `Assets/Resources/Minebot/MinebotPresentationArtSet_Default.asset` 与相关美术配置：需要新增 wall contour、danger contour、硬度 detail 与独立 invalid build preview 资源引用。
- 影响 `Assets/Art/Minebot/Sprites` 与 `Assets/Art/Minebot/Tiles`：需要按 wall contour / danger contour 两类 overlay 重新生成、筛选、切片和落盘新的 Image2 源图与 Unity Tile 资产；这部分素材生产是本 change 的正式范围，不是额外附带工作。
- 影响 `Assets/Scripts/Tests/PlayMode/RenderedGameplayPlayModeTests.cs` 及相关 EditMode / PlayMode 测试：需要同时验证“危险边界可读性”和“危险边界与致死/避险/建造逻辑一致”，而不再只观察单一 `Terrain Tilemap` 或逐格 `DangerTilemap`。
- 影响人工流程：实现阶段需要重新生成并筛选 Image2 像素风素材，记录新的 prompt、筛选标准、切片语义和资源路径。
- 不影响编辑期 Bake 的 world-grid 语义模型；Tilemap 关卡编辑和 `MapDefinition` 输出仍保持现有路径。
