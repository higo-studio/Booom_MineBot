# BOOOM Minebot 文档索引与优先级

## 用途

本文件用于整理当前项目内已经沉淀的文档，并定义它们在项目内的阅读优先级。任何需要人工审查的输出都应默认以简体中文撰写。

## 优先级顺序

当多个文档或说明同时存在时，默认按以下顺序作为依据：

1. 当前用户在对话中的最新明确要求
2. `minebot-project/SKILL.md` 中的项目总约束
3. `unity-mcp-bridge/SKILL.md`（仅当任务涉及 Unity 编译、Play Mode、Console、Scene/Asset、Unity 测试或 MPPM）
4. `unity-mcp-bridge/references/unity-mcp-setup.md`（仅当任务涉及 UnityMCP 接入、排障或配置）
5. `openspec/changes/bootstrap-minebot-foundation/proposal.md`
6. `openspec/changes/bootstrap-minebot-foundation/design.md`
7. `openspec/changes/bootstrap-minebot-foundation/specs/`
8. `openspec/changes/bootstrap-minebot-foundation/tasks.md`
9. `references/planning-summary.md`
10. 飞书原始策划文档（仅在需要校验原文或确认更新时重新读取）

## 当前文档说明

- `proposal.md`
  说明这次变更为什么要做、要覆盖哪些能力、影响哪些范围。
- `design.md`
  说明 Unity 基础框架、技术栈、模块边界、启动流程、开发顺序和取舍。
- `specs/`
  说明每个能力的可验证要求，是实现和测试的直接依据。
- `tasks.md`
  说明按依赖排序后的执行步骤，是后续落地时的任务清单。
- `planning-summary.md`
  说明当前策划提炼后的核心玩法目标、MVP 边界和待定项。
- `unity-mcp-bridge/SKILL.md`
  说明项目内 Unity MCP 的本地包装 skill 入口，以及何时应将 Unity 编辑器任务路由到 MCP。
- `unity-mcp-setup.md`
  说明 UnityMCP 的桥接启用前提、Codex 配置、主实例限制和最小排障路径。

## 审查语言规则

- 所有需要人工审查、确认、汇报、评审或归档的内容统一使用简体中文。
- OpenSpec spec 文件为了兼容解析器，保留 `ADDED Requirements`、`Requirement`、`Scenario` 等结构关键字，其余内容使用中文。
- 如果后续新增项目内约束文档，也默认遵循上述中文规则。
