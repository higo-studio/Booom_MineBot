## Why

第一轮反馈把维修站和机器人工厂迁移为建筑/设施占位后，玩家侧仍缺少统一的交互入口：维修和生产从属机器人保留在代码方法里，但正常操作中没有清晰按钮。现在需要把“靠近建筑后执行建筑能力”的交互固化为统一规则，避免维修站、机器人工厂和后续建筑各自散落独立按键或隐藏测试入口。

## What Changes

- 新增靠近建筑时显示的建筑交互按钮区域，用于执行当前建筑暴露的主动作。
- 维修站通过建筑交互按钮触发维修，继续走现有金属消耗、生命恢复和反馈流程。
- 机器人工厂通过建筑交互按钮触发生产从属机器人，继续走现有金属消耗、机器人生成和反馈流程。
- 交互按钮只在玩家靠近可交互建筑、且当前没有升级锁定、GameOver、标记模式或建筑放置模式冲突时可用。
- HUD 文案从“靠近设施，可通过据点流程……”调整为明确的按钮交互提示。
- 保留底层 `TryRepairAtStation` / `TryBuildRobotAtFactory` 规则入口供测试与 UI 调用，但不再把它们作为玩家唯一可触达路径。
- 为按钮点击路径补 PlayMode 回归测试，覆盖维修站按钮和机器人工厂按钮。

## Capabilities

### New Capabilities

- `building-interaction-buttons`: 覆盖玩家靠近建筑后通过统一按钮执行建筑能力，包括维修站维修、机器人工厂生产从属机器人，以及后续可扩展的建筑主动作。

### Modified Capabilities

- 无。当前 active specs 中没有已归档的建筑交互按钮能力；本变更作为新 capability 承接第一轮反馈后暴露出的 UI 入口缺口。

## Impact

- 影响 `Assets/Scripts/Runtime/Presentation/MinebotGameplayPresentation.cs`：新增建筑交互按钮容器、按钮状态刷新、点击回调和 HUD 提示。
- 影响 `Assets/Scripts/Runtime/Presentation/GameplayInputController.cs`：如保留测试辅助方法，需要确保 UI 按钮和输入锁定规则一致。
- 影响 `Assets/Scripts/Tests/PlayMode/RenderedGameplayPlayModeTests.cs`：将维修/生产机器人烟雾测试迁移到按钮路径。
- 不改变底层维修、机器人生产、资源消耗、建筑占位或自动机器人规则。
- 不新增新的全局快捷键，不重新引入旧的 `R` 维修或 `B` 造机器人按键。
