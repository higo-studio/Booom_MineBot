## 1. OpenSpec 与范围收口

- [x] 1.1 新建 `offline-bake-presentation-assets` change，并补齐 proposal / design / tasks / specs
- [x] 1.2 明确本 change 取代现有“资源缺失时程序化 fallback”的运行时假设，改为 editor 离线资源必备
- [x] 1.3 运行 `openspec validate offline-bake-presentation-assets`

## 2. 运行时 fallback 拆除

- [x] 2.1 调整 `MinebotPresentationAssets`，移除 `CreateFallback()` 及其所有 runtime `Texture2D/Sprite/Tile` 生成逻辑
- [x] 2.2 调整 `MinebotPresentationArtSet` 与 `DualGridTerrainProfile`，移除 dual-grid / fog 的 runtime generated fallback 路径
- [x] 2.3 删除或迁移 runtime 中残留的 minimap 纹理生成代码，确保表现层程序集不再包含贴图算法

## 3. Editor 资源产线承接

- [x] 3.1 把 dual-grid terrain/fog fallback 生成器迁到 `Editor`，继续供默认 PNG/Tile 产线使用
- [x] 3.2 更新 `MinebotPixelArtAssetPipeline`，让默认 Presentation Art Set / DualGrid Profile 始终从离线资源回填完整引用
- [x] 3.3 补充 editor 校验，确保关键默认资源缺失时能被明确发现

## 4. 测试与回归

- [x] 4.1 更新 EditMode 测试，去掉对 runtime fallback 资源的依赖，改为验证默认离线资源和缺失资源诊断
- [x] 4.2 更新必要的 PlayMode 烟测，确认 `Gameplay` / `DebugSandbox` 仍能用默认离线资源启动
- [x] 4.3 使用 Unity MCP 编译校验修改后的表现层与资源产线
