## Context

当前波次链路是 `BootstrapSceneLoader -> MinebotServices.Initialize -> RuntimeServiceRegistry -> MinebotGameplayPresentation.Update()`：表现层每帧调用 `services.Waves.Tick(Time.deltaTime)`，一旦倒计时归零就直接进入 `MinebotGameplayPresentation.ResolveWave()`，随后同步调用 `WaveSurvivalService.ResolveWave(...)` 完成整轮结算。这个实现有三个直接问题：

- `WaveSurvivalService.ResolveWave()` 会在一个方法里完成波次推进、玩家/机器人判死、危险区塌方和计分，外部无法插入“先炸外围炸弹、再重算危险区”的中间步骤。
- `HazardService` 目前只在玩家/机器人主动挖雷时触发，没有“波次结束按外围判定自动引爆炸弹”的入口。
- 输入层和运行时 tick 当前只会因为升级面板或失败而暂停，不存在“地震结算阶段进行中，普通动作先冻结”的统一状态。

本次设计仍然遵守仓库现有边界：

- 规则真相继续留在纯 C# 服务，不把波次结算顺序塞进 `MonoBehaviour` 黑盒。
- 时序和演出间隔继续优先进入 ScriptableObject 配置；本次沿用 `WaveConfig` 承载地震波参数，而不是硬编码常量。
- 模块和 asmdef 不新增跨层反转，仍以现有目录为主：
  - `Assets/Scripts/Runtime/HazardInference`：外围炸弹候选判定与爆炸地形结算
  - `Assets/Scripts/Runtime/WaveSurvival`：危险区评估、波次计分、最终塌方与失败判定
  - `Assets/Scripts/Runtime/Bootstrap`：跨模块的波次结算阶段编排
  - `Assets/Scripts/Runtime/Presentation` / `UI`：阶段反馈、输入冻结提示和地图刷新
  - `Assets/Scripts/Tests/EditMode` / `PlayMode`：规则与场景回归
- 技术栈保持 Unity 6 + C# + ScriptableObject + 轻量 MonoBehaviour；本轮明确不引入 DOTS、多人联机、Timeline、Animator Controller 状态机图或通用暂停框架。

## Goals / Non-Goals

**Goals:**

- 把波次结束改成可观测的三阶段顺序：外围炸弹、危险区重算、按新危险区塌方。
- 为“外围炸弹”建立确定性判定：炸弹所在岩壁格四邻中只要至少一格是空地，就进入本轮炸弹阶段候选。
- 保证危险区塌方、玩家失败、机器人损毁和 HUD 提示统一读取“爆炸后再重算”的那份危险区真相。
- 在阶段演出期间临时冻结玩家实时动作与自动化 tick，避免结算一半时继续移动、挖墙或更新机器人目标。
- 允许配置每个阶段之间的最小停顿时间，使 `Gameplay` 能按步骤演出，而不是单帧闪过。
- 保持 `Bootstrap -> Gameplay` 启动流程和现有 asmdef 结构不变，只在对应模块内增量扩展。

**Non-Goals:**

- 不重做炸弹半径、爆炸伤害模型或新增“被波次炸弹炸到也扣血”的范围伤害规则。
- 不把整个游戏升级成通用暂停系统；本次只处理“波次结算期间的临时动作冻结”。
- 不新增独立 Timeline、过场镜头或复杂演出编辑工具。
- 不改变地图生成、炸弹播种规则、资源掉落或机器人经济配置。
- 不处理存档兼容或回放系统；本次默认针对当前 MVP 运行时。

## Decisions

### 1. 把阶段编排放进 `GameSessionService`，而不是直接塞回 `MinebotGameplayPresentation`

本次会让 `GameSessionService` 成为波次阶段编排器，因为它已经同时持有 `WaveSurvivalService`、`HazardService`、`PlayerVitals`、`RobotState`、`WorldPickupService` 等跨模块依赖。相比之下，`MinebotGameplayPresentation` 更适合消费阶段快照并刷新地图/HUD，而不是拥有规则顺序。

建议新增一组轻量状态对象，例如：

```text
WaveResolutionPhase
├─ None
├─ DetonatePerimeterBombs
├─ ReevaluateDangerZones
├─ CollapseDangerZones
└─ Completed

WaveResolutionState
├─ isActive
├─ targetWave
├─ targetDangerRadius
├─ phase
├─ phaseElapsed
├─ perimeterBombOrigins[]
└─ lastResolutionSummary
```

职责划分：

- `GameSessionService`
  - 开始波次结算
  - 维护当前阶段、阶段计时和动作冻结状态
  - 调用各子服务推进到下一阶段
- `WaveSurvivalService`
  - 负责危险区评估、按危险区判死、按危险区塌方、波次计分
- `HazardService`
  - 负责外围炸弹候选收集与单次/连锁爆炸的地形改写
- `MinebotGameplayPresentation`
  - 只读 `WaveResolutionState`，决定显示哪条提示、何时刷新地图与 HUD

备选方案：继续让 `MinebotGameplayPresentation.ResolveWave()` 直接串联所有阶段。

放弃原因：规则顺序会继续散在表现层里，EditMode 很难只测规则服务而不跑场景，输入冻结也更容易和 HUD 刷新混成一团。

### 2. “外围炸弹”只在波次开始时快照一次，并按稳定顺序依次引爆

用户给出的判定标准很明确：炸弹所在岩壁格如果上下左右任意一格是空地，就算“最外层”。本次不在每次爆炸后重新扫描全图再追加新的外围炸弹，而是在地震结算开始时一次性快照候选集合：

1. 遍历当前 `LogicalGridState`
2. 找出 `TerrainKind.MineableWall && HasBomb`
3. 仅保留四邻至少一格 `TerrainKind.Empty` 的格子
4. 按固定顺序排序，例如 `Y` 再 `X` 的行优先顺序

然后在炸弹阶段依次调用现有 `HazardService.ResolveExplosion(...)`。如果某个候选已经被前一个爆炸连锁清掉，就在执行时跳过。

这样做的原因：

- “先引爆最外层的炸弹”是一个快照概念；如果每炸完一颗都重新扫描，就会把新暴露出来但本不在外层的炸弹也吞进同一轮，步骤边界会变模糊。
- 固定顺序能让 PlayMode 演出和 EditMode 断言稳定，不会因为 `HashSet` / `Queue` 的遍历偶然性让结果漂移。
- 仍然允许单颗外围炸弹通过现有连锁逻辑炸到更深层炸弹；这是“爆炸传播”的结果，不是“外围筛选”再次运行。

备选方案 A：每次爆炸后重新扫描外围炸弹直到没有候选。

放弃原因：会把“外围炸弹阶段”膨胀成不可预测的多轮扫描，也更难向玩家解释为什么某些刚暴露的深层炸弹在同一波里继续自动触发。

备选方案 B：完全不排序，直接用遍历顺序执行。

放弃原因：对测试和演出都不稳定。

### 3. 将当前 `ResolveWave()` 拆成“危险区重算”和“最终塌方/判死”两个子步骤

当前 `WaveSurvivalService.ResolveWave()` 会立即：

1. `CurrentWave++`
2. 按当前 `IsDangerZone` 判玩家/机器人生死
3. 立刻把危险空地塌回墙体

这与新需求冲突，因为危险区必须在外围炸弹改写地图后再计算。本次建议把它拆成三个更小的职责：

- `PrepareWaveResolution()`
  - 捕获 `targetWave = CurrentWave + 1`
  - 捕获本轮要使用的 `targetDangerRadius`
- `EvaluateDangerZonesForResolution(targetDangerRadius)`
  - 基于爆炸后的地图重算本轮危险区真相
- `FinalizeWaveResolution(...)`
  - 用当前危险区快照执行玩家/机器人判死
  - 把危险空地塌回普通可挖墙
  - 提交 `CurrentWave = targetWave`
  - 重置下一轮倒计时
  - 立即再算一次“下一波预警用”的危险区

这里的关键点是：玩家失败、机器人损毁和塌方都绑定到“第二阶段重算出来的危险区”，而不是波次开始前残留的旧危险区。

备选方案：在炸弹阶段前就先 `CurrentWave++`，继续让 `EvaluateDangerZones()` 走现有 `NextDangerRadius` 推导。

放弃原因：阶段进行中 `CurrentWave` 会先跳到新值，HUD 和后续预警语义都更难解释；而且一旦中途需要读取“本轮结算波次”和“下一轮预警波次”，状态会混淆。

### 4. 阶段停顿时间继续放进 `WaveConfig`，不硬编码在表现层

为了支持“按步骤演出”，本次建议扩展 `WaveConfig`，新增与波次阶段演出相关的最小时长，例如：

- `perimeterBombPhaseHoldSeconds`
- `dangerRefreshPhaseHoldSeconds`
- `collapsePhaseHoldSeconds`

这些值继续由 `BootstrapConfig -> WaveConfig` 注入运行时，规则层只消费秒数，不关心具体 HUD 动画。这样有几个好处：

- 时序是可调数值，方便在 `Gameplay` 和 `DebugSandbox` 上快速调试“演出看不看得清”。
- 不需要把阶段延迟散落进 `MinebotGameplayPresentation` 常量和协程里。
- 如果后续想在测试里把阶段停顿改成 `0` 做快速验证，也有清晰入口。

备选方案：表现层写死每阶段 `0.2s` 或直接 `yield return null`。

放弃原因：时序会变成隐藏常量，调参和测试都不透明。

### 5. 输入冻结只锁“规则动作”和自动化 tick，视觉刷新继续运行

“可暂停玩家动作”不等于整帧停摆。本次采用“规则冻结、表现继续”的做法：

- 冻结内容：
  - 玩家移动、挖掘、标记、建造、维修、造机器人输入
  - `TickRobots()`
  - `TickPassiveHazardSense()`
  - `TickWorldPickups()` 的规则收集逻辑
  - 普通波次倒计时推进
- 继续运行的内容：
  - 摄像机与角色现有纯表现更新
  - HUD 文本刷新
  - 地图刷新与阶段提示

实现上，`GameplayInputController.CanAcceptGameplayInput()` 和 `CanChangeMode()` 都增加“波次结算锁”分支；`MinebotGameplayPresentation.Update()` 在 `WaveResolutionState.isActive` 时不再走普通 session tick，而是只推进当前阶段计时并在阶段切换时刷新地图。

这样可以在不引入通用 pause framework 的前提下，保证地震结算期间世界规则不会继续前进。

备选方案：用 `Time.timeScale = 0` 做全局暂停。

放弃原因：会顺手冻住现有许多依赖 `deltaTime` 的表现逻辑，还会把“通用暂停菜单”和“波次步骤演出”耦合起来，超出本次范围。

### 6. 结算完成后立即恢复“下一波预警”视图，避免地图停在无危险区状态

本轮塌方完成后，危险区会被清掉；如果直接解锁输入而不再刷新下一波预警，玩家会短暂看到“地图没有危险区”的空窗。为保持现有玩法压力，`FinalizeWaveResolution(...)` 结束后应立即基于新地图和新的 `CurrentWave` 再运行一次预警危险区评估。

顺序应为：

1. 用本轮危险区快照判死与塌方
2. 提交波次推进与倒计时重置
3. 重新评估下一波的预警危险区
4. 结束阶段锁定，恢复普通输入与自动化 tick

这样玩家一结束地震演出，就能直接看到“下一波从哪里逼近”。

## Risks / Trade-offs

- [阶段状态同时被 session、HUD 和地图刷新读取，容易出现不同步] → Mitigation：只保留一份 `WaveResolutionState` 作为权威；表现层只读，不自己推导阶段。
- [外围炸弹快照顺序改变现有部分极端地图的塌方结果] → Mitigation：用固定遍历顺序和 EditMode 用例锁定“同一地图同一顺序必得同一结果”。
- [把 `ResolveWave()` 拆成多个步骤后，容易遗漏某个旧副作用，例如计分或机器人回收掉落] → Mitigation：保留一份 `WaveResolutionSummary`，在 `FinalizeWaveResolution(...)` 统一产出与断言。
- [阶段停顿如果默认值过长，会拖慢波次节奏] → Mitigation：把时长收在 `WaveConfig`，默认给极短可读值，并允许测试把它压到 `0`。
- [冻结规则但不冻结纯表现，可能让玩家误以为角色还能继续行动] → Mitigation：HUD 明确显示“地震结算中，动作已暂停”，并在输入尝试时返回同一条锁定反馈。

## Migration Plan

1. 扩展 `WaveConfig`，加入阶段停顿时长配置，并给默认 `Gameplay` 资产填入可读但短促的默认值。
2. 在 `HazardInference` 增加外围炸弹候选收集逻辑，保持爆炸地形改写继续复用现有 `ResolveExplosion(...)`。
3. 在 `WaveSurvivalService` 拆分当前一次性 `ResolveWave()` 的职责，让危险区重算与最终塌方/判死可以分步调用。
4. 在 `GameSessionService` 新增 `WaveResolutionState` 和阶段推进入口，作为跨模块编排器。
5. 在 `MinebotGameplayPresentation` / `GameplayInputController` / HUD 绑定层接入阶段提示与动作冻结。
6. 最后补 EditMode / PlayMode 回归，并在 `Gameplay` 做一次地震波烟雾验证。

回退策略：

- 若阶段演出逻辑不稳定，可以先回退到旧的一次性 `ResolveWave()` 路径，同时保留外围炸弹候选工具代码不启用。
- 若 HUD/输入冻结实现不成熟，可先把各阶段停顿时长配置为 `0`，让逻辑顺序落地但不强制拉长演出。

## Open Questions

- 暂无阻塞性问题。本设计默认“波次触发的外围炸弹只改地形，不追加玩家直接挖雷伤害”，并将其视为现有伤害模型的延续，而不是新的范围伤害系统。
