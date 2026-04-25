## 1. 本地 Skill 接入

- [x] 1.1 在 `.codex/skills/` 下创建项目本地 `unity-mcp-bridge` skill 入口，并标注包内 upstream skill 路径。
- [x] 1.2 为本地 `unity-mcp-bridge` skill 补充项目内 setup/reference 文档，说明桥接启用、Codex 配置写入、主实例限制和不可用排查路径。
- [x] 1.3 验证本地 `unity-mcp-bridge` skill 的命名、描述和引用结构能够被项目工作流正确理解。

## 2. 项目总约束与文档路由

- [x] 2.1 更新 `minebot-project`，增加 Unity 编译、Play Mode、Console、Scene/Asset、测试和 MPPM 任务到 `unity-mcp-bridge` 的路由规则。
- [x] 2.2 更新项目文档索引，把 `unity-mcp-bridge` skill 与 setup 文档纳入项目内优先级体系。
- [x] 2.3 在项目文档中明确 `unity.compile` 是 Unity 脚本校验的权威入口，且 Unity MCP 不可用时应报告阻塞而不是回退到 `dotnet build`。

## 3. 流程验证

- [x] 3.1 对照包内 `Codex~/unity-mcp-bridge/SKILL.md` 与 `README.md` 做一次人工核对，确认项目本地文档没有遗漏关键 MCP 规则。
- [x] 3.2 以最小场景验证 UnityMCP 工作流文档是否覆盖编译、Play Mode 和 Console 三类常见操作。
- [x] 3.3 完成 OpenSpec 文档校验，并确认这次整合不会影响现有 `minebot-project` 作为最高优先级项目约束的定位。
