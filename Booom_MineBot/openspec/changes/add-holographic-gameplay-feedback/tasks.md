## 1. 全息资源与配置基础

- [ ] 1.1 扩展 `MinebotPresentationArtSet` 及相关运行时资源封装，加入 holographic overlay 和 BMFont/bitmap glyph 资源引用
- [ ] 1.2 扩展 `MinebotPixelArtAssetPipeline`，让 image2 生成的 hologram atlas、BMFont 描述文件和符号资源进入正式导入与校验流程
- [ ] 1.3 补齐全息反馈资源记录文档模板，明确 prompt、筛选说明、glyph 映射、最终资产路径和 ArtSet 引用关系
- [ ] 1.4 验证默认 Presentation Art Set、导入设置和资源目录能够在编辑器内稳定解析

## 2. 全息 overlay 与 BMFont 扫描数字

- [ ] 2.1 将 `ScanIndicatorPresenter` 从默认 TMP 世界文本切换为项目内 BMFont/bitmap glyph 渲染路径
- [ ] 2.2 更新标记、危险区和扫描数字的表现资源绑定，使其共享同一套全息风味视觉语言
- [ ] 2.3 为危险区表现增加几何适配层，确保当前 outline 方案与后续 danger contour 都能复用同一套全息风格资源
- [ ] 2.4 验证 `Gameplay` 与 `DebugSandbox` 中的扫描数字、标记和危险区不会互相清空或抢占渲染 ownership

## 3. 测试与回归校验

- [ ] 3.1 新增或更新 EditMode 测试，覆盖 hologram/BMFont 导入、ArtSet 引用和资源校验逻辑
- [ ] 3.2 新增或更新 PlayMode 测试，覆盖扫描数字渲染、标记/危险区全息效果与 contour / outline 兼容性
- [ ] 3.3 运行 OpenSpec 校验与相关 Unity 测试，确认新 overlay 风格不破坏建造预览和危险区验收基线
