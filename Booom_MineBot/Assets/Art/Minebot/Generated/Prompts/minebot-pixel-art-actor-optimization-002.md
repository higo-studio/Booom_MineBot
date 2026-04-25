# Minebot Actor Optimization Batch 002

## Final Prompt

```text
Use case: stylized-concept
Asset type: pixel-art character sprite source sheet for a Unity top-down mining game
Primary request: Create a clean pixel-art source sheet with exactly two separate top-down mining robot character sprites on a perfectly flat solid #00ff00 chroma-key background for background removal.
Scene/backdrop: no scene, only a uniform #00ff00 background with generous padding around each sprite.
Subject: two compact cute industrial mine robots, viewed from top-down / slight three-quarter top-down angle. Left sprite is the player minebot: yellow-orange mining helmet, cyan visor/light, sturdy dark metal body, small drill/arm details. Right sprite is helper robot: smaller green support bot, green status light, compact treads/legs, distinct silhouette from player.
Style: crisp 16-bit pixel art, high contrast silhouette, readable at 32x32 pixels, consistent with a dark underground mine tilemap, chunky outlines, limited palette, no antialias blur.
Layout: place the two sprites side by side, each centered in its own invisible 32x32 tile cell with clear empty chroma-key padding. Keep both sprites full body and not touching.
Constraints: the background must be one perfectly uniform #00ff00 color with no shadows, gradients, texture, floor plane, lighting variation, or contact shadow. Do not use #00ff00 anywhere in the robots. No text, no watermark, no labels, no UI, no decorative border.
```

## Selected Output

- 源图：`Assets/Art/Minebot/Generated/SourceSheets/minebot_actor_optimized_sheet_002.png`
- 替换后的玩家 Sprite：`Assets/Art/Minebot/Sprites/Actors/actor_player_minebot.png`
- 替换后的从属机器人 Sprite：`Assets/Art/Minebot/Sprites/Actors/actor_helper_robot.png`

## Processing Notes

- 使用 #00ff00 色键生成源图，再通过本地切片脚本移除色键并输出透明 PNG。
- 最终 Sprite 保持 `32x32`，保留原文件名以复用 `MinebotPresentationArtSet_Default` 中的引用。
- 目标优化点是移除 Batch 001 角色暗色方块底，并提升玩家/从属机器人在 Tilemap 上的轮廓辨识度。
