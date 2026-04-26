# hazard-inference Specification

## Purpose
TBD - created by archiving change bootstrap-minebot-foundation. Update Purpose after archive.
## Requirements
### Requirement: 隐藏炸药必须在开局前生成
游戏 SHALL 在地图生成阶段确定哪些岩壁格埋有炸药，并且 SHALL 在玩家通过交互或爆炸结算揭示之前保持这些位置隐藏。

#### Scenario: 生成新地图
- **WHEN** 系统生成一张新地图
- **THEN** 炸药位置会在玩家执行第一步操作前就固定到本局地图状态中

#### Scenario: 查看未被触发的岩壁
- **WHEN** 玩家尚未对某个岩壁格进行挖掘、探测或爆炸触发
- **THEN** 该岩壁不会直接暴露自己是否含有炸药

### Requirement: 探测必须消耗能量并返回数字提示
探测动作 SHALL 消耗能量，并且 SHALL 按一套一致的邻域规则返回周边炸药信息的数字反馈。

#### Scenario: 执行一次有效探测
- **WHEN** 玩家在能量充足时触发探测
- **THEN** 游戏会扣除配置好的能量消耗，并展示对应区域的数字风险提示

#### Scenario: 在能量不足时尝试探测
- **WHEN** 玩家在能量不足的情况下触发探测
- **THEN** 游戏不会结算新的风险提示，并保持现有炸药隐藏状态不变

### Requirement: 玩家标记必须表示疑似炸药格
玩家 SHALL 能对候选岩壁格进行标记和取消标记，并且该标记状态 SHALL 同时可被玩家 HUD 和从属机器人安全逻辑读取。

#### Scenario: 标记一个可疑危险格
- **WHEN** 玩家对尚未揭示的岩壁格进行标记
- **THEN** 该格会保存持久化标记状态，直到玩家主动清除或该格被摧毁

#### Scenario: 其它系统查询安全信息
- **WHEN** 其它运行时系统查询玩家已经标记的岩壁格
- **THEN** 该系统能够将该格视为玩家确认过的危险数据

### Requirement: 爆炸必须只对玩家造成一次直接伤害并结算连锁地形变化
当玩家挖到含炸药的岩壁格时，系统 SHALL 对玩家结算一次直接伤害，SHALL 按爆炸规则破坏附近地形，SHALL 触发相邻炸药的连锁爆炸，并且 SHALL NOT 因同一次连锁事件对玩家重复结算直接伤害。

#### Scenario: 直接挖到炸药格
- **WHEN** 玩家破坏一块含有炸药的岩壁格
- **THEN** 玩家会损失配置好的直接伤害生命值，并且爆炸会对附近地形进行结算

#### Scenario: 通过连锁反应引爆相邻炸药
- **WHEN** 初始爆炸波及另一块含炸药岩壁
- **THEN** 第二次爆炸会继续结算地形效果，但不会针对同一触发事件再额外结算一次玩家直接伤害

