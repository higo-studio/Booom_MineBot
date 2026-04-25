---
name: unity-mcp-bridge
description: 在包含 `Packages/com.himimi.unity-mcp-bridge` 的 Unity 6 项目中，处理 Unity 编译、Play Mode、Console、Scene、Asset、测试或 Multiplayer Play Mode 任务时使用本 skill。它是本项目对包内 upstream `Codex~/unity-mcp-bridge/SKILL.md` 的本地包装入口，负责把 Unity 编辑器相关工作统一路由到 MCP，而不是手工编辑器操作或 `dotnet build`。
---

# Unity MCP Bridge

## 概览

这是 BOOOM Minebot 项目的本地 UnityMCP 包装 skill。项目内凡是涉及 Unity 编辑器状态、脚本编译、Play Mode、Console、Scene/Asset 或 Unity 测试的任务，都应先走这里，再按需下钻到包内 upstream skill。

upstream 事实来源：

- `Packages/com.himimi.unity-mcp-bridge/Codex~/unity-mcp-bridge/SKILL.md`
- `Packages/com.himimi.unity-mcp-bridge/README.md`

## 触发条件

出现以下任一情况时使用本 skill：

- 需要验证 Unity C# 脚本是否能在编辑器中成功编译
- 需要进入或退出 Play Mode
- 需要读取 Unity Console 日志
- 需要查询或操作 Scene、GameObject、Component、Asset、Prefab、Package
- 需要运行 Unity Test Framework 测试
- 需要处理 Multiplayer Play Mode 多实例环境

## 项目内规则

- Unity 脚本编译校验一律使用 `unity.compile`
- 如果 `unity.compile` 因 Play Mode 被阻塞，使用 `exitPlayMode=true` 重试
- Unity MCP 不可用、未连接或桥接未启用时，报告“验证被阻塞”，不要回退到 `dotnet build`
- 多实例环境下，先查询 `unity.instances`，并将 `unity.compile`、`unity.enter_play_mode`、`unity.exit_play_mode` 视为主实例专属操作
- 如果 MCP 已经暴露 Scene/Asset/Console/Play Mode 操作，不默认要求手工点击 Unity 编辑器

## 使用顺序

1. 先读取 [unity-mcp-setup.md](references/unity-mcp-setup.md)，确认桥接启用、Codex 配置、主实例限制和基础排障路径
2. 再读取包内 upstream skill：
   `Packages/com.himimi.unity-mcp-bridge/Codex~/unity-mcp-bridge/SKILL.md`
3. 按 upstream skill 的工作流执行具体工具调用

## 最小工作流

### 编译

- 先确认 Unity MCP 可用
- 使用 `unity.compile`
- 如果返回 `blocked` 且原因是 Play Mode，使用 `exitPlayMode=true` 重试

### Play Mode

- 先用 `unity.editor_state` 确认当前编辑器状态
- 使用 `unity.enter_play_mode` / `unity.exit_play_mode`
- 若处于多实例环境，先检查 `unity.instances`

### Console

- 使用 `unity.console_logs`
- 当 Play Mode UI 看起来与实际状态不一致时，以 MCP 返回的编辑器状态和 Console 日志为准

## 参考

- [unity-mcp-setup.md](references/unity-mcp-setup.md)
- `Packages/com.himimi.unity-mcp-bridge/Codex~/unity-mcp-bridge/SKILL.md`
- `Packages/com.himimi.unity-mcp-bridge/README.md`
