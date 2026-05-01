## Context

当前仓库已经具备两条并存路径：

1. `MinebotPixelArtAssetPipeline` 与 `MinebotPrefabGameplayArtSupport` 在编辑器中生成默认 PNG、Tile、Prefab、BitmapGlyph、Dual Grid Profile 和 Art Set。
2. 运行时 `MinebotPresentationAssets`、`MinebotPresentationArtSet`、`DualGridTerrainProfile` 仍保留程序化 fallback，用于在资源缺失时动态构造默认贴图。

第一条路径已经足够覆盖默认资源生产；第二条路径带来的问题反而更大：

- Play Mode 会掩盖资源链路缺口，导致资源丢失时不容易被及时发现
- 运行时会携带大量只为 editor 产线服务的 `Texture2D`/`Sprite`/`Tile` 生成代码
- 测试会错误依赖 runtime fallback，而不是验证项目默认资源是否真的完整
- 已停用的 minimap 路径仍然保留运行时贴图生成代码，不符合“全部离线化”的目标

## Goals

- 运行时不再生成任何默认表现纹理、Sprite 或 Tile。
- 默认 `MinebotPresentationArtSet` / `DualGridTerrainProfile` / BitmapGlyph / dual-grid fog & terrain tiles 必须以离线资源形式存在。
- 编辑器资源产线继续可用，并成为唯一的默认资源生成入口。
- 资源缺失时必须显式暴露问题，便于开发者在 Editor 中修复，而不是在 Play Mode 中被临时图掩盖。

## Non-Goals

- 不重做 dual-grid 渲染算法或 scene graph。
- 不把地图生成、危险区、机器人或其它规则系统迁移到离线。
- 不要求本次把所有 prefab 或 UI 创建逻辑改成新的 authoring 系统；只要求它们依赖的默认图形资源来自离线资产。

## Design

### 1. 运行时装载链改成“只读资源，不造资源”

`MinebotGameplayPresentation` 继续优先读取场景显式配置的 `artSet`，否则从 `Resources` 加载默认 `MinebotPresentationArtSet`。但 `MinebotPresentationAssets.Create(...)` 不再构造 fallback 资源对象：

- `artSet == null` 时，尝试加载默认 art set
- 默认 art set 仍为空时，返回显式错误并生成空/缺失报告
- 不再创建临时 `Texture2D`、`Sprite`、`Tile` 或 `TextAsset`

这样运行时缺资源会尽早暴露，而不是默默降级成程序化占位。

### 2. Dual Grid family / fog 的 fallback 从 runtime 改成 editor-only bake

当前 dual-grid family 和 fog family 的“缺失自动补全”依赖：

- `DualGridTerrainFallbackTiles`
- `DualGridFogFallbackTiles`
- `DualGridTerrainProfile.allowGeneratedFallbackForMissing`
- `MinebotPresentationArtSet` 的 `generated*DualGridTiles`

改造后：

- `DualGridTerrainFallbackTiles` / `DualGridFogFallbackTiles` 迁到 `Editor` 目录，仅供离线生成 PNG/Tile 使用
- `DualGridTerrainProfile` 不再允许运行时生成 family fallback；`ResolveTiles` 只做显式字段、atlas/canonical 解析和 legacy 兼容读取
- `MinebotPresentationArtSet` 不再缓存 `generated*` tiles；dual-grid/fog 缺失时直接返回已配置资源或空数组

### 3. 默认占位资源的唯一来源是 editor 产线

`MinebotPixelArtAssetPipeline.EnsureDefaultAssets()` 继续负责生成和回填：

- Dual Grid terrain 16-state PNG 与 Tile
- Fog near/deep 16-state PNG 与 Tile
- danger contour / outline
- hologram overlay atlas
- bitmap glyph atlas / descriptor / font asset
- 默认 tile、detail tile、HUD 图标、pickup/FX/actor 相关资源

但这些算法仅存在于 `Editor` 代码中。运行时只消费生成结果：

- `Assets/Art/Minebot/**`
- `Assets/Resources/Minebot/**`

### 4. 缺资源时的行为改成显式校验与日志

由于不能再 runtime 补图，必须把失败模式从“自动生成占位”改成“显式可诊断”：

- `MinebotPresentationAssets.Create(...)` 在关键资源缺失时记录可读错误，指出缺少的 art set / profile / tile family
- `DualGridPreviewHost.ValidateConfiguration()` 和现有 profile validation 保持/强化对空 family 的检测
- EditMode 测试直接验证默认资源完整性

目标不是让资源缺失时仍然正常展示，而是让问题在 editor / CI 中被第一时间发现。

### 5. minimap 的 runtime 纹理路径彻底移除

当前 minimap 已停用，但 `MinebotGameplayPresentation` 内仍保留 `Texture2D` 创建与逐像素写入逻辑。这个 change 会删除或封存该路径，确保 runtime 程序集中不再包含此类贴图生成实现。

## Alternatives Considered

### 保留 runtime fallback，只把 Dual Grid 挪到 editor

放弃。用户要求的是“取消所有 runtime 计算的纹理算法”，只处理 dual-grid 不足以满足要求，`MinebotPresentationAssets` 仍会在 Play Mode 动态造大量默认资源。

### 运行时改成按需从 PNG bytes 动态组 Sprite/Tile

放弃。这仍然属于运行时资源合成，只是输入从颜色算法换成文件数据，不符合“离线化转成资源”的方向。

### 资源缺失时直接抛异常

部分放弃。完全抛异常会让 editor preview 和个别测试的调试体验过硬。本次优先改成明确日志与校验失败；是否进一步升级为 hard fail，可留到后续根据使用体验决定。

## Verification

- `openspec validate offline-bake-presentation-assets`
- Unity MCP 编译通过
- EditMode 测试覆盖：
  - 默认 `MinebotPresentationArtSet` / `DualGridTerrainProfile` / glyph 资源可加载
  - `MinebotPresentationAssets.Create(null)` 走默认离线资源，而不是 runtime fallback
  - profile/art set 缺失资源时返回空并给出明确诊断，而不是生成临时 Tile
- PlayMode 烟测覆盖：
  - `Gameplay` 仍能装配 terrain/fog/actor/HUD 资源
  - 默认资源存在时不依赖 runtime 纹理算法
