## Why

飞书 `BOOOM2026_Changelog` 的“反馈1”明确改变了当前可玩版本的操作与表现假设：玩家和从属机器人不应按格子逐步移动，挖掘也不应依赖空格键单次触发。现在需要把第一轮修改意见固化为新的 OpenSpec 变更，避免后续实现继续沿用旧的网格步进交互。

本变更的目标是在不推翻既有扫雷式逻辑网格、炸药、探测和标记规则的前提下，改成更顺手的俯视角自由移动体验，并补齐标记模式、建筑模式和多格建筑占地约束。

## What Changes

- **BREAKING** 主机器人和从属机器人在场景表现与移动控制上改为自由移动，不再要求每次输入都按逻辑格逐步落点。
- **BREAKING** 玩家挖掘改为“WASD 持续朝岩壁移动时自动开始挖掘”，不再以空格键或独立挖掘键作为主要挖掘入口。
- 保留逻辑网格作为地形、岩壁硬度、炸药、探测数字、标记、危险区和建筑占位的权威数据源；自由移动角色通过碰撞、接触方向和格子查询向规则服务提交意图。
- 调整输入语义：`Q` 触发探测，`E` 进入/退出标记模式，`R` 进入建筑模式；标记模式下镜头或选区跟随鼠标，点击岩壁执行标记或取消标记。
- 新增建筑模式：按 `R` 弹出建筑菜单，选择建筑后在地图空地上预览并确认建造。
- 新增建筑占地配置与碰撞约束：建筑 footprint 不固定为 `1x1`，可由配置决定，占用格需要写入逻辑状态；表现可优先使用 prefab，而不是强制 Tilemap 设施层。
- 从属机器人本轮只迁移到自由移动表现与碰撞路径，复杂手动指令或玩家直接操作从属机器人继续延期。
- 更新 HUD/反馈文案、PlayMode 烟雾测试和相关 EditMode 测试，使第一轮反馈成为后续实现的验收标准。

## Capabilities

### New Capabilities
- `freeform-actor-control`: 覆盖主机器人和从属机器人的自由移动、碰撞接触、逻辑格查询和自动挖掘触发。
- `mode-based-interaction`: 覆盖 `Q` 探测、`E` 标记模式、`R` 建筑模式、鼠标选格和模式间输入锁定。
- `configurable-building-placement`: 覆盖建筑菜单、建筑 footprint 配置、占地校验、碰撞体、prefab 表现和建造结算。

### Modified Capabilities
- 无。当前已归档的主线规格中没有这些能力的顶层 active spec；本变更会以新 capability 形式承接并覆盖 `build-playable-rendered-mvp` 中旧的输入与表现假设。

## Impact

- 影响 `Assets/InputSystem_Actions.inputactions` 与生成的 `MinebotInputActions` wrapper，尤其是 Player action map 的动作命名和绑定。
- 影响 `Assets/Scripts/Runtime/Presentation/GameplayInputController.cs`、`MinebotGameplayPresentation.cs` 及相关 UI/HUD 文案。
- 影响 `GridMining`、`HazardInference`、`Progression`、`Automation` 与表现层之间的命令入口：角色自由移动不能直接改格子真相，只能通过规则服务结算挖掘、探测、标记和建造。
- 影响建筑/设施配置资产，需要新增可配置 footprint、成本、碰撞尺寸和 prefab 引用。
- 影响 PlayMode 烟雾测试与 EditMode 规则测试，需要覆盖自由移动接触岩壁自动挖掘、标记模式、建筑占位校验和多格建筑碰撞。
- 不引入 DOTS/ECS、联机、第三方 FSM/行为树或 Addressables；本轮仍限定在 GameJam MVP 可实现范围内。
