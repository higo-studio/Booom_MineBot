# Minebot Style Refresh Batch 005

## 风格目标

- 统一风格锚点：`Assets/Art/Minebot/Generated/SourceSheets/minebot_pixel_sheet_001_b.png`
- 本批次目标：把当前运行时可见的岩体 base、detail、设施、角色和 build preview 重新拉回 `001_b` 的世界观语言。
- 约束：`MineableWall` 已迁移到 `detail + wall contour` 结构，因此新的 wall base 只保留连续材质感，不再绘制每格四边重边框。

## 使用的 Prompt

### A. contour family source

```text
Use case: stylized-concept
Asset type: pixel art redraw of an existing Unity contour atlas
Primary request: Redraw the contour family atlas using two reference images already shown in this conversation. Use minebot_contour_family_sheet_004_selected as the exact topology and layout reference, and use minebot_pixel_sheet_001_b as the dominant style reference.
Input images: reference 1 = exact contour atlas layout and state topology to preserve; reference 2 = target visual style for rocks, facilities, robot world, palette, outline, and shading.
Scene/backdrop: flat dark charcoal background exactly like a source sheet, no extra scene elements.
Subject: one organized pixel-art atlas with the same overall structure and ordering as the contour-family reference sheet.
Required preservation: keep the same cell layout, same number of cells, same left-to-right top-to-bottom ordering, same blank cells, and the same semantic groups: wall contour states 00-15, danger contour states 00-15, four hardness detail tiles, two build preview tiles.
Style target: match minebot_pixel_sheet_001_b with chunky top-down cave rocks, clear pixel clusters, thick dark outlines, crisp highlights, restrained earthy palette, and readable 1x silhouettes.
Wall contour instructions: preserve the exact topology language from the contour-family reference, but redraw the pieces as cave rock edges that feel like they belong to the walls from minebot_pixel_sheet_001_b. The pieces must still read as exposed dual-grid edges placed over continuous base textures.
Danger contour instructions: preserve the exact topology from the contour-family reference, but redraw them as warm warning edges in a style that still fits the 001_b sheet. Keep them clearly distinct from invalid build preview.
Detail instructions: four seamless detail tiles only, no framed border, no per-cell edge ring, with materials matching 001_b world art: soil, stone, hard rock, ultra hard.
Build preview instructions: preserve the two preview cells, but render them in the same visual family as 001_b and keep valid vs invalid immediately readable.
Hard constraints: no text, no watermark, no anti-aliased blur, no extra sprites, no rearrangement, no missing states.
```

### B. master sheet

```text
Use case: stylized-concept
Asset type: pixel art master asset sheet for a Unity top-down mining game
Primary request: Redraw the Minebot master asset sheet using the reference image already shown in this conversation, minebot_pixel_sheet_001_b, as the dominant style and rough layout reference. Keep the same visual family, readability, scale, and top-down chunky pixel-art look, but refresh the content for the current art direction where rock bases pair with a separate dual-grid contour layer.
Input images: reference 1 = minebot_pixel_sheet_001_b as the visual style and layout guide.
Scene/backdrop: flat dark neutral background source sheet, no scenery, no labels.
Subject: one cohesive source sheet containing these assets in an organized layout with generous spacing: cave floor tile, soil wall base tile, stone wall base tile, hard rock wall base tile, ultra hard wall base tile, indestructible boundary wall tile, danger warning base tile, red marker flag tile, cyan scan hint tile, blue repair station tile, orange robot factory tile, yellow player mining robot sprite, green helper robot sprite.
Wall base tile direction: unlike old framed wall tiles, the four wall base tiles should read as continuous material textures with only subtle rim cues. They are meant to sit under separate dual-grid contour overlays, so do not draw a heavy per-cell border on every side. Same-type neighboring walls should feel seamless.
Style: match minebot_pixel_sheet_001_b closely: crisp pixel clusters, thick dark outlines, rich but restrained underground palette, metallic facilities, cute functional robots, readable at 1x, no anti-aliased blur, no text, no watermark.
Specific art direction: floor should be dark bluish cave ground; soil earthy brown; stone light gray; hard rock dark slate; ultra hard purple crystal-rock; boundary should feel reinforced and indestructible; danger base should be a warning tile that works under a separate contour edge; marker is a small red planted flag; scan hint is a cyan holographic pulse; repair station is blue steel; robot factory is orange industrial; player robot is sturdy yellow with drill; helper robot is smaller green support bot.
Hard constraints: keep the whole sheet coherent as one asset family, organized like a source sheet, and make sure robots and facilities clearly belong to the same world as the terrain.
```

### C. actor optimization sheet

```text
Use case: stylized-concept
Asset type: pixel art actor source sheet for transparent sprite extraction
Primary request: Create an actor optimization sheet for BOOOM Minebot using the minebot_pixel_sheet_001_b reference already shown in this conversation as the dominant style reference.
Scene/backdrop: a perfectly flat solid #00ff00 chroma-key background for background removal. The background must be exactly one uniform color with no gradients, no texture, no shadow, no floor plane, and no lighting variation.
Subject: two front-facing robot sprites, centered and well separated with generous padding: 1) the player mining robot, sturdy yellow industrial body, cyan visor, drill arm, tracked or heavy wheel base; 2) the helper robot, smaller green support body, simpler face/visor, compact tread or leg base.
Style: crisp top-down/front-leaning pixel art that matches the world and robot language of minebot_pixel_sheet_001_b, thick dark outlines, metallic highlights, readable at 1x, no anti-aliased blur, no text, no watermark.
Hard constraints: no #00ff00 inside the robots, no cast shadow, no contact shadow, no reflection, no extra props, no buildings, no text. Keep both robots fully visible with clean silhouettes and enough empty background around them for clean chroma removal.
```

## 落地结果

- 主 source sheet：`Assets/Art/Minebot/Generated/SourceSheets/minebot_pixel_sheet_005_a.png`
- 主 selected sheet：`Assets/Art/Minebot/Generated/Selected/minebot_pixel_sheet_005_selected.png`
- contour source sheet：`Assets/Art/Minebot/Generated/SourceSheets/minebot_contour_family_sheet_005_a.png`
- contour selected sheet：`Assets/Art/Minebot/Generated/Selected/minebot_contour_family_sheet_005_selected.png`
- actor source sheet：`Assets/Art/Minebot/Generated/SourceSheets/minebot_actor_optimized_sheet_005.png`
- actor alpha 中间件：`Assets/Art/Minebot/Generated/Selected/minebot_actor_optimized_sheet_005_alpha.png`

## 处理说明

- source sheet 使用内置 `image_gen` 生成。
- 当前仓库环境缺少 Pillow，因此 actor 色键去除没有走 skill 自带脚本，而是通过本地切片脚本内建色键规则完成。
- 为避免 dual-grid 轮廓索引错位，`tile_wall_contour_00-15` 与 `tile_danger_contour_00-15` 本轮未直接替换，仍继续使用 Batch 004 的最终切片。
- 本轮已经替换：
  - floor / wall base / boundary / danger base / marker / scan
  - repair station / robot factory
  - player / helper actor
  - detail soil / stone / hard rock / ultra hard
  - build preview valid / invalid
