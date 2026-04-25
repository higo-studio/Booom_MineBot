## Why

仓库已经包含 `Packages/com.himimi.unity-mcp-bridge`，并且包内自带了一份可移植的 `unity-mcp-bridge` skill，但当前项目级 skill 体系还没有正式吸收这套能力。结果是 Agent 在做 Unity 编译、Play Mode、Console、Scene/Asset 操作时，缺少统一的 MCP 优先规则，容易退化到手工编辑器操作或错误地用 `dotnet build` 替代 Unity 编译校验。

## What Changes

- 将 `Packages/com.himimi.unity-mcp-bridge/Codex~/unity-mcp-bridge/SKILL.md` 正式整合进项目内 Codex skill 体系，而不是只把它留在包目录中。
- 在项目级最高优先级 skill 中增加 UnityMCP 路由规则，明确什么情况下必须切换到 `unity-mcp-bridge` skill。
- 统一 Unity 相关验证流程：脚本编译、Play Mode 切换、Console 日志读取、Scene/Asset 查询与测试执行优先走 Unity MCP，而不是手工编辑器交互或 `dotnet build`。
- 为项目文档补充 UnityMCP 的接入前提、桥接启用方式、MCP 不可用时的阻塞判定和多实例使用边界。
- 将这套流程纳入项目文档优先级体系，确保后续实现与调试任务默认继承同一套 Unity 工具约束。

## Capabilities

### New Capabilities
- `unity-mcp-skill-integration`: 将包内 `unity-mcp-bridge` skill 接入项目本地 skill 体系，并定义基于 Unity MCP 的编译、Play Mode、Console、Scene/Asset 与测试工作流。

### Modified Capabilities
- 无。

## Impact

- `.codex/skills/` 下的项目本地 skill 结构与引用关系
- `minebot-project` 项目总约束及其参考文档
- 可能新增的本地 `unity-mcp-bridge` skill 副本或包装文档
- 面向 Agent 的 Unity 验证与编辑器交互流程
- 不涉及 `Packages/com.himimi.unity-mcp-bridge` 桥接包本身的运行时功能扩展
