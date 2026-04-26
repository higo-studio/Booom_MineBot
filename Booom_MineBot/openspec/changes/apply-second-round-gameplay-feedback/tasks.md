## 1. 需求同步与配置入口

- [x] 1.1 将第二轮反馈摘要同步到项目规划文档或相关 skill 参考，明确“探测改为前沿岩壁数字、标记独立层、危险区内描边、玩家碰撞体调优”的验收口径。
- [x] 1.2 扩展 `HazardRules` 或等价配置资产，加入玩家附近探测范围等本轮新增扫描参数，并为默认配置填入安全缺省值。
- [x] 1.3 扩展 `MinebotPresentationArtSet` 或等价表现配置资产，加入危险区 outline 档位、扫描数字偏移/样式和玩家碰撞体半径。
- [x] 1.4 更新默认资源与占位资源，确保新增配置字段在 `Gameplay` 和 `DebugSandbox` 中都能正常加载。

## 2. 探测规则与数据通路

- [x] 2.1 在 `HazardService` 中实现候选岩壁筛选：只返回附近、可挖、且至少相邻一个空地图块的岩壁。
- [x] 2.2 在 `HazardService` 中实现以目标岩壁为中心的 `3x3` 炸药计数，并产出按岩壁分组的扫描快照结果。
- [x] 2.3 改造 `GameSessionService.Scan` 与相关事件/返回值，移除“单个 origin + 单个 bombCount”的主路径假设。
- [x] 2.4 改造 `GameplayInputController`、`MinebotGameplayPresentation` 与 HUD 摘要文案，使探测成功后消费扫描快照而不是中心提示。
- [x] 2.5 补充 EditMode 测试，覆盖能量不足、空结果、候选墙筛选和 `3x3` 计数包含自身/对角线的规则。

## 3. 地图反馈分层

- [x] 3.1 将当前地图反馈拆成独立职责层：`Marker`、`Danger`、`BuildPreview` 和 `ScanIndicator`，不再复用单一 `OverlayTilemap`/扫描提示路径。
- [x] 3.2 改造标记渲染，确保标记只出现在独立层上，岩壁底图保持原样不被替换。
- [x] 3.3 改造危险区渲染，只在空地危险格上绘制内描边，并按波次选择更粗的 outline 档位。
- [x] 3.4 新增或改造扫描数字表现组件，使数字显示在目标岩壁上方，并与其它反馈层互不清空。
- [x] 3.5 补充 PlayMode 测试，覆盖标记独立层、危险区内描边、扫描数字锚点和多层反馈共存。

## 4. 玩家碰撞 footprint 校准

- [x] 4.1 将玩家 `CircleCollider2D` 半径从硬编码迁移到表现配置读取，并在运行时初始化中统一使用。
- [x] 4.2 校准玩家贴墙接触与自动挖掘候选格判定，确保视觉贴墙时阻挡和命中结果稳定一致。
- [x] 4.3 补充 EditMode/PlayMode 测试，覆盖单格通道可通过、斜角缝隙不可误穿越和贴墙命中同一候选岩壁。
- [x] 4.4 手动烟雾验证 `Gameplay` 与 `DebugSandbox`，检查自由移动、贴墙手感和碰撞体调参结果是否符合第二轮反馈。

## 5. 验证与收口

- [x] 5.1 使用 UnityMCP 执行 `unity.compile`，确认新增配置、扫描快照和表现层拆分后项目仍可编译。
- [x] 5.2 运行相关 EditMode / PlayMode 测试，重点验证探测规则、反馈分层和玩家碰撞行为。
- [x] 5.3 运行 `openspec validate apply-second-round-gameplay-feedback`，确保 proposal、design、specs 和 tasks 全部通过校验。
