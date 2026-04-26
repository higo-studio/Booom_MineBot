## Context

当前玩家自由移动位于 `Assets/Scripts/Runtime/Presentation/`，核心链路是：

- `GameplayInputController` 读取输入，并在移动失败后立刻尝试自动挖掘。
- `FreeformActorController` 将输入方向转成连续位移。
- `ActorContactProbe` 依据逻辑网格做逐轴裁剪，并返回接触格。
- `MinebotGameplayPresentation` 在世界坐标移动后，把玩家逻辑格重新同步回 `PlayerMiningState`。

这条链路已经满足了“自由移动表现 + 逻辑网格真相”的总体架构，但当前接触解算仍是逐轴裁剪，导致两个直接问题：

- 持续顶住障碍物时，移动阻挡、自动挖掘和反馈刷新被绑在同一条热路径上，容易出现高频抖动感。
- 靠近角落时，解算受轴顺序影响，角色会卡住而不是沿可用切线方向继续滑动。

同时，当前 change 还缺少一个更稳的边界定义：角色移动解算尚未被抽象成独立组件，`GameplayInputController`、`FreeformActorController`、`ActorContactProbe` 与业务判定之间的职责仍然偏混杂。参考 PhysX Character Controller 文档，本次应借鉴的是它的分层思路，而不是照搬其 3D 组件：

- CCT 是构建在底层碰撞/查询能力之上的独立 locomotion 组件。
- 核心输入是位移 `disp`，核心输出是带碰撞信息的移动结果。
- 自定义 CCT 的基础算法是 `sweep -> 命中 -> 去掉法向分量 -> 继续 sweep`，必要时补 overlap recovery。

本次设计继续遵守项目既有边界：

- 目录布局与模块边界整体不变，但会在现有运行时程序集内抽出独立的 2D locomotion 组件，而不是继续把碰撞解算揉在输入或表现脚本里。
- asmdef 策略不变，继续复用现有运行时与测试程序集，不额外拆新程序集。
- 启动场景流程不变，仍是 `Bootstrap -> Gameplay`；本次只重构进入 `Gameplay` 后的玩家移动表现。
- 逻辑网格仍然是地形、交互、炸药和建筑占位的权威真相，不将 Scene Collider 或 Unity 物理系统升级为规则权威。
- 数据配置方式以现有表现层序列化参数为主；角色 footprint、`contactOffset`、`minMoveDistance`、最大迭代次数、接触锁存和自动挖掘等待时间属于局部手感参数，不单独引入新的 ScriptableObject。

## Goals / Non-Goals

**Goals:**

- 抽出一个独立的 2D `KinematicCharacterMotor` 组件，使角色移动解算与业务层解耦。
- 将玩家自由移动重构为稳定的 kinematic sweep-and-move，让角色贴墙时能沿切线方向滑动。
- 明确分离“位移解算结果”和“交互决策结果”，避免移动失败立刻触发自动挖掘抖动。
- 让接触墙面与接触格结果在连续帧之间保持稳定，便于自动挖掘和反馈系统消费。
- 保持逻辑网格同步、自动挖掘和表现层刷新仍沿用当前架构，不引入场景即真相的倒置。
- 补齐 EditMode 行为测试，并保留必要的 PlayMode 烟雾验证。

**Non-Goals:**

- 不引入 Unity `CharacterController`、`Rigidbody2D` 驱动移动或 Tilemap Collider 作为玩法真相。
- 不同时重构从属机器人移动、敌对单位移动或其它非玩家 locomotion 系统。
- 不在本次 change 中重做扫描、标记、建造、危险区或挖掘数值系统。
- 不调整 `Bootstrap`、地图 Bake、配置资产管线或输入映射方案。

## Decisions

### 1. 将角色移动解算抽象为独立的 2D locomotion 组件

本次不再让 `GameplayInputController`、`FreeformActorController` 或 `ActorContactProbe` 直接组成隐式移动栈，而是收敛为一个独立的 `KinematicCharacterMotor2D` 边界。它只负责：

- 接收移动请求：当前位置、期望位移、footprint、过滤参数
- 向底层碰撞世界发起 sweep / overlap / depenetration 查询
- 产出结构化移动结果：最终位置、实际位移、阻挡信息、命中集合、稳定接触候选

业务层只消费移动结果，不再参与底层 collide-and-slide 迭代。

建议边界：

```text
Gameplay / Input / AutoMine / UI
            │
            ▼
   KinematicCharacterMotor2D
            │
            ├─ ICharacterCollisionWorld2D
            └─ CharacterMotorConfig / MoveRequest / MoveResult
```

选择理由：

- 这更接近 PhysX CCT 的分层思路：控制器是独立模块，业务只通过请求/结果接口使用它。
- 这样更容易被玩家、机器人或未来其它角色复用，也更容易单独写 EditMode 测试。
- 它能阻止业务逻辑继续渗入碰撞细节，降低后续手感迭代成本。

备选方案与取舍：

- 备选：继续在 `FreeformActorController` 和 `ActorContactProbe` 内就地重构。
  放弃原因：即使算法变对，边界仍然混杂，业务层仍容易重新耦合到底层碰撞细节。

### 2. 采用自定义 Kinematic Sweep-and-Move，而不是切换到 Unity 物理解算

玩家移动继续基于逻辑网格阻挡信息做接触查询，但解算方式从“逐轴裁剪”升级为“沿期望位移做 sweep，命中后沿法线移除法向分量，再处理剩余位移”的多次迭代解算。

在 2D 版本里，推荐起点是圆形 footprint，并显式区分：

- `collisionRadius`：角色实际 footprint 尺寸
- `contactOffset`：用于数值稳定和避免卡边的皮肤距离
- `minMoveDistance`：剩余位移小于阈值时提前结束迭代
- `maxIterations`：单次 move 最多处理的碰撞次数
- `overlapRecovery`：初始重叠时的脱困路径

推荐解算顺序：

```text
goalDelta
  -> sweep(current, delta)
  -> 无命中: 走到目标
  -> 有命中: 走到 hit 前安全位置
             去掉朝向 hit normal 的位移分量
             用剩余切向位移继续 sweep
  -> 初始重叠: 执行 depenetration / overlap recovery 后重试
```

选择理由：

- 它与项目“逻辑网格是真相，Scene 只负责表现”的基础设计一致。
- 它能直接使用当前可测试的阻挡格数据，不需要引入新的 2D/3D 物理真相桥接层。
- 它天然更适合解决贴墙滑动、角落阻挡和连续位移残量处理问题。
- 它也与 PhysX 文档给出的自定义 CCT 基础算法一致，只是查询后端从 Scene Query 换成逻辑网格查询。

备选方案与取舍：

- 备选：Unity `CharacterController`。
  放弃原因：它是 3D 组件，会把 2D Tilemap 项目的接触真相拖向另一套世界，与现有逻辑网格架构冲突过大。
- 备选：`Rigidbody2D` / `Collider2D.Cast`。
  放弃原因：虽然是 2D 正统路线，但仍会把接触判定部分迁入 Scene 物理，需要额外解决逻辑格邻接、自动挖掘和测试可重复性映射问题。

### 3. 通过 collision world adapter 连接逻辑网格，而不是让 motor 理解完整业务世界

`KinematicCharacterMotor2D` 不应直接依赖 `LogicalGridState` 的全部业务细节，而应只依赖一个 2D 查询接口，例如：

- `Sweep(shape, from, delta, filter)`
- `Overlap(shape, pose, filter)`
- `ComputePenetration(shape, pose, filter)` 或等价的 depenetration 接口

再由 `GridCharacterCollisionWorld` 一类 adapter 把 `LogicalGridState`、建筑占位和边界阻挡翻译成 motor 可消费的命中结果。

选择理由：

- 这让 motor 保持“角色移动解算组件”而不是“Minebot 世界规则组件”。
- 未来如果机器人、动态障碍或临时阻挡需要接入，可以先扩展 adapter/filter，而不是重写 solver。

备选方案与取舍：

- 备选：让 motor 直接读取 `LogicalGridState`。
  放弃原因：这会把业务世界模型直接泄漏进 locomotion 核心，后续复用和测试边界都会变差。

### 4. 将移动解算结果建模为稳定的接触结果，而不是只返回“是否移动成功”

移动层需要产出比 `bool moved` 更丰富的结果，至少覆盖：

- 解析后世界坐标
- 是否发生位移
- 是否发生阻挡
- 碰撞 flags（例如是否命中阻挡、是否发生 depenetration）
- 阻挡法线或切向滑动信息
- 主命中与命中集合
- 稳定接触格
- 是否存在可继续滑动的剩余位移

上层再基于这些结果决定：

- 是否刷新玩家逻辑格
- 是否进入自动挖掘候选
- 是否保持上一次墙面锁存
- 是否需要刷新反馈

选择理由：

- 当前问题不只是“解算不顺”，还包含“解算结果太粗，导致输入、交互和 UI 误把 blocked 当成 mine intent”。
- 把 locomotion 结果与 interaction 决策分离后，移动和自动挖掘可以各自独立测试。

备选方案与取舍：

- 备选：保留 `bool + contactCell` 接口，只在内部偷偷换算法。
  放弃原因：这样不足以表达滑动、稳定阻挡和剩余位移信息，上层仍然只能用粗糙的 blocked 语义做推断。

### 5. 自动挖掘改为消费“稳定墙面锁存”，而不是消费单帧阻挡

自动挖掘的进入条件改为：

- 玩家与同一面可挖岩壁保持稳定接触若干连续帧或持续一小段时间；
- 当前没有显著的可用切向滑动位移；
- 接触格在逻辑上仍与玩家相邻并可挖掘。

一旦角色仍在沿墙滑动，或接触格在角落附近发生变化，就不应立刻视为“开始挖这面墙”。

选择理由：

- 这能直接消除“持续顶墙时每帧反馈”和“角落误判为挖掘目标”的问题。
- 它保留了“朝岩壁持续移动即可自动挖掘”的高层玩法，不改变玩家输入心智模型。

备选方案与取舍：

- 备选：继续沿用单帧 `contactCell`，仅对反馈做节流。
  放弃原因：这样只能减轻刷屏，不能根治误判与角落卡顿。

### 6. 逻辑格同步继续从表现层回写，但改为基于稳定占位结果同步

玩家逻辑格仍由自由移动世界位置回写到 `PlayerMiningState`，但同步策略要以“当前 footprint 对应的稳定可站立格”为准，而不是简单依赖单次 `FloorToInt` 的瞬时结果。目标是保证：

- 逻辑格始终位于可通行格；
- 贴墙滑动时不会在相邻格之间异常抖动；
- 自动挖掘判定看到的是稳定的相邻关系。

选择理由：

- 这能保留当前逻辑服务层的接口和测试结构，不需要把移动真相下沉到规则层。
- 它是维持“自由移动表现”与“方格规则结算”一致性的关键胶水层。

备选方案与取舍：

- 备选：完全以世界坐标接触结果驱动挖掘，不再依赖逻辑格邻接。
  放弃原因：这会扩大改动范围，动到 `GridMining` 规则边界，不适合这次以手感修复为主的 change。

### 7. 验证以 motor 单测为主，玩家链路验证为辅，并按最小范围推进

开发顺序采用：

1. 先明确 motor、request/result、collision world adapter 的接口。
2. 再实现 sweep-and-move、`contactOffset`、`minMoveDistance` 与 overlap recovery。
3. 之后改玩家移动调用链和自动挖掘触发条件。
4. 先补齐 motor 级 EditMode 测试，再补业务链路级 EditMode / PlayMode 验证。
5. 最后做 `Gameplay` / `DebugSandbox` 的 PlayMode 烟雾验证与手感回归。

选择理由：

- 这符合项目“规则优先 EditMode、联动优先 PlayMode 烟雾”的既有验证偏好。
- 移动手感问题的核心在独立 solver 和状态机边界，先把 motor 单测打牢更稳。

## Risks / Trade-offs

- [独立 motor + adapter 会引入更多类型与接口] → 通过把边界压缩在最小必要接口上，避免把整个世界模型都抽象化。
- [自定义 sweep-and-move 解算复杂度高于逐轴裁剪] → 通过限制迭代次数、分离 `collisionRadius` 与 `contactOffset`、优先写 EditMode 回归用例控制复杂度。
- [稳定墙面锁存可能让自动挖掘起手略微变慢] → 通过短阈值和明确测试口径控制，只允许“足够稳定才触发”，不引入肉眼可感的输入延迟。
- [逻辑格同步策略调整可能影响已有相邻判定] → 在测试里覆盖贴墙滑动、角落逼近和连续挖掘三类路径，避免回归到错误格子。
- [玩家体验改善但机器人移动仍旧粗糙] → 明确本次只处理玩家接入；但 motor 设计为可复用组件，未来再单独评估是否给机器人接线。
- [overlap recovery 处理不当会引入“被弹开”感] → 仅将其用于初始重叠、传送、爆炸改地形或数值精度补救，不让其取代正常 sweep 路径。

## Migration Plan

本次 change 不涉及存档、场景结构或资源资产迁移，迁移重点是代码路径切换与验证：

1. 先抽出独立 `KinematicCharacterMotor2D`、request/result 模型和 collision world adapter。
2. 将现有 `ActorContactProbe` 的逐轴裁剪逻辑迁入新 motor 或被新 motor 替换。
3. 更新 `FreeformActorController`、`GameplayInputController` 与 `MinebotGameplayPresentation`，让它们通过新接口消费移动结果。
4. 通过 EditMode 与 PlayMode 验证后再作为默认路径启用，不保留长期双实现并行。
5. 若回归风险超出预期，可在本 change 内回退到旧解算实现；由于没有资产迁移，回滚成本仅限代码回退。

## Open Questions

- 机器人自由移动未来是否要直接复用同一套 motor，还是只复用 adapter / result 协议。
- 自动挖掘稳定接触阈值最终放在 `GameplayInputController` 还是更集中地落到玩家表现配置上。
- 角落极小缝隙附近是否需要额外“最小剩余位移阈值”，防止数值精度导致假滑动。
- 当前版本是否就把临时障碍 / filter hook 一并暴露，还是先只做静态阻挡与建筑占位。
