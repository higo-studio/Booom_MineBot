using McpBridge;
using UnityEditor;

namespace McpBridge.Editor
{
    [InitializeOnLoad]
    internal static class McpMainThreadBootstrap
    {
        static McpMainThreadBootstrap()
        {
            _ = MainThread.Instance;
        }
    }
}
