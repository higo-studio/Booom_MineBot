## ADDED Requirements

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
