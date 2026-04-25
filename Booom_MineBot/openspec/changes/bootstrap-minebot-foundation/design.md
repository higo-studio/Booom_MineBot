## 背景

BOOOM Minebot 目前是一个空白的 Unity 6 工程，已有 URP、Input System、UGUI、2D Feature Set 与 Unity Test Framework，但没有任何项目代码、场景启动流程、测试边界或 Agent 工作约定。策划案明确了 GameJam MVP 的核心压力来自四件事的叠加：隐藏炸药信息、风险收益权衡、自动化扩张、以及地震波持续施压。

因此本次设计不追求“先把所有玩法都做出来”，而是先把运行时系统边界和工程骨架钉死。后续实现需要优先保证以下系统可独立演进并可测试：

- 方格地图、移动、挖掘、掉落
- 炸药生成、探测反馈、标记、爆炸结算
- 升级、维修、资源、基础建造
- 从属机器人自动模式、地震波危险区、波次结算
- 项目启动、配置资产、测试脚手架、Agent 工作流

## 目标 / 非目标

**目标：**

- 建立适合 GameJam 速度的 Unity 代码基础框架，而不是后期大型项目框架。
- 用确定性的方格模拟承载核心规则，让探测、爆炸、危险区与机器人行为可测试、可复现。
- 明确模块和 asmdef 边界，避免所有逻辑早期堆进单一 `GameManager`。
- 固化 MVP 技术栈和开发顺序，使玩法实现可以按系统层层叠加。
- 初始化仓库内的 Agent skill 指南，让后续任务沿用同一套策划来源、规范和命令路径。

**非目标：**

- 不引入 DOTS/ECS、多人联机、复杂行为树、可视化脚本、第三方 DI 框架。
- 不在本阶段完成指令型机器人模式、完整钻井产出链、正式结局流程或大规模内容生产工具。
- 不做 Addressables、远程配置、复杂存档兼容层等超出 GameJam MVP 的基础设施。

## 关键决策

### 1. 用“逻辑网格 + 表现层同步”作为核心架构

运行时以纯 C# 网格状态作为权威数据源，每个格子记录地块类型、硬度、是否含炸药、是否已标记、危险状态、掉落结果等。MonoBehaviour 负责输入、镜头、动画和 UI 展示，但不直接决定玩法规则。

选择理由：

- 策划案的交互天然是方格制，探测数字、标记、炸药连锁、危险区扩张都适合在离散网格上结算。
- 纯数据模拟更容易写 EditMode 单元测试，尤其适合验证“扫雷式判断”与连锁爆炸。
- 机器人自动模式也能直接基于格子状态做低风险目标选择，而不是依赖物理碰撞推断。

备选方案与取舍：

- 备选：基于物理碰撞和 Tilemap Collider 的场景即真相实现。
- 放弃原因：规则状态会分散在组件上，爆炸/危险区/机器人判定难以回放和验证，后期数值迭代成本高。

### 2. 采用“轻量 Bootstrap 场景 + 单主玩法场景”流程

入口使用一个极小的 `Bootstrap` 场景，只负责初始化共享配置、输入映射、服务装配和日志开关，然后加载 `Gameplay` 主场景。MVP 不拆主菜单系统，但保留 `Bootstrap -> Gameplay` 的启动边界，避免全局单例散落。

建议场景：

- `Assets/Scenes/Bootstrap.unity`
- `Assets/Scenes/Gameplay.unity`
- `Assets/Scenes/DebugSandbox.unity`（仅开发期验证格子与规则）

选择理由：

- GameJam 仍然需要最小启动边界，否则配置资产、调试入口、测试场景会越来越混乱。
- `DebugSandbox` 可以让地图、爆炸、危险区规则脱离完整 UI 验证，提升迭代速度。

备选方案与取舍：

- 备选：单场景 + `DontDestroyOnLoad` 全局对象。
- 放弃原因：依赖关系隐蔽，后续补 UI、测试和重开流程时更容易出现初始化顺序问题。

### 3. 采用模块化目录与 asmdef 边界

建议目录结构：

```text
Assets/
  Scenes/
  Scripts/
    Runtime/
      Bootstrap/
      Common/
      GridMining/
      HazardInference/
      Progression/
      Automation/
      WaveSurvival/
      UI/
    Editor/
    Tests/
      EditMode/
      PlayMode/
  Settings/
```

建议 asmdef：

- `Minebot.Runtime.Common`
- `Minebot.Runtime.Bootstrap`
- `Minebot.Runtime.GridMining`
- `Minebot.Runtime.HazardInference`
- `Minebot.Runtime.Progression`
- `Minebot.Runtime.Automation`
- `Minebot.Runtime.WaveSurvival`
- `Minebot.Runtime.UI`
- `Minebot.Editor`
- `Minebot.Tests.EditMode`
- `Minebot.Tests.PlayMode`

依赖方向：

- `Common` 仅提供共享值对象、事件、接口、工具。
- `GridMining` 与 `HazardInference` 共享格子状态模型，但不反向依赖 UI。
- `Progression`、`Automation`、`WaveSurvival` 依赖前序运行时模块。
- `UI` 只依赖运行时查询接口和事件，不持有规则决策权。

选择理由：

- 模块直接对应策划里的能力边界，后续实现、测试和并行开发更清晰。
- asmdef 能减少 Unity 编译范围，适合频繁迭代。

备选方案与取舍：

- 备选：所有代码放在 `Assets/Scripts` 单程序集。
- 放弃原因：早期看似快，但一旦加入机器人、波次与 UI，编译和耦合都会恶化。

### 4. 用 ScriptableObject 承载配置数据，而不是先做外部表工具链

以下配置统一使用 ScriptableObject：

- 岩壁类型与硬度档位
- 资源掉落规则
- 探测消耗与数字反馈规则参数
- 升级候选池与权重
- 建筑成本
- 机器人参数
- 地震波时间轴与危险区扩张曲线

选择理由：

- 对当前空仓库来说，这是 Unity 内最快可视化且最稳定的内容迭代方式。
- 配置资产天然适合被 Inspector、调试场景和测试复用。

备选方案与取舍：

- 备选：CSV/JSON 外部导表。
- 放弃原因：MVP 阶段会额外引入导入器、热更新或版本同步问题，收益不够。

### 5. 地图编辑采用“Tilemap 辅助编辑 + Bake 到逻辑地图资产”

编辑期允许使用 Unity Tilemap 作为关卡布局的前端工具，但 Tilemap 不作为运行时权威地图。推荐采用以下管线：

```text
编辑期 Tilemap / 自定义 Editor 工具
                ↓ Bake
          MapDefinition.asset
                ↓ Load
          LogicalGridState
                ↓ Sync
     Tilemap / Sprite / VFX / UI
```

推荐分层：

- `Terrain Tilemap`：负责空地、岩壁、硬度层等地形布局
- `POI Tilemap`：负责出生点、维修站、机器人工厂等点位
- 隐藏逻辑层：通过自定义 Editor Overlay、Gizmo 或 Inspector 批量编辑炸药、特殊掉落、生成标签等非纯表现数据
- `MapDefinition`：保存经过 Bake 的纯逻辑初始状态，供运行时加载

进一步的编辑职责划分：

- 适合放在 `Tilemap` 的内容：地形轮廓、岩壁硬度、出生点、单格设施点位
- 适合放在 `Overlay` 的内容：炸药分布、特殊掉落、生成禁区、特殊逻辑标签、调试可视化
- 判断标准：凡是“空间形状优先”的数据优先放 `Tilemap`，凡是“隐藏语义优先”的数据优先放 `Overlay`

#### MapDefinition 建议结构

`MapDefinition` 第一版采用“稠密格子数据 + 稀疏标记点”的结构，不在 MVP 早期引入过多区域系统或脚本事件：

```text
MapDefinition
├─ mapId
├─ size
├─ cells[]            // 按行优先压平的格子初始状态
└─ markers[]          // 出生点、维修站、工厂等少量特殊点
```

推荐的格子初始字段：

- `terrainKind`：空地、可挖岩壁、不可破坏边界
- `hardnessTier`：土、岩石、硬岩、超硬岩
- `staticFlags`：是否埋炸药、保留通道、禁止塌方、是否启用特殊扫描规则等
- `resourceProfile`：该格默认掉落或资源配置引用

推荐的稀疏标记点字段：

- `markerKind`：出生点、维修站、机器人工厂等
- `position`
- `size`：MVP 可先支持 `1x1`，为后续多格建筑预留
- `direction`：先保留字段或默认值，供后续多格设施扩展

结构约束：

- 炸药虽然由 `Overlay` 编辑，但 Bake 后直接写入 `cells[].staticFlags`，因为它属于高频核心规则查询
- `MapDefinition` 只保存“单局初始静态状态”，不保存运行时变化状态
- `LogicalGridState` 负责保存运行中的已挖开、已标记、危险区、实体占位、动态建筑状态等变化数据

#### TilemapBakeProfile 建议职责

引入 `TilemapBakeProfile` 作为编辑层到逻辑层的翻译资产，而不是把 Tile 资源直接绑定到运行时规则：

```text
TilemapBakeProfile
├─ terrainRules[]     // Tile -> terrainKind / hardnessTier / flags / profile
├─ poiRules[]         // Tile -> markerKind / size / uniqueness / placement rules
└─ validationRules    // Bake 时的基础合法性校验
```

它的职责是：

- 解释某个 `TileBase` 在逻辑上对应什么地形和硬度
- 解释某个 POI Tile 对应什么点位类型
- 在 Bake 阶段做基础校验，例如出生点唯一、设施不能压在岩壁里、出生点周边保留安全半径

这样可以避免地图资产与具体美术 Tile 资源强耦合，也方便后续替换美术资源而不改逻辑语义。

#### Bake 流程建议

推荐的 Bake 顺序：

1. 读取 `Terrain Tilemap` / `POI Tilemap`
2. 借助 `TilemapBakeProfile` 生成基础 `cells[]` 与 `markers[]`
3. 合并 `Overlay` 中的炸药、特殊掉落、限制标签等隐藏逻辑
4. 运行地图合法性校验
5. 输出 `MapDefinition.asset`

第一版暂不优先支持的内容：

- 区域型 `regions`
- 脚本化事件点
- 复杂多边形编辑
- 大量视觉变体直接写入逻辑地图

这些内容等 MVP 跑通、且确实出现需求后再扩展。

选择理由：

- Tilemap 保留了方格关卡编辑的手感，适合快速摆地图与调整轮廓。
- 运行时仍然只依赖 `MapDefinition` 和 `LogicalGridState`，不会把玩法真相绑定到 Scene/Tile 组件上。
- 炸药、硬度、特殊点位、掉落规则等隐藏语义不必硬塞进单一 Tile 定义，避免 Tile 组合爆炸。
- Bake 后的逻辑资产更适合做测试、回放、随机生成混合和机器人/爆炸结算。

备选方案与取舍：

- 备选：直接把 Tilemap 作为运行时地图真相。
- 放弃原因：玩法语义会反向依赖场景和 Tile 资源，不利于测试、同步和长期维护。

- 备选：一开始就做纯自定义地图编辑器。
- 暂不采用原因：当前是 GameJam MVP，纯自定义编辑器前期工具成本偏高；先用 Tilemap 提升编辑效率更务实。

触发升级条件：

- 如果后续单格编辑维度显著增加，或需要更强的批量规则编辑、逻辑热力图、生成约束混编，再考虑把主编辑入口从 Tilemap 升级为自定义 Grid Editor。

### 6. 确认 MVP 技术栈，并明确排除项

确认采用：

- Unity `6000.0.59f2`
- C#
- URP 17
- Input System
- UGUI
- Unity Test Framework
- 2D Feature Set 中可复用的 Tilemap / Sprite 能力
- ScriptableObject 配置资产

明确不采用：

- DOTS/ECS/Burst 驱动的核心玩法架构
- Multiplayer / Netcode / 联机同步
- Addressables
- 第三方 DI / FSM / 行为树框架
- Visual Scripting 作为主实现方式

说明：

- 虽然 `com.unity.visualscripting` 当前存在于工程依赖中，但不作为本项目主开发路线，可在后续清理阶段评估是否移除。
- `com.unity.multiplayer.center` 当前存在于工程依赖中，但不进入 MVP 设计，也不参与架构决策。

### 7. 按“可玩闭环”排列开发顺序，而不是按功能列表平铺

推荐阶段顺序：

1. `project-foundation`
   建立场景、目录、asmdef、基础输入、日志、测试、项目 skill 文档。
2. `grid-mining-loop`
   先做可移动、可挖掘、可掉落、可阻挡的方格闭环。
3. `hazard-inference`
   在已可玩的挖掘循环上叠加炸药、探测、标记、连锁爆炸。
4. `progression-and-base-ops`
   接入经验、升级 UI、生命、维修站、基础资源消费和机器人工厂。
5. `automation-and-wave-survival`
   加入机器人自动模式、危险区评估、地震波结算、计分失败。
6. 集成打磨
   调整数值、补反馈、补回归测试、清理场景和配置资产。

这样排序的理由是：

- 地震波和机器人必须建立在稳定的格子/风险规则之上。
- 升级、维修、建造属于“延长循环”的系统，不应先于基础挖掘和炸药判断。
- 到第 3 阶段结束时，团队就能拿到一个最关键的“扫雷式挖掘”原型。

### 8. 用仓库内 Skill 文档初始化 Agent 工作流

在 `.codex/skills/minebot-project/SKILL.md` 中固化以下内容：

- 本项目的玩法摘要与 MVP 边界
- 优先读取的策划来源（本次飞书 Wiki）
- 使用 `openspec-propose` / `openspec-apply-change` / `openspec-archive-change` 的时机
- Unity 工程的目录与 asmdef 约定
- 实现阶段的测试与验证建议

这样做的理由是：

- 后续 Agent 不需要反复从零解释项目是什么、哪些技术栈故意不用、应该先改哪里。
- 项目级 skill 比会话内临时说明更稳定，适合持续开发。

## 风险 / 权衡

- [风险] 过早拆太多模块，反而拖慢 GameJam 初期开发速度。
  Mitigation: 保持模块数量与策划主能力一致，不再继续细分子程序集。

- [风险] 逻辑网格与表现层同步失步，导致格子状态和场景显示不一致。
  Mitigation: 只允许 Domain 层修改权威状态，视图层通过单向事件或快照刷新。

- [风险] ScriptableObject 配置量增长后，引用关系可能混乱。
  Mitigation: 统一使用少量聚合配置入口，例如 `GameBalanceConfig`、`WaveConfig`、`UpgradePoolConfig`。

- [风险] 机器人自动模式如果过早做复杂寻路，会吞掉大量时间。
  Mitigation: MVP 仅实现“最近可挖目标 + 避开标记格 + 无目标待机”的简单策略。

- [风险] 当前工程里已有但不用的包可能误导后续实现。
  Mitigation: 在项目 skill 和 proposal 中显式标记排除项，并在基础阶段评估是否移除无关包。

## 落地与迁移计划

1. 先落仓库文档与 skill 初始化，再创建场景、目录和 asmdef。
2. 以可编译为底线逐模块接入运行时代码，不在同一阶段同时修改所有系统。
3. 每个阶段结束前补一个最小验证入口：EditMode 测试、PlayMode 烟雾测试或 DebugSandbox 手动检查。
4. 若某模块设计不成立，可单独回退对应 asmdef/场景/配置，而不影响其它系统。

当前仓库尚无旧实现，因此不存在运行中数据迁移问题；回退策略主要依赖 git 和模块边界。

## 未决问题

- 探测数字的最终规则是邻接 4 格、8 格，还是基于某个自定义扫描半径。
- 爆炸波及半径是否固定，还是由炸药类型或升级决定。
- 地震波结算是纯时间驱动，还是与玩家推进深度联动。
- 维修是否即时完成，还是需要消耗时间停留在维修站。
- 机器人是否需要最小路径缓存，还是每次只做一步目标重选即可满足 MVP。
