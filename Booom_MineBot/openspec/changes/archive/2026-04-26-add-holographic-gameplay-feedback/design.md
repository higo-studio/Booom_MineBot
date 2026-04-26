## Context

当前 `MinebotGameplayPresentation` 已经负责主玩法的大部分表现装配：它会在 `Bootstrap -> Gameplay` 与 `DebugSandbox` 场景流中解析 `MinebotPresentationArtSet`、创建 Tilemap/Grid Root，并把探测结果交给 `ScanIndicatorPresenter`。但当前 overlay 体系仍有三个明显限制：

- 标记、危险区和探测数字没有共享统一的视觉语言。
- 扫描数字依赖 `TextMeshPro + MinebotHudFontUtility` 的默认字体路径，无法形成用户要求的 BMFont 全息字形风格。
- `MinebotPixelArtAssetPipeline` 当前只管理通用 Sprite/Tile 导入，还没有覆盖 holographic glyph、BMFont atlas 或对应的生成记录。

同时，仓库里还存在两个相关 active change：

- `add-dual-grid-wall-contour-rendering`：会把危险区主显示逐步从逐格内描边转向 contour，本 change 必须兼容两种几何而不改变 `LogicalGridState.IsDangerZone` 的真相 ownership。
- `upgrade-prefab-gameplay-art`：拥有角色 prefab、资源掉落、墙体特效与图形化 HUD。本 change 不再重复拥有这些职责，只负责 overlay 风格、BMFont 和相关导入链。

本变更继续采用项目既有技术边界：

- 引擎与栈：Unity `6000.0.59f2`、URP、Input System、UGUI、Tilemap、TextMeshPro、Unity Test Framework。
- 不采用：DOTS/ECS、VFX Graph、Shader Graph 驱动的复杂后处理、第三方字体/特效框架、多玩家同步。
- 启动流程保持 `Bootstrap -> Gameplay`，`DebugSandbox` 继续复用同一套表现层装配。

目录布局与 ownership 维持现有模块，不新增跨域程序集：

- `Assets/Scripts/Runtime/Presentation`: overlay 接入、扫描数字渲染、标记/危险区风格装配。
- `Assets/Scripts/Editor`: image2/BMFont 导入、资源校验、ArtSet 更新。
- `Assets/Scripts/Tests/EditMode` / `PlayMode`: 资源导入、overlay 渲染与叠层回归测试。
- `Assets/Art/Minebot/Generated` / `Sprites/UI` / `Docs`: holographic source sheet、筛选结果、BMFont atlas 与生成记录。
- `Assets/Resources/Minebot`: `MinebotPresentationArtSet` 及最终消费资源引用。

asmdef 策略保持现状：

- 运行时 overlay 实现继续留在 `Minebot.Runtime.Presentation`。
- 编辑器导入与资产生成继续留在 `Minebot.Editor`。
- 不让纯规则服务为了这次全息风格升级反向依赖表现层。

数据配置方式保持 ScriptableObject 主导：

- `MinebotPresentationArtSet` 继续作为表现资产总入口，扩展 holographic overlay 与 BMFont 资源引用。
- image2 产物、BMFont 描述文件与 glyph 映射通过 Editor 导入流程转成 Unity 可管理资产，再由 ArtSet 绑定。

## Goals / Non-Goals

**Goals:**

- 让标记、危险区和探测数字统一到同一套全息风味视觉语言下，并保持可读性与层级独立性。
- 让扫描数字在正式全息风格下使用 BMFont 生成的位图字形，而不是继续依赖 OS/TMP 动态字体。
- 扩展 image2 资源流程，使 holographic glyph、BMFont atlas、符号片段和生成记录成为项目内正式资产。
- 兼容当前危险区 overlay 与后续 danger contour，避免本变更卡住未归档的 contour change。
- 保持 `Bootstrap`、`Gameplay`、`DebugSandbox` 的装配路径和 `LogicalGridState` 真相模型不变。

**Non-Goals:**

- 不拥有挖矿、爆炸、掉落、捡起的场景瞬时反馈；这些统一交给 `upgrade-prefab-gameplay-art`。
- 不拥有图形化 HUD；HUD 升级统一交给 `upgrade-prefab-gameplay-art`。
- 不引入 VFX Graph、复杂粒子系统或全屏后处理来表现全息效果。
- 不重做地形 contour 几何，只消费当前已有或后续落地的危险区几何表现入口。
- 不把反馈系统扩展成完整音频系统或剧情演出系统。

## Decisions

### 1. 继续以 `MinebotPresentationArtSet` 作为全息 overlay 的单一配置入口

运行时不会从固定路径硬编码读取纹理或字体，而是继续通过 `MinebotPresentationArtSet` 提供：

- 标记、危险区、建造预览相关 tile / contour 资源
- 扫描数字使用的 BMFont/bitmap glyph 定义
- 反馈偏移、尺寸、排序层级等可调参数

这样可以让 `MinebotGameplayPresentation` 与 `ScanIndicatorPresenter` 从同一份数据配置读取资源，并让 `Gameplay` 与 `DebugSandbox` 自动共享结果。

备选方案：直接在 presenter 中写死资源路径，或继续依赖 `MinebotPresentationAssets.CreateFallback()` 生成临时色块。

放弃原因：

- 无法追踪 image2/BMFont 资产来源。
- 很难在 EditMode 测试里验证正式资源是否接好。
- 会继续扩大“开发占位资源”和“正式资源”之间的偏差。

### 2. 扫描数字改为“项目内 bitmap glyph renderer”，不再依赖默认 TMP 世界文本

`ScanIndicatorPresenter` 当前直接生成 `TextMeshPro` 物体并调用 `MinebotHudFontUtility.GetDefaultFontAsset()`。这条路径不满足“BMFont 字体、需要调用 image2 生成”的要求，因此本变更采用分层策略：

- 场景内的扫描数字与可能复用的全息符号，改为读取 BMFont 描述文件和 atlas 的 bitmap glyph renderer。
- HUD 中文文本不在本 change 内处理，避免与图形化 HUD 升级重叠。

实现上可以是：

- 一个 `BitmapGlyphFontDefinition`（命名可在实现期调整）的 ScriptableObject，保存 glyph map、sprite atlas、advance/kerning 等 BMFont 数据。
- `ScanIndicatorPresenter` 改为生成数字 glyph sprite，而不是创建 TMP 文本。

备选方案：继续使用 TMP 动态字体，或把 BMFont 先转回常规 TTF/OTF 流程。

放弃原因：

- 继续使用动态字体无法稳定复现 image2 生成的位图风格。
- 回退到常规矢量字体会丢失用户明确要求的 BMFont 资产链路。

### 3. 保持 overlay ownership 分离，几何与风格解耦

标记、危险区、建造预览和扫描数字不会被合并到单一 tilemap。保持以下所有权：

- 标记 / 危险区 / 建造预览：继续由各自 tilemap 或 contour layer 持有
- 扫描数字：单独的 glyph root

危险区的“几何形状”继续由当前激活的 danger overlay 实现决定：

- 若 contour change 已落地，则风格资源直接驱动 danger contour
- 若 contour 尚未落地，则先驱动现有 danger outline / tile overlay

也就是说，本变更只统一风格语言，不重新定义危险区几何。

备选方案：把扫描数字、marker 和 danger 全部烘进一个 hologram tilemap。

放弃原因：

- 会破坏现有 `layered-grid-feedback-overlays` 的独立渲染所有权要求。
- 扫描数字天然不适合与静态 tile overlay 共享刷新生命周期。

### 4. 扩展 `MinebotPixelArtAssetPipeline`，把 image2 与 BMFont 产物纳入正式导入链

像素资源管线需要从“单张 Sprite/Tile 导入”升级为“hologram 套件导入”。推荐目录与职责如下：

```text
Assets/Art/Minebot/Generated/Prompts/         image2 prompt 与筛选说明
Assets/Art/Minebot/Generated/SourceSheets/    image2 原始候选图
Assets/Art/Minebot/Generated/Selected/        选中的 hologram source sheet / atlas
Assets/Art/Minebot/Sprites/UI/Hologram/       最终消费的 glyph atlas、symbol atlas
Assets/Art/Minebot/Docs/                      BMFont 映射、批次记录、采用说明
Assets/Resources/Minebot/...                  ArtSet 与最终运行时引用
```

`MinebotPixelArtAssetPipeline` 负责：

- 校验 hologram atlas 与 BMFont atlas 的 Point Filter、无 Mipmap、稳定 PPU
- 生成或更新 glyph/tile 资产与 ArtSet 绑定
- 在 EditMode 中提供“资源已接线”的可验证入口

备选方案：由实现者手工导入 BMFont、手工改 ArtSet。

放弃原因：

- 容易丢失 image2 生成批次与 glyph 映射记录。
- 难以在多人/多轮迭代中保持风格一致与路径稳定。

## Risks / Trade-offs

- [未归档 contour change 可能先后顺序不确定] → 风格层只依赖危险区表现适配接口，不假设 danger 必定已经切到 contour。
- [如果 prefab 美术升级 change 也修改扫描/标记，会重新产生重叠] → 本 change 只拥有 overlay 风格、BMFont 和相关导入链；prefab change 只需消费这里定义的资源入口。
- [image2 输出可能风格漂移，导致 marker/danger/scan 不像一套资源] → 把 prompt、筛选标准、glyph 映射和最终采用批次写入项目文档，并要求三类资源共享同一批次或同一风格说明。
- [不引入 VFX Graph 会限制动画复杂度] → 采用 sprite/glyph 位移、透明度和缩放等轻量变化，先满足 GameJam MVP 的读感提升。

## Migration Plan

1. 先扩展 `MinebotPresentationArtSet` 与 Editor 资源导入能力，接入 hologram atlas 与 BMFont 资源引用。
2. 再替换 `ScanIndicatorPresenter` 的数字渲染路径，让 `Gameplay` / `DebugSandbox` 都能读取 bitmap glyph。
3. 随后接入标记与危险区的统一全息风格资源，并打通与 contour / outline 的几何适配。
4. 最后更新 PlayMode / EditMode 测试与资源记录文档，确保 overlay ownership、资源路径和导入设置全部受检。

回滚策略：

- 如果 BMFont 或 hologram atlas 验收失败，可以先保留新的资源导入路径，再把扫描数字临时回退到现有 TMP 显示，不影响玩法真相。
- 如果 danger contour 尚未可用，则危险区继续挂接当前 outline/tile overlay，只保留风格资产升级。

## Open Questions

- BMFont 的 Unity 最终消费形态是直接生成 glyph sprite 资产，还是先导入 descriptor 再转成项目内 ScriptableObject；目前倾向后者，便于测试与 ArtSet 引用。
