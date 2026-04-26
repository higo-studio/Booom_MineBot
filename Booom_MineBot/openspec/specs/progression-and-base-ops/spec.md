# progression-and-base-ops Specification

## Purpose
TBD - created by archiving change bootstrap-minebot-foundation. Update Purpose after archive.
## Requirements
### Requirement: 经验达标时必须立即触发升级选择
游戏 SHALL 跟踪玩家通过推进挖掘获得的经验，并且 SHALL 在达到当前等级阈值时立即打断正常循环，弹出升级选择 UI。

#### Scenario: 达到升级阈值
- **WHEN** 玩家累计经验达到或超过当前升级阈值
- **THEN** 游戏会直接弹出升级选择 UI，而不要求玩家先回到据点

#### Scenario: 选择一个升级项
- **WHEN** 玩家从升级候选中选择其中一个选项
- **THEN** 对应强化会立刻生效，并且游戏返回进行中的玩法循环

### Requirement: 核心资源必须保持职能分离
MVP 经济系统 SHALL 将金属、能量矿石和经验保持为三种独立资源，其中金属用于维修与建造，能量矿石用于探测相关行为，经验只用于角色成长。

#### Scenario: 消耗探测能量
- **WHEN** 玩家执行一次探测
- **THEN** 本次消耗会从能量资源中扣除，而不会消耗金属或经验

#### Scenario: 结算成长奖励
- **WHEN** 玩家通过挖掘获得经验
- **THEN** 这些经验只会进入升级进度，而不会变成可用于建造的货币

### Requirement: 据点运作必须支持维修和机器人生产
MVP 据点系统 SHALL 提供一个只有在玩家返回后才能维修的维修站，以及一个消耗金属来生产从属机器人的机器人工厂。

#### Scenario: 在维修站进行修理
- **WHEN** 受伤玩家回到维修站并触发维修
- **THEN** 游戏会通过维修流程恢复生命值，而不是让玩家在地图任意位置被动回血

#### Scenario: 生产一个从属机器人
- **WHEN** 玩家在机器人工厂拥有足够金属
- **THEN** 工厂会扣除配置好的成本，并为当前这一局生成一个从属机器人实例

