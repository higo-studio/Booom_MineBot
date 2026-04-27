# Unity MCP 实测经验

> 2026-04-28 更新：以下内容已按新版 `Packages/com.himimi.unity-mcp-bridge` 代码能力修正。若当前会话里 `unity.instances` 为空，则说明编辑器尚未连上桥；这时只能确认代码能力，不能完成端到端复测。

## 当前可稳定依赖的能力

- `unity.compile`：当前最稳定的 Unity 验证入口。
- `unity.editor_state`：适合判断是否真的退出了 Play Mode，或者是否还在编译/更新。
- `unity.console_logs`：适合快速确认当前是否有编译或运行时错误。
- `unity.tests_run` + EditMode 精准用例：稳定。
- `unity.scene_get_data`、`unity.scene_list_opened`、`unity.asset_find`、`unity.asset_read`、`unity.selection_get/set`：可用于拿对象 id、定位场景根对象、确认资源存在。
- `unity.gameobject_get_components`：可直接枚举 GameObject 上的组件，不必先手工拿 component id。
- `unity.component_get` / `unity.component_modify`：现在支持 `gameObjectId + componentType + componentIndex` 解析。
- `unity.prefab_get_data`、`unity.prefab_gameobject_*`、`unity.prefab_component_*`：现在可以直接读写 prefab contents。

## 当前高风险能力

- `unity.tests_run` + PlayMode：当前稳定路径已经从“host replay 整个请求”改成了“host 只等重连，Unity 侧持久化 pending request 并在 reload 后续回包”。最小 PlayMode 用例现在可以直接 `passed`，不会再因为 replay 把 `SaveModifiedSceneTask` 顶进 PlayMode。
- 2026-04-28 进一步实测：host/adaptor 已把 editor 断链等待收口到底层。可恢复请求会先进入 reconnect wait，给 5 秒短暂 grace，并在 30 秒内等待 editor 回来；如果 30 秒还没重连，才返回现有断链错误。
- 如果当前会话里 PlayMode 还在干净场景下报 `scene_save_during_play_mode`，优先怀疑 host/editor 仍在跑旧版 bridge，或 open scene 实际未清干净。
- `unity.screenshot source:\"game\"`：新版已加 camera fallback，但在极端编辑器态下仍可能需要手工改用 `camera` 或 `scene`。

## 新确认的 PlayMode 测试坑

- `unity.tests_run(mode:"play")` 之前，如果 open scene 处于 dirty 状态，Unity Test Framework 仍可能在错误时机触发 `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()`。
- 旧版桥接下，这类失败常被 host replay 放大成“干净场景也报 `This cannot be used during play mode`”。2026-04-28 的修复点是：PlayMode 测试请求不再 replay，而是由 Unity 侧 tracker 在 reload 后继续原始 pending request。
- 对 `unity.console_logs` 这类不带 reconnect 语义的普通请求，compile / PlayMode reload 窗口里仍可能直接返回断链错误；如果随后 `unity.editor_state` 很快恢复正常，这通常只是瞬时 editor reconnect，不是 host 卡死。
- 2026-04-28 实测：`BootstrapSceneLoaderInitializesServices` 现在可直接 `passed`；`GameplaySceneSupportsMiningUpgradeRepairAndRobotLoop` 则能回出真实断言失败，而不是 transport error 或 `scene_save_during_play_mode`。

## Prefab / UI 工作流结论

- MCP 已经足够支持“新建对象 -> 加组件 -> 改属性 -> 存成 prefab”这条链。
- 新版 MCP 也已经支持“打开现有 prefab -> 查层级 -> 改子节点 -> 改组件 -> 回存 prefab”。
- 仍然不建议硬上 MCP 的场景，主要是“大规模迁移”和“项目逻辑强耦合的 builder 任务”，不是普通 prefab 内部修改。

## 推荐工作流

1. 先用 `tool_search` 把当前任务真正需要的 Unity 工具拉出来，不要假设它们已经在会话里。
2. 读状态：`unity.editor_state`、`unity.console_logs`、`unity.scene_list_opened`。
3. 查对象：`unity.scene_get_data`、`unity.gameobject_find`、`unity.selection_get/set`。
4. 先用 `gameobject_get_components` 拿组件快照，再用 `component_get` / `component_modify` 做组件级调整。
5. 做场景内结构改动：`gameobject_*`、`component_add`、`object_modify`、`component_modify`。
6. 做 prefab 产物与 prefab contents 改动：`prefab_create`、`prefab_instantiate`、`prefab_get_data`、`prefab_gameobject_*`、`prefab_component_*`、`asset_find/read/delete`。
7. 做最终验证：`unity.compile(exitPlayMode:true)`，必要时补 EditMode 测试。
8. 跑 PlayMode 测试前，再确认一次：`isPlaying=false`、`isCompiling=false`、`isUpdating=false`，且 open scenes 没有 `isDirty=true`。

## PlayMode 失败分流

- 先看 `console_logs` 和异常堆栈。
- 如果是 `SaveCurrentModifiedScenesIfUserWantsTo()` / `This cannot be used during play mode`，先查 dirty scene；若场景干净，再查 host 是否已切到新版 wait-only + tracker 逻辑。
- 如果请求断开但随后 `unity.instances` / `unity.editor_state` 能恢复，优先看是不是 PlayMode reload 的正常重连窗口。
- 如果后续所有 Unity 请求都开始卡，优先检查是否仍有旧 host 进程占着端口，或是否有保存弹窗未处理。

## 何时不要硬上 MCP

- 需要大规模迁移 prefab 结构或批量补引用。
- 需要稳定地跑 PlayMode 截图流水线。
- 需要把一整套项目特定生成规则封成长期复用的 builder。

这三类更适合：

- 先写项目内 Editor builder / migrator。
- 或直接改文本资产，再回到 Unity MCP 做编译与验证。
