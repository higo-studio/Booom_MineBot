# Minebot contour family prompt 003

## 用途

本批次 prompt 用于当前 `add-dual-grid-wall-contour-rendering` change，对应目标是：

- `Wall Contour` 15/16 形态 atlas
- `Danger Contour` 15/16 形态 atlas
- `Soil` / `Stone` / `HardRock` / `UltraHard` 的 world-grid detail

这些资源服务于：

- `Wall Contour Tilemap`
- `Danger Contour Tilemap`
- `Terrain Base Tilemap` 上的硬度细节

而不是继续生成“每种硬度一张完整单格墙体 tile”。

## 目标清单

### wall contour atlas

- 四外角
- 四边
- 四内角
- 全实心
- 两个对角分离形态
- empty

### danger contour overlay

- 与 wall contour 完全相同的拓扑清单
- 视觉语义必须是危险边界，不是岩壁
- 不得回退成逐格空心框

### hardness detail

- `Soil`
- `Stone`
- `HardRock`
- `UltraHard`

## 主 prompt

```text
Create a cohesive pixel art contour-family asset set for BOOOM Minebot, a top-down underground mining survival game. The output should prioritize a dual-grid wall contour atlas, a matching but semantically distinct danger contour atlas, and four separate world-grid hardness detail tiles for Soil, Stone, HardRock, and UltraHard. Use crisp readable pixel art, earthy underground colors for rock, high-contrast warning colors for danger, no text, no watermark, no perspective distortion, and keep every contour aligned to the tile midline for half-cell offset rendering in Unity.
```

## 验收重点

- contour atlas 是否成套
- 中线是否可直接用于 half-cell offset
- wall / danger 是否拓扑一致但语义独立
- hardness detail 是否避免重画轮廓
- build preview invalid 是否仍需保持独立视觉语言

## 当前落地状态

- Prompt 已定稿
- 代码侧 contour lookup / offset / fallback 已接入
- Batch 004 已基于本 prompt 生成 3 组 contour family 候选，并选定 `minebot_contour_family_sheet_004_b.png` 作为当前默认消费源图
