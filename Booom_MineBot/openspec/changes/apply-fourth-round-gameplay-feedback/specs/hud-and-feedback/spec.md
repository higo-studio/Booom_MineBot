## MODIFIED Requirements

### Requirement: 地震波和失败状态必须有明确提示
项目 SHALL 在地震波倒计时、分步结算和失败发生时提供明确反馈；当地震触发炸弹爆炸时，系统 MUST 同时播放与玩家/机器人挖雷一致的爆炸特效。

#### Scenario: 地震阶段炸弹爆炸
- **WHEN** 地震外围炸弹阶段引爆了一颗炸弹
- **THEN** 表现层会在对应格子播放爆炸特效与等价音频反馈

#### Scenario: 玩家失败后显示成绩
- **WHEN** 玩家生命值归零或地震结算导致失败
- **THEN** HUD 会显示本局最终得分，并允许玩家进入本地排行榜录入流程

### Requirement: 失败录分界面必须复用 HUD prefab 体系
失败后的录分与排行榜界面 SHALL 作为 HUD slot 下的 prefab 面板存在，并由 `MinebotHudView` 在运行时绑定；实现 MUST NOT 继续依赖 `OnGUI` 即时绘制。

#### Scenario: 失败录分界面通过 HUD prefab 显示
- **WHEN** 玩家进入失败状态
- **THEN** HUD 会显示一个 prefab 化的失败面板，其中包含得分摘要、名字录入、提交按钮和本地排行榜摘要
