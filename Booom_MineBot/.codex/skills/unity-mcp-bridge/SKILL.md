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
2. 再读取 [unity-mcp-quick-guide.md](references/unity-mcp-quick-guide.md)，先建立短路径心智模型
3. 再读取 [unity-mcp-practical-notes.md](references/unity-mcp-practical-notes.md)，优先采用项目内已经验证过的工作流和已知坑规避策略
4. 再读取包内 upstream skill：
   `Packages/com.himimi.unity-mcp-bridge/Codex~/unity-mcp-bridge/SKILL.md`
5. 按 upstream skill 的工作流执行具体工具调用

## 最小工作流

### 编译

- 先确认 Unity MCP 可用
- 使用 `unity.compile`
- 如果返回 `blocked` 且原因是 Play Mode，使用 `exitPlayMode=true` 重试

### Play Mode

- 先用 `unity.editor_state` 确认当前编辑器状态
- 跑 `unity.tests_run(mode:"play")` 前，再补查一次 `unity.scene_list_opened`，确认 open scenes 没有 `isDirty=true`
- 使用 `unity.enter_play_mode` / `unity.exit_play_mode`
- 若处于多实例环境，先检查 `unity.instances`

### Console

- 使用 `unity.console_logs`
- 当 Play Mode UI 看起来与实际状态不一致时，以 MCP 返回的编辑器状态和 Console 日志为准

## 实测边界

- 当前 Codex 会话里，Unity MCP 工具经常是“延迟暴露”的。除了编译/PlayMode 这类基础工具外，Scene、GameObject、Asset、Prefab、Component 工具往往需要先用 `tool_search` 按名字显式拉出来。
- `unity.compile`、`unity.editor_state`、`unity.console_logs` 当前是稳定路径，优先用它们判断桥接是否真的在线。
- 当前 bridge 对 `unity.tests_run(mode:"play")` 采用“host 只等重连、Unity 侧持久化 pending request 并在 reload 后续回包”的路径，不再靠 replay 整个测试请求来穿过 PlayMode domain reload。
- 当前 host/adaptor 在 editor 链路掉线后会先挂起可恢复请求，默认给 5 秒短暂 grace，并在 30 秒内等待 reconnect；30 秒还没回来，再返回现有断链错误。
- 如果干净场景下仍报 `InvalidOperationException: This cannot be used during play mode`，且堆栈里出现 `SaveCurrentModifiedScenesIfUserWantsTo()`，优先怀疑当前 host/editor 还在跑旧版 replay 逻辑，或 open scenes 实际仍有 dirty 状态。
- 如 PlayMode 测试请求最终仍失败或断连，可补查 `~/Library/Application Support/DefaultCompany/Booom_MineBot/TestResults.xml`，但新版桥接下更常见的是直接拿到真实测试结果，而不是靠 XML 兜底。
- 新版 `unity.screenshot` 的 `source:\"game\"` 已增加相机 fallback，优先按 `game` 使用；只有当当前编辑器态仍然拿不到图时，再显式改用 `camera` 或 `scene`。
- 新版 `unity.gameobject_find` 应同时支持短类型名和全名，例如 `Camera` 与 `UnityEngine.Camera`。
- 新版 MCP 已补齐 prefab 内容级工具：`unity.prefab_get_data`、`unity.prefab_gameobject_*`、`unity.prefab_component_*`。能直接对现有 prefab 做层级、节点和组件修改时，优先使用这些工具，不要默认退回 YAML。
- 新版 MCP 已补齐组件枚举与按宿主解析：`unity.gameobject_get_components` 以及 `unity.component_get` / `unity.component_modify` 的 `gameObjectId + componentType + componentIndex` 解析链。复杂组件调整不再默认依赖裸 `componentId`。

## Prefab 建议

- 新建或重建 prefab：优先走 `scene_open` / `scene_create`、`gameobject_create`、`component_add`、`gameobject_modify`、`object_modify`、`prefab_create`。
- 查询现有 prefab：优先走 `asset_find`、`asset_read`、`prefab_get_data`。
- 修改现有 prefab：优先走 `prefab_gameobject_*`、`prefab_component_*`，只有在批量迁移规模过大或需要项目特定逻辑时，才转成 Editor builder。

## PlayMode 测试建议

- 推荐顺序：`tool_search` 拉工具 -> `unity.editor_state` -> `unity.scene_list_opened` -> `unity.console_logs` -> `unity.compile(exitPlayMode:true)` -> `unity.tests_run(mode:"play")`
- 如果 `scene_list_opened` 里有 `isDirty=true`，先处理场景保存，再跑 PlayMode 测试。
- 如果 `tests_run(play)` 后续请求也开始卡住，优先检查当前 host 是否还是旧版，或是否存在保存弹窗/脏场景；在新版链路下，最小 PlayMode 用例和真实断言失败都应直接回包，而不是拖住后续 MCP 请求。
- 如果 `unity.console_logs`、`unity.editor_state` 这类普通工具刚好撞上 compile / PlayMode reload 的断链窗口，它们仍可能立刻收到断链错误；先等 editor 重连，再补打一轮，不要把这类瞬时错误误判成 bridge 长期失效。

## 参考

- [unity-mcp-setup.md](references/unity-mcp-setup.md)
- [unity-mcp-quick-guide.md](references/unity-mcp-quick-guide.md)
- [unity-mcp-practical-notes.md](references/unity-mcp-practical-notes.md)
- `Packages/com.himimi.unity-mcp-bridge/Codex~/unity-mcp-bridge/SKILL.md`
- `Packages/com.himimi.unity-mcp-bridge/README.md`
