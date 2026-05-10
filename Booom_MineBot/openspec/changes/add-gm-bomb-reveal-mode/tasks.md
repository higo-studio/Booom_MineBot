## 1. 文档

- [x] 1.1 新增 `add-gm-bomb-reveal-mode` change，明确 GM 显雷仅影响表现层，不改变炸药规则真相。
- [x] 1.2 在 spec / design 中写清默认隐藏、开启可见、关闭清空的边界。

## 2. 运行时实现

- [x] 2.1 在 `MinebotGameplayPresentation` / `TilemapGridPresentation` 增加独立的 GM 炸药 overlay 层与刷新逻辑。
- [x] 2.2 在 `GameplayInputController` 增加 GM 显雷切换入口，并提供 HUD / feedback 状态提示。

## 3. 验证

- [x] 3.1 补 PlayMode 回归，覆盖默认隐藏、开启显示、关闭清空。
- [x] 3.2 运行 `unity.compile(exitPlayMode:true)`、相关 PlayMode 测试和 `openspec validate add-gm-bomb-reveal-mode`。
