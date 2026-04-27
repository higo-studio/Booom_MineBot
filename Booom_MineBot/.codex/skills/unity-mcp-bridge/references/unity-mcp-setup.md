# Unity MCP Bridge 接入与排障

## 用途

本文件说明 BOOOM Minebot 项目内使用 UnityMCP 的启用前提、最小检查项和常见阻塞情况。它是项目本地 `unity-mcp-bridge` skill 的配套 setup/reference 文档。

## 现有前提

本项目已经包含桥接包：

- `Packages/com.himimi.unity-mcp-bridge`

包内关键文件：

- upstream skill：`Packages/com.himimi.unity-mcp-bridge/Codex~/unity-mcp-bridge/SKILL.md`
- 说明文档：`Packages/com.himimi.unity-mcp-bridge/README.md`
- 宿主工程：`Packages/com.himimi.unity-mcp-bridge/Tools~/UnityMcpBridge.Host/`

## 启用清单

使用 UnityMCP 前，至少确认以下项目：

1. 项目使用 Unity 6
2. Unity Editor 能正常运行 `dotnet`
3. 在 `Project Settings > MCP Bridge` 中启用桥接
4. 让桥接把 host 发布到 `Library/McpBridge/Host`
5. 如有需要，启用自动写入 `~/.codex/config.toml`

说明：

- 包内代码已经提供 `McpCodexConfigWriter`，可将 MCP 服务写入 `~/.codex/config.toml`
- 包 README 明确说明了启用桥接和复制 skill 的迁移步骤

## 项目内工作流规则

- Unity 编译校验必须使用 `unity.compile`
- 不能用 `dotnet build` 替代 Unity 编译检查
- 若 Unity MCP 不可用、断连或桥未启用，应报告“验证被阻塞”
- Play Mode、Console、Scene/Asset 操作优先走 MCP，而不是默认要求手工操作编辑器

## 多实例规则

如果项目处于多实例或 Multiplayer Play Mode 环境：

- 先查询 `unity.instances`
- 主实例专属操作：
  - `unity.compile`
  - `unity.enter_play_mode`
  - `unity.exit_play_mode`
- 镜像实例默认只用于观察，除非某个 MCP 工具明确支持镜像实例操作

## 最小验证场景

### 1. 编译检查

- 使用 `unity.compile`
- 如果返回 `blocked` 且 Unity 处于 Play Mode，带 `exitPlayMode=true` 重试

### 2. Play Mode 检查

- 使用 `unity.editor_state` 查看当前状态
- 跑 `unity.tests_run(mode:"play")` 前，使用 `unity.scene_list_opened` 确认 open scenes 没有 `isDirty=true`
- 使用 `unity.enter_play_mode` / `unity.exit_play_mode` 切换

### 3. Console 检查

- 使用 `unity.console_logs` 读取最近日志
- 若界面按钮状态和实际运行状态不一致，以 MCP 状态与日志为准

## 常见状态解释

- `blocked`：Unity 当前状态不适合继续该操作
- `busy`：同类操作已有进行中的请求
- `retry`：正在发生 Play Mode 切换或域重载，应等待后重试

## 排障顺序

当 UnityMCP 当前不可用时，按下面顺序检查：

1. 桥接包是否仍在 `Packages/com.himimi.unity-mcp-bridge`
2. Unity Editor 中是否已启用 `MCP Bridge`
3. `dotnet` 是否可被 Unity Editor 调用
4. Codex 配置中是否存在对应 MCP 服务条目
5. Unity 当前是否正在 Play Mode 切换、编译或域重载
6. 是否存在场景保存弹窗，或 Unity Test Framework 因 `SaveCurrentModifiedScenesIfUserWantsTo()` 卡住

## 参考来源

- `references/unity-mcp-quick-guide.md`
- `references/unity-mcp-practical-notes.md`
- `Packages/com.himimi.unity-mcp-bridge/Codex~/unity-mcp-bridge/SKILL.md`
- `Packages/com.himimi.unity-mcp-bridge/README.md`
