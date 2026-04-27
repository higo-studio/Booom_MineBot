## Context

当前 Dual Grid terrain 的运行时链路已经存在，并且主要集中在 `Minebot.Runtime.Presentation`：

- `MinebotGameplayPresentation` 在 `Awake` / `OnEnable` 中执行 `EnsureSceneInfrastructure()`，创建 `Grid Root`、6 个 dual-grid terrain family Tilemap、危险区 / 标记 / 建造预览层，然后把这些引用交给 `TilemapGridPresentation`
- `TilemapGridPresentation` 直接持有 `LayeredBinaryTerrainResolver`、terrain material cache 和 Tilemap 写入逻辑，按 `LogicalGridState` 逐格刷新 dual-grid family layers
- `MinebotPresentationArtSet` 同时承担 terrain family atlas、legacy wall/danger contour、danger outline、detail tile 和大量非 dual-grid 资源的配置容器
- `MinebotPixelArtAssetPipeline` 会直接生成默认 dual-grid Tile 资源，并把这些引用写回 `MinebotPresentationArtSet_Default`

这条路径能满足 Play Mode 表现，但它有三个明显问题：

1. dual-grid 的采样、resolver、Tilemap 目标装配和配置入口混在运行时表现组件里，复用边界不清晰  
2. dual-grid 相关配置分散在 `MinebotPresentationArtSet`、pipeline 常量和场景装配代码里，没有一个单独可迁移、可审查、可编辑的 authoring 入口  
3. 当前 editor 侧只有 `TilemapMapDefinitionBaker` 负责从 authoring tilemap 生成 `MapDefinition`，没有一个不进 Play Mode 就能看 dual-grid 结果的预览工具

本 change 需要继续遵守项目边界：

- 玩法真相仍然来自 `MapDefinition -> LogicalGridState`，Scene / Tilemap / Editor 工具只负责 authoring 和表现装配
- 现有程序集边界保留：共享渲染核心继续放在 `Minebot.Runtime.Presentation`；Editor 专属工具放在 `Minebot.Editor`；回归测试落在 `Minebot.Tests.EditMode` 和必要的 PlayMode 测试
- 继续使用 Unity 原生 Tilemap、ScriptableObject 和轻量 MonoBehaviour；本阶段明确不引入 DOTS/ECS、Shader Graph 驱动的 terrain、RuleTile 规则表或自定义图形节点编辑器

建议目录布局如下：

- `Assets/Scripts/Runtime/Presentation/DualGrid/`
  dual-grid 共享数据结构、resolver、renderer、runtime adapter
- `Assets/Scripts/Runtime/Presentation/Authoring/`
  可被场景序列化的 dual-grid profile / preview host MonoBehaviour
- `Assets/Scripts/Editor/DualGrid/`
  迁移器、custom inspector、菜单入口、校验工具
- `Assets/Scripts/Tests/EditMode/DualGrid/`
  迁移、预览一致性、编辑器模式刷新测试

## Goals / Non-Goals

**Goals:**

- 提供一个可在 Edit Mode 使用的 Dual Grid 编辑工具，让开发者基于现有 authoring tilemap 和 bake profile 直接预览 terrain family layers。
- 抽象当前 dual-grid 渲染链路，把采样、resolver、脏区刷新和 Tilemap 写入从 `TilemapGridPresentation` 的 runtime 特化实现中拆出来，形成可被运行时和编辑器共同调用的核心。
- 引入统一的 dual-grid 配置资产，承接当前所有已知 dual-grid 相关配置，并提供对默认 art set 的迁移与兼容。
- 保持 `Gameplay` / `DebugSandbox` 的启动流程不变：Bootstrap 继续初始化服务，`MinebotGameplayPresentation` 继续装配场景，但 dual-grid terrain 的具体渲染由共享 renderer 执行。
- 保留现有“单个 world cell 变化只影响周围 4 个 display cells”的刷新语义，并把它扩展到 editor preview。

**Non-Goals:**

- 不改变 `LogicalGridState`、碰撞、挖掘、危险区、建造占位和任何玩法规则真相。
- 不在本 change 内重新设计 dual-grid 的视觉算法；当前仍然沿用已有的 `LayeredBinaryTerrainResolver` 接口与 family layering 语义。
- 不把所有 presentation 资源都拆进新资产；HUD、actor、pickup、FX 等非 dual-grid 资源仍可保留在 `MinebotPresentationArtSet`。
- 不在本 change 内引入自定义 EditorWindow 驱动的复杂节点式工作流，也不引入新的第三方编辑器框架。
- 不把 editor preview 变成新的 bake 权威层；真正的玩法输入仍然来自 bake 后的逻辑资产。

## Decisions

### 1. 新增独立的 dual-grid profile 资产，但保留 `MinebotPresentationArtSet` 作为兼容外壳

新增一个运行时可读的 `DualGridRenderProfile`（命名可在实现期微调）ScriptableObject，负责承接 dual-grid terrain 的专属配置，包括：

- family atlas：`Floor`、`Soil`、`Stone`、`HardRock`、`UltraHard`、`Boundary`
- 图层命名、sorting order、display offset
- dual-grid fallback / validation 所需参数
- 当前已知的 legacy dual-grid 相关配置分组，例如 wall contour、danger contour、danger outline 或其它仍需要保留来源记录的字段

`MinebotPresentationArtSet` 不直接被移除，而是退化为“总资源入口 + 兼容桥”：

- 新路径：优先引用 `DualGridRenderProfile`
- 旧路径：当 profile 缺失或局部字段未迁移时，继续读 legacy fields / fallback

这样做的原因是：

- 现有默认 art set、pipeline 和场景都依赖 `MinebotPresentationArtSet`
- 直接删旧字段会让默认资源、测试和场景装配一次性失稳
- dual-grid 需要独立 authoring 能力，但项目整体 presentation 资源暂时不需要一并大重构

备选方案：继续只扩充 `MinebotPresentationArtSet`。  
放弃原因：会继续把 dual-grid 专属配置和 actor / HUD / FX 混在一起，迁移与编辑体验都很差。

### 2. 把渲染链拆成“共享核心 + runtime/editor adapter”，不新增新的核心 asmdef

保留当前 asmdef 策略：

- `Minebot.Runtime.Presentation`：放共享数据模型、resolver、renderer、runtime preview host
- `Minebot.Editor`：放 custom inspector、迁移器、菜单入口、Edit Mode 触发器
- `Minebot.Tests.EditMode` / `Minebot.Tests.PlayMode`：分别验证 migration / preview parity 和 runtime 集成

本 change 不新增新的 Presentation 子 asmdef。原因是当前 dual-grid 改动仍集中在同一能力边界内，先复用现有 `Minebot.Runtime.Presentation` 与 `Minebot.Editor` 能降低 asmdef 震荡；等 dual-grid authoring 工具再扩大时，再考虑拆更细的模块。

共享核心建议抽象为：

- `IDualGridMaterialSource`
  负责按 world cell 提供 `TerrainMaterialId`
- `IDualGridRenderTarget`
  负责按 layer command 写入、清理和压缩目标 Tilemap
- `DualGridRenderer`
  负责 display cell 遍历、2x2 采样、resolver 调用和 dirty-region 更新
- `LogicalGridMaterialSource`
  运行时从 `LogicalGridState` 读取材质
- `TilemapAuthoringMaterialSource`
  编辑器通过 `Tilemap + TilemapBakeProfile` 把 authoring tilemap 映射为 dual-grid 材质

这让 `TilemapGridPresentation` 从“自己实现 dual-grid”变成“协调 overlay + 调用共享 renderer”，而 editor preview 也可以直接复用同一核心。

备选方案：继续把逻辑留在 `TilemapGridPresentation`，再额外复制一份 editor preview 路径。  
放弃原因：会形成两套 dual-grid 规则实现，后续 atlas、offset、排序和 dirty update 很容易漂移。

### 3. Edit Mode 预览采用场景宿主组件 + custom inspector，而不是先做独立窗口

第一版 editor 工具采用一个可挂在场景中的 preview host（建议 `[ExecuteAlways]`），配合 `Minebot.Editor` 中的 custom inspector / 菜单入口工作。

preview host 至少序列化：

- source terrain tilemap
- `TilemapBakeProfile`
- dual-grid profile 或 art set 引用
- target grid root 或 preview tilemap root
- refresh / rebuild 选项

工作流是：

1. 开发者在编辑场景中选择 terrain authoring tilemap 与 bake profile  
2. inspector 或菜单触发 preview refresh  
3. 工具确保 `DG Floor` 到 `DG Boundary` 这 6 个 tilemap 存在，并使用运行时相同的命名、sorting order 和 `(-0.5, -0.5, 0)` offset  
4. preview host 通过共享 renderer 生成当前 dual-grid 结果

选择宿主组件而不是独立窗口的原因：

- scene-local 的 Tilemap 引用、Prefab 覆写和层级装配更自然
- 可以直接复用 `MinebotGameplayPresentation` 现有的 layer naming 约定
- 更容易写 EditMode 集成测试

第一版入口进一步收口为三层：

```text
DualGridPreviewHost [ExecuteAlways]
  └─ CustomEditor
       ├─ Refresh Preview
       ├─ Clear Preview
       ├─ Rebuild Layers
       ├─ Solo / Mute Family
       └─ Validate Configuration

MenuItem
  ├─ Migrate Legacy Dual Grid Config
  ├─ Rebuild Default Dual Grid Assets
  └─ Validate Dual Grid Profiles
```

也就是说：

- `CustomEditor` 是第一版主入口
- `MenuItem` 负责迁移、批处理和校验
- `EditorWindow` 不是第一版核心路径，只在后续若出现跨场景批量管理需求时再考虑
- `EditorTool` 暂不作为主入口，仅在后续需要 SceneView 直接刷写、点击 inspect 或交互式调试时再引入

这样定的原因是当前问题核心是“配置、迁移、预览一致性”，不是“发明新的场景操作手势”。

备选方案：独立 `EditorWindow`。  
放弃原因：第一版需要解决的核心问题是“让现有 scene authoring 能直接看结果”，不是做新的复杂操作台。

备选方案：直接以 `EditorTool` 作为主入口。  
放弃原因：当前仓库 editor 侧几乎没有 SceneView 交互工具先例；如果第一版就把主流程建立在 `EditorTool` 上，会把问题从“dual-grid 配置与预览”扩大成“交互模式设计”，收益不成比例。

### 4. editor preview 的数据源以 authoring tilemap 为主，而不是直接要求先 bake 出 `MapDefinition`

当前项目的编辑期真相路径是：

`Terrain Tilemap + Bake Profile -> TilemapMapDefinitionBaker -> MapDefinition`

本 change 的 preview 以 authoring tilemap 为主输入，再通过 `TilemapBakeProfile` 映射到 `TerrainMaterialId`。原因是：

- 这是当前关卡编辑的第一手数据源
- 用户要的是“编辑工具”，优先诉求是改 tile 后立刻能看 dual-grid 结果
- 这样不会要求每次画图后都先跑 bake 才能看表现

与此同时，preview 仍然是表现层工具，不会取代 bake：

- 不自动写回 `MapDefinition`
- 不初始化 runtime services
- 不把 preview tilemap 当成玩法真相

preview 的场景结构也进一步固定为独立预览根，而不是复用 authoring tilemap 所在层：

```text
Terrain Authoring Tilemap
    + TilemapBakeProfile
              │
              ▼
TilemapAuthoringMaterialSource
              │
              ▼
       DualGridRenderer
              │
              ▼
     Dual Grid Preview Root
     ├─ DG Floor Tilemap
     ├─ DG Soil Tilemap
     ├─ DG Stone Tilemap
     ├─ DG HardRock Tilemap
     ├─ DG UltraHard Tilemap
     └─ DG Boundary Tilemap
```

独立 preview root 的好处是：

- 可以一键开关、清理和重建
- 不会污染 authoring tilemap 的层级语义
- 更容易在 EditMode 测试中断言“预览层不是玩法真相层”

### 5. 迁移采用“生成新 profile + 回填 art set 引用 + 保留 legacy fallback”的渐进策略

迁移顺序明确如下：

1. `MinebotPixelArtAssetPipeline` 生成默认 dual-grid profile 资产，并从现有默认资源填充 family atlas / legacy dual-grid 字段
2. `MinebotPresentationArtSet_Default` 增加对该 profile 的引用
3. `MinebotPresentationAssets.Create(...)` 改为优先读取 profile，再回退 legacy fields / 程序化 fallback
4. 新增 editor 菜单或 inspector 按钮，对其它 art set 执行同样的迁移

这样做的好处是：

- 默认资源先通
- 其它资源集可以按需迁移
- 任何一步出问题都还能退回旧字段继续显示，不会直接黑屏

### 6. dual-grid 配置采用“外部友好、内部归一”的双层结构

运行时和共享 renderer 的最终输入仍然统一收口为：

- 每个 family 一组 `Tile[16]`
- 固定的 `ComputeIndex(topLeft, topRight, bottomLeft, bottomRight)` 映射
- 固定的 family order、sorting order 和 display offset

这是为了保持和现有 dual-grid 运行时兼容，避免 renderer 直接理解更高层的编辑语义。

但 editor 配置层不直接把“6 x 16 个裸 ObjectField”暴露给用户，而是支持三种 authoring 模式：

1. `Atlas16`
   开发者拖入一张 4x4 atlas，由工具切片并映射成 16 个状态。这是推荐默认模式，最适合美术资源接入。
2. `Explicit16`
   直接逐项指定 16 个状态，用于特殊资源、修补和精确 override。
3. `Canonical6 + AutoRotate`
   只输入若干基础拓扑原型，由工具自动旋转补齐剩余状态。该模式只服务原型阶段和 fallback authoring，不作为最终美术长期形态。

Inspector 展示方式也按“语义矩阵”而不是“线性数组”设计：

- 每个状态显示 mini 2x2 mask
- 同时显示 atlas index
- 显示当前 tile 缩略图
- 支持 `Ping`、`Replace`、`Use Fallback` 和 `Fill Missing`
- 支持按 family 做 `Solo / Mute`

这样做的原因是：当前 dual-grid 资源的真实复杂度不在算法，而在“96 个状态入口如何被人类理解和维护”。

备选方案：直接暴露 `Tile[16]` 数组。  
放弃原因：虽然实现最简单，但对美术和设计同学几乎不可用，也不利于迁移和校验。

### 7. 启动场景流程保持不变，只替换 dual-grid terrain 的内部装配方式

`Bootstrap -> Gameplay` 的流程不改。`MinebotGameplayPresentation` 仍负责：

- 初始化 camera / light / roots
- 确保 grid root 和各类 tilemap layer 存在
- 在 Play Mode 下调用 `RefreshAll()`

本 change 只把 dual-grid terrain 的内部路径替换为：

`MinebotGameplayPresentation -> TilemapGridPresentation -> DualGridRenderer(shared core)`

而 Edit Mode 则新增一条平行但共享核心的路径：

`DualGridPreviewHost(ExecuteAlways) -> DualGridRenderer(shared core)`

这样可以保证场景启动逻辑不被 editor 工具反向污染。

## Risks / Trade-offs

- [Risk] 新 profile 与 legacy art set 字段短期内并存，可能出现双写后内容漂移
  → Mitigation：迁移器统一以 profile 为主、legacy 为兼容只读来源；当检测到 profile 与 legacy 不一致时，在 editor 中给出明确警告。

- [Risk] `[ExecuteAlways]` preview host 可能频繁把场景标脏，或在编译 / 资源导入时反复重建 tilemap
  → Mitigation：使用显式 refresh 入口和编辑器侧 debounce；避免在 `OnValidate` 中直接递归改资产；只在必要时增量刷新。

- [Risk] authoring tilemap 预览与 bake 后逻辑结果可能因为 `TilemapBakeProfile` 变更不同步
  → Mitigation：preview 强制依赖同一个 `TilemapBakeProfile`；增加“preview source mapping 校验”测试与 inspector 提示。

- [Risk] 不新增 asmdef 会让 `Minebot.Runtime.Presentation` 继续承载较多 dual-grid 代码
  → Mitigation：先通过子目录和清晰接口收口；等 editor tooling 稳定后再评估是否继续拆 asmdef。

- [Risk] “迁移所有已知配置”如果范围失控，可能把非 dual-grid 资源也卷入
  → Mitigation：本 change 明确只迁移 dual-grid 相关配置；actor、HUD、pickup、FX 继续留在原 art set 内。

- [Risk] `Canonical6 + AutoRotate` 会让一部分人误以为最终美术只需要 6 张图
  → Mitigation：在 inspector 和文档中明确标注该模式仅服务原型与补洞；正式资源推荐 `Atlas16` 或 `Explicit16`。

- [Risk] 预览继续使用 Tilemap 可能让未来切到 shader / mesh 的路线看起来受限
  → Mitigation：把 Tilemap 限定在 render target adapter 层；只要共享 renderer 输出的 command 结构不变，未来仍可替换目标载体。

## Migration Plan

1. 新增 dual-grid profile 资产与共享 renderer 抽象，先让运行时代码在不改行为的前提下能读取新结构。
2. 更新 `MinebotPixelArtAssetPipeline`，生成默认 dual-grid profile，并把默认 art set 回填到新结构。
3. 给 `MinebotPresentationAssets` 和 `TilemapGridPresentation` 接上 profile 优先、legacy fallback 次之的读取路径。
4. 实现 scene preview host 与 custom inspector，让编辑场景可以直接生成 / 刷新 dual-grid preview。
5. 增加 EditMode 测试：迁移结果、同输入同输出、一格变化只刷新 4 个 display cells、Edit Mode 不改写 bake 真相。
6. 最后清理场景与默认资源引用，并保留 rollback 开关：只要 art set 仍保有 legacy 字段，就可以临时退回旧读取路径。

## Open Questions

- 第一版是否需要同时支持“从 `MapDefinition` 资产直接预览”的第二种 source adapter？当前设计优先支持 authoring tilemap，因为它更符合编辑器使用场景；如果后续调试资产比现场景更多，再补 `MapDefinition` adapter 即可。
