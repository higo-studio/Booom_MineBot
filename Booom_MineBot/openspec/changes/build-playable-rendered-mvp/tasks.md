## 1. 表现层基础

- [x] 1.1 创建 `Gameplay Presentation` 组件目录和 asmdef 依赖，明确表现层只依赖运行时服务并不保存玩法真相。
- [x] 1.2 创建程序化占位 Tile / 材质资源，覆盖空地、岩壁、边界、危险区、标记、维修站和机器人工厂；主机器人和从属机器人使用 SpriteRenderer 占位资源。
- [x] 1.3 在 `Gameplay` 场景中装配 `Presentation Root`、Grid、多层 Tilemap、Actor Root、Camera、基础光照和可复用的表现层入口。
- [x] 1.4 在 `DebugSandbox` 场景中复用同一套 Tilemap 表现层入口，保留调试验证能力。

## 2. 地图与实体渲染

- [x] 2.1 实现 `TilemapGridPresentation`，从 `LogicalGridState` 全量刷新 `Terrain / Facility / Overlay / Hint` 多层 Tilemap。
- [x] 2.2 实现挖掘后的 Tilemap 刷新，使岩壁变为空地、奖励结算后画面同步变化。
- [x] 2.3 实现主机器人表现对象，并随玩家网格位置刷新。
- [x] 2.4 实现维修站和机器人工厂表现对象，并在玩家靠近时暴露交互提示数据。
- [x] 2.5 实现从属机器人表现对象，在生产、移动或死亡时同步增删和刷新。

## 3. 输入与可玩闭环

- [x] 3.1 实现 `GameplayInputController`，支持移动输入并调用现有移动服务。
- [x] 3.2 实现面向方向或选中相邻格的挖掘输入，并刷新地图、玩家和 HUD。
- [x] 3.3 实现探测输入，扣除能量并显示探测数字反馈。
- [x] 3.4 实现标记输入，刷新地图标记表现并保证机器人避开标记格。
- [x] 3.5 实现维修站维修输入和机器人工厂造机器人输入，并刷新 HUD 与实体表现。

## 4. HUD 与反馈

- [x] 4.1 创建 UGUI HUD，显示生命、金属、能量、经验、波次和核心按键提示。
- [x] 4.2 实现 HUD 与 `RuntimeServiceRegistry` 状态同步，覆盖挖掘、探测、维修、升级和造机器人后的刷新。
- [x] 4.3 实现升级选择面板，经验达标时显示候选项，选择后关闭并应用升级。
- [x] 4.4 实现探测数字、资源/经验获得、爆炸伤害和标记状态的最小可读反馈。
- [x] 4.5 实现地震波倒计时、危险区覆盖和失败提示的最小 HUD/场景反馈。

## 5. 场景验证与测试

- [x] 5.1 添加 PlayMode 测试，验证 `Bootstrap -> Gameplay` 后存在 Camera、地图表现、玩家表现和 HUD。
- [x] 5.2 添加 PlayMode 测试，验证移动和挖掘会改变规则状态并刷新表现对象。
- [x] 5.3 添加 PlayMode 测试，验证升级、维修和造机器人在主场景中有可见 UI/实体反馈。
- [x] 5.4 手动烟雾验证 `Gameplay`：从启动进入后完成移动、挖掘、探测、标记、升级、维修和造机器人。
- [x] 5.5 手动烟雾验证 `DebugSandbox`：能显示地图、主机器人、HUD，并可快速检查危险区和波次反馈。
- [x] 5.6 通过 `unity.compile`、PlayMode 测试和 `openspec validate build-playable-rendered-mvp` 完成最终校验。
