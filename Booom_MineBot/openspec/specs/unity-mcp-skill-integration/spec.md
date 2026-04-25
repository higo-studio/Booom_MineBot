# unity-mcp-skill-integration Specification

## Purpose
TBD - created by archiving change integrate-unity-mcp-skill. Update Purpose after archive.
## Requirements
### Requirement: 项目本地必须存在可发现的 UnityMCP skill 入口
仓库 SHALL 在项目本地 skill 目录中提供一个可被 Codex 发现和触发的 `unity-mcp-bridge` skill 入口，并将包内 `Packages/com.himimi.unity-mcp-bridge/Codex~/unity-mcp-bridge/SKILL.md` 作为其上游事实来源。

#### Scenario: 在项目内发现 UnityMCP skill
- **WHEN** Agent 在此仓库中需要处理 Unity 编辑器相关任务
- **THEN** 仓库本地存在一个可发现的 `unity-mcp-bridge` skill，而不要求 Agent 直接从包目录手工定位 skill 文件

#### Scenario: 对照上游 skill 内容
- **WHEN** 开发者检查本地 `unity-mcp-bridge` skill 的来源
- **THEN** 文档能够明确指出包内 upstream skill 路径，并说明本地 skill 与其关系

### Requirement: 项目总约束必须将 Unity 编辑器任务路由到 UnityMCP
项目级最高优先级 skill SHALL 明确规定，当任务涉及 Unity 编译、Play Mode、Console、Scene、Asset、Unity 测试或 Multiplayer Play Mode 时，应先切换到 `unity-mcp-bridge` skill。

#### Scenario: 处理 Unity 编译校验
- **WHEN** Agent 在项目内执行 Unity 脚本编译校验
- **THEN** 项目总约束会把该任务路由到 `unity-mcp-bridge`，而不是停留在通用项目 skill 说明层

#### Scenario: 处理 Unity 编辑器交互
- **WHEN** Agent 在项目内需要读取 Console、切换 Play Mode 或操作 Scene/Asset
- **THEN** 项目文档会指向 `unity-mcp-bridge` 作为首选工具工作流入口

### Requirement: Unity 验证必须以 MCP 为权威入口
项目文档 SHALL 规定 Unity 脚本编译校验必须使用 `unity.compile`，并且在 Unity MCP 不可用、断连或桥接未启用时 SHALL 将验证状态报告为阻塞，而 SHALL NOT 将 `dotnet build` 作为 Unity 编译校验的替代方案。

#### Scenario: Unity MCP 可用时执行编译
- **WHEN** Agent 需要验证 Unity 脚本编译状态且 Unity MCP 可用
- **THEN** 文档会要求使用 `unity.compile` 作为规范编译检查入口

#### Scenario: Unity 处于 Play Mode 导致编译被阻塞
- **WHEN** `unity.compile` 因 Unity 正处于 Play Mode 而返回阻塞
- **THEN** 文档会说明应以 `exitPlayMode=true` 的方式重试，而不是切换到其它非 MCP 编译手段

#### Scenario: Unity MCP 不可用
- **WHEN** Agent 需要执行 Unity 编译校验但 Unity MCP 不可用或未连接
- **THEN** 文档会将该状态定义为“验证被阻塞”，而不是要求使用 `dotnet build` 替代

### Requirement: 项目内必须提供 UnityMCP 的启用与排障说明
仓库 SHALL 提供一份项目内 UnityMCP setup/reference 文档，说明桥接包存在、桥接启用入口、Codex 配置写入、主实例限制以及不可用时的排查路径。

#### Scenario: 首次接入或排查 UnityMCP
- **WHEN** 开发者或 Agent 需要确认为什么 Unity MCP 当前不可用
- **THEN** 仓库内存在一份 setup/reference 文档可用于检查桥接启用、Codex 配置和运行前提

#### Scenario: 多实例环境下使用 UnityMCP
- **WHEN** 项目处于多实例或 Multiplayer Play Mode 环境
- **THEN** 文档会说明主实例与镜像实例的职责边界，并要求先检查实例状态再执行主实例专属操作

