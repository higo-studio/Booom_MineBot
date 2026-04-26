# Minebot 像素风资源生成记录

## 目录规范

- `Assets/Art/Minebot/Generated/Prompts/`：image2 prompt、生成批次说明和筛选记录。
- `Assets/Art/Minebot/Generated/SourceSheets/`：image2 原始输出，不直接被运行时引用。
- `Assets/Art/Minebot/Generated/Selected/`：从源图中筛选出的候选资源或整合预览。
- `Assets/Art/Minebot/Sprites/Tiles/`：最终消费的地形、覆盖层和设施 PNG。
- `Assets/Art/Minebot/Sprites/Actors/`：最终消费的主机器人和从属机器人 PNG。
- `Assets/Art/Minebot/Sprites/UI/`：后续 HUD 或图标资源。
- `Assets/Art/Minebot/Tiles/`：Unity Tile 资产。
- `Assets/Art/Minebot/Palettes/`：Tile Palette 或调色板说明。
- `Assets/Art/Minebot/Presets/`：导入设置或资源配置辅助资产。
- `Assets/Art/Minebot/Docs/`：本文件和资源管理说明。

## 命名规则

- Tile PNG：`tile_<semantic>.png`，例如 `tile_wall_soil.png`。
- Actor PNG：`actor_<semantic>.png`，例如 `actor_player_minebot.png`。
- Unity Tile：`Tile_<Semantic>.asset`，例如 `Tile_WallSoil.asset`。
- 美术配置：`MinebotPresentationArtSet_Default.asset`。
- 源图：`minebot_pixel_sheet_<batch>.png`。
- 角色优化源图：`minebot_actor_optimized_sheet_<batch>.png`。

## 当前运行时资源清单

说明：下面这批资源仍然保留在项目里，供当前可运行版本消费；其中单格墙 tile 和单格 danger overlay 已降级为过渡资产，后续会逐步被 contour family 替代。

| 语义 | 最终 PNG | Unity Tile / Sprite 用途 |
| --- | --- | --- |
| 空地 | `tile_floor_cave.png` | `TerrainKind.Empty` |
| 土层墙 | `tile_wall_soil.png` | `MineableWall + Soil` |
| 石层墙 | `tile_wall_stone.png` | `MineableWall + Stone` |
| 硬岩墙 | `tile_wall_hard_rock.png` | `MineableWall + HardRock` |
| 极硬岩墙 | `tile_wall_ultra_hard.png` | `MineableWall + UltraHard` |
| 不可破坏边界 | `tile_boundary.png` | `TerrainKind.Indestructible` |
| 危险覆盖 | `tile_overlay_danger.png` | 地震危险区 |
| 标记 | `tile_overlay_marker.png` | 玩家疑似炸药标记 |
| 探测提示 | `tile_hint_scan.png` | 探测中心提示 |
| 合法建造预览 | `tile_build_preview_valid.png` | `BuildPreview` valid |
| 非法建造预览 | `tile_build_preview_invalid.png` | `BuildPreview` invalid |
| wall contour family | `tile_wall_contour_00.png` - `tile_wall_contour_15.png` | `Wall Contour Tilemap` |
| danger contour family | `tile_danger_contour_00.png` - `tile_danger_contour_15.png` | `Danger Contour Tilemap` |
| hardness detail family | `tile_detail_soil.png` / `tile_detail_stone.png` / `tile_detail_hard_rock.png` / `tile_detail_ultra_hard.png` | world-grid detail / 资源台账 |
| 维修站 | `tile_facility_repair_station.png` | 维修站 |
| 机器人工厂 | `tile_facility_robot_factory.png` | 机器人工厂 |
| 主机器人 | `actor_player_minebot.png` | 玩家 Sprite |
| 从属机器人 | `actor_helper_robot.png` | 从属机器人 Sprite |

## contour family 目标结构

当前 change 的正式目标不再是“每种硬度一张完整墙 tile”，而是以下组合：

| 资源族 | 目标 | 备注 |
| --- | --- | --- |
| wall contour atlas | 15/16 形态 dual-grid 轮廓 | half-cell offset，服务 `Wall Contour Tilemap` |
| danger contour overlay | 15/16 形态危险边界轮廓 | 与 wall contour 共享 2x2 拓扑，但保持独立视觉语义 |
| hardness detail | `Soil` / `Stone` / `HardRock` / `UltraHard` world-grid detail | 继续服务运行时硬度可读性，不重复绘制四套 contour |
| terrain base | floor / boundary | world-grid 对齐 |
| overlays | marker / build preview valid / build preview invalid / scan hint | 与 danger contour 语义分离 |
| facilities / actors | repair station / robot factory / player / helper robot | 保持既有风格统一 |

## contour family Prompt 模板

### A. wall contour atlas

```text
Use case: stylized-concept
Asset type: pixel art contour atlas for a Unity top-down dual-grid tilemap
Primary request: Create a 4x4 contour atlas for BOOOM Minebot mine walls. The atlas must support 15/16 marching-squares style states for half-cell offset rendering over a square world grid.
Subject: rounded mine wall contour pieces only, not full standalone terrain tiles.
Required states: empty, full solid, four outer corners, four edges, four inner corners, two diagonal split states.
Style: crisp pixel art, underground mining robot game, earthy palette, readable at 1x, no anti-aliased blur, no text, no watermark.
Layout: evenly spaced atlas on a flat neutral background, each tile centered with padding.
Hard constraints: contour line must pass through the exact center line of each tile; openings must stay visually narrow enough to match square-grid collision.
```

### B. danger contour overlay

```text
Use case: stylized-concept
Asset type: pixel art contour atlas for a Unity danger overlay
Primary request: Create a 4x4 danger contour atlas for BOOOM Minebot that matches the wall contour topology but reads as a hazardous earthquake boundary, not as rock.
Subject: glowing or high-contrast danger edge pieces for 15/16 marching-squares states.
Required states: empty, full solid placeholder, four outer corners, four edges, four inner corners, two diagonal split states.
Style: crisp pixel art, limited warm warning palette, readable over dark cave floor, no text, no watermark.
Layout: evenly spaced atlas on a flat dark background, each tile separated with padding.
Hard constraints: keep the topology identical to wall contour usage; do not draw full-cell red fills as the main signal; danger contour must remain visually distinct from invalid build preview.
```

### C. hardness detail overlays

```text
Use case: stylized-concept
Asset type: pixel art terrain detail set for a Unity top-down tilemap
Primary request: Create four world-grid wall detail tiles for BOOOM Minebot: Soil, Stone, HardRock, UltraHard.
Subject: material texture detail only, meant to sit under a separate contour overlay.
Style: crisp pixel art, underground palette, readable hardness progression, no text, no watermark.
Layout: compact sheet or separated tiles with padding.
Hard constraints: do not redraw rounded outer silhouette; focus on cracks, grain, strata, and density differences.
```

## 筛选标准（contour family）

- 先验收拓扑完整性：15/16 状态是否成套，两个对角分离形态是否可辨。
- 轮廓中线必须与 tile 中线对齐，适合 `(-0.5, -0.5)` 偏移后的 dual-grid 显示。
- wall contour 与 danger contour 必须共享拓扑语言，但颜色、材质、线宽或发光感上能立即区分。
- danger contour 不能退回“逐格空心框”或整格红块。
- hardness detail 只能表达材料差异，不能与 contour 争夺轮廓职责。
- 非法建造预览必须保留独立视觉语言，不与 danger contour 复用同一语义。

## 旧版 Prompt 模板（归档）

```text
Use case: stylized-concept
Asset type: pixel art asset sheet for a Unity top-down tilemap game
Primary request: Create a cohesive pixel art asset sheet for BOOOM Minebot, a top-down underground mining survival game about a mining robot, hidden bombs, scanning risk, helper robots, and earthquake danger zones.
Scene/backdrop: dark underground mine, compact readable tile sprites, orthographic top-down view.
Subject: 16x16 game-ready tiles and small actor sprites.
Required assets: cave floor, soil wall, stone wall, hard rock wall, ultra hard wall, indestructible boundary wall, red danger overlay, red marker flag, blue scan hint, blue repair station, orange robot factory, yellow-blue player mining robot, green helper robot.
Style: crisp pixel art, limited earthy palette, strong silhouettes, high contrast edges, no anti-aliased blur, no text, no watermark, no UI labels.
Layout: organized asset sheet on a flat dark neutral background, each sprite separated with padding, no shadows crossing between sprites.
```

## 旧版筛选标准（归档）

- 俯视角轮廓清晰，单格语义能在 1x 缩放下辨认。
- 色彩服务玩法信息：岩壁硬度逐步变冷/变亮，设施使用蓝色/橙色区分，危险与标记使用红色体系。
- 资源可以被裁切成独立 PNG；源图可以不完美，但最终消费 PNG 必须清晰。
- 不接受带文字、水印、复杂背景、明显透视错误或强烈写实光影的输出。

## contour family 当前状态

- `Wall Contour` / `Danger Contour` 的运行时与 fallback 资源结构已经在代码中接入。
- contour family Batch 004 已生成 3 组候选源图：
  - `Assets/Art/Minebot/Generated/SourceSheets/minebot_contour_family_sheet_004_a.png`
  - `Assets/Art/Minebot/Generated/SourceSheets/minebot_contour_family_sheet_004_b.png`
  - `Assets/Art/Minebot/Generated/SourceSheets/minebot_contour_family_sheet_004_c.png`
- 已选中 `Assets/Art/Minebot/Generated/Selected/minebot_contour_family_sheet_004_selected.png` 作为当前默认消费源图。
- 最终切片、语义和层级职责记录在 `Assets/Art/Minebot/Generated/Selected/minebot-contour-family-asset-manifest.md`。
- 运行时默认 `MinebotPresentationArtSet_Default.asset` 已绑定 wall contour / danger contour / hardness detail / build preview 资源；代码生成 fallback 仅作为缺资源兜底，不再是主路径。

## 已知限制

- 第一批资源是 MVP 占位美术，不代表最终商业品质。
- 角色暂不包含方向动画，只提供静态 Sprite。
- 危险区、标记和探测提示先作为覆盖层 Tile，不做粒子或动画。
- Batch 002 已将角色替换为透明底静态 Sprite；后续如需更强操作反馈，应扩展为方向动画或工作状态动画。

## Batch 001 结果

- 主候选源图：`Assets/Art/Minebot/Generated/SourceSheets/minebot_pixel_sheet_001_a.png`
- 备选源图：`Assets/Art/Minebot/Generated/SourceSheets/minebot_pixel_sheet_001_b.png`
- 已选源图副本：`Assets/Art/Minebot/Generated/Selected/minebot_pixel_sheet_001_selected.png`

| 语义 | 最终路径 |
| --- | --- |
| 空地 | `Assets/Art/Minebot/Sprites/Tiles/tile_floor_cave.png` |
| 土层墙 | `Assets/Art/Minebot/Sprites/Tiles/tile_wall_soil.png` |
| 石层墙 | `Assets/Art/Minebot/Sprites/Tiles/tile_wall_stone.png` |
| 硬岩墙 | `Assets/Art/Minebot/Sprites/Tiles/tile_wall_hard_rock.png` |
| 极硬岩墙 | `Assets/Art/Minebot/Sprites/Tiles/tile_wall_ultra_hard.png` |
| 不可破坏边界 | `Assets/Art/Minebot/Sprites/Tiles/tile_boundary.png` |
| 危险覆盖 | `Assets/Art/Minebot/Sprites/Tiles/tile_overlay_danger.png` |
| 标记 | `Assets/Art/Minebot/Sprites/Tiles/tile_overlay_marker.png` |
| 探测提示 | `Assets/Art/Minebot/Sprites/Tiles/tile_hint_scan.png` |
| 维修站 | `Assets/Art/Minebot/Sprites/Tiles/tile_facility_repair_station.png` |
| 机器人工厂 | `Assets/Art/Minebot/Sprites/Tiles/tile_facility_robot_factory.png` |
| 主机器人 | `Assets/Art/Minebot/Sprites/Actors/actor_player_minebot.png` |
| 从属机器人 | `Assets/Art/Minebot/Sprites/Actors/actor_helper_robot.png` |

## Batch 002 角色优化结果

- 角色源图：`Assets/Art/Minebot/Generated/SourceSheets/minebot_actor_optimized_sheet_002.png`
- Prompt 记录：`Assets/Art/Minebot/Generated/Prompts/minebot-pixel-art-actor-optimization-002.md`
- 最终玩家 Sprite：`Assets/Art/Minebot/Sprites/Actors/actor_player_minebot.png`
- 最终从属机器人 Sprite：`Assets/Art/Minebot/Sprites/Actors/actor_helper_robot.png`

| 语义 | 优化内容 |
| --- | --- |
| 主机器人 | 替换为透明底 32x32 Sprite，强化黄色机身、青色头灯/面罩和钻头轮廓。 |
| 从属机器人 | 替换为透明底 32x32 Sprite，强化绿色机身、状态灯和更小的支持机器人轮廓。 |

处理方式：保留 Batch 001 的运行时路径和 ArtSet 引用，只替换同名 Actor PNG；源图使用 #00ff00 色键生成，本地切片后移除色键并输出带 alpha 的 PNG。

## Batch 004 contour family 结果

- Prompt / 批次记录：`Assets/Art/Minebot/Generated/Prompts/minebot-pixel-art-contour-family-batch-004.md`
- 候选源图：
  - `Assets/Art/Minebot/Generated/SourceSheets/minebot_contour_family_sheet_004_a.png`
  - `Assets/Art/Minebot/Generated/SourceSheets/minebot_contour_family_sheet_004_b.png`
  - `Assets/Art/Minebot/Generated/SourceSheets/minebot_contour_family_sheet_004_c.png`
- 已选源图：`Assets/Art/Minebot/Generated/Selected/minebot_contour_family_sheet_004_selected.png`
- 资源台账：`Assets/Art/Minebot/Generated/Selected/minebot-contour-family-asset-manifest.md`

| 资源族 | 最终路径 |
| --- | --- |
| wall contour atlas | `Assets/Art/Minebot/Sprites/Tiles/tile_wall_contour_00.png` - `Assets/Art/Minebot/Sprites/Tiles/tile_wall_contour_15.png` |
| danger contour atlas | `Assets/Art/Minebot/Sprites/Tiles/tile_danger_contour_00.png` - `Assets/Art/Minebot/Sprites/Tiles/tile_danger_contour_15.png` |
| hardness detail | `Assets/Art/Minebot/Sprites/Tiles/tile_detail_soil.png` / `tile_detail_stone.png` / `tile_detail_hard_rock.png` / `tile_detail_ultra_hard.png` |
| build preview | `Assets/Art/Minebot/Sprites/Tiles/tile_build_preview_valid.png` / `tile_build_preview_invalid.png` |

## Unity 资产化结果

- 默认表现配置：`Assets/Resources/Minebot/MinebotPresentationArtSet_Default.asset`
- Tile Palette prefab：`Assets/Art/Minebot/Palettes/MinebotTilePalette.prefab`

| 语义 | Tile 资产 |
| --- | --- |
| 空地 | `Assets/Art/Minebot/Tiles/Tile_FloorCave.asset` |
| 土层墙 | `Assets/Art/Minebot/Tiles/Tile_WallSoil.asset` |
| 石层墙 | `Assets/Art/Minebot/Tiles/Tile_WallStone.asset` |
| 硬岩墙 | `Assets/Art/Minebot/Tiles/Tile_WallHardRock.asset` |
| 极硬岩墙 | `Assets/Art/Minebot/Tiles/Tile_WallUltraHard.asset` |
| 不可破坏边界 | `Assets/Art/Minebot/Tiles/Tile_Boundary.asset` |
| 危险覆盖 | `Assets/Art/Minebot/Tiles/Tile_OverlayDanger.asset` |
| 标记 | `Assets/Art/Minebot/Tiles/Tile_OverlayMarker.asset` |
| 探测提示 | `Assets/Art/Minebot/Tiles/Tile_HintScan.asset` |
| wall contour atlas | `Assets/Art/Minebot/Tiles/Tile_WallContour_00.asset` - `Tile_WallContour_15.asset` |
| danger contour atlas | `Assets/Art/Minebot/Tiles/Tile_DangerContour_00.asset` - `Tile_DangerContour_15.asset` |
| build preview | `Assets/Art/Minebot/Tiles/Tile_BuildPreviewValid.asset` / `Tile_BuildPreviewInvalid.asset` |
| hardness detail | `Assets/Art/Minebot/Tiles/Tile_DetailSoil.asset` / `Tile_DetailStone.asset` / `Tile_DetailHardRock.asset` / `Tile_DetailUltraHard.asset` |
| 维修站 | `Assets/Art/Minebot/Tiles/Tile_FacilityRepairStation.asset` |
| 机器人工厂 | `Assets/Art/Minebot/Tiles/Tile_FacilityRobotFactory.asset` |

导入设置由 `Minebot/Art/Rebuild Pixel Art Assets` Editor 工具维护。Tile PNG 使用 `Texture Type = Sprite`、`Filter Mode = Point`、禁用 Mipmap、`Pixels Per Unit = 16`；Actor PNG 使用相同像素设置但 `Pixels Per Unit = 32`。
