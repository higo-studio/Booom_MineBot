## ADDED Requirements

### Requirement: 启动页必须提供开始与退出入口
`Bootstrap` 场景 SHALL 在进入玩法前显示启动页，并且 MUST 至少提供“开始游戏”和“退出游戏”两个入口。

#### Scenario: 启动页显示基础入口
- **WHEN** 玩家进入启用了启动页的 `Bootstrap` 场景
- **THEN** 界面会显示“开始游戏”和“退出游戏”两个可交互入口

### Requirement: 启动页必须复用项目 UI prefab 体系
启动页 SHALL 作为项目 UI 系统中的 prefab 资源存在，并由运行时加载和绑定；实现 MUST NOT 继续依赖 `OnGUI` 即时绘制。

#### Scenario: 启动页通过 prefab 实例化
- **WHEN** `Bootstrap` 场景启用了启动页
- **THEN** 运行时会实例化一个预制的启动页 View，并通过该 View 绑定开始、退出和排行榜摘要

#### Scenario: 点击开始游戏
- **WHEN** 玩家在启动页点击“开始游戏”
- **THEN** 系统会进入配置指定的玩法场景

#### Scenario: 点击退出游戏
- **WHEN** 玩家在启动页点击“退出游戏”
- **THEN** 系统会调用运行时退出流程；在编辑器中至少返回可观察到的反馈

### Requirement: 本地排行榜必须保留前十名并支持名字录入
系统 SHALL 使用 `PlayerPrefs` 持久化本地前十名成绩；每条记录 MUST 至少包含玩家名字和最终分数，并能在启动页或失败界面被读取展示。

#### Scenario: 新成绩进入前十
- **WHEN** 玩家在失败后输入名字并提交一条足以进入前十的成绩
- **THEN** 系统会把该名字和分数写入本地排行榜，并按分数重新排序

#### Scenario: 启动页显示已有排行榜
- **WHEN** 本地已经存在历史排行榜记录，且玩家重新进入启动页
- **THEN** 启动页会显示当前保存的前十名摘要
