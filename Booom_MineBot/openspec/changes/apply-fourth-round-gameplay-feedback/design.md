## Context

第四轮反馈最初落地时，运行时链路还是 `BootstrapSceneLoader -> MinebotServices.Initialize -> RuntimeServiceRegistry -> MinebotGameplayPresentation.Update()`。在补齐 UI、排行榜和波次逻辑后，又暴露出一个额外问题：`GameplayInputController`、HUD presenter 和表现层初始化都还在直接读取全局 `MinebotServices`，导致场景耦合偏高，PlayMode 注入顺序也不够清晰。除原有问题外，本轮还顺手把运行时组装改为显式注入。

- `GameSessionService.Mine()` 与机器人误挖炸药路径只会把目标格打开并返回 `TriggeredBomb`，但没有继续调用 `HazardService.ResolveExplosion(...)`，因此连锁破坏被截断。
- `WaveSurvivalService.CollapseResolvedDangerZone()` 会把危险区空地直接回填成普通墙体，且没有独立的“回填墙体混雷”配置，也没有稳定配额抽样逻辑。
- `GameplayInputController` 仍把标记绑定到模式切换；`HazardService.ToggleMarker()` 也没有上限概念，无法承接“标记数量升级”。
- `UpgradeSelectionService` 当前只会直接截取升级池前 `N` 个候选，且成长只支持 `drillTierDelta` / `maxHealthDelta`，不支持移动速度、标记数量和显式伤害加值。
- 项目尚无统一的得分、排行榜和启动页模块；失败反馈只停留在 HUD 文案，无法记录局外结果。

本次设计继续遵守仓库边界：

- 规则真相留在纯 C# 服务，不把计分或波次厚度写死在 MonoBehaviour。
- 数值优先进入 `ScriptableObject`：危险区逐波厚度、塌方混雷比例、升级项效果、计分规则都走配置资产。
- 启动页和排行榜表现继续走项目现有 `Runtime/UI + Resources prefab + prefab builder` 体系，不再使用即时模式 `OnGUI`。

## Goals / Non-Goals

**Goals:**

- 恢复炸弹触发后的连锁破坏，并让玩家、机器人、地震触发统一走同一套爆炸地形逻辑。
- 让危险区塌方回填的新墙体按稳定配额随机混入炸弹，实际占比贴近配置值。
- 移除标记模式，支持直接点击切换标记，并通过升级增加可同时保留的标记数。
- 将钻头成长改为显式伤害加值，并加入移动速度与标记数量升级；升级候选固定为每次随机两项。
- 新增分行为计分、本地前十排行榜和启动页开始/退出流程。

**Non-Goals:**

- 不重做建筑类型、资源系统或机器人寻路策略。
- 不引入联网排行榜、云存档或跨设备同步。
- 不把启动页扩展成完整设置菜单；本轮只落地开始、退出和排行榜摘要。

## Decisions

### 1. 用 `ScoreConfig + ScoreService` 承载局内评分真相

新增 `ScoreConfig` 资产，集中配置：

- 不同硬度岩壁的手动破坏得分
- 地震炸毁单颗炸弹得分
- 成功渡过一次地震得分
- 建筑建成得分默认值

运行时新增 `ScoreService`，由 `GameSessionService` 和 `MinebotGameplayPresentation` 驱动以下事件：

- 玩家手动挖开岩壁后加分
- 地震炸弹阶段炸毁炸弹后加分
- 波次结算结束且玩家未死后加分
- 建筑建造成功后加分

选择理由：

- 计分不再散落到 UI 文案或波次服务里，便于 EditMode 直接断言。
- 分值配置与升级配置解耦，后续调分不会误伤成长平衡。

### 2. 连锁破坏通过 `GameSessionService` 统一补回，而不是把爆炸逻辑塞回表现层

`MiningService` 继续只负责“挖开目标格”和基础产出，不直接依赖 `HazardService`。当 `GameSessionService.Mine()` 或机器人误挖返回 `TriggeredBomb` 时，再由 session 统一：

1. 读取爆炸半径配置
2. 调用 `HazardService.ResolveExplosion(...)`
3. 合并被炸开的格子、奖励与炸弹统计
4. 触发计分、被动感知刷新和表现层刷新

地震波外围炸弹阶段沿用同一套 `HazardService` 爆炸入口，并把“本次波次实际炸毁了哪些炸弹”记录给表现层和计分系统。

这样做的原因：

- 玩家、机器人、地震都共享同一份爆炸地形真相。
- 表现层只消费爆炸摘要，避免再推导“哪些格该播爆炸”。

### 3. 危险区厚度改为 50 档表驱动，超过范围后钳制到最后一档

`WaveConfig` 新增 `dangerRadiusByWave` 数组，语义固定为：

- 下标 `0` 对应第 1 波
- 下标 `49` 对应第 50 波
- `wave > 50` 时继续返回数组最后一项

保留旧基础字段只作为缺省回退，避免旧资产立即失效；但 `Gameplay` 默认资产将切到逐波表配置。

选择理由：

- 用户明确要求 `0~50 波` 独立配置，公式增长不再满足需求。
- 钳制到最后一档能让高波继续稳定运行，不需要无限补表。

### 4. 塌方混雷使用“确定数量 + 稳定洗牌”而不是逐格伯努利

对一次塌方中所有即将回填的空地：

1. 先收集候选格总数
2. 用 `round(count * ratio)` 得到本轮应混入的炸弹数量
3. 基于波次、地图坐标和配置种子做稳定洗牌
4. 只给前 `N` 个候选格写入炸弹标记

这样做比原来的“每格各抽一次 `random < chance`”更接近“按规定比例”，也更稳定可测。

### 5. 玩家成长状态扩展到 `PlayerMiningState`

`PlayerMiningState` 继续保存玩家的局内成长真相，并新增：

- `MiningDamageBonus`
- `MoveSpeedMultiplier`
- `MarkerCapacity`

`UpgradeDefinition` 扩展为显式配置：

- `maxHealthDelta`
- `miningDamageDelta`
- `moveSpeedMultiplierDelta`
- `markerCapacityDelta`

`UpgradeSelectionService` 每次从升级池中做带权随机抽取两项候选，不再按数组顺序截取。这样可以直接把“钻头升级”解释为伤害加值，而不再把升级增幅隐含在挖掘规则档位表里。

### 6. 本地排行榜采用 `PlayerPrefs + JSON`，入口挂在启动页和失败界面

新增轻量 `LocalLeaderboardService`：

- 只保存前十名
- 每条记录包含名字、分数、波次和时间戳
- 数据序列化后写入一个 `PlayerPrefs` key

失败后如果当前成绩达到可展示范围，HUD 会允许输入名字并保存；启动页读取并展示当前前十。两处新增界面都通过 `Resources/Minebot/UI/...` 下的 prefab 资源提供，由 `BootstrapSceneLoader` 和 `MinebotHudView` 在运行时实例化并绑定，不再直接写 `OnGUI()`。

### 7. 运行时服务组装改为 `composition root + RuntimeContext + 显式注入`

保留 `RuntimeServiceRegistry` 作为规则层服务包，但不再让运行时表现脚本主动依赖 `MinebotServices.Current`。新的装配方式是：

1. `RuntimeServiceFactory` 只负责根据 `BootstrapConfig` 构造 `RuntimeServiceRegistry`
2. `BootstrapSceneLoader` 作为 composition root，持有 `MinebotRuntimeContext`
3. `MinebotRuntimeContext` 在场景加载后把 `RuntimeServiceRegistry` 与 `BootstrapConfig` 注入给实现 `IMinebotServiceConsumer` 的组件
4. `MinebotGameplayPresentation` 再把同一份 registry 继续传给输入控制器和 HUD presenter

`MinebotServices` 仍保留为兼容层和测试辅助入口，但不再作为运行时主读取路径。这样能减少表现层对全局状态的隐式依赖，也让 Bootstrap 场景与 Gameplay 场景之间的装配顺序更容易测试。

## Risks / Trade-offs

- [连锁破坏补回后，现有测试里“只清一个炸弹格”的断言会变化] → 通过补充 EditMode 用例锁定新的连锁结果。
- [标记上限引入后，机器人安全逻辑会更频繁遇到“旧标记被挤掉”] → 默认实现采用“超上限则拒绝新增”，不自动替换旧标记，减少意外。
- [启动页和失败录分都需要局外 UI，新代码容易和现有 HUD 生命周期打架] → 启动页只存在于 `Bootstrap` 场景；失败录分只存在于 `Gameplay` HUD，职责分离。
- [逐波厚度表和塌方混雷比例都依赖新资产字段，旧资源可能为空] → 所有读取路径必须提供安全回退，避免空数组导致运行时异常。

## Migration Plan

1. 扩展 OpenSpec 和配置资产骨架：`WaveConfig`、`UpgradePoolConfig`、`ScoreConfig`。
2. 补回 `GameSessionService` 的炸弹连锁、塌方混雷和计分调用。
3. 改输入与成长：去掉标记模式，两选一升级，接入移动速度与标记上限。
4. 接入启动页、本地排行榜和失败录分 UI。
5. 更新相关 EditMode / PlayMode 测试并跑 Unity 编译、OpenSpec 校验。
