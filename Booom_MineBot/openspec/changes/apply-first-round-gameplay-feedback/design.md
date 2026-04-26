## Context

`build-playable-rendered-mvp` 当前设计把主玩法输入收敛到语义动作，并假设 `Move` 会被离散化为四方向网格步进，挖掘由 `Mine` 动作触发。飞书 `BOOOM2026_Changelog` 的第一轮反馈推翻了这部分交互假设：玩家机器人和从属机器人需要自由移动，玩家持续朝墙移动即可自动挖掘，标记和建筑需要进入独立模式并通过鼠标选择地图目标。

本设计承接这些反馈，同时保留 `bootstrap-minebot-foundation` 的核心边界：`LogicalGridState`、规则服务和 ScriptableObject 配置仍是玩法真相；Scene、Prefab、Collider、Tilemap 和 UI 只负责输入、表现、碰撞反馈与预览。

## Goals / Non-Goals

**Goals:**

- 将主机器人和从属机器人从“逐格步进表现”迁移为“世界坐标自由移动表现”。
- 保持逻辑网格负责地形、岩壁硬度、炸药、探测、标记、危险区、资源和建筑占位。
- 实现 WASD 自由移动与持续贴墙自动挖掘，不再要求玩家按空格或独立挖掘键。
- 实现 `Q` 探测、`E` 标记模式、`R` 建筑模式与鼠标选格。
- 支持建筑 footprint 配置、资源消耗、占地校验、碰撞阻挡和 prefab 表现。
- 更新测试与烟雾验证，使第一轮反馈成为可检查的实现标准。

**Non-Goals:**

- 不引入 DOTS/ECS、联机、Addressables、第三方 FSM/行为树或第三方寻路库。
- 不把 Unity 物理碰撞、场景对象或 Tilemap 变成玩法真相。
- 不在本轮实现复杂从属机器人手动指令、编队控制或玩家直接操作从属机器人。
- 不制作最终美术、完整动画状态机或正式建筑科技树。

## Decisions

### 1. 采用“连续世界坐标 + 离散逻辑网格”的混合模型

主机器人和从属机器人拥有连续 `Vector2` 世界坐标与碰撞体；规则服务继续保存离散格子状态。表现层通过角色碰撞体中心、接触点和朝向查询对应逻辑格，并向规则服务提交“尝试挖掘某格”“标记某格”“尝试放置建筑”等命令。

选择理由：

- 反馈要求自由移动，但扫雷式炸药、探测数字和标记天然依赖离散格。
- 规则层继续可 EditMode 测试，避免把可变场景物理状态揉进核心判定。
- 后续仍能复用现有网格挖掘、风险判断、波次危险区和机器人安全逻辑。

备选方案：完全改成物理碰撞场景即真相。

放弃原因：炸药隐藏信息、探测邻域、标记避险、建筑占位和地震危险区会分散到场景对象上，难以测试和回放。

### 2. 输入层改为模式化状态机

`GameplayInputController` 继续只转发意图，但 Player action map 需要改为第一轮反馈语义：

```text
Move              Value Vector2       WASD / Arrow / Gamepad leftStick
Scan              Button              Q
ToggleMarkerMode  Button              E
ToggleBuildMode   Button              R
PointerPosition   PassThrough Vector2 Mouse / Pointer
PointerClick      Button              Mouse left
Cancel            Button              Escape / Mouse right / Gamepad east
SelectUpgrade1    Button              1
SelectUpgrade2    Button              2
SelectUpgrade3    Button              3
Pause             Button              Escape / Gamepad start
```

控制器至少区分 `Normal`、`Marker`、`Build`、`UpgradeLocked`、`GameOver`。`Normal` 中 Move 驱动自由移动和自动挖掘，`Marker` 中鼠标点击岩壁执行标记，`Build` 中鼠标点击合法空地执行建造，`UpgradeLocked` 和 `GameOver` 阻止主玩法输入继续改状态。

选择理由：

- `E` 和 `R` 都是持续模式，而不是一次性动作。
- 模式化状态机比在每个输入回调里散落布尔条件更容易测试。
- 后续加入从属机器人操作时，可以新增 `RobotCommand` 模式，而不改掉当前模式边界。

备选方案：保留旧的 `Mine`、`ToggleMarker`、`BuildRobot` 独立按钮。

放弃原因：这会和反馈中的“贴墙自动挖掘”“E 标记模式”“R 建筑模式”冲突。

### 3. 自动挖掘由接触检测驱动，结算仍走规则服务

表现层新增接触探测职责：当玩家持续按住 Move，且角色碰撞体在移动方向上接触可挖岩壁对应的逻辑格时，进入自动挖掘状态。挖掘状态按配置的节奏向 `GameSessionService` 或等价规则入口提交挖掘命令；硬度门槛、炸药、掉落、经验和地形转换仍由规则服务结算。

实现上优先使用 Unity 2D Collider/Rigidbody2D 做阻挡和接触回调，但规则服务不读取 Collider 作为真相。表现层只把“接触到哪个逻辑格”转成命令。

选择理由：

- 玩家手感接近自由移动动作游戏。
- 旧的钻头等级、硬度和奖励规则无需重写为物理对象逻辑。
- 挖掘失败、硬度不足和炸药触发仍能沿用已有测试思路。

备选方案：把岩壁拆成可破坏 prefab，每个 prefab 自己保存血量和炸药。

放弃原因：会破坏隐藏炸药和探测数字的集中模型，也会让机器人和地震波查询变重。

### 4. 从属机器人使用自由移动表现，自动策略仍基于逻辑格

从属机器人自动模式继续按“低风险目标、避开标记、无目标待机”的策略选择逻辑格目标，但移动表现改为连续世界坐标。机器人会移动到目标岩壁附近的可达接触点，再由规则服务执行挖掘结算。

第一轮只迁移移动与碰撞表现，不新增玩家直接命令从属机器人。机器人路径可以先使用简单的格子路径或直线段插值到目标接触点，遇到建筑或墙体阻挡时进入受阻状态并重选目标。

选择理由：

- 接受反馈中的“从属机器人也自由移动”。
- 不把本轮扩大成复杂机器人指挥系统。
- 保留当前机器人不会自动探测、不会挖玩家标记格的安全边界。

### 5. 建筑采用 ScriptableObject 定义 + Prefab 表现 + 逻辑占位

新增或扩展建筑配置资产，例如 `BuildingDefinition`：

```text
BuildingDefinition
├─ id
├─ displayName
├─ cost
├─ footprintCells / size
├─ allowedTerrain
├─ prefab
├─ colliderSize
└─ constructionTime / buildRules
```

运行时建筑实例由规则层记录占用格、建筑类型、状态和资源消耗结果。表现层根据实例生成 prefab，并把 prefab collider 用作自由移动阻挡。Tilemap 可继续用于调试或简化设施标记，但不能作为建筑真相。

选择理由：

- 反馈明确建筑占地不一定是 `1x1`，需要配置并考虑碰撞。
- Prefab 适合非单格建筑的视觉、碰撞和后续动画。
- 逻辑占位能被玩家、机器人、地震危险区和后续建造规则统一查询。

备选方案：继续把所有设施作为单格 Tile。

放弃原因：无法表达多格 footprint、连续碰撞体和未来更多建筑表现需求。

### 6. 目录、asmdef 与数据归属

新增或扩展目录：

```text
Assets/Scripts/Runtime/
  Presentation/
    FreeformActorController.cs
    ActorContactProbe.cs
    GameplayInteractionMode.cs
    BuildingPlacementController.cs
  Progression/
    BuildingDefinition.cs
    BuildingPlacementService.cs
  Automation/
    HelperRobotMotionController.cs
Assets/Settings/Minebot/
  BuildingDefinitions/
Assets/Prefabs/Minebot/Buildings/
```

asmdef 方向保持现有模块边界：`Presentation` 依赖运行时服务接口；`Progression` 或据点模块持有建筑定义和资源结算；`Automation` 可读取占位与路径查询，不依赖 UI。

### 7. 开发顺序

1. 更新输入资产和 HUD 提示，移除旧主挖掘键作为核心入口。
2. 实现主机器人自由移动、碰撞阻挡和接触格查询。
3. 接入持续贴墙自动挖掘，并补硬度不足、炸药、奖励回归测试。
4. 实现 `E` 标记模式和鼠标选岩壁标记。
5. 实现 `R` 建筑模式、建筑菜单、placement preview 和资源扣除。
6. 增加建筑 footprint 配置、逻辑占位和 prefab/collider 同步。
7. 将从属机器人表现迁移到自由移动，并确保旧自动模式仍遵守标记和危险边界。
8. 跑 Unity 编译、EditMode 测试、PlayMode 烟雾测试，并手动验证 `Gameplay` 与 `DebugSandbox`。

## Risks / Trade-offs

- [Risk] 自由移动与逻辑格边界可能出现“看起来碰到墙但查到相邻格错误”的错位。→ Mitigation: 统一由 `ActorContactProbe` 做世界坐标到逻辑格转换，并补边界和角落测试。
- [Risk] 贴墙自动挖掘可能过于频繁触发规则结算。→ Mitigation: 使用配置化挖掘间隔/进度门槛，并在目标格变化时重置状态。
- [Risk] 多格建筑占位会影响机器人寻路和玩家碰撞。→ Mitigation: 建筑实例写入统一 occupancy 查询，玩家碰撞、机器人目标筛选和 placement validation 复用同一接口。
- [Risk] 模式输入和升级 UI、暂停 UI 发生冲突。→ Mitigation: `UpgradeLocked` / `GameOver` / `Pause` 作为更高优先级输入状态，阻止地图模式继续处理点击。
- [Risk] Prefab 表现比 Tilemap 设施更重。→ Mitigation: MVP 建筑数量很小，优先选择正确表达 footprint 和碰撞；后续再做对象池或渲染优化。

## Migration Plan

1. 保留现有逻辑服务和测试，先在表现层引入自由移动适配，不直接删除旧规则入口。
2. 更新输入动作后同步更新 HUD 文案和 PlayMode 测试，避免测试继续依赖旧按键。
3. 将旧维修站、机器人工厂迁移为 `BuildingDefinition` 默认数据；如果实现期时间不足，先保留旧设施，同时让新建筑占位服务兼容它们。
4. 自动挖掘和标记模式通过新测试稳定后，再移除旧 `Mine`/`ToggleMarker`/`BuildRobot` 主路径。
5. 回退策略：可以单独禁用自由移动表现层，恢复旧的网格步进输入；规则服务和配置资产不应被破坏。

## Open Questions

- 建筑菜单第一批是否只包含维修站和机器人工厂，还是需要额外加入墙体/钻机类建筑。
- 自动挖掘是否需要显示挖掘进度条，还是第一版只用音效/反馈文本。
- 标记模式下“镜头跟随鼠标移动”的范围和速度需要在实现期调试定案。
