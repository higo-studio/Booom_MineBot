## 1. 资产目录与生成准备

- [x] 1.1 创建 `Assets/Art/Minebot/` 目录结构，覆盖 `Generated/Prompts`、`Generated/SourceSheets`、`Generated/Selected`、`Sprites/Tiles`、`Sprites/Actors`、`Tiles`、`Palettes`、`Presets` 和 `Docs`。
- [x] 1.2 编写 `Assets/Art/Minebot/Docs/pixel-art-generation.md` 初版，记录资源语义、命名规则、image2 prompt 模板和筛选标准。
- [x] 1.3 明确第一批资源清单：空地、土层墙、石层墙、硬岩墙、极硬岩墙、不可破坏边界、危险覆盖、标记、探测提示、维修站、机器人工厂、主机器人、从属机器人。

## 2. image2 资源生成与处理

- [x] 2.1 调用 image2 生成 2-3 组符合地底矿洞背景的像素风资源源图，并保存源图与最终 prompt 记录。
- [x] 2.2 从 image2 输出中筛选一组风格统一、轮廓清晰、适合俯视角 Tilemap 的资源候选。
- [x] 2.3 将候选资源切片/整理为最终消费 PNG，保存到 `Sprites/Tiles` 与 `Sprites/Actors`，并避免覆盖源图。
- [x] 2.4 对最终 PNG 做人工视觉检查，确认边缘清晰、单格语义明确、无文字水印、与矿洞背景一致。

## 3. Unity 资产化与导入设置

- [x] 3.1 为 Minebot 像素 PNG 设置 Sprite、Point Filter、禁用 Mipmap、稳定 PPU 的导入配置，并提供可重复检查路径。
- [x] 3.2 从最终 PNG 创建对应 Unity Tile 资产，按地形、覆盖层和设施分组保存。
- [x] 3.3 创建或更新 Tile Palette，使美术和关卡编辑能直接查看与复用核心 Tile。
- [x] 3.4 补齐资源生成记录，写明每个 Tile/Sprite 的源图、语义和最终路径。

## 4. 表现层配置接入

- [x] 4.1 新增 `MinebotPresentationArtSet` ScriptableObject，集中引用 Terrain、Overlay、Facility 和 Actor 资源。
- [x] 4.2 创建默认 `MinebotPresentationArtSet` 资产，并填入本次生成的像素风 Tile/Sprite。
- [x] 4.3 改造 `MinebotPresentationAssets`，优先从 `MinebotPresentationArtSet` 读取资源，缺失时 fallback 到当前程序化资源。
- [x] 4.4 改造 `TilemapGridPresentation`，按 `TerrainKind + HardnessTier` 选择不同岩壁 Tile，并保持标记优先于危险区覆盖。
- [x] 4.5 在 `Gameplay` 和 `DebugSandbox` 中装配默认美术配置资产，确认两个场景复用同一套资源。

## 5. 验证与回归

- [x] 5.1 更新或新增 PlayMode 测试，验证 `Bootstrap -> Gameplay` 后 Tilemap 仍存在，且表现层使用配置化资源或 fallback 资源。
- [x] 5.2 验证挖掘、探测、标记、设施显示、机器人生成和地震危险覆盖不会因资源替换改变规则结果。
- [x] 5.3 使用 UnityMCP 运行 `unity.compile`，再运行 EditMode / PlayMode 测试。
- [x] 5.4 手动烟雾验证 `Gameplay` 与 `DebugSandbox`：画面中能区分地形硬度、设施、标记、危险区、主机器人和从属机器人。
- [x] 5.5 运行 `openspec validate improve-tilemap-pixel-art-assets`，确认提案和规格有效。
