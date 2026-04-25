## Why

当前工程已经具备方格挖掘、风险判断、成长、据点、机器人和波次生存的规则骨架，但 `Bootstrap -> Gameplay` 后只有占位渲染，玩家无法看到策划案要求的地图、角色、资源、危险、设施或 UI 状态。现在需要补齐一层可手玩的表现层，把已有运行时服务连接成可验证的 2D 俯视角 MVP 垂直切片。

## What Changes

- 新增 `Gameplay` 场景内的方格地图表现层，用占位美术清晰区分空地、岩壁、不可破坏边界、危险区和已标记格。
- 新增主机器人、从属机器人、维修站和机器人工厂的运行时表现对象，并让它们随规则状态刷新。
- 新增输入桥接，使玩家能在主场景中移动、挖掘、探测、标记、维修和生产机器人。
- 新增 HUD 与反馈层，显示生命、金属、能量、经验、波次、探测数字、升级选择、危险预警和失败提示。
- 新增 `Gameplay` / `DebugSandbox` 场景烟雾验证，确认启动后能看到地图、能操作主循环，并能完成“挖掘 -> 升级 -> 维修 -> 造机器人”的可玩流程。
- 不引入正式美术生产管线；第一版使用可读的程序化色块、基础 UGUI 和少量 prefab 占位资产。

## Capabilities

### New Capabilities

- `gameplay-presentation`: 覆盖方格地图、玩家、机器人、设施和基础场景元素的可视化同步。
- `playable-interaction`: 覆盖主玩法场景中的键鼠输入、网格移动、挖掘、探测、标记、维修和造机器人操作。
- `hud-and-feedback`: 覆盖 HUD、升级选择、资源/经验反馈、探测数字、标记、危险区、地震预警和失败提示。

### Modified Capabilities

- 无。

## Impact

- 影响 `Assets/Scenes/Gameplay.unity`、`Assets/Scenes/DebugSandbox.unity` 和相关场景装配。
- 在 `Assets/Scripts/Runtime/UI` 或新的表现层模块中新增 MonoBehaviour 表现组件。
- 复用现有 `Bootstrap`、`GridMining`、`HazardInference`、`Progression`、`Automation`、`WaveSurvival` 服务，不改变规则真相来源。
- 新增占位材质、prefab、UGUI 对象和 PlayMode 烟雾测试。
- Unity 编译、PlayMode、Console 和场景验证仍通过 UnityMCP 执行。
