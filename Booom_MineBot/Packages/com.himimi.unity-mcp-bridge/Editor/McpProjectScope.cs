using System;
using System.IO;
using System.Text;

namespace McpBridge.Editor
{
    internal static class McpProjectScope
    {
        private const int k_BasePort = 38000;
        private const int k_PortPairCount = 9000;

        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static void ComputePorts(string projectPath, out int httpPort, out int ipcPort)
        {
            var normalized = NormalizePath(projectPath);
            var hash = ComputeStableHash(normalized);
            var slot = (int)(hash % k_PortPairCount);
            httpPort = k_BasePort + slot * 2;
            ipcPort = httpPort + 1;
        }

        public static string ComputeCodexServerName(string projectPath)
        {
            var normalized = NormalizePath(projectPath);
            var projectName = Path.GetFileName(normalized);
            var slug = Slugify(projectName);
            var hash = ComputeStableHash(normalized).ToString("x8");
            return $"unity_{slug}_{hash[..6]}";
        }

        private static uint ComputeStableHash(string value)
        {
            unchecked
            {
                const uint offset = 2166136261;
                const uint prime = 16777619;
                var hash = offset;
                foreach (var character in value.ToLowerInvariant())
                {
                    hash ^= character;
                    hash *= prime;
                }

                return hash;
            }
        }

        private static string Slugify(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unity";
            }

            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
                else if (builder.Length == 0 || builder[^1] != '_')
                {
                    builder.Append('_');
                }
            }

            var slug = builder.ToString().Trim('_');
            return string.IsNullOrEmpty(slug) ? "unity" : slug;
        }
    }
}
