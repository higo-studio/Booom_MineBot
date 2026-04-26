# pixel-art-asset-pipeline Specification

## Purpose
TBD - created by archiving change improve-tilemap-pixel-art-assets. Update Purpose after archive.
## Requirements
### Requirement: 像素风资源必须有项目内目录规范
项目 SHALL 为 Minebot 像素风资源提供固定目录结构，用于区分 image2 源图、筛选后 PNG、Unity Tile、Tile Palette、导入 Preset 和生成记录文档。

#### Scenario: 新增一批像素资源
- **WHEN** 开发者或 Agent 生成新的 Minebot 像素资源
- **THEN** 源图、最终消费 PNG、Tile 资产和生成记录会被保存到约定目录，而不是散落在场景或临时目录中

#### Scenario: 查找某个 Tile 的来源
- **WHEN** 美术或开发者需要追踪某个 Tile 资源的来源
- **THEN** 项目内文档能够说明该资源来自哪个 image2 prompt、源图和切片结果

### Requirement: image2 生成资源必须保留提示词和筛选记录
项目 SHALL 在使用 image2 生成像素风资源时记录最终 prompt、用途、生成批次、筛选标准、源图路径和最终资产路径。

#### Scenario: 使用 image2 生成首批 Tile/Sprite
- **WHEN** 实现阶段调用 image2 生成符合地底矿洞背景的像素风资源
- **THEN** 仓库会保存可审查的 prompt 与生成记录，说明哪些输出被采用以及为什么采用

#### Scenario: 后续追加同风格资源
- **WHEN** 后续需要生成新的设施、怪物、地形或 UI 图标
- **THEN** 开发者可以复用已有 prompt 和风格说明生成一致方向的候选资源

### Requirement: 最终消费 PNG 必须使用像素风导入设置
所有进入 Minebot 最终消费资源目录的 PNG SHALL 使用适合像素风 Tilemap 的 Unity 导入设置，包括 Sprite 类型、Point Filter、禁用 Mipmap 和稳定 Pixels Per Unit。

#### Scenario: 导入地形 Tile PNG
- **WHEN** PNG 被放入 Minebot 最终消费的 Tile/Sprite 目录
- **THEN** Unity 导入设置会保持像素边缘清晰，不会因双线性过滤、mipmap 或错误 PPU 导致画面发糊

#### Scenario: 验证资源导入
- **WHEN** 开发者运行资源导入检查或打开 Inspector 审查资源
- **THEN** 能确认 Minebot 像素资源符合 Point Filter、Sprite 类型和无 Mipmap 的约束

### Requirement: 资源资产必须可被 Unity 原生工具管理
最终进入场景表现的地图资源 SHALL 以 Unity 可管理的 Sprite、Tile、Tile Palette 或 ScriptableObject 配置资产存在，而不是只由运行时代码临时生成。

#### Scenario: 美术替换某个岩壁 Tile
- **WHEN** 美术或开发者替换某个岩壁像素图
- **THEN** 可以通过更新 PNG/Tile/配置资产完成替换，而不需要修改玩法规则代码

#### Scenario: 审查项目资产
- **WHEN** 开发者在 Unity Project 窗口查看 Minebot 美术资源
- **THEN** 能看到具名的 Tile、Sprite 和配置资产，而不是只能在 Play Mode 中看到临时纹理

### Requirement: 全息反馈资源必须保留 image2 到 BMFont 的生成记录
项目 SHALL 为全息反馈相关资源保留完整生成记录，包括 image2 prompt、筛选说明、选中的 source sheet、BMFont atlas、glyph 映射关系、最终消费路径和被哪个运行时资源引用。该记录 MUST 能追溯扫描数字、标记或危险区效果来自哪一批生成结果。

#### Scenario: 生成一批新的全息字形资源
- **WHEN** 开发者或 Agent 使用 image2 生成新的 holographic glyph 或符号资源
- **THEN** 项目会记录本批次的 prompt、筛选原因、atlas 路径、BMFont 描述文件和最终采用结果

#### Scenario: 追踪扫描数字或标记资源来源
- **WHEN** 开发者需要确认某个扫描数字字形或标记效果来自哪次 image2 生成
- **THEN** 项目内记录能够定位到对应的 source sheet、glyph 映射和最终消费资产路径

### Requirement: BMFont 与全息符号资源必须作为项目正式资产导入和校验
全息反馈使用的 BMFont atlas、描述文件、符号图集和相关 Sprite/Tile 资产 SHALL 通过项目内导入流程进入 Unity，并 MUST 具备适合像素风显示的 Point Filter、禁用 Mipmap、稳定 PPU 和可被 ArtSet 引用的正式资产路径。

#### Scenario: 导入全息 BMFont atlas
- **WHEN** 一张用于扫描数字的 BMFont atlas 被放入 Minebot 的最终消费目录
- **THEN** 项目内导入流程会校验并应用像素风导入设置，并生成可被运行时引用的正式资产

#### Scenario: 审查 Presentation Art Set 的全息反馈引用
- **WHEN** 开发者检查默认的 Presentation Art Set 或其相关配置
- **THEN** 能看到扫描数字、标记、危险区或动作反馈所需的全息资产引用，而不是只能依赖隐藏的临时路径或手工拖拽状态

