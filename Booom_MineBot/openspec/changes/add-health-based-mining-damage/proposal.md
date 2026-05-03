## Why

当前挖掘链路在自动挖掘接触成立后会直接触发一次性破坏结算，钻头成长也主要表现为“能不能挖”的硬门槛。这导致四档岩层虽然已经有独立资源配置，却没有独立的耐久节奏，也无法稳定支持按破坏进度显示 crack、离开后短暂停留再回满等更细的挖掘手感。

## What Changes

- 将四种岩层的挖掘数值从单纯硬度门槛扩展为独立配置的生命值与防御力，并继续复用现有奖励配置。
- 为玩家增加基础攻击力与钻头升级加值；当攻击力不高于目标防御力时不造成伤害，高于时按固定挖掘 tick 造成 `攻击力 - 防御力` 的持续伤害。
- 将玩家自动挖掘从“一次提交即直接破坏”改为“稳定接触期间持续推进破坏进度”，并在停止挖掘后保留 `0.5s` 当前生命值与 crack 状态，超时后恢复满生命值。
- **BREAKING**：取消当前运行时对零风险安全墙的瞬时连锁开空，改为每面岩壁都必须走自己的生命/防御与破坏流程。
- 将 crack 表现改为按剩余生命百分比映射到配置的 sprite sequence 帧；中断宽限期内保持当前 crack 帧暂停，真正破坏后再播放碎裂动画并生成资源。
- 将共享挖掘规则同步到从属机器人，避免机器人继续绕过墙体生命/防御模型直接瞬时挖开目标。
- 为规则层、自动挖掘接触链路、表现层和自动化测试补充新的数据结构与回归验证。

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `grid-mining-loop`: 挖掘成功条件从瞬时破坏改为基于岩层生命、防御与矿机攻击力的持续伤害模型，并新增中断宽限与回满规则。
- `gameplay-presentation`: 岩壁 crack / wall break 表现改为跟随破坏进度、支持暂停显示，并只在真正破坏时播放碎裂动画。
- `helper-robot-auto-mode`: 从属机器人对相邻岩壁的处理从单次挖掘结算改为遵守共享的持续伤害与破坏完成规则。

## Impact

- 受影响代码主要位于 `Assets/Scripts/Runtime/GridMining`、`Assets/Scripts/Runtime/Presentation`、`Assets/Scripts/Runtime/Automation`、`Assets/Scripts/Runtime/Bootstrap` 与对应 `Tests`。
- 需要扩展至少一份 ScriptableObject 数值配置，承载四档岩层生命/防御、玩家基础攻击与钻头升级攻击加值，以及挖掘 tick / 中断宽限等参数。
- 现有玩家自动挖掘和机器人自动挖掘都将改为多 tick 结算路径，相关反馈文本、crack 特效与规则回归测试都要同步更新。
