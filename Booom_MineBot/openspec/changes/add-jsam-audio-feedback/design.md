## Context

当前项目已经有一个基于 `UIToolkit` 的 `Minebot` 配置编辑器，并且 `BootstrapConfig` 负责聚合数值、地图、挖掘、波次和建筑等主链路配置。但音频仍然完全缺位，现有玩法反馈主要分布在 `MinebotGameplayPresentation` 与 `GameplayInputController` 的文本提示、状态刷新和少量特效逻辑里。

本次变更是一次跨模块接入：

- 需要引入新的外部依赖 `Simple-Unity-Audio-Manager`
- 需要扩展 `BootstrapConfig` 与编辑器托管逻辑
- 需要在 `Presentation` 层为已有交互、掉落、机器人和波次流程挂接 cue
- 需要保持运行时规则服务的确定性，不把音频副作用下沉进规则层

约束如下：

- 音频配置仍然必须以 `ScriptableObject` 为主，不引入外部表格或新数据库层。
- 配置入口必须继续收敛到现有 `Minebot` 配置编辑器，而不是新增第二套独立音频面板。
- 项目仍然坚持轻量 Unity 原生实现，不引入 FMOD、Wwise、Addressables 或自建复杂 mixer 框架。

## Goals / Non-Goals

**Goals:**

- 通过 UPM Git URL 导入 JSAM，并稳定接入现有 asmdef 体系。
- 新增 `MinebotAudioConfig`，由 `BootstrapConfig` 聚合并由编辑器自动创建、自动引用。
- 在配置编辑器中新增 `音频` 大类，并用合理分类组织完整 cue 清单。
- 为每个 cue 自动创建对应的 `MusicFileObject` 或 `SoundFileObject` 占位资产，减少手工拖拽成本。
- 在 `Presentation` 层为玩家、机器人、掉落、据点、波次、升级和失败等关键反馈挂接运行时播放。
- 在 cue 未配置任何 `AudioClip` 时保持安全降级，不影响正常游玩。
- 为音频配置资产托管逻辑补齐 EditMode 测试，并完成 Unity 编译与 OpenSpec 验证。

**Non-Goals:**

- 不实现完整的音量设置菜单、存档化音量偏好或暂停菜单。
- 不接入动态分层音乐、区域环境音、持续 ambience 或语音系统。
- 不为高频且噪声大的事件添加提示音，例如每次风险感知刷新、每帧建造预览合法性变化、每个机器人移动步声。
- 不改动底层规则服务的权威逻辑，也不把音频播放调用塞进 `GameSessionService` 等规则层代码。

## Decisions

### 1. 使用 JSAM 的 UPM Git 包，而不是拷贝源码或自建 `AudioSource` 池

本次通过 `Packages/manifest.json` 引入 JSAM 的 Git URL 依赖：

- `https://github.com/jackyyang09/Simple-Unity-Audio-Manager.git#master`

选择理由：

- 仓库本身已经提供 `package.json`，包名是 `com.brogrammist.jsam`，适合直接走 Unity Package Manager。
- JSAM 已经内建 `SoundFileObject` / `MusicFileObject`、音量分类、淡入淡出、2D/3D 播放和最大实例限制，能覆盖本次 GameJam 所需的音效组织能力。
- 直接复用现成包，比在项目里临时搭一层 `AudioSource` 池更快，也更方便让策划继续通过 SO 维护 clip 列表。

备选方案：

- 直接 vendoring 整个仓库到 `Packages/` 或 `Assets/Plugins/`
- 自建轻量音频播放器

放弃原因：

- vendoring 更新成本更高，也会把第三方源码直接带进主仓。
- 自建方案短期可行，但会重新发明 JSAM 已经提供的文件对象、循环和混音分层能力。

### 2. `BootstrapConfig` 新增 `MinebotAudioConfig` 聚合引用，音频仍然走配置资产主链路

音频配置不会散落在场景对象上，而是作为 `BootstrapConfig` 的一个新子配置：

- `MinebotAudioConfig`

`MinebotAudioConfig` 负责维护按组分类的 cue 引用，而不直接存 `AudioClip`。每个 cue 指向 JSAM 的 `SoundFileObject` 或 `MusicFileObject`。

目录布局采用当前 Bootstrap 配置根目录下的托管子目录：

- `Assets/Settings/Gameplay/Audio/Minebot Audio Config.asset`
- `Assets/Settings/Gameplay/Audio/Music/*.asset`
- `Assets/Settings/Gameplay/Audio/Sounds/*.asset`

这样做的理由：

- 继续遵守现有“所有主链路配置挂在 BootstrapConfig 下”的项目边界。
- 音频与数值、地图、波次配置拥有一致的生命周期和定位方式。
- 资产路径可预测，便于自动创建、自动复用与测试。

备选方案：

- 在场景里给 `MinebotGameplayPresentation` 直接序列化一套音频引用
- 使用 JSAM 的自动生成 enum 作为项目主入口

放弃原因：

- 场景序列化会再次回到“拖引用 + 场景耦合”的旧问题。
- 自动生成 enum 更适合手写 API 访问，但本次需求核心是“统一配置编辑器”，不是在代码里到处写 enum 名。

### 3. 配置编辑器只新增一个顶级分类 `音频`，但在右侧按 7 组展示 cue 清单

为了保持“配置列表是大项配置分类”的原则，左侧分类只新增一个同级大项：

- `音频`

`音频` 分类内部再按以下 7 组展示：

- 音乐
- 模式与界面
- 玩家与地形
- 掉落与成长
- 建筑与据点
- 从属机器人
- 波次与失败

本次固定的 cue 清单如下：

- 音乐：`Bgm_GameplayLoop`、`Bgm_WaveWarning`、`Bgm_WaveResolution`
- 模式与界面：`Sting_UpgradeAvailable`、`Sting_GameOver`、`Ui_ModeMarkerToggle`、`Ui_ModeBuildToggle`、`Ui_BuildingSelect`、`Ui_MarkerSet`、`Ui_MarkerClear`、`Ui_ActionDenied`
- 玩家与地形：`Player_Move`、`Player_Block`、`Player_MiningLoop`、`Player_MiningWeak`、`Terrain_WallBreak`、`Hazard_BombExplosion`、`Player_Damage`
- 掉落与成长：`Pickup_MetalAbsorb`、`Pickup_EnergyAbsorb`、`Pickup_ExpAbsorb`、`Upgrade_Apply`
- 建筑与据点：`Repair_Success`、`Robot_BuildSuccess`、`Build_PlaceSuccess`
- 从属机器人：`Robot_MiningLoop`、`Robot_WallBreak`、`Robot_Destroyed`
- 波次与失败：`Wave_WarningStart`、`Wave_DangerRefresh`、`Wave_Collapse`、`Wave_Survived`

这样既满足用户要求的“合理分类”，又避免把左侧配置列表膨胀成几十个碎条目。

### 4. 音频配置和 cue 资产都由编辑器托管自动创建，用户只负责往 JSAM 资产里填 clip

`MinebotConfigAssetUtility.EnsureManagedAssets` 会扩展为：

- 自动定位或创建 `MinebotAudioConfig`
- 自动为每个 cue 创建对应的 `MusicFileObject` / `SoundFileObject`
- 自动把这些资产引用回 `MinebotAudioConfig`
- 优先复用托管目录下已存在、命名匹配的资产，避免重复生成

这意味着用户不需要：

- 手动新建 `MinebotAudioConfig`
- 手动新建 32 个 cue SO
- 手动把 cue SO 一个个拖到配置里

用户真正需要做的事只有：

- 在配置编辑器里定位对应 cue
- 打开对应的 JSAM 资产
- 为它填入一个或多个 `AudioClip`

### 5. 运行时播放适配层放在 `Presentation`，规则层只暴露既有结果和状态

音频触发点主要位于现有表现层与输入层：

- `GameplayInputController`：模式切换、标记、非法操作、玩家移动/受阻、玩家挖掘接触
- `MinebotGameplayPresentation`：建筑、维修、机器人生产、升级、掉落吸收、机器人结果、游戏失败、波次 HUD 状态

因此本次采用一个轻量运行时适配器，放在 `Presentation` 侧，负责：

- 从 `BootstrapConfig` 读取 `MinebotAudioConfig`
- 确保 JSAM 运行时播放器存在
- 提供简单 API：播放 one-shot、启动/停止 loop、切换主音乐、根据状态切换警报/结算音乐
- 跟踪几个需要去重的状态转换：
  - 升级面板从隐藏变为显示
  - 玩家从存活变为死亡
  - 波次倒计时首次进入 warning window
  - 地震结算步骤切换
  - 玩家挖掘 loop 与机器人挖掘 loop 的启停

规则层仍然只返回已有结果，如 `MineInteractionResult`、`RobotAutomationResult`、`WaveResolutionState`，不直接知道 JSAM 的存在。这能保持确定性逻辑与表现副作用解耦。

### 6. 对空 clip 资产做安全降级，保证“未配音频”不会阻塞游玩

自动创建出来的 JSAM cue 资产初始可能没有任何 `AudioClip`。运行时适配层在播放前必须检查该 cue 是否存在且至少包含一个 clip；如果没有，则直接跳过播放。

这样可以保证：

- 功能先落地，再逐步补音频素材
- 编译和游玩不会因为某个 cue 还没填 clip 而报错
- 配置编辑器能先把完整清单和资产结构建立起来

## Risks / Trade-offs

- [第三方包的 asmdef 或 API 细节与预期不一致] -> 先通过 UPM 导入并读取本地包源码，运行时只封装项目实际用到的最小播放接口，减少对包内部细节的耦合。
- [32 个 cue 初始全部自动创建，托管目录内资产数量会明显增加] -> 使用固定目录分层与可预测命名，保持可查找性；同时这是换取“无需手动拖引用”的必要成本。
- [多个机器人同时挖掘时，逐个实例化 3D loop 可能过于吵闹] -> 本轮先实现“有任一机器人在挖掘时播放共享 loop”的轻量策略，后续若需要再细化到多实例空间音。
- [音频事件几乎都集中在 `MinebotGameplayPresentation`，代码职责会再变重] -> 新增独立的音频适配类型，把状态判定和具体 JSAM 调用从主表现脚本里拆出去。
- [当前 change 依赖尚未归档的配置编辑器实现] -> 本次直接在现有实现上增量扩展，并在本 change 的 spec 中自包含描述音频配置行为，不依赖其它未归档 spec 先落地。

## Migration Plan

1. 在 `Packages/manifest.json` 中加入 JSAM Git 依赖并等待 Unity 拉取包。
2. 新增 `MinebotAudioConfig` 与相关运行时/编辑器代码，扩展 `BootstrapConfig` 聚合引用。
3. 扩展配置编辑器与托管工具，自动创建音频配置资产和 cue 资产。
4. 在 `Presentation` 层接入运行时播放适配器，逐步把分类清单中的事件挂上。
5. 运行 Unity 编译与 EditMode 测试，确认缺省空 clip 的工程也能正常通过。

## Open Questions

- 若 JSAM 的主播放器初始化必须依赖特定场景对象或额外资源，本次将以“自动创建最小运行时播放器”为准，不把初始化职责交给手工场景装配。
