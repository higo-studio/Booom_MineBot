## Why

当前可玩版已经具备基础循环，但角色、资源、墙体交互和 HUD 仍主要依赖 Sprite 占位、Tile 叠层和文字面板，操作回报感与美术统一性都明显落后于玩法完成度。前一版提案把扫描/标记的全息 overlay 也纳入了这里，导致它与 `add-holographic-gameplay-feedback` 职责重叠；这次整理后，本 change 只保留 prefab 化角色、掉落、破墙特效和图形化 HUD，作为独立的表现升级提案。

## What Changes

- 将核心机器人和从属机器人升级为 prefab 驱动的运行时表现对象，并要求为关键状态预留动画与配置入口，而不是继续只靠单张 Sprite 与颜色变化表达状态。
- 将金属、能量矿石和经验升级为有独立图片资源的世界掉落物；成功破墙后资源先从墙体中弹出，再在玩家靠近时自动吸收，而不是继续只在后台即时入账。
- 为机器人钻墙增加持续裂缝表现：钻墙时播放裂缝序列帧，停止钻墙时裂缝逐渐淡出；钻墙完成后播放墙体裂开动画，如果目标含炸药，则随后接爆炸动画。
- 墙体交互表现必须服从 `add-dual-grid-wall-contour-rendering` 已定义的岩体连接语言：同类型岩体内部保持连续纹理，裂缝、裂墙和破口只强化暴露外缘与新开洞口，不得重新画回“每格一块砖”的内部描边。
- 将 HUD 从当前文字面板优先的可读版升级为更图形化的 prefab 版本，并明确允许先通过 AI 生成首版视觉资源，再接入项目内正式资产流程。
- 扩展像素资源与 prefab 资产管线，把角色状态帧、裂缝动画、墙体裂开动画、资源掉落图片和 HUD 图形资源一起纳入 image2 生成、筛选、导入和记录流程。
- 明确本变更不再拥有标记/危险区/探测数字的全息风格与 BMFont 资产链路；这些继续由 `add-holographic-gameplay-feedback` 负责，本变更只需与其资源入口兼容。

## Capabilities

### New Capabilities

- 无。本变更通过扩展现有表现、反馈、资源与 HUD capability 完成，不新增独立玩法能力。

### Modified Capabilities

- `gameplay-presentation`: 主机器人、从属机器人、世界资源掉落物和墙体破坏动画必须以 prefab 化运行时表现接入，并支持状态/动画配置。
- `grid-mining-loop`: 挖掘奖励的玩家体验从“后台即时结算”升级为“世界掉落物弹出并在接近后自动吸收”，同时保留同一份奖励真相。
- `progression-and-base-ops`: 金属、能量矿石和经验除了资源数值职能分离，还必须支持作为可见掉落物被吸收进入经济与成长系统。
- `helper-robot-auto-mode`: 从属机器人自动模式状态必须能驱动对应的运行时动画或表现状态，而不再只由 HUD 文案或颜色变化表达。
- `hud-and-feedback`: HUD 必须支持更图形化的 prefab 版本，且挖墙、破墙、爆炸、掉落和吸收反馈需要在场景中可见。
- `pixel-art-asset-pipeline`: image2 资源流程必须覆盖角色状态帧、裂缝/裂墙/爆炸动画帧、资源掉落图与图形化 HUD 资源。

## Impact

- 影响 `Assets/Scripts/Runtime/Presentation` 下的 actor view、世界反馈与资源掉落表现接入，需要从 Sprite/文字占位扩展到 prefab 与动画状态驱动。
- 影响 `Assets/Scripts/Runtime/GridMining`、`Bootstrap/GameSessionService` 与奖励结算路径，需要支持世界掉落物生成、接近吸收和与现有资源/经验结算同步。
- 影响 `Assets/Scripts/Runtime/Automation` 与机器人表现同步，需要让移动、钻墙、待机、受阻、损毁等状态对接动画配置。
- 影响 `Assets/Scripts/Runtime/UI` 与 `Assets/Scripts/Editor/MinebotHudPrefabBuilder.cs`，需要把当前文字型 HUD prefab 升级为图形化版本，同时保持 prefab 可迭代。
- 影响 `Assets/Scripts/Editor/MinebotPixelArtAssetPipeline.cs`、`Assets/Art/Minebot/Generated`、`Assets/Art/Minebot/Sprites`、`Assets/Resources/Minebot`，需要把 AI 生成的动画帧、图标和 HUD 资源纳入正式导入与引用。
- 影响 `Assets/Scripts/Tests/EditMode` 与 `PlayMode` 的表现层验收基线，需要验证 prefab 化显示、掉落吸收时序、裂缝/破墙动画触发以及图形化 HUD 装配。
