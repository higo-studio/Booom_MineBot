## 1. 资源入口与导入基础

- [ ] 1.1 扩展 `MinebotPresentationArtSet` 或其分组配置，加入角色 prefab、资源掉落 prefab、裂缝/裂墙/爆炸序列资源和图形化 HUD 资源入口，并与 holographic change 的 overlay 分组兼容
- [ ] 1.2 扩展 `MinebotPixelArtAssetPipeline`，让角色状态帧、效果帧、资源图标和 HUD 图形资源进入正式目录、导入设置与生成记录流程
- [ ] 1.3 补齐 AI 生成资源的记录模板，覆盖 prompt、筛选说明、最终资产路径和对应 prefab / HUD 用途
- [ ] 1.4 验证默认 Presentation Art Set 与资源目录在编辑器内可被稳定解析

## 2. 掉落物规则与奖励结算

- [ ] 2.1 新增纯 C# 的世界掉落物状态服务，并把它接入 `RuntimeServiceRegistry`
- [ ] 2.2 将挖掘奖励与机器人相关掉落从“即时入账”改为“生成掉落物并在吸收时结算”
- [ ] 2.3 实现玩家靠近自动吸收的规则路径，并确保金属、能量和经验分别进入正确系统
- [ ] 2.4 验证掉落物对升级触发、机器人挖掘结果和失败/回收场景的结算时机

## 3. Prefab 化角色、掉落与墙体交互表现

- [ ] 3.1 将主机器人和从属机器人表现升级为 prefab 驱动，并建立运行时状态到动画/图片序列的映射
- [ ] 3.2 新增资源掉落物 view 和吸收表现，使不同资源类型在场景中可辨认
- [ ] 3.3 实现钻墙裂缝、停止淡出、墙体裂开和炸药爆炸的分阶段效果，并与挖掘/爆炸时序对齐，同时遵守“同类型岩体内部连续、只有暴露外缘显著”的边界语言
- [ ] 3.4 验证新的 actor / pickup / cell FX prefab 与现有 overlay 层级不会互相遮挡或抢占 ownership

## 4. 图形化 HUD 升级

- [ ] 4.1 为 HUD 生成并筛选首版 AI 图形资源，确定状态区、资源区、波次区和交互区的视觉方案
- [ ] 4.2 更新 `MinebotHudPrefabBuilder` 与 `MinebotHudView`，让图形化 HUD prefab 成为默认加载路径，同时保留最小 fallback 结构
- [ ] 4.3 将图形化 HUD 接入 `Gameplay` 与 `DebugSandbox`，验证与现有升级、建造和交互按钮逻辑兼容

## 5. 测试与回归校验

- [ ] 5.1 新增或更新 EditMode 测试，覆盖资源导入、ArtSet 配置、HUD prefab 和 prefab 资源引用校验
- [ ] 5.2 新增或更新 PlayMode 测试，覆盖掉落物生成/吸收、角色状态表现和墙体交互特效，并验证连续岩体内部不会因裂缝/裂墙效果重新出现假边界
- [ ] 5.3 运行 OpenSpec 校验与相关 Unity 测试，确认本变更与现有 contour / holographic 提案的资源入口不冲突
