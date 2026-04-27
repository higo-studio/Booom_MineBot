# Unity MCP 快速手册

## 先用它做什么

- 编译：`unity.compile`
- 看编辑器状态：`unity.editor_state`
- 看报错：`unity.console_logs`
- 跑精准 EditMode 用例：`unity.tests_run`
- 查场景根对象和资源：`unity.scene_get_data`、`unity.asset_find`、`unity.asset_read`

## 先别指望它做什么

- 不把它当“大规模 prefab 迁移器”。
- 不把 `PlayMode` 请求断连直接当成测试失败。
- 不把 `source:"game"` 截图当唯一可靠链路。

## 当前真边界

- 许多 Unity 工具是延迟暴露的，先用 `tool_search` 拉工具。
- 新版 bridge 已支持 `prefab_get_data`、`prefab_gameobject_*`、`prefab_component_*`。
- 新版 bridge 已支持 `gameobject_get_components`，组件修改不再默认卡在 `componentId`。
- `PlayMode` 测试若仍失败，先区分“桥接断连”还是“测试本身失败”，必要时补看 `~/Library/Application Support/DefaultCompany/Booom_MineBot/TestResults.xml`。
- `PlayMode` 测试前必须补查 `unity.scene_list_opened`；如果 open scene 是 dirty，先别跑 `tests_run(play)`。
- 当前 bridge 对 `unity.tests_run(mode:"play")` 走的是“host 等重连，Unity 侧续回包”的链路，不再靠 replay 整个测试请求穿过 domain reload。
- 当前 host/adaptor 对 editor 断链会先挂起可恢复请求，给 5 秒短暂 grace，并在 30 秒内等 reconnect；超过 30 秒才返回断链错误。
- 如果报 `SaveCurrentModifiedScenesIfUserWantsTo()` 或 `This cannot be used during play mode`，先查 open scenes 是否真的 dirty；如果场景干净还复现，再怀疑 host/editor 还在跑旧版 bridge，而不是先归因到测试内容本身。
- `console_logs` 这类普通请求如果恰好打在 compile / PlayMode reload 的瞬间，仍可能直接看到断链错误；先补一次 `editor_state`，确认 editor 回来后再重试。

## 推荐顺序

1. `tool_search` 拉出要用的 Unity 工具。
2. `unity.editor_state` + `unity.scene_list_opened` + `unity.console_logs` 确认桥在线且编辑器状态干净。
3. `scene_*` / `asset_*` / `selection_*` 拿对象和资源上下文。
4. 先用 `gameobject_get_components` 拿组件快照。
5. 轻量改动走 `gameobject_*` / `component_*` / `object_modify`。
6. prefab 级改动优先走 `prefab_*`。
7. 先用 `unity.compile(exitPlayMode:true)` 收口，再跑 EditMode 或 PlayMode 测试。

## 何时换策略

- 要批量迁移大量 prefab。
- 要稳定跑 PlayMode 截图流水线。
- 要把项目特定生成规则长期沉淀成 builder。

这三类直接转成：

- 项目内 Editor builder / migrator
- 或文本资产改动 + Unity MCP 验证
