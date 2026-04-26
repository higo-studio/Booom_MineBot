## Why

当前 `Gameplay` / `DebugSandbox` 的地形表现仍然采用“一个逻辑格直接选择一个地形 Tile”的渲染方式。这个方案与现有 `LogicalGridState`、碰撞和 Bake 管线兼容，但墙体轮廓始终是严格方格状，空地与岩壁的交界会显得生硬，也限制了后续像素风矿洞的整体观感。

本轮探索确认，Minebot 不适合直接照搬 Godot 原型里的“隐藏 world tilemap + 偏移 display tilemap”结构，因为项目已经有更高优先级的世界真相：`MapDefinition` / `LogicalGridState`。同时，玩家移动、接触判定和测试验收都仍然建立在 world-grid 方格边界上，如果把整套地形直接改成纯 dual-grid 主层，会放大“圆角视觉 vs 方格碰撞”的错位感，并打断现有测试与资源语义。

归档 spec 也已经给这轮提案设定了边界：`tilemap-art-presentation` 要继续服从 world-grid 权威与配置化美术资产，`automation-and-wave-survival` 要继续让所有下游统一读取 `IsDangerZone`，`layered-grid-feedback-overlays` 则要求危险区、标记、建造预览和扫描数字保持独立渲染所有权。当前提案的核心方向成立，但需要把这轮改动拆成两类：对 `tilemap-art-presentation` 只做增量 requirement 扩展，不改写归档 requirement；对危险区主显示从逐格内描边切到连续 contour / danger base 的部分，则明确声明为对 overlay spec 的修订，而不是误记成 `gameplay-presentation` 的新增范围。

因此更合适的方案是：保留 terrain world-grid、Bake 流程与基础地形表达不变，但把危险区从当前占位性的“origin + 半径整片覆盖”改为“空地与可挖岩壁交界处的边沿带”真相。`MineableWall` 使用 `Wall Contour` 表达更自然的矿洞边界，并把 contour 视觉明确保留给岩体；危险区只以同一份边沿带真相驱动 `LogicalGridState.IsDangerZone` 与 world-aligned 的 `Danger Base` warning tile，让玩家能从底图直接读出危险格范围，但不会额外形成角色碰撞。这样可以取消危险区描边，避免与岩体轮廓争夺视觉语言。

## What Changes

- 本 change 明确同时包含两部分交付：contour family 渲染接入，以及与之配套的 Image2 像素风素材重生与资产落盘；它不是只改代码结构、不改美术资源的技术重构。
- 保留 `MapDefinition`、`LogicalGridState`、编辑期 Bake 和 world-grid 碰撞为玩法真相，不把 dual-grid 反推成新的地形权威层。
- 借鉴参考仓库 `jess-hammer/dual-grid-tilemap-system-unity` 已验证有效的最小拓扑做法：contour layer 作为同一 `Grid Root` 下的子 Tilemap，以半格偏移承载 2x2 邻域结果；但不照搬其“隐藏 placeholder tilemap 作为真相层”的架构。
- 在现有 `Grid Root` 下新增半格偏移的 `Wall Contour` 图层，只为 `TerrainKind.MineableWall` 提供轮廓表达；`TerrainKind.Indestructible` 继续保持 world-aligned 边界表现。
- 将波次危险区真相从“danger origin + 半径整片覆盖”改为“空地一侧、与 `MineableWall` 以 4 邻接接壤并按当前波次向内扩张的边沿带”，继续统一写回 `LogicalGridState.IsDangerZone`；对角仅接触的空地不应被额外判成危险区，同时只收掉不属于出生主空地腔体的孤立安全 pocket，避免同一洞穴里被危险带切开的安全 pocket 被误判成空岛。
- 将已归档 `layered-grid-feedback-overlays` 中危险区的主显示，从“空地逐格内描边”收口为仅保留 world-aligned 的 `Danger Base` warning tile；同时继续保留标记、危险区、建造预览和扫描数字各自独立的渲染所有权。
- 保留 world-aligned 的基础地形层表达空地、不可破坏边界和硬度细节；`Wall Contour` 只承载岩体轮廓，`Danger Base` 单独消费 `IsDangerZone`，不再为危险区额外绘制 contour。
- 将当前“每个岩体方格的 base 自带完整边缘”的旧资源语义迁移为 dual-grid 方案：岩体 `base/detail` 只负责连续纹理与硬度信息，所有显著边缘统一收口到 `Wall Contour`，避免内部再次出现按格切开的描边。
- 明确同类型岩体在成片相邻时必须优先读成连续岩面，而不是每格各自一块独立砖。同硬度岩体内部连接处不得再画出强轮廓缝；但不同 `HardnessTier` 岩体的交界允许直接出现 contour，帮助玩家快速读出材质分区。
- `Wall Contour` 的刷新策略采用“一个 world cell 变化只重算周围 4 个 contour cells”的局部更新；`Danger Base` 继续随地图初始化、波次厚度变化和重大地形变化整图重算即可。
- 改造 `MinebotPresentationArtSet` / Tile 资源组织方式，从“每种语义一个单格 tile”扩展为“基础地形/detail + danger base + wall contour”的组合式配置；`BuildPreviewInvalid` 必须继续与危险区资源语义分离。
- 重新规划并生成一批适配新渲染结构的像素风源图。运行时主消费目标不再包含 danger contour，重点收口到 wall contour atlas 与 hardness detail。
- 更新 `Gameplay` / `DebugSandbox` 的装配与测试，使其验证 `Danger Base` 图层存在、墙体轮廓会随挖掘同步变化、玩家可见危险区与失败/避险/建造阻挡读取的是同一份 `IsDangerZone`，同时危险空地仍可通行，且标记/建造预览/扫描数字不会被 danger base 抢占。
- 不修改 `LogicalGridState`、`MapDefinition`、`TilemapBakeProfile` 的数据结构，也不修改方格碰撞求解算法；本变更聚焦危险区求值语义、表现层装配和美术资源管线，并明确危险区不会成为额外阻挡层。

## Capabilities

### New Capabilities

- `dual-grid-wall-contour-rendering`: 从逻辑网格生成半格偏移的墙体轮廓层，以更自然地表达空地与可挖矿壁的边界。
- `danger-zone-contour-overlay`: 从“空地与可挖岩壁交界的危险边沿带”真相生成 `IsDangerZone` mask，但当前运行时只保留 `Danger Base` warning tile，不再消费 danger contour overlay。

### Modified Capabilities

- `automation-and-wave-survival`: 地震波危险区从占位性的 origin + 半径整片覆盖，调整为沿 `MineableWall` 边沿向空地内侧扩张的危险带；玩家失败、机器人避险与其它危险区查询继续统一读取 `IsDangerZone`。
- `layered-grid-feedback-overlays`: 保留危险区、标记、建造预览和扫描数字的独立渲染所有权，但将危险区主显示从逐格内描边收口为 `Danger Base` warning tile。
- `tilemap-art-presentation`: 在现有配置化地形表现要求之上，新增“world-aligned 基础层 + contour family overlay”的组合式渲染约束，不改写归档 spec 里已有的地形/硬度区分 requirement。
- `pixel-art-asset-pipeline`: 像素风资源生产目标从单格墙体 tile 扩展为 wall contour atlas、danger warning base tile、硬度 detail 资源与配套导入配置。

## Impact

- 影响 `Assets/Scripts/Runtime/WaveSurvival` 与危险区求值入口：需要把 `IsDangerZone` 的生成从“origin + 半径整片覆盖”改为“空地-岩壁边沿带”，并重新定义波次增长对危险带厚度的作用。
- 影响 `Assets/Scripts/Runtime/Presentation`：需要为地形渲染和危险区 overlay 新增 `Danger Base` 装配，并确保 contour 视觉只保留给岩体，且 danger base 刷新不会清空标记、建造预览或扫描提示。
- 影响 `Assets/Scripts/Runtime/Automation`、`Assets/Scripts/Runtime/Progression` 及相关测试：机器人避险、建筑阻挡等继续读取 `IsDangerZone`，但预期结果将跟随新的边沿带真相变化。
- 影响 `Assets/Resources/Minebot/MinebotPresentationArtSet_Default.asset` 与相关美术配置：需要启用 danger base、wall contour、硬度 detail 与独立 invalid build preview 资源引用；已存在的 danger contour 资源不再参与运行时装配。
- 影响 `Assets/Art/Minebot/Sprites` 与 `Assets/Art/Minebot/Tiles`：需要围绕 wall contour 与 hardness detail 继续维护运行时消费资源；既有 danger contour 源图可保留存档，但不再作为当前显示 requirement。
- 影响 `Assets/Scenes/Gameplay.unity`、`Assets/Scenes/DebugSandbox.unity` 和相关测试装配：需要接入新的 contour layer，但不新增独立的玩法 capability。
- 影响 `Assets/Scripts/Tests/PlayMode/RenderedGameplayPlayModeTests.cs` 及相关 EditMode / PlayMode 测试：需要验证“危险格底图可读性”“岩体 contour 是唯一显著描边”“危险区与致死/避险/建造逻辑一致”以及“danger base 不抢占其它反馈层”。
- 影响人工流程：实现阶段需要重新生成并筛选 Image2 像素风素材，记录新的 prompt、筛选标准、切片语义和资源路径。
- 不影响编辑期 Bake 的 world-grid 语义模型；Tilemap 关卡编辑和 `MapDefinition` 输出仍保持现有路径。
