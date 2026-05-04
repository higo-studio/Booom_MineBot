## 1. 波次规则底座

- [x] 1.1 扩展 `WaveConfig` 与默认配置资产，加入波次阶段停顿时长和本次结算所需的最小配置读取入口。
- [x] 1.2 拆分 `WaveSurvivalService.ResolveWave()`，提供“准备本轮波次”“按指定半径重算危险区”“按当前危险区执行判死与塌方”的分步接口。
- [x] 1.3 为 `WaveSurvivalService` 补 EditMode 测试，覆盖“先重算危险区再塌方”“用更新后的危险区判玩家/机器人生死”“结算后立即恢复下一波预警”。

## 2. 外围炸弹与阶段编排

- [x] 2.1 在 `HazardService` 增加外围炸弹候选收集逻辑，按“岩壁含炸药且四邻至少一格空地”的规则生成稳定顺序的快照列表。
- [x] 2.2 让波次炸弹阶段复用现有爆炸与连锁地形改写逻辑，并确保环境触发不会额外结算玩家直接挖雷伤害。
- [x] 2.3 在 `GameSessionService` 中新增 `WaveResolutionState`（或等价结构），编排“外围炸弹 -> 危险区重算 -> 塌方回填”三阶段和阶段计时。
- [x] 2.4 为阶段编排补 EditMode 测试，覆盖“外围炸弹只按开始时快照选取”“前一个爆炸清掉后续候选时会跳过”“爆炸后地图先重算危险区再进入塌方”。

## 3. 表现层与输入冻结

- [x] 3.1 改造 `MinebotGameplayPresentation.Update()`，把当前一帧式 `ResolveWave()` 切到按阶段推进，并在阶段切换时刷新地图与 HUD。
- [x] 3.2 调整 `GameplayInputController`、机器人自动化 tick、被动危险感知和拾取收集逻辑，在波次结算阶段统一进入动作锁定。
- [x] 3.3 更新 HUD / 反馈文案，明确显示“外围炸弹”“危险区重算”“塌方回填”当前步骤和“动作已暂停”的提示。
- [x] 3.4 补 PlayMode 测试，覆盖波次结算期间普通输入被冻结、阶段提示可见，以及结算完成后恢复常规操作。

## 4. 验证与收口

- [ ] 4.1 在 `Gameplay` 与 `DebugSandbox` 做手动烟雾验证，确认外围炸弹、危险区更新、最终塌方和失败判定顺序符合预期。
- [x] 4.2 使用 UnityMCP 执行 `unity.compile`，并运行本次相关 EditMode / PlayMode 测试集，锁定阶段化波次结算回归。
- [x] 4.3 运行 `openspec validate stage-wave-end-perimeter-bomb-collapse`，确认 proposal、design、specs 和 tasks 结构完整可进入 `/opsx:apply`。
