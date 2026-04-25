---
name: unity-mcp-bridge
description: Use this skill when a Unity project includes the embedded Unity MCP Bridge package and you need to drive compile, play mode, console, scene, asset, or Multiplayer Play Mode workflows through MCP instead of manual editor interaction.
---

# Unity MCP Bridge

## Use This Skill When

- The repository includes `Packages/com.himimi.unity-mcp-bridge`
- Unity validation must run through MCP instead of `dotnet build`
- The task needs editor state, play mode, console logs, scene hierarchy, assets, packages, or Multiplayer Play Mode control

## Core Rules

- Use `unity.compile` for Unity script validation.
- If `unity.compile` returns `blocked` because Unity is in Play Mode, retry with `{ "exitPlayMode": true }`.
- In multi-instance setups, treat the primary project as the only valid target for:
  - `unity.compile`
  - `unity.enter_play_mode`
  - `unity.exit_play_mode`
- Mirror instances are for observation only unless a tool explicitly supports them.
- If Unity MCP is unavailable or disconnected, report validation as blocked instead of falling back to `dotnet build`.

## Workflow

1. Inspect active instances with `unity.instances` when Multiplayer Play Mode mirrors may exist.
2. Use `unity.editor_state` and `unity.console_logs` to understand current editor/runtime state.
3. Use `unity.enter_play_mode` and `unity.exit_play_mode` only on the primary project.
4. Use `unity.compile` as the canonical compile check.
5. Use scene/object/asset tools for editor manipulation instead of asking for manual clicks whenever MCP already exposes the operation.

## Tool Groups

- Workflow:
  - `unity.instances`
  - `unity.editor_state`
  - `unity.enter_play_mode`
  - `unity.exit_play_mode`
  - `unity.compile`
  - `unity.console_logs`
  - `unity.screenshot`
  - `unity.selection_get`
  - `unity.selection_set`
  - `unity.tests_run`
- Scene / Object:
  - `unity.scene_*`
  - `unity.gameobject_*`
  - `unity.component_*`
  - `unity.object_*`
- Asset / Project:
  - `unity.asset_*`
  - `unity.prefab_*`
  - `unity.script_*`
  - `unity.package_*`
- Multiplayer Play Mode:
  - `unity.mppm_*`

## Troubleshooting

- `blocked`: Unity is in a state where the requested operation should not proceed yet.
- `busy`: another operation of the same kind is already in flight.
- `retry`: Play Mode transition or domain reload is in progress; retry after the editor stabilizes.
- If Play Mode UI looks stale after a transition, trust MCP state and Console logs rather than the visible editor buttons alone.
