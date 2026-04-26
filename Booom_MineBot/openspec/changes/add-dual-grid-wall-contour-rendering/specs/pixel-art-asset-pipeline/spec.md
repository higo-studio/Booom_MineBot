## MODIFIED Requirements

### Requirement: Image2 生成的墙体资源必须优先适配 dual-grid contour atlas
项目在使用 Image2 生成墙体相关像素风资源时 SHALL 以 dual-grid wall contour atlas 作为主产物目标，而不是继续把“每种硬度各一张完整单格墙 tile”作为主要资产结构。

#### Scenario: 生成新一批墙体资源
- **WHEN** 开发者或 Agent 为 Minebot 重生墙体美术素材
- **THEN** 输出目标会优先覆盖 dual-grid 轮廓所需的 15/16 形态，而不是只生成若干彼此独立的单格墙体图

#### Scenario: 筛选 Image2 候选输出
- **WHEN** 团队从多组 Image2 输出中挑选最终资源
- **THEN** 会优先验收 contour atlas 的成套完整性、边界是否位于 tile 中线、以及是否适合半格偏移渲染

### Requirement: 墙体材质差异必须通过 world-grid detail 资源补充表达
Soil、Stone、HardRock 和 UltraHard 的美术差异 SHALL 主要由 world-grid 对齐的 detail / overlay 资源表达，而不是要求每种硬度重复绘制一整套 dual-grid 轮廓 atlas。相邻同类型岩体拼接后 MUST 优先读成连续材质面，只有暴露外缘才保留明显边界。

#### Scenario: 生成不同硬度的墙体资源
- **WHEN** 团队为四种硬度准备配套像素风资源
- **THEN** 资源会优先组织为共享 contour atlas + 各硬度 detail，而不是四套重复 contour atlas

#### Scenario: 筛选同类型岩体 detail 资源
- **WHEN** 团队筛选一组同类型岩体 detail 贴图
- **THEN** 会优先保留那些在相邻拼接后内部连接缝较弱、只在外缘需要 contour 补边的资源，而不是每格自带完整粗边框的贴图

#### Scenario: 检查最终资源目录
- **WHEN** 开发者审查进入项目的最终墙体资源
- **THEN** 能看到 dual-grid contour 资源与 hardness detail 资源分别归档，并且两者职责清晰可辨

### Requirement: 危险区资源当前必须至少包含 warning base tile
项目在生产危险区相关像素风资源时 SHALL 至少提供可按格子平铺的 warning base tile。当前运行时不再要求消费 half-cell danger contour overlay；如继续保留既有 danger contour 素材，也只作为存档或后续探索候选。

#### Scenario: 生成危险区相关资源
- **WHEN** 团队为危险区重生像素风资源
- **THEN** 输出至少会覆盖可平铺的 danger base tile，而不会把 danger contour 作为当前运行时必需交付

#### Scenario: 筛选 danger base tile
- **WHEN** 团队从多组 Image2 输出中挑选 danger base tile
- **THEN** 会优先保留那些能在地图上低透明铺底、且不会压掉空地/设施辨识的候选

### Requirement: 资源生成记录必须明确 dual-grid wall 语义
项目 SHALL 在墙体资源的 prompt、筛选说明和生成记录中明确写出这些资源服务于 half-cell offset 的 dual-grid wall contour 渲染，而不是笼统记录为“墙体 tile”。

#### Scenario: 查看某个 contour tile 的来源
- **WHEN** 开发者需要追踪某个 dual-grid 墙体轮廓 tile 的来源
- **THEN** 文档能够说明其所属的 contour atlas、目标形态和对应的 Image2 生成批次

#### Scenario: 后续追加同风格墙体资源
- **WHEN** 团队后续要补更多墙体相关资源
- **THEN** 现有记录能指导继续按 dual-grid contour 语义扩展，而不会退回旧的单格墙 tile 资产思路

#### Scenario: 记录连续岩面约束
- **WHEN** 团队记录某一批墙体 contour 与 detail 资源的 prompt 和筛选标准
- **THEN** 文档会明确写出“同类型岩体内部连续、只有暴露外缘有明显边界”的约束，而不是只记录单格资源本身是否好看
