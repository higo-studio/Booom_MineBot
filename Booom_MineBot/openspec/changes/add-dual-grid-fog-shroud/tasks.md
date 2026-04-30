## 1. Fog 资源与配置

- [x] 1.1 新增 near/deep fog dual-grid fallback 生成逻辑，并为默认资源生成两套 16 张 fog sprite / tile 资产
- [x] 1.2 扩展 `MinebotPresentationArtSet` / `MinebotPresentationAssets`，让 near/deep fog dual-grid tiles 支持配置化读取与程序化 fallback
- [x] 1.3 更新默认 art set / asset pipeline 回填 near/deep fog 资源，并验证资源缺失时仍可回退

## 2. 运行时迷雾层

- [x] 2.1 在 `MinebotGameplayPresentation` / `TilemapGridPresentation` 中新增独立 `DG Fog Near Tilemap` / `DG Fog Deep Tilemap` 装配与排序
- [x] 2.2 基于 `!IsRevealed && TerrainKind != Empty` 实现 near/deep fog band 的 dual-grid 刷新，并在单格变化时正确前推 1 格亮边带
- [x] 2.3 更新 minimap，使未揭示非空地格不再直接泄露完整地形类别

## 3. 回归验证

- [x] 3.1 增加 EditMode 测试，覆盖 near/deep fog band、局部 dirty refresh、默认资源接入与塌方后重新起雾
- [x] 3.2 增加 PlayMode / 场景回归，覆盖 `Gameplay` 中 near/deep fog tilemap 存在且与挖掘/扫描/标记共存
- [ ] 3.3 使用 Unity 编译与目标 EditMode / PlayMode 测试验证 dual-grid fog shroud 全链路通过
