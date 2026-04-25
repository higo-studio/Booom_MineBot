## Why

当前 `Gameplay` / `DebugSandbox` 的方格地图、设施和角色表现仍由运行时程序化纯色 Tile/Sprite 生成，虽然可读，但不利于美术资产管理、风格统一、Tile Palette 编辑或后续替换正式资源。现在核心玩法闭环已经可运行，应把画面表现升级为可管理的像素风资产管线，并用 image2 生成一批符合“地底矿洞 + 机器人 + 扫雷风险”背景的初版资源。

## What Changes

- 新增项目内像素风资产目录规范，区分源图、切片后 Sprite、Tile 资产、Tile Palette、材质/导入 Preset 和生成提示词记录。
- 用 image2 生成首批 16x16 或 32x32 像素风 Tile/Sprite 资源，包括空地、岩壁硬度层、不可破坏边界、危险区覆盖、标记、扫描提示、维修站、机器人工厂、主机器人和从属机器人。
- 将当前 `MinebotPresentationAssets` 的运行时程序化纯色图块替换为可序列化、可复用、可审查的 Unity 资产引用。
- 建立 Tilemap 美术配置资产，使 `TilemapGridPresentation` 能按 `TerrainKind`、`HardnessTier`、危险/标记/设施状态选择不同 Tile，而不是所有岩壁共用一个色块。
- 补齐导入设置要求：像素资源使用 Point Filter、无压缩或低损压缩、稳定 Pixels Per Unit、禁用 mipmap，并提供可重复的导入验证。
- 保留 `LogicalGridState` 作为玩法真相；Tilemap 与像素资源只负责表现，不参与规则判断。
- 不引入 Addressables、第三方 Tilemap 扩展、动画系统或正式特效管线；本变更聚焦 MVP 视觉可读性和资产管理基础。

## Capabilities

### New Capabilities

- `pixel-art-asset-pipeline`: 规范 Minebot 像素风资源的生成、落盘、导入、切片、Tile 化和提示词记录流程。
- `tilemap-art-presentation`: 让 Gameplay / DebugSandbox 的 Tilemap 和实体表现从可管理的像素风资产读取，并按地形、硬度、设施、风险反馈状态展示差异化资源。

### Modified Capabilities

- 无。

## Impact

- 影响 `Assets/Scripts/Runtime/Presentation`：需要从程序化图块生成改为资产引用与配置驱动。
- 影响 `Assets/Art` 或等价新增目录：新增 image2 生成源图、处理后 PNG、Sprite、Tile、Tile Palette、导入 Preset 和提示词记录。
- 影响 `Assets/Scenes/Gameplay.unity` 与 `Assets/Scenes/DebugSandbox.unity`：需要装配新的 Tilemap 美术配置资产。
- 影响测试：PlayMode 烟测需要验证 Tilemap 仍能渲染，并确认不再依赖运行时临时纹理作为主要地图资源。
- 影响人工流程：实现阶段需要调用 image2 生成项目内可用的像素风资源，并记录最终 prompt、筛选标准和落盘路径。
