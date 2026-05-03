## Context

当前主玩法的挖掘链路由 `BootstrapSceneLoader -> MinebotServices.Initialize -> RuntimeServiceRegistry -> GameplayInputController / MinebotGameplayPresentation` 串起来。规则层里，`MiningService.TryMineDetailedFrom()` 会在一次调用内直接把目标墙体改成空地，非炸药墙还可能继续走 `ExpandSafeRegion()` 连锁清空；表现层里，`GameplayInputController.TryAutoMineContact()` 通过现有稳定接触判定累计到阈值后，直接调用一次 `Session.Mine()`，`MinebotGameplayPresentation.RefreshMiningCrack()` 只会短时重播 crack 序列并在极短时间后淡出。

这和本次目标存在三类冲突：

- 四档岩层虽然已经有 `HardnessTier` 和奖励配置，但没有独立生命值、防御力和持续伤害节奏。
- 现有“零风险安全墙连锁开格”会绕过相邻墙体自己的生命/防御/crack 生命周期。
- 现有 crack 表现只能“重播一段循环动画”，不能按生命百分比停在某一帧，也不能在中断 `0.5s` 内保持暂停状态。

仓库边界上，本次设计仍应遵守：

- 玩法真相继续放在纯 C# 规则服务，Scene / MonoBehaviour 只消费快照和事件。
- 可调数值优先进入 ScriptableObject，不把四档岩层生命/防御和玩家攻击常量散落到脚本里。
- 目录与 asmdef 继续沿用现有模块边界：
  - `Assets/Scripts/Runtime/GridMining`：挖掘规则、墙体生命状态、配置读取
  - `Assets/Scripts/Runtime/Bootstrap`：服务装配与 session tick
  - `Assets/Scripts/Runtime/Automation`：机器人挖掘节奏与目标资格
  - `Assets/Scripts/Runtime/Presentation`：crack / wall break 表现与接触反馈
  - `Assets/Scripts/Tests/EditMode` / `PlayMode`：规则与场景回归
- 技术栈仍然是 Unity 6 + C# + ScriptableObject + 轻量 MonoBehaviour；本阶段明确不引入 DOTS、多人联机、Animator Controller 状态机图或第三方行为树。

## Goals / Non-Goals

**Goals:**

- 为 `Soil / Stone / HardRock / UltraHard` 四档岩层分别配置生命值与防御力。
- 为玩家提供基础攻击力与按钻头升级阶段递增的攻击加值，并据此替代现有“只看等级门槛”的瞬时挖掘判定。
- 让玩家自动挖掘改为按固定 tick 持续扣减目标生命值，默认支持 `0.1s` 节奏。
- 当玩家停止对某墙体提交有效挖掘判定时，保留 `0.5s` 当前生命值和 crack 帧；若超时仍未恢复，则自动回满。
- 让 crack 表现根据当前生命百分比平均映射到 sprite sequence 帧数，而不是持续循环重播。
- 让从属机器人也遵守共享的墙体生命/防御规则，避免继续一次调用直接挖空目标。
- 用 EditMode / PlayMode 回归把攻击、防御、恢复、crack 暂停和机器人多 tick 挖掘锁定下来。

**Non-Goals:**

- 不重做四档岩层的资源掉落体系；现有奖励配置继续保留。
- 不新增独立血条 UI、浮动数字或额外的挖掘按钮输入。
- 不把挖掘推进改成基于真实物理碰撞伤害或 Rigidbody 驱动。
- 不在本轮扩展炸药种类、扫描公式或波次危险区规则。
- 不保留当前“零风险命中后瞬时连锁开空相邻安全墙”的行为；该逻辑与逐墙生命/防御模型冲突，本次明确替换掉。

## Decisions

### 1. 新增独立的 `MiningRules` ScriptableObject，而不是把生命/攻击硬塞进 `GameBalanceConfig`

本次会新增一份专门的挖掘规则资产，例如 `MiningRules` 或等价命名，挂到 `BootstrapConfig` 上，由 `MinebotServices.Initialize()` 注入运行时。

建议字段分组：

- 全局挖掘时序：
  - `playerMiningTickIntervalSeconds`，默认 `0.1`
  - `miningDisengageGraceSeconds`，默认 `0.5`
- 玩家攻击：
  - `playerBaseAttack`
  - `drillTierAttackBonuses[Soil..UltraHard]`
- 岩层耐久：
  - `rockStats[HardnessTier] -> maxHealth, defense`

选择独立资产而不是扩展 `GameBalanceConfig` 的原因：

- `GameBalanceConfig` 目前已承担资源、维修、机器人等综合经济配置；继续塞入墙体生命和攻击规则，会把“资源掉落”和“挖掘结算”搅在一起。
- 用户已经说明资源配置基本完成；单独资产能降低对现有掉落表的扰动。
- 后续如果要针对 `DebugSandbox`、局部测试场景或关卡脚本替换挖掘规则，独立资产更容易挂接。

备选方案：直接把 `maxHealth / defense / attack` 字段加进 `GameBalanceConfig`。

放弃原因：会让 balance 资产继续膨胀，且难以清晰表达“已有奖励配置保持不动，新加的是挖掘生命周期配置”。

### 2. 用运行时墙体耐久状态替代瞬时 `TryMine()` 结算，并取消当前安全墙连锁瞬开

`GridCellState` 当前只存静态真相：地形、硬度、炸药、奖励等。墙体的“当前剩余生命值”和“最近一次受击时间”不应直接写回这个静态结构，而应由 `MiningService` 内部维护一份懒初始化的运行时状态表，例如：

```text
MiningCellProgress
├─ position
├─ maxHealth
├─ currentHealth
├─ lastDamageTime
└─ graceDeadline
```

每次玩家或机器人提交一次有效挖掘 tick 时：

1. 根据 `HardnessTier` 读取该墙体 `maxHealth / defense`
2. 计算矿机有效攻击力
   - 玩家：`playerBaseAttack + drillTierBonus[currentDrillTier]`
   - 机器人：沿用其当前“用玩家钻头等级 / 固定钻头等级”分支，但只取对应 tier 的攻击加值，不额外叠加玩家基础攻击
3. 若 `effectiveAttack <= defense`：
   - 返回“无伤害 / 强度不足”结果
   - 不扣生命值
4. 若 `effectiveAttack > defense`：
   - 扣减 `effectiveAttack - defense`
   - 更新 `lastDamageTime / graceDeadline`
5. 只有当 `currentHealth <= 0` 时，才真正把该格打开，并继续按现有炸药/奖励路径结算

这里明确取消当前 `ExpandSafeRegion()` 的自动连锁开墙。原因很直接：

- 一旦保留连锁瞬开，相邻安全墙就会绕过自己的 `maxHealth / defense / crack` 生命周期。
- 用户本次需求强调“对不同岩层分别配置生命值和防御力，并按破坏进度显示 crack”，这本质上要求“每面墙独立结算”。

备选方案 A：保留链式开墙，只让玩家当前命中的第一面墙走生命/防御。

放弃原因：功能会在第一次击穿后立刻退化回旧模型，玩家看不到相邻墙的耐久与 crack，数值配置也失去一致性。

备选方案 B：把 `currentHealth` 直接写回 `GridCellState`。

放弃原因：`GridCellState` 是地图真相快照，混入短时的受击中间态后，会让地形序列化、测试构造和其它系统读取边界变差。

### 3. 继续复用当前“稳定墙面接触”作为玩家挖掘意图来源，不再新增额外的开始/停止输入

用户已经明确“中途离开”就是现有的挖掘判定失效，因此本次不会新增单独的“停止挖掘”命令。玩家侧继续走：

`Freeform contact -> AutoMineContactResolver -> GameplayInputController.TryAutoMineContact()`

但 `TryAutoMineContact()` 的行为从“累计到阈值后触发一次 `MineTarget()` 并重置状态”改为：

- 稳定接触期间，按 `playerMiningTickIntervalSeconds` 周期性提交同一目标的挖掘 tick
- 失去稳定接触后，不再显式下发破坏命令；只是不再刷新该目标的 `lastDamageTime`
- 由规则层根据 `graceDeadline` 决定是暂停保留还是恢复满血

这样做的好处：

- 完全复用现有“稳定顶住同一面墙才算挖掘”的判定，不需要再造一套射线或 overlap 规则。
- “中途离开”有明确含义：当前帧起不再收到同一目标的有效挖掘 tick。
- `freeform-player-movement` 与 `freeform-actor-control` 的既有接触稳定性要求仍成立，不用再改场景碰撞策略。

备选方案：额外引入一套独立的“正在挖掘目标锁定器”来判断是否离开。

放弃原因：这会和已有稳定接触格判定重叠，既增加复杂度，也更难证明两套判定不会分叉。

### 4. crack 表现改成“按生命百分比选帧并暂停”，而不是持续播放循环序列

当前 `MinebotCellFxView.RefreshPersistent()` 每次刷新都会重新 `Play(sequence)`，更适合“短时间持续闪烁”，不适合表达具体破坏百分比。本次建议扩展 `MinebotSpriteSequencePlayer` / `MinebotCellFxView`，增加“按帧索引静态显示”的能力，例如：

- `SetFrame(SpriteSequenceAsset sequence, int frameIndex, Vector3 worldPosition, int sortingOrder)`
- `ShowPausedFrame(...)`

帧映射规则：

- 读取该 crack sequence 的总帧数 `frameCount`
- 计算 `damage01 = 1 - currentHealth / maxHealth`
- 用平均分布映射到 `0 .. frameCount - 1`
- 在宽限期内保持当前帧不推进
- 当墙体恢复满血时移除 crack view
- 当墙体真正破坏时清除 crack view，并播放现有 `WallBreakFx / ExplosionFx`

这样既满足“crack 帧数根据具体 sprite sequence 决定，平均分布”的要求，也最大化复用现有 `SpriteSequenceAsset` 资源。

备选方案 A：继续让 crack sequence 正常播放，只在不同生命阶段切换不同 prefab。

放弃原因：表现会更多反映时间而不是生命百分比，且资源维护成本更高。

备选方案 B：给每个生命阶段单独做固定 sprite，不再使用 sequence。

放弃原因：用户已经明确希望帧数跟具体 sequence 绑定，本仓库也已经有现成 sequence 资产。

### 5. 机器人沿用共享墙体生命模型，但攻击来源继续贴着现有机器人配置走

机器人当前由 `RobotAutomationService.TickRobot()` 在相邻时直接调用一次 `MiningService.TryMineDetailedFrom()`。本次改造后：

- 目标资格不再只看“硬度是否不高于机器人钻头等级”，而是看“机器人当前有效攻击力是否高于目标防御力”
- 相邻后不再一次性直接打开墙体，而是按自己的 `RobotActionInterval` 提交共享挖掘 tick
- 目标应在墙体未被破坏前继续保留；只有目标失效、路径受阻、危险区变化或墙体破坏后才清空

这里不额外新增“机器人基础攻击力”配置，而是复用现有机器人钻头分支：

- `RobotUsesPlayerDrillTier = true` 时，机器人用玩家当前钻头阶段对应的攻击加值
- `RobotUsesPlayerDrillTier = false` 时，机器人用 `RobotFixedDrillTier` 对应的攻击加值

这样能把本次范围控制在“共享耐久模型 + 最少新配置”内，避免把玩家需求扩大成一整套新的机器人成长树。

备选方案：为机器人单独新增基础攻击和升级表。

放弃原因：用户没有提出这部分需求，而且当前 MVP 机器人本来就不是完整成长主体。

### 6. 开发顺序按“规则状态 -> 会话 tick -> 输入/机器人 -> 表现 -> 测试”推进

推荐顺序：

1. 新增 `MiningRules` 资产与 `BootstrapConfig` 引用。
2. 重构 `MiningService`，引入墙体耐久状态、攻击/防御计算、宽限恢复和真正破坏时机。
3. 改 `GameSessionService`，增加墙体耐久恢复 tick 与玩家挖掘结果快照。
4. 改 `GameplayInputController`，让玩家稳定接触时周期性提交挖掘 tick，而不是一次性 `Mine()`。
5. 改 `RobotAutomationService`，让机器人遵守多 tick 挖掘与新的目标资格判定。
6. 扩展 `MinebotSpriteSequencePlayer` / `MinebotCellFxView` / `MinebotGameplayPresentation`，实现按生命百分比的 crack 暂停/恢复/碎裂表现。
7. 最后补 EditMode / PlayMode 测试和默认数值资产。

## Risks / Trade-offs

- [共享墙体耐久状态会引入额外运行时字典和清理逻辑] → Mitigation：只为被实际挖掘过的墙体懒初始化状态，并在恢复满血或成功破坏后及时移除。
- [取消零风险连锁开墙会改变现有扫雷节奏] → Mitigation：把它明确写入 spec delta，并用回归测试锁定“每面墙都必须走自己的生命/防御过程”。
- [crack 从循环动画改成按帧静态显示，需要扩展现有 sequence 播放器] → Mitigation：只在 `MinebotSpriteSequencePlayer` 上补精确选帧能力，不引入 Animator 或额外第三方表现系统。
- [玩家和机器人使用不同挖掘 tick 节奏，可能导致 DPS 体感不一致] → Mitigation：把玩家 tick 和机器人行动间隔都作为显式 balance 输入，并在测试里按“多少 tick 后破坏”而不是单帧时长断言。
- [墙体生命恢复是时间驱动的，会让离开/返回边界情况更敏感] → Mitigation：统一由规则层管理 `graceDeadline`，不要在输入层和表现层各自维护一份倒计时。

## Migration Plan

1. 在 `BootstrapConfig` 接入新的 `MiningRules` 资产，并为默认 `Gameplay` 配置填入四档生命/防御、玩家基础攻击、钻头加值、`0.1s` tick 和 `0.5s` 宽限。
2. 在 `GridMining` 内实现新的墙体耐久状态机，同时移除当前瞬时 `ExpandSafeRegion()` 主路径。
3. 把玩家和机器人挖掘调用点都改成多 tick 提交，并保留现有 `MineInteractionResult` 中文反馈的兼容文本出口。
4. 扩展表现层 crack 视图，使其读取规则快照而不是单纯“命中一次就重播动画”。
5. 补回归测试后再做默认场景烟雾验证。

回退策略：

- 如果新的耐久状态机或 crack 暂停表现未稳定，可先回退到旧的瞬时 `TryMineDetailedFrom()` 路径和旧 crack 重播逻辑。
- `MiningRules` 资产可在 `BootstrapConfig` 上解除引用，回到现有默认逻辑，不影响地图、奖励和危险区其它配置。

## Open Questions

- 暂无阻塞性问题。本设计默认采用 `0.1s` 的玩家挖掘 tick，并保留以后再切到固定物理帧节奏的空间，但不把这一步作为当前变更的前置条件。
