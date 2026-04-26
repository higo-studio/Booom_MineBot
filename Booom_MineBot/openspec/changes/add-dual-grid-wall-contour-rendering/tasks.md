## 1. 方案与资源语义定稿

- [ ] 1.1 明确运行时 contour family 图层职责：`Terrain Base`、`Wall Contour`、`Danger Contour`、`Facility`、`Marker`、`BuildPreview`、`ScanIndicator`。
- [ ] 1.2 明确危险区玩法真相从“origin + 半径整片覆盖”改为“空地与 `MineableWall` 接壤的边沿带”，并定义波次增长如何映射为危险带厚度。
- [ ] 1.3 明确 world-solid dual-grid 只覆盖 `MineableWall`，`DangerZone` 通过独立 contour family 表现，`Indestructible` 继续使用 world-aligned 边界资源且不参与危险前沿。
- [ ] 1.4 明确 `MinebotPresentationArtSet` 的目标配置结构：基础地形、wall contour、danger contour、hardness detail、独立 invalid build preview、facility、actor。

## 2. Image2 素材重生规范

- [ ] 2.0 将“wall contour + danger contour 配套素材重生”标记为本 change 的正式实现范围，而不是可选美术跟进项。
- [ ] 2.1 重写 `pixel-art-generation.md` 中的 prompt 模板，明确主产物是 wall contour atlas 与 danger contour overlay，而不是每种硬度各一张完整墙体单格 tile。
- [ ] 2.2 为 wall contour atlas 定义目标形态清单：四外角、四边、四内角、全实心、两个对角分离形态和 empty。
- [ ] 2.3 为 danger contour overlay 定义目标资源清单与视觉约束：共享 15/16 拓扑语言、保持危险边界语义、不再使用逐格空心框。
- [ ] 2.4 为 hardness detail 定义目标资源清单：`Soil`、`Stone`、`HardRock`、`UltraHard` 的 world-grid detail / overlay 资源。
- [ ] 2.5 约束 Image2 筛选标准：先验收 wall / danger contour 的成套完整性、边界落在 tile 中线、圆角清晰且不误导通道宽度，再验收材质细节，并确保危险区与非法建造预览语义分离。

## 3. dual-grid 轮廓资源生成与整理

- [ ] 3.1 调用 Image2 生成 2-3 组围绕 contour family 的像素风源图，记录最终 prompt 与批次说明。
- [ ] 3.2 从源图中筛选一套最适合 15/16 形态 wall contour atlas 的轮廓资源。
- [ ] 3.3 从源图中筛选一套最适合 danger contour overlay 的轮廓资源，并切片为最终消费 PNG / Unity Tile。
- [ ] 3.4 为 floor、boundary、facility、marker、build preview、actor 保持风格统一，同时确保 danger contour 与 invalid build preview 语义不混淆。

## 4. hardness detail 与配置接入准备

- [ ] 4.1 生成或整理 `Soil`、`Stone`、`HardRock`、`UltraHard` 的 detail 资源，不重复绘制完整轮廓。
- [ ] 4.2 更新默认 `MinebotPresentationArtSet` 的资源规划，使 wall contour、danger contour、hardness detail 与独立 invalid build preview 资源可以组合使用。
- [ ] 4.3 记录每个最终资源的语义、源图路径、切片路径和所属层级职责。

## 5. 运行时渲染与验收准备

- [ ] 5.1 在表现层设计中明确共享 contour 解析规则：wall mask / danger mask 的 2x2 邻域都映射到 contour atlas index。
- [ ] 5.2 明确 `IsDangerZone` 的刷新触发：波次危险带厚度变化、墙体被挖开或炸开导致边沿变化，以及地图初始化装配。
- [ ] 5.3 明确玩家失败、机器人避险、建造阻挡继续统一读取边沿带 `IsDangerZone`，不再与玩家可见 danger contour 分叉。
- [ ] 5.4 更新 EditMode / PlayMode / 手动烟测验收项，覆盖“base/detail 正确、wall contour 正确、danger contour 连续、波次或挖掘后边界变化、危险带与失败/避险逻辑一致、碰撞仍按方格”。
- [ ] 5.5 运行 `openspec validate add-dual-grid-wall-contour-rendering`，确认提案工件有效。
