# Minebot 像素风资源生成记录

## 目录规范

- `Assets/Art/Minebot/Generated/Prompts/`：image2 prompt、生成批次说明和筛选记录。
- `Assets/Art/Minebot/Generated/SourceSheets/`：image2 原始输出，不直接被运行时引用。
- `Assets/Art/Minebot/Generated/Selected/`：从源图中筛选出的候选资源或整合预览。
- `Assets/Art/Minebot/Sprites/Tiles/`：最终消费的地形、覆盖层和设施 PNG。
- `Assets/Art/Minebot/Sprites/Tiles/DualGridTerrain/`：dual-grid terrain family 的 16-state PNG。
- `Assets/Art/Minebot/Sprites/Actors/`：最终消费的主机器人和从属机器人 PNG。
- `Assets/Art/Minebot/Sprites/Actors/States/`：主机器人 / 从属机器人状态序列帧。
- `Assets/Art/Minebot/Sprites/Pickups/`：金属 / 能量 / 经验掉落物图标。
- `Assets/Art/Minebot/Sprites/Effects/`：裂缝、裂墙、爆炸等 cell FX 序列帧。
- `Assets/Art/Minebot/Sprites/UI/`：后续 HUD 或图标资源。
- `Assets/Art/Minebot/Sprites/UI/HUD/`：HUD 背景、状态区图标和交互区图标。
- `Assets/Art/Minebot/Sprites/UI/Hologram/`：全息 overlay atlas、BMFont atlas 与描述文件。
- `Assets/Art/Minebot/Sprites/UI/Hologram/Glyphs/`：由 BMFont atlas 对应出的最终 digit sprite。
- `Assets/Art/Minebot/Tiles/`：Unity Tile 资产。
- `Assets/Art/Minebot/Tiles/DualGridTerrain/`：dual-grid terrain family 的 Unity Tile 资产。
- `Assets/Art/Minebot/Palettes/`：Tile Palette 或调色板说明。
- `Assets/Art/Minebot/Presets/`：导入设置或资源配置辅助资产。
- `Assets/Art/Minebot/Docs/`：本文件和资源管理说明。
- `Assets/Resources/Minebot/Presentation/`：默认 actor / pickup / cell FX prefab 与 sprite sequence 资源。

## 命名规则

- Tile PNG：`tile_<semantic>.png`，例如 `tile_wall_soil.png`。
- dual-grid Tile PNG：`tile_dg_<family>_<index>.png`，例如 `tile_dg_floor_15.png`、`tile_dg_hard_rock_08.png`。
- Actor PNG：`actor_<semantic>.png`，例如 `actor_player_minebot.png`。
- Actor state PNG：`<actor>_<state>_<frame>.png`，例如 `player_mining_1.png`、`robot_destroyed_0.png`。
- Pickup PNG：`pickup_<semantic>.png`，例如 `pickup_energy.png`。
- FX PNG：`<effect>_<frame>.png`，例如 `crack_mining_2.png`、`explosion_4.png`。
- HUD PNG：`hud_<semantic>.png`，例如 `hud_panel_background.png`、`hud_icon_warning.png`。
- Unity Tile：`Tile_<Semantic>.asset`，例如 `Tile_WallSoil.asset`。
- dual-grid Unity Tile：`Tile_DG_<Family>_<Index>.asset`，例如 `Tile_DG_Floor_15.asset`、`Tile_DG_Boundary_03.asset`。
- SpriteSequence：`<Group>_<Semantic>.asset`，例如 `Player_Mining.asset`、`Fx_WallBreak.asset`。
- Presentation prefab：`<Semantic>.prefab`，例如 `PlayerActor.prefab`、`PickupMetal.prefab`、`ExplosionFx.prefab`。
- 全息 atlas：`hologram_<semantic>.png`，例如 `hologram_overlay_atlas.png`、`hologram_bmfont_digits.png`。
- 全息 digit sprite：`hologram_digit_<n>.png`，例如 `hologram_digit_3.png`。
- BMFont 描述文件：`hologram_bmfont_digits.fnt`。
- 美术配置：`MinebotPresentationArtSet_Default.asset`。
- 源图：`minebot_pixel_sheet_<batch>.png`。
- 角色优化源图：`minebot_actor_optimized_sheet_<batch>.png`。

## 当前运行时资源清单

说明：当前运行时已经切到 `dual-grid primary terrain rendering`。默认 `MinebotPresentationArtSet_Default.asset` 会优先绑定 `6 x 16` dual-grid terrain family；若缺失则退回程序生成的同名 fallback PNG/Tile。旧的单格 wall/detail/contour 资源继续保留，作为迁移期兼容和历史批次归档。

| 语义 | 最终 PNG | Unity Tile / Sprite 用途 |
| --- | --- | --- |
| DG Floor atlas | `Sprites/Tiles/DualGridTerrain/tile_dg_floor_00.png` - `tile_dg_floor_15.png` | `DG Floor Tilemap` |
| DG Soil atlas | `Sprites/Tiles/DualGridTerrain/tile_dg_soil_00.png` - `tile_dg_soil_15.png` | `DG Soil Tilemap` |
| DG Stone atlas | `Sprites/Tiles/DualGridTerrain/tile_dg_stone_00.png` - `tile_dg_stone_15.png` | `DG Stone Tilemap` |
| DG HardRock atlas | `Sprites/Tiles/DualGridTerrain/tile_dg_hard_rock_00.png` - `tile_dg_hard_rock_15.png` | `DG HardRock Tilemap` |
| DG UltraHard atlas | `Sprites/Tiles/DualGridTerrain/tile_dg_ultra_hard_00.png` - `tile_dg_ultra_hard_15.png` | `DG UltraHard Tilemap` |
| DG Boundary atlas | `Sprites/Tiles/DualGridTerrain/tile_dg_boundary_00.png` - `tile_dg_boundary_15.png` | `DG Boundary Tilemap` |
| 危险覆盖 | `tile_overlay_danger.png` | 地震危险区 |
| 标记 | `tile_overlay_marker.png` | 玩家疑似炸药标记 |
| 探测提示 | `tile_hint_scan.png` | 探测中心提示 |
| hologram overlay atlas | `Sprites/UI/Hologram/hologram_overlay_atlas.png` | marker / danger / scan 统一风格来源 |
| BMFont atlas | `Sprites/UI/Hologram/hologram_bmfont_digits.png` | 扫描数字位图字形来源 |
| BMFont descriptor | `Sprites/UI/Hologram/hologram_bmfont_digits.fnt` | digit advance / atlas 记录 |
| digit sprites | `Sprites/UI/Hologram/Glyphs/hologram_digit_0.png` - `hologram_digit_9.png` | `BitmapGlyphFontDefinition` 运行时消费 |
| 合法建造预览 | `tile_build_preview_valid.png` | `BuildPreview` valid |
| 非法建造预览 | `tile_build_preview_invalid.png` | `BuildPreview` invalid |
| 旧单格地形 | `tile_floor_cave.png` / `tile_wall_soil.png` / `tile_wall_stone.png` / `tile_wall_hard_rock.png` / `tile_wall_ultra_hard.png` / `tile_boundary.png` | 迁移期兼容 / 旧批次归档 |
| 旧 contour family | `tile_wall_contour_00.png` - `tile_wall_contour_15.png` | 过渡 change 归档 |
| 旧 danger contour family | `tile_danger_contour_00.png` - `tile_danger_contour_15.png` | 过渡 change 归档 |
| hardness detail family | `tile_detail_soil.png` / `tile_detail_stone.png` / `tile_detail_hard_rock.png` / `tile_detail_ultra_hard.png` | 资源台账 / 旧 world-grid detail |
| 维修站 | `tile_facility_repair_station.png` | 维修站 |
| 机器人工厂 | `tile_facility_robot_factory.png` | 机器人工厂 |
| 主机器人 | `actor_player_minebot.png` | 玩家 Sprite |
| 从属机器人 | `actor_helper_robot.png` | 从属机器人 Sprite |
| 玩家状态序列 | `Sprites/Actors/States/player_*` | 玩家 prefab 状态播放 |
| 机器人状态序列 | `Sprites/Actors/States/robot_*` | 从属机器人 prefab 状态播放 |
| 掉落物图标 | `Sprites/Pickups/pickup_*` | 掉落物 prefab / HUD 图标 |
| 墙体交互 FX | `Sprites/Effects/crack_mining_*` / `wall_break_*` / `explosion_*` | cell FX prefab 时序 |
| HUD 图形资源 | `Sprites/UI/HUD/hud_*` | HUD panel 背景与标题图标 |
| Presentation prefabs | `Resources/Minebot/Presentation/**` | actor / pickup / cell FX / HUD 默认资源 |

当前风格来源说明：

- floor / wall base / boundary / danger base / marker / scan / facilities / actors / detail / build preview 已在 Batch 005 刷新到 `minebot_pixel_sheet_001_b` 风格族。
- `tile_dg_*` 当前由 Editor pipeline 基于共享 shape mask + family tint 程序生成，保证默认 art set 在缺少最终 image2 atlas 时也能稳定落盘。
- 全息反馈默认资源当前也由 Editor pipeline 程序生成并落盘到正式目录，作为 image2 / BMFont 资源到位前的可审查默认值。
- `wall contour` / `danger contour` 的最终运行时切片已不再承担 terrain 主渲染，只作为过渡 change 的归档资产保留。

## 全息反馈记录模板

- 模板文件：`Assets/Art/Minebot/Docs/holographic-feedback-record-template.md`
- 默认批次记录：`Assets/Art/Minebot/Generated/Prompts/minebot-hologram-feedback-batch-001.md`
- 默认资产台账：`Assets/Art/Minebot/Generated/Selected/minebot-hologram-asset-manifest-001.md`

要求：

- 记录 prompt、筛选原因、atlas 路径、BMFont 描述文件、glyph 映射和 ArtSet 引用关系。
- danger / marker / scan 三类全息资源应共用同一批次说明，避免风格链路分叉。

## Prefab Gameplay Art 记录模板

- 模板文件：`Assets/Art/Minebot/Docs/prefab-gameplay-art-record-template.md`
- 默认批次记录：`Assets/Art/Minebot/Generated/Prompts/minebot-prefab-gameplay-art-batch-001.md`
- 默认资产台账：`Assets/Art/Minebot/Generated/Selected/minebot-prefab-gameplay-art-manifest-001.md`

要求：

- 记录 actor state frame、pickup icon、cell FX frame、HUD 图形资源的 prompt 和筛选理由。
- 明确每一组资源最终落到哪个 `SpriteSequenceAsset`、哪个 prefab、哪个 HUD panel/icon。
- 墙体交互 FX 必须单独记录“连续岩体内部不重新造边”的筛选结论。

## dual-grid terrain family 目标结构

当前 change 的正式目标是“world grid 不再直接绘制 terrain，offset dual-grid family 成为唯一 terrain 主显示”。

| 资源族 | 目标 | 备注 |
| --- | --- | --- |
| floor dual-grid atlas | `Floor` family 的 16-state atlas | half-cell offset，服务 `DG Floor Tilemap` |
| hardness dual-grid atlas | `Soil` / `Stone` / `HardRock` / `UltraHard` 各自 16-state atlas | 混合硬度通过多层 family 叠加表达 |
| boundary dual-grid atlas | `Boundary` family 的 16-state atlas | 覆盖不可破坏边界和外框语义 |
| overlays | marker / build preview valid / build preview invalid / scan hint | 与 danger contour 语义分离 |
| facilities / actors | repair station / robot factory / player / helper robot | 保持既有风格统一 |

默认命名与目录约定：

- PNG：`Assets/Art/Minebot/Sprites/Tiles/DualGridTerrain/tile_dg_<family>_<index>.png`
- Tile：`Assets/Art/Minebot/Tiles/DualGridTerrain/Tile_DG_<Family>_<Index>.asset`
- art set 字段：
  - `floorDualGridTiles`
  - `soilDualGridTiles`
  - `stoneDualGridTiles`
  - `hardRockDualGridTiles`
  - `ultraHardDualGridTiles`
  - `boundaryDualGridTiles`

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
- 角色目前仍是占位式序列帧，不含方向朝向分支。
- 墙体交互 FX 目前是程序生成的首版时序资源，后续可替换为筛选后的 image2 批次。
- 危险区、标记和探测提示先作为覆盖层 Tile，不做独立粒子系统。
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

## Batch 005 风格统一刷新

- Prompt / 批次记录：`Assets/Art/Minebot/Generated/Prompts/minebot-style-refresh-batch-005.md`
- 主源图：`Assets/Art/Minebot/Generated/SourceSheets/minebot_pixel_sheet_005_a.png`
- 主 selected：`Assets/Art/Minebot/Generated/Selected/minebot_pixel_sheet_005_selected.png`
- contour 源图：`Assets/Art/Minebot/Generated/SourceSheets/minebot_contour_family_sheet_005_a.png`
- contour selected：`Assets/Art/Minebot/Generated/Selected/minebot_contour_family_sheet_005_selected.png`
- actor 源图：`Assets/Art/Minebot/Generated/SourceSheets/minebot_actor_optimized_sheet_005.png`
- 资源台账：`Assets/Art/Minebot/Generated/Selected/minebot-style-refresh-asset-manifest-005.md`

| 资源族 | 本轮处理 |
| --- | --- |
| floor / wall base / boundary | 使用 `minebot_pixel_sheet_005_selected.png` 重新切片，统一到 `001_b` 风格且 wall base 改为连续材质读感 |
| danger base / marker / scan | 使用 `minebot_pixel_sheet_005_selected.png` 重新切片 |
| facilities | 使用 `minebot_pixel_sheet_005_selected.png` 重新切片，保持蓝色维修站 / 橙色工厂语言 |
| actors | 使用 `minebot_actor_optimized_sheet_005.png` 重新切片，输出新的玩家与从属机器人 |
| detail / build preview | 使用 `minebot_contour_family_sheet_005_selected.png` 重新切片 |
| wall contour / danger contour | 为避免 dual-grid 索引回归，本轮不直接替换最终切片，继续沿用 Batch 004 |

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
