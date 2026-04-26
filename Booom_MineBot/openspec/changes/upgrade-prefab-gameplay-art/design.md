## Context

当前 `MinebotGameplayPresentation` 的运行时表现仍以三类方式为主：

- 角色：主机器人和从属机器人使用 `SpriteRenderer` 直接显示单张 Sprite，仅通过位置变化和颜色区分部分状态。
- 反馈层：墙体破坏、爆炸和资源奖励仍缺少真正的 prefab 化世界表现；标记、危险区与扫描数字的风格升级则已拆分到独立的 holographic change。
- HUD：`MinebotHudPrefabBuilder` 自动生成以文字面板为中心的 prefab，`MinebotHudView` 负责绑定文本与按钮。

同时，`GameSessionService` 当前在挖掘成功时直接把奖励加到 `PlayerEconomy` 和 `ExperienceService`，`RewardGranted` 只向表现层发送一条文本摘要。这意味着“资源从墙里爆出来，再在靠近后自动吸收”的体验无法仅靠表现层补丁完成，必须引入新的规则态服务来承载掉落物生命周期。

这次 change 还与两个 active change 存在关联：

- `add-dual-grid-wall-contour-rendering`：危险区和墙体轮廓将继续影响表现层组织方式。
- `add-dual-grid-wall-contour-rendering`：危险区和墙体轮廓将继续影响表现层组织方式，并且已经明确“同类型岩体内部连续、只有暴露外缘有明显边界”的地形连接语言；本 change 的裂缝、裂墙和资源弹出表现必须服从这个约束。
- `add-holographic-gameplay-feedback`：拥有扫描、标记、危险区的全息风格、BMFont 和相关资源导入链；本 change 不再重复定义这些 overlay，只消费它提供的资源入口。

本设计继续遵守仓库既有边界：

- 引擎与技术栈：Unity `6000.0.59f2`、URP、Input System、UGUI、Tilemap、TextMeshPro、ScriptableObject、Unity Test Framework。
- 规则真相继续留在纯 C# 服务和数据模型中，场景对象只负责表现。
- 不采用 DOTS/ECS、第三方状态机框架、Addressables、VFX Graph 或复杂后处理工作流。

目录布局与 asmdef 策略保持现有模块边界：

- `Assets/Scripts/Runtime/Presentation`：角色 prefab 驱动、裂缝/裂墙/爆炸特效、资源掉落 view、图形化 HUD 装配。
- `Assets/Scripts/Runtime/Progression`：世界掉落物生命周期与吸收结算。
- `Assets/Scripts/Runtime/GridMining` / `Automation`：继续输出挖掘与机器人状态真相，不直接依赖 Unity 组件。
- `Assets/Scripts/Editor`：HUD prefab 生成兜底、AI 资源导入、动画帧和 Sprite 序列导入、ArtSet 更新。
- `Assets/Scripts/Tests/EditMode` / `PlayMode`：配置资产、导入设置、prefab 装配和表现回归测试。

数据配置继续优先走 ScriptableObject：

- 现有 `MinebotPresentationArtSet` 继续充当表现资源入口，但要扩展为可引用 prefab、Sprite 序列、图形化 HUD 资源和可选材质。
- 资源掉落、机器人状态动画、裂缝/裂墙/爆炸效果不直接写死路径，而通过配置对象映射到具体 prefab 与序列帧资源。

## Goals / Non-Goals

**Goals:**

- 将主机器人、从属机器人、资源掉落和墙体交互表现升级为 prefab 驱动，并为状态/材质/动画预留配置入口。
- 把挖掘奖励体验从“墙破后即时入账”升级为“世界掉落物弹出后自动吸收”，同时保持奖励真相可测试。
- 为钻墙过程增加裂缝、停止淡出、墙体裂开和炸弹爆炸的分阶段视觉反馈。
- 在不破坏“同类岩体读成连续岩面”的前提下，为钻墙和破墙行为增加局部可见反馈。
- 把 HUD 升级为图形化 prefab 版本，同时保留现有绑定层与最小 fallback 能力。
- 将 AI 生成的角色状态帧、资源图标和 HUD 图形资源纳入项目内正式资产流程。

**Non-Goals:**

- 不改动方格挖掘、炸药判定、波次危险区或机器人选目标的核心规则。
- 不在本 change 内完成最终 shader 美术效果；扫描/标记相关材质入口继续由 holographic change 拥有。
- 不引入完整的 Timeline/Cinemachine/VFX Graph 演出系统。
- 不把 HUD 绑定逻辑重写成新的 UI 框架；继续沿用 UGUI prefab + 绑定脚本。
- 不把地形 Tilemap 表现整体改成 prefab 化；本次重点是 actor、pickup、cell FX 和 HUD。

## Decisions

### 1. 资源掉落改为“纯规则服务持有真相 + prefab 视图跟随”的双层结构

为满足“墙里爆出资源，再靠近自动吸收”，本设计引入纯 C# 的掉落物生命周期服务，负责：

- 记录每个掉落物的类型、数量、出生格、弹出后状态和是否已被吸收
- 在玩家靠近时判定自动吸收
- 将吸收结果结算回 `PlayerEconomy` 与 `ExperienceService`

表现层只负责把这些掉落物渲染成资源 prefab，不拥有奖励真相。这样既能支持视觉升级，也不会把资源时机藏进 `MonoBehaviour`。

推荐落点：

- 新服务放在 `Runtime/Progression`，因为它最终负责把世界掉落转为金属、能量和经验
- `RuntimeServiceRegistry` 增加对掉落服务的引用
- `GameSessionService` 从“直接 GrantMiningReward”调整为“生成掉落请求 + 在吸收时结算奖励”

备选方案：只在表现层生成假掉落物，同时保持后台即时加资源。

放弃原因：

- 与“靠近后自动吸收”的行为不一致。
- 升级触发时机会与玩家视觉不一致，后续测试会更难写。

### 2. 表现层采用 prefab root 分治，而不是继续把所有反馈塞回 Tilemap 或 TextMeshPro

`Presentation Root` 下继续保留 Tilemap，但新增更清晰的 prefab 化子根：

```text
Presentation Root
├─ Grid Root
│  ├─ Terrain / Facility / Danger / BuildPreview ...
├─ Actor Root
├─ Pickup Root
├─ Cell FX Root
└─ HUD Root
```

这样做的结果是：

- 主机器人和从属机器人不再只是 `SpriteRenderer` 列表，而是可替换 prefab
- 裂缝、裂墙、爆炸和掉落物都能有自己的生命周期，不会与静态地形刷新互相覆盖

备选方案：继续用 animated Tile / TMP 文本 / 更复杂的 Tilemap 层堆叠解决。

放弃原因：

- 裂缝淡出、掉落吸收和机器人状态动画都不适合放在静态 Tilemap 刷新模型里。
- 用户明确要求 prefab 化，继续沿用旧通路只会得到更多占位逻辑。

### 3. 使用“配置化 Sprite Sequence / 状态映射”而不是一开始就堆 Animator Controller

短时特效（裂缝、裂墙、爆炸、资源弹出）和角色状态变化都可以统一落到一套轻量的状态映射配置中：

- actor prefab 上保留可替换 renderer / material / sequence player
- 配置对象按 `Idle / Moving / Mining / Blocked / Destroyed` 等状态映射到对应序列帧或静态帧
- 短时特效使用一次性 sequence，支持自动销毁或淡出
- 裂缝与裂墙效果默认只贴附在当前被钻开的暴露边缘、角点或新破口附近，不在同类型相邻岩体的内部连接缝上补画完整边框

这样比一开始就为每种效果建立独立 Animator Controller 更轻，且更符合项目“配置优先、GameJam MVP”节奏。后续如果某些 prefab 确实需要 Animator，也可以在配置里保留可选入口，而不是把 Animator 设成唯一方案。

备选方案：全部使用 Animator Controller 和 AnimationClip。

放弃原因：

- 当前资产量还不大，先引入大量 Animator Controller 会增加维护与接线负担。
- 裂缝这类效果更多是图片序列和材质切换，不值得先复杂化。

额外约束：

- 这套 sequence 不能重新定义地形轮廓所有权。连续岩面的边界仍由 contour / base-detail 方案决定，裂缝与裂墙只是局部事件反馈。

### 4. 扩展 `MinebotPresentationArtSet` 为统一视觉配置入口，但按分组维护资源

不新增第二套平行的全局资源入口，而是继续由 `MinebotPresentationArtSet` 统筹：

- 角色 prefab 与状态资源
- 资源掉落 prefab 与吸收表现
- 裂缝 / 裂墙 / 爆炸序列帧或 effect prefab
- 图形化 HUD prefab 与图标资源

但实现上使用分组序列化结构，避免把所有字段散落在平铺的单层 Inspector 上。这样可以减少 `MinebotGameplayPresentation` 的接线分散，同时继续兼容现有 `Resources.Load<MinebotPresentationArtSet>()` 路径。

备选方案：新建完全独立的 `MinebotPresentationPrefabSet`。

放弃原因：

- 会让当前已经围绕 ArtSet 构建的导入管线、fallback 资源和测试都变成双入口维护。
- 对当前仓库来说，扩展现有 ArtSet 的代价更低。

### 5. HUD 升级保留现有绑定层，但把 `MinebotHudPrefabBuilder` 降为 fallback 生成器

图形化 HUD 仍然使用独立 prefab 资产迭代，`MinebotHudView` 继续负责绑定数据和按钮，但：

- 默认 HUD prefab 不再以纯文本块为视觉目标
- `MinebotHudPrefabBuilder` 用于在 prefab 缺失时生成最小可运行 fallback
- 首版图形资源允许通过 AI 生成后人工筛选导入，再交给 HUD prefab 使用

这样可以保证：

- UI 美术升级不需要重写全部逻辑绑定
- 即使图形资源尚未最终定稿，也不会阻塞运行时装配和测试

## Risks / Trade-offs

- [奖励从即时入账改为掉落吸收，会影响资源和升级到达时机] → 通过纯规则服务承载掉落物真相，并在 spec 中明确“靠近吸收才结算”作为新基线。
- [资源掉落对机器人自动化节奏可能产生新摩擦] → 第一版只保证玩家接近会自动吸收，并在实现期验证机器人挖出的资源是否需要额外引导表现。
- [与 `add-holographic-gameplay-feedback` 的 ArtSet 扩展可能冲突] → 明确该 change 拥有 overlay/BMFont 入口，本 change 只扩展 actor、pickup、cell FX 和 HUD 分组，不重复改同一组字段语义。
- [如果裂缝/裂墙帧在每个墙格四边都带完整描边，会把连续岩面重新切碎] → 把“只强化暴露外缘与破口，不重画内部连接缝”写入资源筛选标准、spec 和 PlayMode 验收。
- [prefab 与序列帧资源数量增加，人工接线容易出错] → 所有关键资源统一收口到 ArtSet 分组配置，并补 EditMode 资产校验。
- [AI 生成 HUD 和动画帧可能质量不稳定] → 管线必须保留 prompt、筛选和最终采用记录，并允许 fallback prefab 持续工作。

## Migration Plan

1. 先扩展 ArtSet 与 Editor 资源导入流程，确保角色 prefab、资源掉落图、序列帧和 HUD 图形资源都可被正式引用，并与 holographic change 的 overlay 分组保持兼容。
2. 再引入掉落物规则服务，并把当前即时奖励路径切换为“生成掉落 -> 靠近吸收 -> 结算资源”。
3. 随后替换角色、资源掉落和墙体交互表现通路，接入 prefab root 与状态映射。
4. 最后升级 HUD prefab、补齐测试，并校验与 active changes 的资源入口兼容。

回滚策略：

- 如果图形资源质量不达标，可以保留新的服务和 prefab 接口，但暂时回退到旧 Sprite / 文字资源。
- 如果掉落吸收路径影响太大，可以在实现期临时保留“掉落生成后立即自动吸收”的兼容模式，先确保逻辑与表现链路打通。

## Open Questions

- 从属机器人挖出的资源，在第一版是否只允许玩家靠近吸收，还是需要在视觉上额外引导玩家去回收。
- 图形化 HUD 的 AI 首版是否只覆盖状态面板和波次/资源区，还是同时覆盖升级、建造和交互按钮组。
