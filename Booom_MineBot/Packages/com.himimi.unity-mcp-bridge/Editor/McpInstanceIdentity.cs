using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace McpBridge.Editor
{
    internal static class McpInstanceIdentity
    {
        private static UnityInstanceInfo s_Info;

        public static UnityInstanceInfo Get()
        {
            if (s_Info != null)
            {
                return s_Info;
            }

            var projectPath = Path.GetFullPath(Directory.GetCurrentDirectory());
            var primaryProjectPath = ResolvePrimaryProjectPath(projectPath);
            s_Info = new UnityInstanceInfo
            {
                InstanceId = $"{Process.GetCurrentProcess().Id}@{projectPath}",
                ProcessId = Process.GetCurrentProcess().Id,
                ProjectPath = projectPath,
                PrimaryProjectPath = primaryProjectPath,
                ProjectName = Path.GetFileName(projectPath),
                ProductName = Application.productName
            };
            return s_Info;
        }

        public static bool IsPrimaryInstance()
        {
            var info = Get();
            return string.Equals(
                Path.GetFullPath(info.ProjectPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(info.PrimaryProjectPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                System.StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolvePrimaryProjectPath(string projectPath)
        {
            var marker = Path.DirectorySeparatorChar + "Library" + Path.DirectorySeparatorChar + "VP" + Path.DirectorySeparatorChar;
            var normalized = projectPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var markerIndex = normalized.IndexOf(marker, System.StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                return normalized.Substring(0, markerIndex);
            }

            return projectPath;
        }
    }
}
