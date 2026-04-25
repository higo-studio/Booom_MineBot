# Minebot Pixel Art Batch 001

## Final Prompt

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

## Selected Output

- 主候选源图：`Assets/Art/Minebot/Generated/SourceSheets/minebot_pixel_sheet_001_a.png`
- 备选源图：`Assets/Art/Minebot/Generated/SourceSheets/minebot_pixel_sheet_001_b.png`
- 已选源图副本：`Assets/Art/Minebot/Generated/Selected/minebot_pixel_sheet_001_selected.png`

## Selection Notes

- 采用 `minebot_pixel_sheet_001_a.png` 作为主候选，因为它覆盖了全部第一批资源清单，且 Tile、设施和角色之间的像素风格更统一。
- `minebot_pixel_sheet_001_b.png` 保留为同风格备选源图，本轮不直接切片。
- 最终消费 PNG 从主候选裁切并规格化：地形/设施/覆盖层为 16x16，角色为 32x32。
- 抽查项：极硬岩墙、机器人工厂和主机器人在 1x 尺寸下轮廓可辨；未发现文字、水印或明显非矿洞题材元素。
