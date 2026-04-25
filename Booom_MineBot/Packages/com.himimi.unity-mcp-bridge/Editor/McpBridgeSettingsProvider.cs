using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace McpBridge.Editor
{
    internal static class McpBridgeSettingsProvider
    {
        private static Vector2 s_ToolScroll;

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/MCP Bridge", SettingsScope.Project)
            {
                label = "MCP Bridge",
                guiHandler = _ => DrawGui(),
                keywords = new HashSet<string>(new[] { "MCP", "Bridge", "Codex", "Tools", "Compile" })
            };
        }

        private static void DrawGui()
        {
            var settings = McpBridgeSettings.instance;

            EditorGUILayout.LabelField("Bridge", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            settings.Enabled = EditorGUILayout.Toggle("Enabled", settings.Enabled);
            settings.AutoStartHost = EditorGUILayout.Toggle("Auto Start Host", settings.AutoStartHost);
            settings.HttpPort = EditorGUILayout.IntField("HTTP Port", settings.HttpPort);
            settings.IpcPort = EditorGUILayout.IntField("IPC Port", settings.IpcPort);
            var bridgeChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Codex", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            settings.AutoWriteCodexConfig = EditorGUILayout.Toggle("Auto Write ~/.codex/config.toml", settings.AutoWriteCodexConfig);
            settings.CodexServerName = EditorGUILayout.TextField("Server Name", settings.CodexServerName);
            var codexChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("High Risk", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            settings.EnableReflectionTools = EditorGUILayout.Toggle("Enable Reflection Tools", settings.EnableReflectionTools);
            var highRiskChanged = EditorGUI.EndChangeCheck();
            EditorGUILayout.HelpBox(
                "Reflection tools are disabled by default. They expose method discovery and invocation inside the Unity editor and should only be enabled deliberately.",
                MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Write Codex Config Now"))
                {
                    McpCodexConfigWriter.Write(settings.CodexServerName, settings.HttpUrl);
                }
                EditorGUILayout.SelectableLabel(McpCodexConfigWriter.ConfigPath, GUILayout.Height(18));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Host", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = !McpBridgeProcessManager.IsHostRunning();
                if (GUILayout.Button("Start Host")) McpBridgeProcessManager.StartHost();
                GUI.enabled = true;
                if (GUILayout.Button("Stop Host")) McpBridgeProcessManager.StopHost();
                if (GUILayout.Button("Refresh Tools"))
                {
                    McpToolRegistry.Invalidate();
                    McpBridgeConnection.EnsureConnected();
                    McpBridgeConnection.NotifyToolsChanged();
                }
            }

            EditorGUILayout.LabelField("HTTP URL", settings.HttpUrl);
            EditorGUILayout.LabelField("Host Running", McpBridgeProcessManager.IsHostRunning() ? "Yes" : "No");
            EditorGUILayout.LabelField("Unity Connected", McpBridgeConnection.IsConnected ? "Yes" : "No");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Toggle individual MCP tools. Changes broadcast notifications/tools/list_changed to connected clients.",
                MessageType.None);

            var toolsChanged = false;
            using (var scroll = new EditorGUILayout.ScrollViewScope(s_ToolScroll, GUILayout.MaxHeight(220)))
            {
                s_ToolScroll = scroll.scrollPosition;
                var currentGroup = string.Empty;
                foreach (var descriptor in McpToolRegistry.GetAllDescriptors())
                {
                    var group = McpToolConventions.GetGroup(descriptor.Name);
                    if (!string.Equals(group, currentGroup, System.StringComparison.Ordinal))
                    {
                        currentGroup = group;
                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField(currentGroup, EditorStyles.miniBoldLabel);
                    }

                    var wasEnabled = settings.IsToolEnabled(descriptor.Name);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var nowEnabled = EditorGUILayout.ToggleLeft(descriptor.Name, wasEnabled, GUILayout.Width(260));
                        EditorGUILayout.LabelField(descriptor.Title ?? "", EditorStyles.miniLabel);
                        if (nowEnabled != wasEnabled)
                        {
                            settings.SetToolEnabled(descriptor.Name, nowEnabled);
                            toolsChanged = true;
                        }
                    }
                }
            }

            if (bridgeChanged || codexChanged || highRiskChanged || toolsChanged)
            {
                settings.SaveSettings();
                if (bridgeChanged)
                {
                    McpBridgeProcessManager.EnsureDesiredState();
                    if (settings.AutoWriteCodexConfig)
                    {
                        McpCodexConfigWriter.Write(settings.CodexServerName, settings.HttpUrl);
                    }
                }
                if (codexChanged && settings.AutoWriteCodexConfig)
                {
                    McpCodexConfigWriter.Write(settings.CodexServerName, settings.HttpUrl);
                }
                if (highRiskChanged || toolsChanged)
                {
                    McpToolRegistry.Invalidate();
                    McpBridgeConnection.NotifyToolsChanged();
                }
            }
        }
    }
}
