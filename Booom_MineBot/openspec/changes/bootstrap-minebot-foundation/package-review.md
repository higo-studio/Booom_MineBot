# Unity 包复查记录

复查时间：2026-04-26

## 已移除

- `com.unity.multiplayer.center`：当前 MVP 不做联机或 Multiplayer Play Mode 玩法验证，保留该入口会误导架构边界。
- `com.unity.visualscripting`：当前规则层要求用确定性 C# 服务实现，不使用 Visual Scripting 作为主实现路径。

## 保留

- `com.unity.inputsystem`：作为 `Assets/InputSystem_Actions.inputactions` 与生成 wrapper 的输入输出层。
- `com.unity.ugui` 和 TextMesh Pro 资源：用于 HUD、升级选择和中文文本渲染。
- `com.unity.render-pipelines.universal` 与 `com.unity.feature.2d`：用于 2D Tilemap、Sprite 和 URP 2D 场景表现。
- `com.unity.test-framework`：用于 EditMode / PlayMode 回归验证。
- IDE、Timeline、Collab Proxy 与 Unity modules：不参与玩法架构决策，当前不与 MVP 边界冲突；后续若出现构建体积或导入时间压力，再单独清理。
