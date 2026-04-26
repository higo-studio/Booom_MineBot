## 1. 文档与变更边界

- [x] 1.1 新建 `add-dual-grid-primary-terrain-rendering` change，并完成 proposal / design / tasks / specs 草稿
- [x] 1.2 明确该 change 取代 `add-dual-grid-wall-contour-rendering` 的 terrain 主渲染前提，但不回滚其已落地的危险区与 overlay 规则
- [x] 1.3 运行 `openspec validate add-dual-grid-primary-terrain-rendering`

## 2. 运行时 terrain resolver 与渲染装配

- [x] 2.1 定义表现层 `TerrainMaterialId`、`CornerMaterialSample`、`RenderLayerCommand` 和 `IDualGridTerrainResolver`
- [x] 2.2 实现 `LayeredBinaryResolver`，把 2x2 world-cell 材质样本拆成多 family 的 `16-state` dual-grid 输出
- [x] 2.3 在 `MinebotGameplayPresentation` / `TilemapGridPresentation` 中引入 6 个具名 dual-grid family Tilemap，并固定 layer name、sorting order 与 `(-0.5, -0.5, 0)` offset
- [x] 2.4 实现“1 个 world cell 变化 -> 4 个 display cells 刷新”的局部失效路径
- [x] 2.5 保持 `Danger`、`Marker`、`BuildPreview`、`ScanIndicator`、设施和角色图层独立，不并入 terrain renderer
- [x] 2.6 清理或弃用旧的 world-grid terrain / wall contour 主渲染路径，但保留迁移期所需的最小兼容桥接

## 3. 资源配置与 fallback

- [x] 3.1 扩展 `MinebotPresentationArtSet` / `MinebotPresentationAssets`，支持 6 个 terrain family 的 `16-state atlas`
- [x] 3.2 为旧 `empty/wall/detail/boundary/wallContour` 字段制定迁移期兼容策略，避免默认 art set 直接失效
- [x] 3.3 设计默认 fallback 资源生成方式，使用共享 shape mask + family tint 保证 art set 缺失时仍能看到可读的 dual-grid terrain
- [x] 3.4 更新默认 Minebot art set，使其能配置 floor / 四档硬度 / boundary 的 dual-grid terrain family
- [x] 3.5 规划 image2 / 切片 / Tile 资产命名与目录，覆盖新的 family atlas 结构
- [x] 3.6 约定 PNG / Tile / art set 字段的命名规则，并补到资源文档中

## 4. 测试迁移

- [x] 4.1 为 `LayeredBinaryResolver` 增加 EditMode 单测，覆盖纯 floor、纯岩体、floor-wall 交界、不同硬度交界和 boundary 交界
- [x] 4.2 为 display cell 局部刷新增加 EditMode 回归，验证单格变化只影响 4 个 display cells
- [x] 4.3 更新 PlayMode 烟测，断言 6 个 dual-grid family Tilemap 的存在、命名、offset 与 sorting order
- [x] 4.4 更新 PlayMode 烟测，改为断言 dual-grid family Tilemap 的显示结果，而不是 world-grid terrain tile
- [x] 4.5 增加“更换 terrain art / resolver 不改变碰撞、挖掘、危险区和建筑判定”的回归

## 5. 场景与人工验收

- [x] 5.1 更新 `Gameplay` / `DebugSandbox` 的 terrain 主层装配与排序
- [x] 5.2 在默认地图和人工构造的 mixed-material 小图上截图验收 floor、不同硬度岩体和 boundary 的交界显示
- [x] 5.3 确认 overlays 在新 terrain 主层之上仍然可读，且不会被 terrain 刷新路径误清空
