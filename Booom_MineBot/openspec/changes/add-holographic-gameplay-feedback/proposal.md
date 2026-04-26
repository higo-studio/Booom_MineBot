## Why

当前可玩版的标记、危险区和探测数字仍分别依赖占位 tile、纯色描边和默认字体，没有形成统一的全息仪表语言；同时危险区还要兼容后续 contour change，因此这部分需要一个独立、聚焦的视觉提案来统一 overlay 风格与 BMFont 资产链路。上一轮我把操作反馈、HUD 和世界掉落也塞进了这个 change，导致它与后续 prefab 化美术升级提案职责重叠；现在需要把边界收紧，避免两个 change 同时改同一块。

## What Changes

- 统一标记、危险区和探测数字的视觉语言，改为共享“全息风味”资源，而不是继续混用占位 tile、实色描边和默认文本。
- 将探测数字与玩家标记明确收口到独立的 prefab / 图片化 overlay 表现，避免它们后续在 prefab 美术升级 change 中再次被重复定义。
- 将扫描数字与全息符号的正式产物切换为 BMFont 方案，并要求相关字形与符号通过 image2 生成和筛选，而不是只依赖运行时默认 TMP 字体。
- 扩展像素资源生产规范，把 image2 生成范围从通用图标扩展到 holographic glyph sheet、BMFont atlas、符号切片和配套生成记录。
- 调整 overlay 装配和测试基线，使 holographic overlay 与现有危险区 overlay 以及待落地的 danger contour 都能兼容，并继续保持与建造预览、地形层各自独立的渲染职责。
- 明确本 change 不再拥有挖矿/爆炸/掉落/捡起的场景反馈，也不再拥有图形化 HUD；这些职责统一转移到 `upgrade-prefab-gameplay-art`。
- 将本变更明确定位为 `add-dual-grid-wall-contour-rendering` 的后续视觉反馈层补充：若 danger contour 先落地，则全息危险区直接挂接 contour；若 contour 尚未落地，则先兼容当前危险区 overlay 形态。

## Capabilities

### New Capabilities

- 无。本变更聚焦扩展现有 overlay 与像素资源能力，不新增独立玩法 capability。

### Modified Capabilities

- `layered-grid-feedback-overlays`: 标记、危险区和探测数字必须共享统一的全息风味 overlay 语言，同时继续保持独立渲染所有权。
- `pixel-art-asset-pipeline`: image2 资源流程必须覆盖 holographic glyph、BMFont atlas、符号切片、导入配置和生成记录。

## Impact

- 影响 `Assets/Scripts/Runtime/Presentation` 下的 overlay 装配、探测数字显示、危险区/标记资源绑定与 BMFont 字形渲染路径。
- 影响 `Assets/Scripts/Editor/MinebotPixelArtAssetPipeline.cs`、`Assets/Art/Minebot/Generated`、`Assets/Art/Minebot/Sprites/UI` 与相关生成记录文档，需要把 BMFont/image2 产物纳入项目内正式目录。
- 影响 `Assets/Resources/Minebot/MinebotPresentationArtSet_Default.asset` 及其后续资源引用，新增或替换全息标记、危险区与扫描数字相关资产入口。
- 影响 `Assets/Scripts/Tests/EditMode` 与 `Assets/Scripts/Tests/PlayMode` 的验收基线，需要验证扫描数字可读、全息 overlay 不会抢占其它反馈层。
- 实现阶段需要与未归档的 `add-dual-grid-wall-contour-rendering` 协调危险区表现接入点，但不会改变当前危险区真相接口的 ownership 边界。
