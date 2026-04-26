## 1. 移动解算基础重构

- [x] 1.1 抽出独立的 `KinematicCharacterMotor2D` 边界，并定义 `MoveRequest`、`MoveResult`、碰撞 flags 与稳定接触候选数据结构
- [x] 1.2 定义 `ICharacterCollisionWorld2D` 一类查询接口，并用逻辑网格/建筑占位实现对应的 collision world adapter
- [x] 1.3 将 `ActorContactProbe` 的逐轴裁剪替换为基于 sweep-and-move 的 collide-and-slide 解算，补齐 `contactOffset`、`minMoveDistance`、最大迭代次数与 overlap recovery
- [x] 1.4 为独立 motor 编写或重写 EditMode 测试，覆盖直墙滑行、封闭角落阻挡、初始重叠恢复与连续帧接触格稳定性

## 2. 玩家链路与自动挖掘整合

- [x] 2.1 更新 `FreeformActorController` 以消费独立 motor 的 move request / move result，并继续使用表现配置中的统一 footprint 参数
- [x] 2.2 更新 `MinebotGameplayPresentation`，让 `PlayerMiningState` 基于稳定占位结果同步，而不是直接依赖瞬时 `FloorToInt`
- [x] 2.3 重构 `GameplayInputController` 的自动挖掘入口，仅在稳定阻挡命中同一面相邻可挖岩壁后开始自动挖掘
- [x] 2.4 调整阻挡态反馈与刷新触发，避免沿墙滑行或持续顶墙时进入高频 `ShowFeedback -> RefreshAll` 循环
- [x] 2.5 运行聚焦的 EditMode 验证，确认独立 motor、逻辑格同步和自动挖掘门槛协同成立

## 3. 场景回归与变更收口

- [x] 3.1 视需要补充或更新 PlayMode 烟雾测试，覆盖玩家在 `Gameplay`/`DebugSandbox` 中的滑墙与过角表现
- [x] 3.2 手动冒烟验证 `Gameplay` 与 `DebugSandbox`，确认滑墙、角落通过、自动挖掘和逻辑格同步行为一致
- [x] 3.3 运行 `unity.compile`、相关 EditMode/PlayMode 测试与 `openspec validate refine-freeform-player-movement`，整理最终验证结果
