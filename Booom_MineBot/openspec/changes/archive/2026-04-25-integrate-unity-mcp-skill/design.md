## 背景

仓库已经引入 `Packages/com.himimi.unity-mcp-bridge`，包内同时提供了可移植的 `Codex~/unity-mcp-bridge/SKILL.md`。这意味着项目已经具备 Unity MCP 的宿主、Editor 工具和一份通用 skill，但当前项目本地的 Codex skill 体系并没有正式接住这套能力：

- `.codex/skills/` 中还没有本地可发现的 `unity-mcp-bridge` skill
- `minebot-project` 还没有把 Unity 相关任务明确路由到 Unity MCP
- Unity 脚本校验、Play Mode、Console、Scene/Asset 操作仍缺少统一的 MCP 优先规则
- 包 README 明确要求“如果使用 Codex，需要把 `Codex~/unity-mcp-bridge/SKILL.md` 复制到项目本地 skill 位置”，但当前仓库没有完成这一步

这不是桥接包功能开发问题，而是项目工作流整合问题。目标是让项目级最高优先级约束能够识别并使用 UnityMCP，而不是把这份 skill 留在包目录里变成“存在但不会被项目自动继承”的孤岛。

## 目标 / 非目标

**目标：**

- 让 `unity-mcp-bridge` skill 在项目本地可发现、可触发，并纳入 `.codex/skills/` 体系
- 在 `minebot-project` 中明确规定：Unity 编译、Play Mode、Console、Scene/Asset、测试等任务优先走 Unity MCP
- 统一文档：说明桥接启用前提、MCP 不可用时的阻塞规则、多实例下的主实例限制
- 避免 Unity 校验回退到 `dotnet build` 或手工点击编辑器

**非目标：**

- 不修改 `Packages/com.himimi.unity-mcp-bridge` 的运行时或 Editor 代码逻辑
- 不扩展新的 Unity MCP 工具或协议
- 不处理与 UnityMCP 无关的通用 skill 重构
- 不把项目整体验证体系完全改写成新的自动化平台

## 关键决策

### 1. 使用“项目内包装 skill + 包内 upstream skill”模式整合

项目将新增本地 `unity-mcp-bridge` skill，使 Codex 可以从 `.codex/skills/` 直接发现并触发它；同时保留包内 `Packages/com.himimi.unity-mcp-bridge/Codex~/unity-mcp-bridge/SKILL.md` 作为 upstream 事实来源。

推荐结构：

```text
.codex/skills/
  minebot-project/
  unity-mcp-bridge/
    SKILL.md
    references/
      unity-mcp-setup.md

Packages/com.himimi.unity-mcp-bridge/
  Codex~/unity-mcp-bridge/SKILL.md   // upstream source
```

本地 skill 的职责：

- 提供项目内可触发的 frontmatter 和描述
- 指向包内 upstream skill 的核心工作流
- 补充本项目特有的路由规则和约束

选择理由：

- 包内 `Codex~/` 路径天然适合随包分发，但不等于已经接入当前仓库的本地 skill 发现体系。
- 直接依赖包路径会让项目级技能约束难以统一收口；只复制一份完整内容到本地又容易长期漂移。
- 采用“本地包装 + upstream 引用”可以兼顾可发现性和单一事实来源。

备选方案与取舍：

- 备选：完全直接复制包内 skill 到 `.codex/skills/unity-mcp-bridge/`
- 放弃原因：短期简单，但后续包升级时容易出现两份 skill 内容漂移

- 备选：只保留包内 skill，不建立项目本地 skill
- 放弃原因：无法保证项目级 skill 自动继承与稳定触发

### 2. 由 `minebot-project` 负责最高优先级路由，不在每个任务里重复解释

`minebot-project` 仍然是项目内最高优先级 skill，但需要补充一条明确路由：

- 当任务涉及 Unity 编译校验、Play Mode 切换、Console 查询、Scene/Asset 操作、Unity 测试或 Multiplayer Play Mode 时，先切换到 `unity-mcp-bridge`

这样分层：

- `minebot-project`：负责项目范围、优先级、中文人审和工作流路由
- `unity-mcp-bridge`：负责 Unity MCP 的具体工具约束与执行方式

选择理由：

- 保持项目总约束和工具专属工作流的职责分离
- 避免把 Unity MCP 细节塞满 `minebot-project`
- 让后续如果接入更多工具 skill，也能沿用相同路由模式

### 3. 明确 MCP 是 Unity 编辑器交互的权威入口

项目文档需要明确以下规则：

- Unity 脚本编译校验必须使用 `unity.compile`
- 如果 `unity.compile` 因 Play Mode 被阻塞，可带 `exitPlayMode=true` 重试
- Unity MCP 不可用、断连或桥未启用时，应报告“验证被阻塞”，而不是回退到 `dotnet build`
- 读取 Console、切换 Play Mode、操作 Scene/Asset 时优先使用 Unity MCP 工具，不以“手工点编辑器”作为默认路径

选择理由：

- 包内 upstream skill 与 README 已经明确这套规则，项目侧只需要正式采纳，而不是发明新标准
- 这能避免 Agent 在 Unity 项目里误用通用 .NET 校验手段

### 4. 补充桥接启用与验证前提文档

项目本地 `unity-mcp-bridge` skill 需要附带一份项目内可读的 setup/reference 文档，至少覆盖：

- 项目已包含 `Packages/com.himimi.unity-mcp-bridge`
- 在 `Project Settings > MCP Bridge` 中启用桥接
- Editor 需要能运行 `dotnet`
- Bridge 可以自动写入 `~/.codex/config.toml`
- 多实例下主实例和镜像实例的限制

这样做的原因是：skill 本体负责“什么时候用、怎么用”，setup 文档负责“为什么当前不可用、应该先检查什么”。

### 5. 将 UnityMCP 文档纳入项目文档优先级体系

`document-index.md` 需要新增 UnityMCP 文档条目和使用顺序，让后续任务在需要 Unity 编辑器能力时有明确的二级入口。

推荐顺序：

1. `minebot-project/SKILL.md`
2. `unity-mcp-bridge/SKILL.md`
3. `unity-mcp-bridge/references/unity-mcp-setup.md`
4. 包内 upstream skill 与 README

这样能保证项目先应用本地约束，再下钻到工具细节。

## 风险 / 权衡

- [风险] 本地包装 skill 与包内 upstream skill 漂移
  Mitigation: 本地 skill 只保留路由与项目约束，把工具细节尽量指向 upstream 文件

- [风险] 团队误以为整合 skill 等于已经完成桥接启用
  Mitigation: 在 setup 文档中显式区分“文档已接入”和“Unity MCP 可用”两件事，并提供启用清单

- [风险] Agent 仍然沿用 `dotnet build` 做 Unity 编译检查
  Mitigation: 在项目总约束和 UnityMCP skill 中重复强调 `unity.compile` 是唯一权威入口

- [风险] 多实例 / MPPM 环境下误对镜像实例执行主实例操作
  Mitigation: 在 skill 中保留 primary-only 规则，并要求涉及多实例时先查询 `unity.instances`

## 落地与迁移计划

1. 在 `.codex/skills/` 下建立本地 `unity-mcp-bridge` skill 和 setup/reference 文档
2. 更新 `minebot-project`，增加 UnityMCP 路由规则
3. 更新 `document-index.md`，把 UnityMCP 文档纳入项目文档体系
4. 用包内 upstream skill 与 README 做一次人工对照，确认没有遗漏关键 MCP 规则
5. 在实现完成后，以“编译/Play Mode/Console”三个最小场景验证文档可执行性

回退成本很低：主要是文档和本地 skill 结构调整，不涉及运行时代码或资产迁移。

## 未决问题

- 本地包装 skill 是否需要附带 `agents/openai.yaml`，还是沿用最小可发现结构即可
- UnityMCP setup 文档是否需要进一步细化成“本项目日常调试清单”
- 是否要在后续单独提一个 change，把 MCP 驱动的 Unity 测试/截图/场景操作纳入更细的实现工作流
