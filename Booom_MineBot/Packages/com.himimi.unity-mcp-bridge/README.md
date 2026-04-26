# Unity MCP Bridge

This package embeds the Unity-side MCP bridge and its external Streamable HTTP host project so the bridge can be copied into another Unity repository as a single embedded package directory.

## Contents

- `Runtime/`: public attributes and main-thread helper for custom MCP tools
- `Editor/`: bridge connection, tool registry, settings UI, compile/play-mode helpers, scene/asset tools, MPPM tools
- `Tools~/UnityMcpBridge.Host/`: external .NET host project published and launched by the editor bridge
- `Codex~/unity-mcp-bridge/SKILL.md`: portable Codex skill for projects that adopt this package

## Migration

1. Copy the full package directory into the target Unity project under `Packages/`.
2. Ensure the target project uses Unity 6.
3. Open the project and enable the bridge from `Project Settings > MCP Bridge`.
4. Let the bridge publish the host into `Packages/com.himimi.unity-mcp-bridge/Published~/host/<rid>`.
5. If you use Codex, copy `Codex~/unity-mcp-bridge/SKILL.md` into a suitable local Codex skill location for that project.

## Host Runtime Modes

- Default:
  - The editor publishes a framework-dependent host and launches it through `dotnet`.
  - This requires a local .NET runtime.
- Self-contained / single-file:
  - Publish the host with a runtime identifier and `SelfContained=true`.
  - The editor will prefer the published executable directly if it exists in the current platform RID folder under `Packages/com.himimi.unity-mcp-bridge/Published~/host`.
  - Example for Apple Silicon macOS:
    - `dotnet publish Packages/com.himimi.unity-mcp-bridge/Tools~/UnityMcpBridge.Host/UnityMcpBridge.Host.csproj -c Release -r osx-arm64 --self-contained true /p:PublishSingleFile=true -o Packages/com.himimi.unity-mcp-bridge/Published~/host/osx-arm64`
  - Example for Windows x64:
    - `dotnet publish Packages/com.himimi.unity-mcp-bridge/Tools~/UnityMcpBridge.Host/UnityMcpBridge.Host.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o Packages/com.himimi.unity-mcp-bridge/Published~/host/win-x64`
- Native AOT:
  - The editor also supports directly launching a native executable if you publish one to the current platform RID folder under `Packages/com.himimi.unity-mcp-bridge/Published~/host`.
  - Example for Apple Silicon macOS:
    - `dotnet publish Packages/com.himimi.unity-mcp-bridge/Tools~/UnityMcpBridge.Host/UnityMcpBridge.Host.csproj -c Release -r osx-arm64 --self-contained true /p:PublishAot=true -o Packages/com.himimi.unity-mcp-bridge/Published~/host/osx-arm64`
  - Native AOT removes the runtime dependency, but should be validated on each target platform because not every .NET API surface is equally AOT-friendly.

## Notes

- The bridge host is primary-project aware and treats compile / play-mode transitions as primary-only operations when multiple Unity instances are connected.
- The editor currently selects `osx-arm64`, `osx-x64`, or `win-x64` under `Published~/host` based on the running Unity editor platform, and falls back to a framework-dependent `dotnet + dll` launch only when no matching native/self-contained publish is present.
- Unity gameplay script validation should go through `unity.compile`, not `dotnet build`.
- During Play Mode transitions and domain reload, reconnect-aware requests wait for the Unity service to come back online until their request timeout expires.
