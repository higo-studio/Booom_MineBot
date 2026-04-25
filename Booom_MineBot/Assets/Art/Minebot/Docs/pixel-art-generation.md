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

## 第一批资源清单

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
| 维修站 | `tile_facility_repair_station.png` | 维修站 |
| 机器人工厂 | `tile_facility_robot_factory.png` | 机器人工厂 |
| 主机器人 | `actor_player_minebot.png` | 玩家 Sprite |
| 从属机器人 | `actor_helper_robot.png` | 从属机器人 Sprite |

## image2 Prompt 模板

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

## 本批次筛选标准

- 俯视角轮廓清晰，单格语义能在 1x 缩放下辨认。
- 色彩服务玩法信息：岩壁硬度逐步变冷/变亮，设施使用蓝色/橙色区分，危险与标记使用红色体系。
- 资源可以被裁切成独立 PNG；源图可以不完美，但最终消费 PNG 必须清晰。
- 不接受带文字、水印、复杂背景、明显透视错误或强烈写实光影的输出。

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
| 维修站 | `Assets/Art/Minebot/Tiles/Tile_FacilityRepairStation.asset` |
| 机器人工厂 | `Assets/Art/Minebot/Tiles/Tile_FacilityRobotFactory.asset` |

导入设置由 `Minebot/Art/Rebuild Pixel Art Assets` Editor 工具维护。Tile PNG 使用 `Texture Type = Sprite`、`Filter Mode = Point`、禁用 Mipmap、`Pixels Per Unit = 16`；Actor PNG 使用相同像素设置但 `Pixels Per Unit = 32`。
