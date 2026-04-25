using System.IO;
using UnityEditor.PackageManager;
using UnityEngine;

namespace McpBridge.Editor
{
    internal static class McpBridgePaths
    {
        private const string k_CompileStateFileName = "compile-state.json";
        private const string k_HostStateFileName = "host-state.json";

        public static string StateDirectory
        {
            get
            {
                var path = Path.Combine(Path.GetTempPath(), "McpBridge", Application.productName);
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string CompileStatePath => Path.Combine(StateDirectory, k_CompileStateFileName);
        public static string HostStatePath => Path.Combine(StateDirectory, k_HostStateFileName);

        public static string PackageRoot
        {
            get
            {
                var package = PackageInfo.FindForAssembly(typeof(McpBridgePaths).Assembly);
                if (package != null && !string.IsNullOrWhiteSpace(package.resolvedPath))
                {
                    return Path.GetFullPath(package.resolvedPath);
                }

                return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Packages/com.himimi.unity-mcp-bridge"));
            }
        }

        public static string HostProjectPath =>
            Path.GetFullPath(Path.Combine(PackageRoot, "Tools~/UnityMcpBridge.Host/UnityMcpBridge.Host.csproj"));

        public static string HostProgramPath =>
            Path.GetFullPath(Path.Combine(PackageRoot, "Tools~/UnityMcpBridge.Host/Program.cs"));

        public static string HostPublishDir =>
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Library/McpBridge/Host"));
    }
}
