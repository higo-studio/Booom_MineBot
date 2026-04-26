## 1. 建筑交互 UI

- [x] 1.1 在 `MinebotGameplayPresentation` 中新增建筑交互按钮容器，独立于升级面板和建筑模式菜单。
- [x] 1.2 新增维修站按钮和机器人工厂按钮，按钮文案明确显示“维修”和“生产从属机器人”。
- [x] 1.3 按玩家与维修站、机器人工厂的逻辑交互范围刷新按钮显隐，离开范围后隐藏。
- [x] 1.4 更新 HUD 交互提示文案，明确靠近建筑后点击按钮执行建筑动作。

## 2. 按钮行为与输入锁定

- [x] 2.1 将维修站按钮点击绑定到现有 `TryRepairAtStation` 流程，不在 UI 层复制资源扣除或回血逻辑。
- [x] 2.2 将机器人工厂按钮点击绑定到现有 `TryBuildRobotAtFactory` 流程，不在 UI 层复制机器人生产逻辑。
- [x] 2.3 在升级待选择、GameOver、Marker 模式和 Build 模式期间禁用或隐藏建筑交互按钮。
- [x] 2.4 保留 `GameplayInputController.Repair()` 和 `BuildRobot()` 作为内部/测试辅助入口，但不把它们作为玩家唯一可见路径。

## 3. 测试覆盖

- [x] 3.1 更新 PlayMode 烟雾测试，使维修流程通过维修站 UI 按钮触发并验证生命恢复。
- [x] 3.2 更新 PlayMode 烟雾测试，使从属机器人生产通过机器人工厂 UI 按钮触发并验证机器人实例与表现生成。
- [x] 3.3 补充按钮显隐测试：远离建筑时隐藏，靠近维修站/机器人工厂时显示对应按钮。
- [x] 3.4 补充输入锁定测试：升级面板或 Build 模式打开时，建筑交互按钮不会执行维修或生产机器人。

## 4. 验证

- [x] 4.1 使用 UnityMCP 执行 `unity.compile`，如 Play Mode 阻塞则按项目 skill 约定退出 Play Mode 后重试。
- [x] 4.2 运行相关 PlayMode 测试，确认维修、生产机器人和按钮锁定路径通过。
- [x] 4.3 手动烟雾验证 `Gameplay`：靠近维修站和机器人工厂时按钮可读、可点、反馈正确。
- [x] 4.4 运行 `openspec validate unify-building-interaction-buttons`。
