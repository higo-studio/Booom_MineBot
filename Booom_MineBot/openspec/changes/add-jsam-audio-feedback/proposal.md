## Why

当前项目的反馈几乎只依赖 HUD 文本、tilemap 变化和少量表现动画，还没有统一的音频系统。现在如果直接在各脚本里临时加 `AudioSource` 或硬编码播放逻辑，配置会分散、引用会失控，也无法复用刚刚建立好的分类配置编辑器工作流。

用户已经明确要求引入 `Simple-Unity-Audio-Manager`，并且要继续通过现有 `Minebot` 配置编辑器来维护音效清单。这意味着项目需要一套集中式音频配置、按大类组织的 cue 列表，以及“缺失就自动创建并引用”的编辑器托管流程。

## What Changes

- 以 UPM Git 依赖的方式导入 `Simple-Unity-Audio-Manager`（JSAM），作为项目统一的底层音频播放引擎。
- 在 `BootstrapConfig` 下新增 `MinebotAudioConfig`，并把它纳入现有 `Minebot` 配置编辑器的自动托管体系。
- 在配置编辑器中新增 `音频` 大类，并按音乐、模式与界面、玩家与地形、掉落与成长、建筑与据点、从属机器人、波次与失败等分组展示 cue 清单。
- 对每个音乐 cue 和音效 cue 自动创建并引用对应的 JSAM `MusicFileObject` / `SoundFileObject` 占位资产，避免手动新建和手动拖拽 SO。
- 在运行时接入核心音频反馈，包括：
  - 主循环 / 预警 / 地震结算音乐切换
  - 模式切换、标记、无效操作、升级可选、失败等 UI/状态提示音
  - 玩家移动、挖掘、岩壁破坏、炸药爆炸、受伤等交互音效
  - 掉落吸收、维修、建造、生产机器人、机器人挖掘/损毁等反馈音效
  - 波次预警、阶段切换、存活结算等阶段性提示音
- 为音频配置资产自动创建逻辑补充 EditMode 测试。

## Capabilities

### New Capabilities
- `audio-feedback`: 项目提供统一的音频配置与运行时 cue 播放体系，并通过现有配置编辑器按分类维护。

### Modified Capabilities

## Impact

- 影响 `Packages/manifest.json` 与相关 asmdef 引用，新增第三方包依赖 `com.brogrammist.jsam`。
- 影响 `Assets/Scripts/Runtime/Bootstrap`、`Assets/Scripts/Runtime/Presentation` 和 `Assets/Scripts/Editor`，新增音频配置资产、编辑器托管逻辑和运行时播放适配层。
- 影响 `Minebot` 配置编辑器的分类结构与自动创建逻辑。
- 影响 EditMode 测试与本次 change 对应的 OpenSpec 文档。
