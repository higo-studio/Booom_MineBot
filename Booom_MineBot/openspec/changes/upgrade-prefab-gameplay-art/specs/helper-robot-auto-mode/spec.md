## ADDED Requirements

### Requirement: 自动模式状态必须驱动从属机器人 prefab 表现
从属机器人自动模式 SHALL 将 `Idle`、`Moving`、`Mining`、`Blocked`、`Destroyed` 或等价运行时状态同步到从属机器人 prefab 的表现层，使玩家能通过动画、图片序列、材质或等价视觉状态理解机器人当前行为，而不是只能依赖 HUD 文案。

#### Scenario: 移动和挖掘状态切换
- **WHEN** 一个从属机器人从寻路移动切换到贴墙挖掘
- **THEN** 该机器人的 prefab 表现会从移动状态切换到挖掘状态，并与其实际自动模式状态保持一致

#### Scenario: 受阻或损毁状态可被直接读出
- **WHEN** 一个从属机器人因为路径受阻而停下，或因为炸药/地震被摧毁
- **THEN** 玩家能从该 prefab 的视觉状态直接看出它已受阻或已损毁，而不是只有数量变化或纯文本提示
