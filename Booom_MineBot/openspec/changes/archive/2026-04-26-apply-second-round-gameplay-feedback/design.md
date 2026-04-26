## Context

第一轮反馈已经把主玩法切到自由移动、自动贴墙挖掘、`Q/E/R` 模式化输入和建筑 prefab 装配，但第二轮反馈指出当前实现还保留了几处明显偏差：

- `GameSessionService.Scan(GridPosition origin)` 仍只返回单个 `bombCount`，`MinebotGameplayPresentation` 只记录一次 `scanOrigin`，`TilemapGridPresentation` 也只会在单个格子上放一个 `ScanHintTile`，这与“给符合条件的岩壁逐个显示 3x3 炸弹数”不一致。
- `TilemapGridPresentation` 目前把标记和危险区混在一个 `OverlayTilemap` 里，并通过 `if / else if` 二选一渲染，虽然没有直接改 TerrainTilemap 数据，但视觉上仍是整格覆盖，不满足“标记独立一层、危险区为空地内描边”的要求。
- `MinebotGameplayPresentation.EnsureCircleCollider()` 仍把玩家碰撞体半径硬编码为 `0.34f`，缺少和角色视觉尺寸、格子通道宽度、贴墙接触体验对齐的数据调节入口。

当前启动链路仍是：

1. `BootstrapSceneLoader` 初始化 `MinebotServices` 并切到 `Gameplay`。
2. `SceneRenderBootstrap` 保底创建相机。
3. `MinebotGameplayPresentation` 在场景里生成 Tilemap 层、玩家/机器人表现、建筑 prefab 和 HUD。
4. 表现层每帧从 `RuntimeServiceRegistry` 读取逻辑网格、波次、玩家状态并刷新可视反馈。

本次设计只修正第二轮反馈涉及的探测、叠层反馈和碰撞体，不改第一轮已经确定的输入模式、建筑模式和机器人自动模式边界。

## Goals / Non-Goals

**Goals:**

- 将探测改成“对玩家附近的可探测岩壁批量显示数字”，而不是对玩家当前位置显示单点提示。
- 让标记、危险区、建造预览和探测数字各自拥有稳定的渲染所有权，不再互相覆盖或争抢同一 Tilemap。
- 让危险区表现为空地图块上的内描边，并且随波次提升粗细档位。
- 把玩家碰撞体尺寸从硬编码改为数据驱动，使贴墙挖掘、通道通过和阻挡体验可调且可测。
- 保持逻辑网格、炸药真相、地震危险区和建筑占位仍由运行时规则服务负责，Scene/Tilemap/Text 只负责表现。

**Non-Goals:**

- 不改 `Q/E/R` 输入语义，不新增新的交互模式。
- 不重写炸药生成、爆炸传播、建筑占位或机器人目标选择逻辑。
- 不引入 DOTS/ECS、Shader Graph 特效、第三方寻路或新的 UI 框架。
- 不要求这一轮同步产出正式美术；缺失资源时允许先用占位 tile / 文本样式验证行为。

## Decisions

### 1. 探测改为“前沿岩壁批量探测”，规则仍放在 HazardInference

`HazardService` 不再只暴露“以某个 origin 统计邻格炸药数量”的单值接口。本轮新增扫描快照模型，例如：

```text
ScanReading
├─ wallPosition
├─ bombCount
└─ anchorWorldOffset / 或由表现层统一换算
```

以及一个“收集候选岩壁”的规则入口。候选岩壁必须同时满足：

- 是未揭示、可挖的岩壁；
- 至少存在一个 cardinal 相邻空地图块；
- 其最近的相邻空地距离玩家当前位置处于配置范围内。

每个候选岩壁的数字都按该岩壁为中心统计 `3x3`（Chebyshev radius = 1）范围内的炸药数，且包含自身格。

`GameSessionService.Scan(...)` 继续负责扣除能量与派发事件，但返回值从单个 `bombCount` 升级为扫描快照集合。这样规则真相仍集中在 `HazardInference` / `Bootstrap`，表现层只消费结果。

选择理由：

- 第二轮反馈改的是探测语义，不只是表现位置。
- 让“哪些岩壁可显示数字”在规则层统一判断，EditMode 可直接测。
- 批量快照比“表现层自己遍历地图猜候选墙”更稳，避免 UI 逻辑反向决定玩法结果。

备选方案：保留现有 `ScanBombCount(origin)`，在表现层根据玩家附近再拼数字。

放弃原因：候选墙筛选、3x3 计数和能量扣除会散落到多个层级，后续很难保证规则与反馈一致。

### 2. 探测数字改用世界空间文本锚点，不再复用 Hint Tilemap

当前 `HintTilemap` 只能表达“有提示”这一类离散 tile，无法自然表达 `0-9` 数字，也不适合“显示在岩壁上方”。本轮在 `Minebot.Runtime.Presentation` 内新增扫描指示器表现组件，例如 `ScanIndicatorPresenter`，由它根据扫描快照生成或复用世界空间 `TextMeshPro` / `TextMeshProUGUI` 节点，锚点统一放在：

`GridToWorldCenter(wallPosition) + Vector3.up * configuredScanLabelOffset`

`HintTilemap` 从这一轮开始只负责建造预览等“格子着色”用途，不再承担扫描数字显示。

采用技术栈：

- Unity 2D Tilemap 继续负责地形、设施和格子型覆盖。
- TextMeshPro 负责扫描数字，因项目已有 UGUI/TMP 依赖，不新增第三方组件。

明确不采用：

- 不为扫描数字额外引入 Spine、SpriteFont 插件或自定义 mesh 数字系统。
- 不继续把数字编码成多张 tile 贴图去拼接。

选择理由：

- “上方显示数字”天然更适合锚点文本。
- 数字文本可以直接复用字体和颜色样式，不需要为每个数字单独出 tile。
- 后续如果要加淡出、描边或颜色层级，文本路径扩展成本最低。

备选方案：继续使用 Hint Tilemap，但为 `0-8` 准备一整套数字 tile。

放弃原因：资产成本高，且 tile 仍然难以表达“上方偏移”而不是“占据整格”。

### 3. 将地图反馈拆成专用渲染层，避免标记/危险区/预览互相覆盖

`MinebotGameplayPresentation` 启动时不再只创建 `OverlayTilemap + HintTilemap` 两层，而是拆成明确职责的层级：

```text
Grid Root
├─ Terrain Tilemap
├─ Facility Tilemap
├─ Marker Tilemap
├─ Danger Tilemap
├─ BuildPreview Tilemap
└─ ScanIndicator Root
```

职责约束：

- `Marker Tilemap` 只画标记，不能替换岩壁底图。
- `Danger Tilemap` 只画危险区内描边，且仅在当前格是 `TerrainKind.Empty` 时显示。
- `BuildPreview Tilemap` 只画建造合法/非法预览。
- `ScanIndicator Root` 只承载扫描数字文本。

这样即使同一轮刷新同时发生标记、危险区重算和建造预览，也不会因为共享 `OverlayTilemap` 而丢失别的信号。

选择理由：

- 第二轮反馈本质上是在修“反馈所有权”。
- 各层单独刷新后，测试可以明确断言某个信号是否在正确图层出现。
- 这一拆分也与用户上一轮提出的“UI/功能面板拆 prefab、要有抽象层”思路一致，地图反馈层也应该按功能分层。

备选方案：继续保留一个 `OverlayTilemap`，仅把 marker tile 改成半透明。

放弃原因：这只能缓解视觉问题，不能解决不同反馈信号共享一层导致的覆盖顺序和验收边界不清。

### 4. 危险区只显示为空地内描边，粗细档位从表现配置资产读取

逻辑上的 `IsDangerZone` 仍允许存在于任何格子，因为波次结算、路径限制和后续挖掘后显现都要读同一份真相；但表现层只在 `TerrainKind.Empty` 的危险格上绘制危险反馈。危险区资源不再是单张纯覆盖 tile，而是扩展 `MinebotPresentationArtSet`，提供按粗细排序的危险内描边变体，例如：

```text
MinebotPresentationArtSet
├─ markerTile
├─ dangerOutlineTiles[thin..thick]
├─ scanLabelStyle / 颜色 / 偏移
└─ playerColliderRadius
```

`TilemapGridPresentation` 根据当前波次选择危险描边档位，规则为“波次越高，使用越粗的 outline 资源；超过资源数量后取最后一档”。这保证视觉反馈与波次压力同步，但不改危险区逻辑半径或伤害规则。

选择理由：

- 反馈要求的是表现方式变化，不是危险区规则重做。
- 将粗细变化放进表现配置，可以让策划/美术后续只改资产和序列，不改代码。

备选方案：用 shader 或运行时线框生成器按 cell 动态画内描边。

放弃原因：MVP 成本高，且不符合当前仓库“轻量 MonoBehaviour + Tilemap + ScriptableObject”的技术边界。

### 5. 玩家碰撞体改为表现配置驱动，并与接触判定共用同一尺寸来源

当前玩家碰撞体半径写死在 `MinebotGameplayPresentation.EnsureCircleCollider()`。本轮把玩家碰撞尺寸移到 `MinebotPresentationArtSet` 或等价的表现配置资产，并要求：

- `CircleCollider2D.radius` 从配置读取；
- `FreeformActorController` / `ActorContactProbe` 的贴墙命中与自动挖掘候选格判定基于同一世界坐标和碰撞来源；
- PlayMode 和 EditMode 测试同时覆盖“贴墙时能稳定命中候选岩壁”和“单格通道仍可通过”两类边界。

asmdef 策略保持不变：

- `Minebot.Runtime.HazardInference` 负责扫描候选墙与计数规则；
- `Minebot.Runtime.Bootstrap` 负责 `GameSessionService` 扣能量、返回扫描快照；
- `Minebot.Runtime.Presentation` 负责 Tilemap 层、扫描文本和碰撞体挂载；
- `Minebot.Tests.EditMode` / `Minebot.Tests.PlayMode` 分别验证规则层与场景层。

不新增新的顶层 asmdef；如果需要新的扫描 DTO，优先放进已存在的 `HazardInference` 或 `Bootstrap` 程序集，而不是新拆一层共享程序集。

选择理由：

- 玩家碰撞体过小本质上是表现调参问题，不应该继续靠代码常量修。
- 让碰撞和接触判定共享同一配置来源，才能避免“看起来碰到了，但规则没碰到”。

备选方案：单纯把硬编码半径从 `0.34f` 改大。

放弃原因：这只能解决当前一张图，不会形成后续可调的验收基线。

### 6. 开发顺序按“规则结果 -> 表现层 -> 资源配置 -> 测试”推进

建议开发顺序：

1. 扩展 `HazardRules` / `HazardService`，定义扫描范围、候选岩壁筛选和 `ScanReading` 快照。
2. 改 `GameSessionService.Scan` 与 `MinebotGameplayPresentation.RecordScan` 的数据通路，移除“单 origin + 单 bombCount”假设。
3. 拆分 `TilemapGridPresentation` 图层职责，接入 `Marker/Danger/BuildPreview/ScanIndicator` 独立刷新。
4. 扩展 `MinebotPresentationArtSet`，加入危险区 outline 变体、扫描文本样式/偏移和玩家碰撞半径。
5. 把 `EnsureCircleCollider()` 改成读取配置，并补接触边界测试。
6. 更新 PlayMode / EditMode / OpenSpec 验证，最后再替换具体美术资源。

## Risks / Trade-offs

- [Risk] 批量扫描结果可能让一次 `Q` 生成过多文本节点。→ Mitigation：只对“玩家附近且有相邻空地”的前沿岩壁生成快照，并复用 `ScanIndicatorPresenter` 内的对象池。
- [Risk] 逻辑危险区仍存在于墙体上，但表现只画空地，用户可能误解“某些危险格不存在”。 → Mitigation：明确规定危险区表现只服务可站立区域；当墙体被挖开变为空地后，下一次刷新立即显示 outline。
- [Risk] 扩展 `MinebotPresentationArtSet` 后，旧默认资源可能缺字段。 → Mitigation：为新增字段提供缺省占位资源和安全回退，保证无资源时也能编译和进入场景。
- [Risk] 玩家碰撞体变大后，旧测试里依赖具体世界坐标的断言可能失效。 → Mitigation：把测试断言从“硬编码 radius 数值”改成“能否通过通道、能否稳定命中墙体”的行为断言。
- [Risk] 扫描从单点计数改成批量快照后，旧 HUD 文案会失真。 → Mitigation：HUD 只保留“探测成功/失败”和能量变化摘要，详细数字完全交给地图上的扫描指示器。

## Migration Plan

1. 先在运行时服务层引入新的扫描快照结构，同时保留旧接口的适配层，避免一次改动打断所有调用点。
2. 在表现层新增 `ScanIndicatorPresenter` 与独立 Tilemap 层，确认新图层可用后，再删除旧的 `scanOrigin` / `ScanHintTile` 主路径。
3. 扩展 `MinebotPresentationArtSet` 并为默认资源填充占位 outline / collider 值，确保 `Gameplay` 和 `DebugSandbox` 都能直接启动。
4. 更新 EditMode 用例，验证扫描候选墙筛选与 3x3 计数；更新 PlayMode 用例，验证数字锚点、标记独立层、危险区内描边和玩家碰撞体验。
5. 最后运行 `openspec validate apply-second-round-gameplay-feedback`，把 proposal/design/specs/tasks 校验收口。

回退策略：

- 若批量扫描表现未稳定，可临时保留旧 HUD 提示，但不能回退到“单点 origin 计数”规则。
- 若新 outline 资源未就绪，可先用程序化占位 tile 表示 thin/medium/thick 三档，不阻塞规则和测试接入。

## Open Questions

- 暂无阻塞性问题。本设计已将“玩家附近”的具体距离收敛为配置项，由 `HazardRules` 给默认值并通过测试锁定。
