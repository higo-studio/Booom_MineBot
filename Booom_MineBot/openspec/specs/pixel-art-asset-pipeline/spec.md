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

