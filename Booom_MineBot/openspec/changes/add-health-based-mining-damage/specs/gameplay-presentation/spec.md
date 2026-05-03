## ADDED Requirements

### Requirement: 主玩法场景必须按挖掘进度显示岩壁破坏反馈
主玩法场景 SHALL 将正在被挖掘的岩壁显示为与当前破坏进度一致的 crack 状态。crack 帧 MUST 按目标岩壁当前生命百分比平均映射到该岩壁使用的 sprite sequence 帧数；当玩家停止挖掘且尚处于宽限期内时，画面 MUST 保持当前 crack 帧暂停；当岩壁真正破坏时，场景 MUST 清除 crack 并播放碎裂动画。

#### Scenario: 按生命百分比更新 crack 帧
- **WHEN** 玩家持续挖掘一块岩壁并多次成功造成伤害
- **THEN** 该岩壁上显示的 crack 会随剩余生命值下降切换到更靠后的序列帧

#### Scenario: 宽限期内暂停 crack 状态
- **WHEN** 玩家暂时离开某块仍处于宽限期内的受损岩壁
- **THEN** 该岩壁继续显示离开瞬间的 crack 帧，而不会自动继续播放或立刻消失

#### Scenario: 真正破坏后播放碎裂动画
- **WHEN** 某块岩壁生命值降到 `0` 并被转换为空地
- **THEN** 场景会移除该岩壁的 persistent crack 表现，并播放对应的碎裂动画
