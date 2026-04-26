# automation-and-wave-survival Specification

## Purpose
TBD - created by archiving change bootstrap-minebot-foundation. Update Purpose after archive.
## Requirements
### Requirement: MVP 从属机器人必须执行低风险自动挖掘
MVP SHALL 至少支持一种从属机器人自动模式，该模式只挖掘低风险目标、不会使用探测、会避开玩家标记格，并且在没有安全目标时待机。

#### Scenario: 选择下一个安全挖掘目标
- **WHEN** 一个从属机器人处于激活状态，且至少存在一个可达且未标记的挖掘目标
- **THEN** 机器人会按自动模式优先级规则选择一个安全目标

#### Scenario: 只剩下标记区或不安全目标
- **WHEN** 所有可达目标都已被标记或被判定为不安全
- **THEN** 机器人不会尝试挖掘这些目标，而是保持待机直到条件变化

### Requirement: 地震波必须形成递增的危险压力
游戏 SHALL 周期性触发地震波，对不稳定地形周边结算危险区，并随着波次推进扩大危险覆盖范围，从而迫使玩家持续维护据点周边与关键通道的安全。

#### Scenario: 结算前期地震波
- **WHEN** 单局进行中触发一次地震波
- **THEN** 游戏会按当前波次规则评估并标记不稳定地形周边的危险区

#### Scenario: 进入更高波次
- **WHEN** 单局存活到更高的波次阶段
- **THEN** 地震系统会按配置的成长规则维持或扩大危险覆盖，而不是回退到更弱的基线

### Requirement: 波次必须主导计分和致死结算
单局 SHALL 将存活波次作为主要计分指标，SHALL 在玩家生命值归零时失败，SHALL 在地震结算时玩家仍位于危险区时立即失败，并且 SHALL 销毁处于危险区内的从属机器人并掉落可回收金属。

#### Scenario: 成功撑过新的一波
- **WHEN** 玩家在一次地震结算后仍然存活且不在致死危险区中
- **THEN** 本局的主要“存活波次”计分状态会向前推进

#### Scenario: 在危险区中遭遇地震结算
- **WHEN** 玩家在地震结算时处于致死危险区内
- **THEN** 本局会立即以失败结束

#### Scenario: 从属机器人被危险区消灭
- **WHEN** 一台从属机器人在地震结算时仍位于致死危险区内
- **THEN** 该机器人会被销毁，并留下配置好的可回收金属结果

