using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace McpBridge.Editor
{
    internal static class McpCodexConfigWriter
    {
        public static string ConfigPath
        {
            get
            {
                var home = Environment.GetEnvironmentVariable("HOME")
                    ?? Environment.GetEnvironmentVariable("USERPROFILE")
                    ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, ".codex", "config.toml");
            }
        }

        public static void Write(string serverName, string url)
        {
            if (string.IsNullOrWhiteSpace(serverName) || string.IsNullOrWhiteSpace(url)) return;
            var path = ConfigPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var existing = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
            var updated = UpsertSection(existing, serverName, url);
            if (updated == existing) return;

            File.WriteAllText(path, updated, new UTF8Encoding(false));
            Debug.Log($"[McpBridge] Wrote Codex MCP entry '[mcp_servers.{serverName}]' → {path}");
        }

        internal static string UpsertSection(string existing, string serverName, string url)
        {
            var header = $"[mcp_servers.{serverName}]";
            var body = $"{header}\nurl = \"{url}\"\n";

            if (string.IsNullOrEmpty(existing))
            {
                return body;
            }

            var escaped = Regex.Escape(header);
            var pattern = new Regex($@"(^|\n){escaped}\s*\r?\n([^\[]*)", RegexOptions.Singleline);
            if (pattern.IsMatch(existing))
            {
                return pattern.Replace(existing, match =>
                {
                    var prefix = match.Groups[1].Value;
                    return $"{prefix}{body}";
                }, 1);
            }

            var trailing = existing.EndsWith("\n") ? string.Empty : "\n";
            return existing + trailing + "\n" + body;
        }
    }
}
