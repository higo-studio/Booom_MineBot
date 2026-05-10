# Why

当前运行时仍然严格隐藏炸药真相，这对正常玩法是对的，但在调试地图、验证爆炸链路或排查机器人行为时，缺少一个可随时开启的显雷入口。现有工程里也没有独立的 GM / 调试可视化模式，只能靠手工改数据或临时打日志，效率太低。

# What Changes

- 新增一个默认关闭的 GM 显雷模式，用于在运行时显示当前仍存在的炸药格位置。
- GM 显雷只影响表现层可视化，不改变炸药生成、隐藏规则、探测、标记、机器人选点或爆炸结算。
- 为 `GameplayInputController`、`MinebotGameplayPresentation` 和 `TilemapGridPresentation` 增加独立的开关与 overlay 刷新链路。
- 补 PlayMode 回归，覆盖默认隐藏、开启后显示、关闭后清空。

# Impact

### Modified Capabilities

- `hazard-inference`: 隐藏炸药在正常模式下继续保持不可见，但运行时允许通过 GM 模式临时显示当前位置。
- `hud-and-feedback`: HUD 交互提示会反映 GM 显雷开关状态，便于确认当前是否处于调试可视化模式。
