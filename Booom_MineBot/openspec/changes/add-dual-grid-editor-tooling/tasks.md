## 1. 共享配置基础

- [ ] 1.1 新增 dual-grid profile ScriptableObject，并定义 terrain family atlas、layer metadata、display offset 与 legacy dual-grid 配置分组
- [ ] 1.2 调整 `MinebotPresentationArtSet` / `MinebotPresentationAssets`，让运行时优先读取 dual-grid profile，同时保留旧字段与程序化 fallback
- [ ] 1.3 把 dual-grid 相关常量与命名约定从 `MinebotGameplayPresentation`、`DualGridTerrain` 的场景装配细节中收口到共享配置与 helper
- [ ] 1.4 为 dual-grid profile 序列化、legacy fallback 和默认字段装配补充 EditMode 测试

## 2. 共享渲染核心

- [ ] 2.1 从 `TilemapGridPresentation` 中提取 `IDualGridMaterialSource`、`IDualGridRenderTarget` 和共享 `DualGridRenderer`
- [ ] 2.2 实现 `LogicalGridMaterialSource` 与 Tilemap render target，让 `Gameplay` / `DebugSandbox` 继续沿用现有启动流程但改走共享 renderer
- [ ] 2.3 保留“单个 world cell 变化只刷新周围 4 个 display cells”的 dirty refresh 语义，并把 terrain family 写入顺序固定在共享核心内
- [ ] 2.4 更新现有 EditMode / PlayMode 回归，验证 runtime dual-grid 输出在重构后与既有行为一致

## 3. Editor 预览与迁移工具

- [ ] 3.1 实现基于 `Tilemap + TilemapBakeProfile` 的 `TilemapAuthoringMaterialSource`，供 Edit Mode dual-grid 预览复用
- [ ] 3.2 新增 `[ExecuteAlways]` 的 dual-grid preview host 与 custom inspector，在编辑器中创建 / 刷新 `DG Floor` 到 `DG Boundary` 预览图层
- [ ] 3.3 实现 dual-grid 配置迁移入口，把默认 art set 与现有已知 dual-grid 引用迁移到新 profile，并对缺失映射给出明确警告
- [ ] 3.4 增加 EditMode 测试，验证 editor preview 不会隐式改写 `MapDefinition`、初始化 runtime services 或产出与运行时漂移的 layer 结果

## 4. 默认资源接入与验证

- [ ] 4.1 更新 `MinebotPixelArtAssetPipeline`，自动生成或更新默认 dual-grid profile，并把默认 `MinebotPresentationArtSet` 回填到新结构
- [ ] 4.2 迁移当前默认 dual-grid 资源与场景引用到 profile-backed 工作流，同时保留 legacy rollback 路径
- [ ] 4.3 补充默认资源与迁移链路测试，覆盖 profile 生成、资源缺失 fallback、editor preview 校验和共享 renderer 一致性
- [ ] 4.4 使用 Unity 编译与目标 EditMode / PlayMode 测试验证 dual-grid 编辑工具、运行时 terrain 刷新与默认资源集成全部通过
