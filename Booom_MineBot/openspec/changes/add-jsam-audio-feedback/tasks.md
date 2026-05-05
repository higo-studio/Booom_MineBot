## 1. 依赖与配置资产底座

- [x] 1.1 在 `Packages/manifest.json` 中加入 JSAM Git 依赖，并完成 Unity 侧包导入。
- [x] 1.2 为运行时新增 `MinebotAudioConfig` 与分类 cue 数据结构，并把它挂入 `BootstrapConfig`。
- [x] 1.3 扩展配置资产托管工具，自动创建/复用音频配置资产与预定义 cue 资产。

## 2. 配置编辑器扩展

- [x] 2.1 在 `Minebot` 配置编辑器左侧新增 `音频` 大类。
- [x] 2.2 在 `音频` 大类中按音乐、模式与界面、玩家与地形、掉落与成长、建筑与据点、从属机器人、波次与失败分组展示 cue 清单。
- [x] 2.3 为每个 cue 提供定位资源与直接编辑入口，并保持“补齐引用”后可自动回填缺失资产。

## 3. 运行时音频接入

- [x] 3.1 新增最小运行时音频适配层，负责读取 `MinebotAudioConfig`、确保 JSAM 播放器可用并封装 one-shot / loop / music 切换。
- [x] 3.2 在输入与表现层接入模式切换、无效操作、玩家移动/挖掘/爆炸/受伤、升级与失败音效。
- [x] 3.3 接入掉落吸收、维修、建造、机器人生产、机器人挖掘/破墙/损毁音效。
- [x] 3.4 接入常态 / 预警 / 地震结算音乐，以及波次预警、阶段切换、存活结算提示音。
- [x] 3.5 对空 cue 或空 clip 做安全降级，确保未配素材时不报错、不阻塞玩法。

## 4. 验证

- [x] 4.1 为音频配置与 cue 资产自动创建逻辑补 EditMode 测试。
- [x] 4.2 使用 UnityMCP 执行 `unity.compile(exitPlayMode:true)`，确认导包和代码接入后可编译。
- [ ] 4.3 运行本次相关 EditMode 测试，确认音频配置托管逻辑稳定。
- [x] 4.4 运行 `openspec validate add-jsam-audio-feedback`，确认 proposal、design、specs 和 tasks 结构完整。
