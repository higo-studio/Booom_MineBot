---
name: minebot-project
description: BOOOM Minebot 项目内的最高优先级约束与工作流说明。只要任务发生在这个仓库内，无论是规划、实现、调试、评审、文档整理还是把飞书策划案转成 Unity 代码与测试，都先使用本 skill；它负责统一文档优先级、中文人审规则、OpenSpec 流程、Unity 技术边界和模块划分。
---

# Minebot 项目总约束

## 最高优先级约束

- 本 skill 对仓库内所有任务默认生效，优先级高于其它项目内 skill 的细节约定。
- 所有需要人工审查、确认、评估、评审或归档的文档与说明统一使用简体中文。
- 如果当前任务会改变范围、架构、开发顺序或实现边界，先更新文档，再继续代码实现。
- 如果用户给出的新要求与现有文档冲突，以最新用户要求为准，然后同步修正文档和 skill 约束。

## 文档优先级

开始任何任务前，按下面顺序建立上下文：

1. [document-index.md](references/document-index.md)
2. `openspec/changes/bootstrap-minebot-foundation/proposal.md`
3. `openspec/changes/bootstrap-minebot-foundation/design.md`
4. `openspec/changes/bootstrap-minebot-foundation/specs/`
5. `openspec/changes/bootstrap-minebot-foundation/tasks.md`
6. [planning-summary.md](references/planning-summary.md)

只有当用户明确要求校验原始策划、或怀疑策划发生变化时，才重新读取飞书源文档。

## 项目技术边界

- 引擎：Unity `6000.0.59f2`
- 技术栈：C#、URP、Input System、UGUI、ScriptableObject、Unity Test Framework
- 核心规则模型：确定性的方格模拟；场景对象负责表现，不拥有玩法真相
- 推荐模块：`Bootstrap`、`Common`、`GridMining`、`HazardInference`、`Progression`、`Automation`、`WaveSurvival`、`UI`
- 验证偏好：规则系统优先写 EditMode 测试，启动与场景联动优先写 PlayMode 烟雾测试
- 除非用户明确改变方向，否则不要引入 DOTS/ECS、多人联机、Addressables、第三方 DI/FSM/行为树框架，也不要把 Visual Scripting 作为主实现路径

## 当前能力边界

- `project-foundation`：场景、asmdef、bootstrap、配置资产、测试、项目 skill 文档
- `grid-mining-loop`：网格状态、移动、挖掘、岩壁硬度与奖励
- `hazard-inference`：炸药、探测数字、标记状态、连锁爆炸
- `progression-and-base-ops`：经验、升级 UI、维修站、机器人工厂、经济系统
- `automation-and-wave-survival`：从属机器人、波次计时、危险区、计分与失败条件

## 工作流程

1. 先判断任务是否属于当前 OpenSpec change，再决定是补文档还是直接实现。
2. 实现顺序默认遵循：
   - 项目基础
   - 方格挖掘
   - 风险判断
   - 成长/据点
   - 自动化/地震波
   - 集成打磨
3. 运行时规则先落在纯 C# 服务和数据模型中，再接入 MonoBehaviour 表现层。
4. 可调数值优先进入 ScriptableObject 配置，不要散落到场景脚本常量里。
5. 当任务涉及 Unity 编译、Play Mode、Console、Scene/Asset、Unity 测试或 Multiplayer Play Mode 时，先切换到 `unity-mcp-bridge` skill。
6. 用户引用飞书策划或存在时效性不确定时，用 `lark-doc` 或 `lark-wiki` 校验原文再改行为。
7. 如果任务超出当前 MVP 边界，先明确说明范围变化，再继续落文档和实现。

## UnityMCP 路由

- Unity 脚本编译校验必须使用 `unity.compile`
- 如果 `unity.compile` 因 Play Mode 被阻塞，可使用 `exitPlayMode=true` 重试
- Unity MCP 不可用、断连或桥接未启用时，报告“验证被阻塞”，不要回退到 `dotnet build`
- 读取 Console、切换 Play Mode、操作 Scene/Asset、运行 Unity 测试时，优先走 `unity-mcp-bridge`

## 验证要求

- 基础框架改动后：验证编译、Bootstrap 场景流和测试发现
- 规则改动后：补齐或更新 EditMode 测试
- 循环改动后：手动冒烟验证 `Gameplay` 与 `DebugSandbox`
- OpenSpec 文档改动后：运行 `openspec validate bootstrap-minebot-foundation`

## 参考文档

- [document-index.md](references/document-index.md)
- [planning-summary.md](references/planning-summary.md)
- `../unity-mcp-bridge/SKILL.md`
- `openspec/changes/bootstrap-minebot-foundation/design.md`
- `openspec/changes/bootstrap-minebot-foundation/tasks.md`
