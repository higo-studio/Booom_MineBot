## Context

当前 `MinebotPresentationAssets` 在运行时用 `Texture2D.SetPixel` 生成 16x16 纯色 Tile 和 Sprite。这个方案让 MVP 很快可见，但存在三个问题：

- 美术资源不在 `Assets/` 中作为可审查资产存在，无法用 Unity Project、Tile Palette 或 Inspector 管理。
- 所有岩壁只用一个 Tile，硬度、边界、危险、设施缺少稳定的像素风视觉语言。
- 后续地图编辑/Bake 管线需要真实 Tile 资产与 Tilemap Bake Profile，而不是运行时临时纹理。

本变更定位为“表现层和资产管线升级”，不改变 `LogicalGridState`、挖掘、炸药、机器人或地震波规则。image2 用于生成首批像素风资源源图，但进入项目的资源必须经过筛选、切片、导入设置和 Unity Tile 资产化。

## Goals / Non-Goals

**Goals:**

- 建立 Minebot 像素风资源目录与命名规范，便于美术、策划和 Agent 后续协作。
- 调用 image2 生成符合地底矿洞背景的像素风首批资源，并保存 prompt、模型、筛选结果和处理说明。
- 将地图 Tile、设施 Tile、覆盖层 Tile、主机器人和从属机器人 Sprite 从运行时程序化生成迁移到项目资产。
- 让 `TilemapGridPresentation` 按地形、硬度、设施、危险区、标记和探测状态选择不同 Tile。
- 保持 `Gameplay` 与 `DebugSandbox` 复用同一套美术配置资产。
- 通过 Unity 编译、PlayMode 烟测和人工视觉检查确认画面仍可读。

**Non-Goals:**

- 不制作最终商业级美术，不要求一次性完成动画、VFX、音频或完整 UI 皮肤。
- 不引入 Addressables、第三方 Tilemap 工具、第三方像素编辑器依赖或运行时资源下载。
- 不把 Tilemap、Tile 或 Sprite 作为玩法真相；它们只读取并表现运行时状态。
- 不在本变更内重做地图编辑器或 MapDefinition Bake 工具，但资产布局要为后续地图编辑服务。

## Decisions

### 1. 使用 image2 生成“源图”，项目只消费处理后的 PNG / Sprite / Tile

实现阶段调用 image2 生成 2-3 张像素风资源源图，而不是逐个生成最终 Unity Tile。推荐提示词方向：

```text
BOOOM Minebot, top-down underground mining survival game, cohesive pixel art asset sheet,
16x16 tiles, dark cave floor, soil wall, stone wall, hard rock, indestructible boundary,
red danger overlay icon, marker flag, scan hint, blue repair station, orange robot factory,
small yellow-blue mining robot, small green helper robot,
limited palette, readable silhouettes, no text, no watermark, orthographic, transparent-safe layout
```

源图进入：

```text
Assets/Art/Minebot/Generated/
  Prompts/
  SourceSheets/
  Selected/
```

处理后的项目消费资源进入：

```text
Assets/Art/Minebot/Sprites/
  Tiles/
  Actors/
  UI/
Assets/Art/Minebot/Tiles/
Assets/Art/Minebot/Palettes/
Assets/Art/Minebot/Presets/
Assets/Art/Minebot/Docs/
```

选择理由：

- image2 输出适合快速探索风格，但不保证每个格子严格 16x16、无边界污染或 Tilemap 可无缝拼接。
- 把源图和最终项目资产分层，可以允许后续手修、替换或重新切片，而不破坏运行时引用。

备选方案：直接把 image2 输出整张图作为 Tilemap 贴图。

放弃原因：无法稳定映射每个 Tile 语义，也不利于 Tile Palette、导入设置和后续美术替换。

### 2. 新增 `MinebotPresentationArtSet` 作为表现层美术配置资产

新增一个 ScriptableObject，例如：

```text
MinebotPresentationArtSet
├─ terrainTiles
│  ├─ empty
│  ├─ soilWall
│  ├─ stoneWall
│  ├─ hardRockWall
│  ├─ ultraHardWall
│  └─ boundary
├─ overlayTiles
│  ├─ danger
│  ├─ marker
│  └─ scanHint
├─ facilityTiles
│  ├─ repairStation
│  └─ robotFactory
└─ actorSprites
   ├─ player
   └─ helperRobot
```

`MinebotGameplayPresentation` 持有该配置资产引用；如果场景未配置，则使用当前程序化资源作为 fallback，避免开发期空引用导致场景不可运行。`TilemapGridPresentation` 只关心“给定状态取哪个 Tile”，不关心资源来自 image2、手绘或程序生成。

选择理由：

- 资产替换集中在一个配置点，避免 Tile 引用散落在场景和代码里。
- 保留 fallback 能降低迁移风险，后续正式美术可以逐步替换。

备选方案：直接在 `MinebotGameplayPresentation` 上序列化所有 Tile/Sprite 字段。

放弃原因：组件会变得臃肿，且难以在 `Gameplay` / `DebugSandbox` / 后续地图编辑工具间复用。

### 3. Tilemap 显示按语义分层，不按单一颜色块分层

保留现有四层 Tilemap：

```text
Terrain Tilemap   空地 / 岩壁硬度 / 不可破坏边界
Facility Tilemap  维修站 / 机器人工厂
Overlay Tilemap   标记 / 危险区 / 后续爆炸残留
Hint Tilemap      探测中心 / 后续数字提示
```

本变更要求 `TilemapGridPresentation` 至少按 `TerrainKind + HardnessTier` 显示不同地形 Tile：

- `Empty` → cave floor
- `MineableWall + Soil` → soil wall
- `MineableWall + Stone` → stone wall
- `MineableWall + HardRock` → hard rock wall
- `MineableWall + UltraHard` → ultra hard wall
- `Indestructible` → boundary wall

覆盖层优先级保持“玩家标记 > 危险区”，避免疑似炸药标记被红区覆盖掉。探测提示仍使用独立 Hint 层。

选择理由：

- 硬度是玩家决策信息，必须从画面上可区分。
- 分层可让后续替换美术时不改变规则查询。

### 4. 像素导入设置必须可验证

所有进入最终消费目录的 PNG 资源应满足：

- `Texture Type = Sprite (2D and UI)`
- `Filter Mode = Point`
- `Compression = None` 或平台可接受的低损配置
- `Generate Mip Maps = false`
- `Pixels Per Unit` 与 Tile 尺寸一致，第一版推荐 16
- 单格 Tile 推荐 16x16；如果使用 32x32，需要同时更新 PPU 和视觉校验说明

实现可以选择两种方式：

- 低成本：提供 `TextureImporter` Editor 校验工具，检查 `Assets/Art/Minebot/Sprites` 下资源。
- 更自动：提供 `AssetPostprocessor`，对指定目录自动写入导入设置。

第一版推荐先做校验工具或轻量后处理，避免过度自动化影响其它资源。

### 5. 资源生成记录必须可追溯

新增 `Assets/Art/Minebot/Docs/pixel-art-generation.md`，记录：

- image2 最终 prompt
- 生成批次、用途和筛选标准
- 源图路径与最终切片路径
- 每个最终 Tile/Sprite 的语义说明
- 已知问题，例如边缘不够无缝、角色方向暂未完整、危险区只是占位覆盖层

选择理由：

- 生成式资产如果没有 prompt 和筛选记录，后续无法一致地追加资源。
- 美术和 Agent 都需要知道哪些资源是源图、哪些是项目消费资产。

## Risks / Trade-offs

- [Risk] image2 输出不一定严格像素网格对齐。→ Mitigation: 只把 image2 当源图，最终消费资源经过裁切、缩放、Point 导入和人工检查。
- [Risk] 首批资源风格可能与后续美术不一致。→ Mitigation: 保存 prompt 与风格约束，所有 Tile 先通过单一 `MinebotPresentationArtSet` 集中替换。
- [Risk] Tilemap 资产引用缺失会导致空画面。→ Mitigation: 保留程序化 fallback，并用 PlayMode 测试覆盖基础渲染对象存在。
- [Risk] 资源目录和 Tile Palette 增加管理成本。→ Mitigation: 第一版只建立最小目录和核心 Tile 集，不做复杂变体系统。
- [Risk] 硬度 Tile 过多会增加视觉噪音。→ Mitigation: 第一版控制色相和亮度差异，保持格子轮廓和资源提示清晰。

## Migration Plan

1. 建立 `Assets/Art/Minebot/` 目录结构和生成记录文档。
2. 调用 image2 生成像素风资源源图，并筛选一套符合矿洞背景的 Tile/Sprite。
3. 将筛选资源切成最终 PNG，设置 Unity 像素导入参数并生成 Tile 资产。
4. 新增 `MinebotPresentationArtSet` 并创建默认配置资产。
5. 改造 `MinebotPresentationAssets` / `TilemapGridPresentation`，优先读取配置资产，缺失时 fallback 到程序化资源。
6. 装配 `Gameplay` 与 `DebugSandbox`，运行 Unity 编译、PlayMode 测试和人工视觉烟测。

回退策略：如果新资产或配置异常，移除场景中的 `MinebotPresentationArtSet` 引用即可回到程序化 fallback；规则服务不受影响。

## Open Questions

- 第一批资源采用 16x16 还是 32x32。建议 16x16 保持当前 PPU 和网格尺度不变。
- image2 输出是否需要透明背景。第一版建议生成在平铺资产表背景中，再本地裁切；只有角色 Sprite 确认需要透明时再做透明处理。
- 是否需要立即支持 Tile Palette 手工刷图。当前更重要的是运行时显示资产化；Tile Palette 可作为同一资源集的附带产物。
