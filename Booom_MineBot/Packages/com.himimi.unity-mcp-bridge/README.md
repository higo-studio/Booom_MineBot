# Unity MCP Bridge

This package embeds the Unity-side MCP bridge and its external Streamable HTTP host project so the bridge can be copied to another Unity repository as a single `Packages/com.himimi.unity-mcp-bridge` directory.

## Contents

- `Runtime/`: public attributes and main-thread helper for custom MCP tools
- `Editor/`: bridge connection, tool registry, settings UI, compile/play-mode helpers, scene/asset tools, MPPM tools
- `Tools~/UnityMcpBridge.Host/`: external .NET host project published and launched by the editor bridge
- `Codex~/unity-mcp-bridge/SKILL.md`: portable Codex skill for projects that adopt this package

## Migration

1. Copy the full `Packages/com.himimi.unity-mcp-bridge` folder into the target Unity project.
2. Ensure the target project uses Unity 6 and has a working `dotnet` runtime available to the editor.
3. Open the project and enable the bridge from `Project Settings > MCP Bridge`.
4. Let the bridge publish the host into `Library/McpBridge/Host`.
5. If you use Codex, copy `Codex~/unity-mcp-bridge/SKILL.md` into a suitable local Codex skill location for that project.

## Notes

- The bridge host is primary-project aware and treats compile / play-mode transitions as primary-only operations when multiple Unity instances are connected.
- Unity gameplay script validation should go through `unity.compile`, not `dotnet build`.
