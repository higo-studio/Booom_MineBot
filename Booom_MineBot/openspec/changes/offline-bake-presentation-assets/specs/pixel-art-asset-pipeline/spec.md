## MODIFIED Requirements

### Requirement: 资源资产必须可被 Unity 原生工具管理
最终进入场景表现的地图资源 SHALL 以 Unity 可管理的 Sprite、Tile、Tile Palette 或 ScriptableObject 配置资产存在，而不是只由运行时代码临时生成。默认 dual-grid terrain/fog、danger contour/outline、bitmap glyph、HUD/overlay 占位资源 MUST 在编辑期生成并落盘为项目资产。

#### Scenario: 构建默认表现资源
- **WHEN** 开发者执行 Minebot 默认像素资源产线
- **THEN** dual-grid terrain/fog、overlay、glyph、HUD 和默认 tile/sprite 会被写入项目目录并可在 Project 窗口中直接检查

#### Scenario: 运行时加载默认资源
- **WHEN** `Gameplay` 或 `DebugSandbox` 在没有场景内显式覆盖 art set 的情况下启动
- **THEN** 运行时会读取默认离线资源，而不是现场生成临时 `Texture2D`、`Sprite` 或 `Tile`

### Requirement: dual-grid 与全息默认资源必须由 editor 流水线统一生成
项目 SHALL 使用 editor-only 资源流水线生成 dual-grid terrain/fog family、danger contour/outline、bitmap glyph atlas 与相关默认占位资源；这些算法 MUST NOT 作为运行时表现层 fallback 存在。

#### Scenario: 重新生成 dual-grid 默认资源
- **WHEN** 开发者重新执行默认美术生成菜单
- **THEN** dual-grid terrain/fog 的 PNG、Tile 与 profile/art set 引用会在编辑期更新

#### Scenario: 审查运行时程序集边界
- **WHEN** 开发者检查运行时表现层代码
- **THEN** 不会再看到仅用于默认资源 fallback 的 `Texture2D`/`Sprite`/`Tile` 生成算法
