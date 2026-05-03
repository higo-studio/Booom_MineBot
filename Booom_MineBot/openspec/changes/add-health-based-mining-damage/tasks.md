## 1. 挖掘配置与规则底座

- [x] 1.1 新增 `MiningRules`（或等价命名）ScriptableObject，配置四档岩层的生命值/防御力、玩家基础攻击、钻头阶段攻击加值、玩家挖掘 tick 间隔和 `0.5s` 中断宽限。
- [x] 1.2 在 `BootstrapConfig`、默认 `Gameplay` 配置资产和 `MinebotServices.Initialize()` 中接入新的挖掘规则配置，保证运行时可读取默认数值。
- [x] 1.3 重构 `MiningService`，为被挖掘岩壁维护运行时生命状态、攻击/防御结算、宽限恢复和真正破坏时机，并移除当前 `ExpandSafeRegion()` 的瞬时连锁开墙主路径。
- [x] 1.4 为 `GridMining` 规则层补 EditMode 测试，覆盖四档岩层生命/防御读取、攻击不足不掉血、累计伤害到 `0` 才破坏，以及超过宽限自动回满。

## 2. 玩家挖掘循环

- [x] 2.1 扩展 `GameSessionService`，提供玩家挖掘 tick、墙体恢复 tick 和可供表现层读取的挖掘进度快照或事件。
- [x] 2.2 改造 `GameplayInputController.TryAutoMineContact()`，继续复用现有稳定接触判定，但改为按配置节奏重复提交同一目标的挖掘 tick，而不是一次性直接 `Mine()`。
- [x] 2.3 调整玩家挖掘相关反馈文案与结果枚举兼容路径，确保“攻击不足 / 正在挖掘 / 挖掘完成 / 触发炸药”都能区分。
- [x] 2.4 补玩家挖掘 EditMode / PlayMode 回归，覆盖稳定接触后持续掉血、离开 `0.5s` 内续挖、离开超时回满，以及炸药墙在生命归零后才触发破坏。

## 3. 从属机器人共享耐久模型

- [x] 3.1 改造 `RobotAutomationService` 目标资格判定，从“硬度不高于钻头等级”切换为“当前有效攻击力高于目标防御力”，并保持不读取隐藏炸药信息。
- [x] 3.2 让机器人在相邻目标后按自身 `RobotActionInterval` 持续提交共享挖掘 tick，直到目标破坏、失效或路径/风险条件变化后才清空目标。
- [x] 3.3 补机器人 EditMode 测试，覆盖跳过高防御目标、对同一目标多 tick 持续挖掘、成功破坏后发奖，以及升级/失败期间暂停自动挖掘。

## 4. 挖掘表现与 crack 动画

- [x] 4.1 扩展 `MinebotSpriteSequencePlayer` / `MinebotCellFxView`，支持按指定帧静态显示 crack，并在宽限期内保持暂停而不是循环重播。
- [x] 4.2 改造 `MinebotGameplayPresentation`，根据墙体当前生命百分比平均映射 crack 帧，在恢复满血时清除 crack，在真正破坏时播放碎裂动画并移除 persistent crack。
- [x] 4.3 检查默认 crack / wall break / explosion 资源与 sprite sequence 帧数兼容性，保证不同 sequence 长度都能正确映射。
- [x] 4.4 补 PlayMode 验证，覆盖 crack 随破坏进度推进、宽限期暂停停帧、超时恢复后清除 crack，以及破坏后播放碎裂动画。

## 5. 集成验证与文档收口

- [ ] 5.1 在 `Gameplay` 与 `DebugSandbox` 做手动烟雾验证，确认玩家贴墙自动挖掘、离开续挖/回满、机器人挖掘和资源掉落都符合新规则。
- [x] 5.2 使用 UnityMCP 执行 `unity.compile`，并运行本次相关 EditMode / PlayMode 测试集，锁定新挖掘模型与表现层回归。
- [x] 5.3 运行 `openspec validate add-health-based-mining-damage`，确认 proposal、design、specs 和 tasks 结构完整可归档。
