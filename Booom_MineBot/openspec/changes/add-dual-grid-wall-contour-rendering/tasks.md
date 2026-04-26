> 注：根据最新方案，运行时已取消 `Danger Contour` 显示。下方涉及 danger contour 的已完成任务保留为历史产物记录，但当前验收以 `Danger Base` + `Wall Contour` 为准。

## 1. 方案与资源语义定稿

- [x] 1.1 明确运行时 contour family 图层职责：`Terrain Base`、`Danger Base`、`Wall Contour`、`Danger Contour`、`Facility`、`Marker`、`BuildPreview`、`ScanIndicator`。
- [x] 1.2 明确危险区玩法真相从“origin + 半径整片覆盖”改为“空地与 `MineableWall` 接壤的边沿带”，并定义波次增长如何映射为危险带厚度。
- [x] 1.3 明确 world-solid dual-grid 只覆盖 `MineableWall`，`DangerZone` 通过独立 contour family 表现，`Indestructible` 继续使用 world-aligned 边界资源且不参与危险前沿。
- [x] 1.4 明确 `MinebotPresentationArtSet` 的目标配置结构：基础地形、danger base、wall contour、danger contour、hardness detail、独立 invalid build preview、facility、actor。
- [x] 1.4.1 明确 OpenSpec 边界：`tilemap-art-presentation` 在本 change 中只新增 requirement，补充 base/detail 与 contour family 的分层职责，不改写归档 spec 里已有的地形/硬度 requirement。
- [x] 1.4.2 明确并记录迁移约束：当前“每个岩体方格 base 自带完整边缘”的旧资源语义必须退役，岩体 base/detail 只保留连续纹理与硬度信息，显著边缘统一迁移到 `Wall Contour`。
- [x] 1.5 明确本 change 会修改已归档 `layered-grid-feedback-overlays` 的危险区主显示约束：从逐格内描边升级为 `Danger Base` + 连续 contour 边界，但保留标记、建造预览和扫描数字的独立渲染所有权。
- [x] 1.6 参考 `jess-hammer/dual-grid-tilemap-system-unity` 明确 contour layer 的 shared Grid 坐标约定：base/detail/danger base 保持 `(0,0,0)`，wall/danger contour 作为子 Tilemap 使用 `(-0.5,-0.5,0)` 偏移。
- [x] 1.7 明确只借参考仓库的 offset / lookup / 局部刷新思路，不引入隐藏 placeholder truth Tilemap，也不把 `RuleTile` 作为第一版依赖。

## 2. Image2 素材重生规范

- [x] 2.0 将“wall contour + danger contour 配套素材重生”标记为本 change 的正式实现范围，而不是可选美术跟进项。
- [x] 2.1 重写 `pixel-art-generation.md` 中的 prompt 模板，明确主产物是 wall contour atlas 与 danger contour overlay，而不是每种硬度各一张完整墙体单格 tile。
- [x] 2.2 为 wall contour atlas 定义目标形态清单：四外角、四边、四内角、全实心、两个对角分离形态和 empty。
- [x] 2.3 为 danger warning base + contour overlay 定义目标资源清单与视觉约束：共享 15/16 拓扑语言、保持危险边界语义、不再使用逐格空心框，并让危险格本身有低透明 base tile 可读性。
- [x] 2.4 为 hardness detail 定义目标资源清单：`Soil`、`Stone`、`HardRock`、`UltraHard` 的 world-grid detail / overlay 资源。
- [x] 2.5 约束 Image2 筛选标准：先验收 wall / danger contour 的成套完整性、边界落在 tile 中线、圆角清晰且不误导通道宽度，再验收材质细节；同类型岩体内部不得出现重复描边缝，并确保危险区与非法建造预览语义分离。

## 3. dual-grid 轮廓资源生成与整理

- [x] 3.1 调用 Image2 生成 2-3 组围绕 contour family 的像素风源图，记录最终 prompt 与批次说明。
- [x] 3.2 从源图中筛选一套最适合 15/16 形态 wall contour atlas 的轮廓资源。
- [x] 3.3 从源图中筛选一套最适合 danger contour overlay 的轮廓资源，并切片为最终消费 PNG / Unity Tile。
- [x] 3.4 为 floor、boundary、facility、marker、build preview、actor 保持风格统一，同时确保 danger base / contour 与 invalid build preview 语义不混淆。

## 4. hardness detail 与配置接入准备

- [x] 4.1 生成或整理 `Soil`、`Stone`、`HardRock`、`UltraHard` 的 detail 资源，不重复绘制完整轮廓，并保证同类型相邻岩体拼接后优先读成连续纹理面。
- [x] 4.2 更新默认 `MinebotPresentationArtSet` 的资源规划，使 danger base、wall contour、danger contour、hardness detail 与独立 invalid build preview 资源可以组合使用。
- [x] 4.3 记录每个最终资源的语义、源图路径、切片路径和所属层级职责。
- [x] 4.4 将当前带四边边缘的 `MineableWall` base 资源替换或改造成可无缝拼接的 base/detail 资源，并确认内部拼接时不会再出现每格一圈的完整边框。

## 5. 运行时渲染与验收准备

- [x] 5.1 在表现层设计中明确共享 contour 解析规则：wall mask / danger mask 的 2x2 邻域都映射到项目自管的 4-bit contour index，而不是依赖 `RuleTile`。
- [x] 5.2 明确 wall contour 的局部刷新粒度：一个 world cell 变化只重算周围 4 个 contour cells，不退回固定区域全量刷新。
- [x] 5.3 明确 `IsDangerZone` 的刷新触发：波次危险带厚度变化、墙体被挖开或炸开导致边沿变化，以及地图初始化装配；danger base / contour 第一版允许随 danger truth 整图重算。
- [x] 5.4 明确玩家失败、机器人避险、建造阻挡继续统一读取边沿带 `IsDangerZone`，不再与玩家可见 danger base / contour 分叉。
- [x] 5.5 更新 EditMode / PlayMode / 手动烟测验收项，覆盖“base/detail 正确、同类型岩体内部连续、wall contour 只在暴露外缘显著、danger base + contour 同步、波次或挖掘后边界变化、危险带与失败/避险逻辑一致、marker/build preview/scan indicator 不互相清层、碰撞仍按方格”。
- [x] 5.5.1 更新运行时装配与验收断言，显式验证“岩体 base 不再自带四边边缘，内部连续纹理由 base/detail 表达，暴露外缘只由 wall contour 表达”。
- [x] 5.6 运行 `openspec validate add-dual-grid-wall-contour-rendering`，确认提案工件有效。
