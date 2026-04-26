# grid-mining-loop Specification

## Purpose
TBD - created by archiving change bootstrap-minebot-foundation. Update Purpose after archive.
## Requirements
### Requirement: 单局开始时必须生成离散可通行网格
游戏 SHALL 在每次开局时生成一张离散方格地图，其中包含玩家初始安全空地和与之相邻、可继续向外推进的可挖岩壁。

#### Scenario: 创建新的一局
- **WHEN** 游戏开始新的一局
- **THEN** 玩家会出生在可通行格中，而不是出生在岩壁或危险格内

#### Scenario: 从起始空地区域向外推进
- **WHEN** 玩家移动到初始空地边缘
- **THEN** 相邻的岩壁格会成为下一步可交互、可挖掘的推进目标

### Requirement: 移动与挖掘必须遵守网格和钻头门槛
玩家 SHALL 在可通行格之间移动，并且 SHALL 能挖掘自己接触到的岩壁格，但对于硬度高于当前钻头等级的岩壁 SHALL NOT 成功破坏。

#### Scenario: 在空地中移动
- **WHEN** 玩家向相邻可通行格输入移动指令
- **THEN** 玩家会按网格步进进入目标格

#### Scenario: 尝试挖掘高于当前钻头等级的岩壁
- **WHEN** 玩家与一块硬度高于当前钻头等级的岩壁交互
- **THEN** 该岩壁保持完整，且本次交互不会结算成功的挖掘奖励

### Requirement: 挖掘必须改变地形并发放配置化奖励
成功挖开可破坏岩壁时，系统 SHALL 将该格转换为可通行空间，并且 SHALL 按岩壁类型发放预设资源与经验。

#### Scenario: 成功破坏可挖岩壁
- **WHEN** 玩家完成对一块可破坏岩壁格的挖掘
- **THEN** 该格会转变为后续可用于移动和寻路的可通行空间

#### Scenario: 结算挖掘奖励
- **WHEN** 一块岩壁格被成功破坏
- **THEN** 游戏会按该岩壁定义发放对应的金属、能量矿石和/或经验奖励

