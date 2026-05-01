## Why

当前 Minebot 的默认表现层虽然已经有一条 editor 资源产线，但运行时仍保留了大量“资源缺失时现场造图”的兜底路径：

- `MinebotPresentationAssets.Create(null)` 会直接生成默认 Tile、Sprite、BitmapGlyph 和 contour 资源
- `MinebotPresentationArtSet` / `DualGridTerrainProfile` 会在 dual-grid family 或 fog 资源缺失时运行时补齐
- minimap 仍保留 `Texture2D` 逐像素绘制路径，即使当前功能已停用

这和现在的目标相冲突。用户要求取消所有 runtime 计算的纹理算法，把默认占位和 dual-grid/fog 资源全部离线化成项目资产。运行时只负责读取资源、装配表现和在资源缺失时显式报错，不再承担生成 `Texture2D` / `Sprite` / `Tile` 的职责。

## What Changes

- 新增一个独立 change，把 Minebot 表现层的默认占位资源、dual-grid terrain/fog 资源、danger contour/outline、bitmap glyph、HUD 基础贴图等统一收口到 editor 离线资产流水线。
- 移除运行时对程序化 `Texture2D`、`Sprite.Create`、`ScriptableObject.CreateInstance<Tile>` fallback 的依赖；默认 `MinebotPresentationArtSet` 与默认 `DualGridTerrainProfile` 改为必备离线资源入口。
- 当运行时缺失默认表现资源或关键 tile family 时，不再自动生成临时纹理，而是返回明确错误/警告并暴露缺失项，避免“看起来能跑，实际资源链路已断”。
- 把仍然需要保留的图形生成算法迁到 `Editor` 资源产线，只在离线生成 PNG/Tile/ScriptableObject 资产时使用。
- 更新测试与校验链路，使其围绕“默认资源存在且可加载”进行验证，而不是继续依赖 `Create(null)` 的运行时 fallback。

## Capabilities

### New Capabilities

- `offline-bake-presentation-assets`: 表现层默认纹理、Tile、Sprite 和 dual-grid/fog 占位资源必须在编辑期生成并落为项目资产，运行时只读取这些资产。

### Modified Capabilities

- `pixel-art-asset-pipeline`: 资源流水线从“生成正式资源 + 运行时可程序化兜底”调整为“生成正式资源 + 运行时禁止程序化贴图 fallback”。
- `tilemap-art-presentation`: 地图、覆盖层和相关表现资源改为强依赖离线配置资产；资源缺失时必须显式暴露问题，而不是在 Play Mode 中悄悄造临时纹理。

## Impact

- 影响 `Assets/Scripts/Runtime/Presentation`：需要删除或迁移 runtime 纹理生成算法，改造 art set / profile 的 fallback 解析逻辑。
- 影响 `Assets/Scripts/Editor`：需要承接 dual-grid terrain/fog、danger outline/contour、bitmap glyph、默认 tile/sprite 等离线生成与回填职责。
- 影响 `Assets/Resources/Minebot` 与 `Assets/Art/Minebot`：默认表现资源必须完整落盘，并保持可追溯、可校验。
- 影响 `Assets/Scripts/Tests/EditMode` 与 `Assets/Scripts/Tests/PlayMode`：需要改写原先依赖 runtime fallback 的断言，改为验证默认资源加载、缺失资源报错和 editor 资源产线。
- 不改变 `LogicalGridState`、规则服务、碰撞、挖掘、危险区真相或地图生成逻辑；本 change 只调整表现资源的生成与装载边界。
