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
                label = "MCP 桥接",
                guiHandler = _ => DrawGui(),
                keywords = new HashSet<string>(new[] { "MCP", "桥接", "Codex", "工具", "编译" })
            };
        }

        private static void DrawGui()
        {
            var settings = McpBridgeSettings.instance;
            settings.EnsureProjectScopedDefaults();

            EditorGUILayout.LabelField("桥接", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            settings.Enabled = EditorGUILayout.Toggle("启用桥接", settings.Enabled);
            settings.AutoStartHost = EditorGUILayout.Toggle("自动启动宿主进程", settings.AutoStartHost);
            settings.HttpPort = EditorGUILayout.IntField("HTTP 端口", settings.HttpPort);
            settings.IpcPort = EditorGUILayout.IntField("IPC 端口", settings.IpcPort);
            var bridgeChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Codex", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            settings.AutoWriteCodexConfig = EditorGUILayout.Toggle("自动写入 ~/.codex/config.toml", settings.AutoWriteCodexConfig);
            settings.CodexServerName = EditorGUILayout.TextField("服务名", settings.CodexServerName);
            var codexChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("高风险", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            settings.EnableReflectionTools = EditorGUILayout.Toggle("启用反射工具", settings.EnableReflectionTools);
            var highRiskChanged = EditorGUI.EndChangeCheck();
            EditorGUILayout.HelpBox(
                "反射工具默认关闭。它会暴露 Unity 编辑器内的方法发现与调用能力，只有在明确需要时才应手动开启。",
                MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("立即写入 Codex 配置"))
                {
                    McpCodexConfigWriter.Write(settings.CodexServerName, settings.HttpUrl);
                }
                EditorGUILayout.SelectableLabel(McpCodexConfigWriter.ConfigPath, GUILayout.Height(18));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("宿主进程", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = !McpBridgeProcessManager.IsHostRunning();
                if (GUILayout.Button("启动宿主进程")) McpBridgeProcessManager.StartHost();
                GUI.enabled = true;
                if (GUILayout.Button("停止宿主进程")) McpBridgeProcessManager.StopHost();
                if (GUILayout.Button("刷新工具列表"))
                {
                    McpToolRegistry.Invalidate();
                    McpBridgeConnection.EnsureConnected();
                    McpBridgeConnection.NotifyToolsChanged();
                }
            }

            EditorGUILayout.LabelField("HTTP 地址", settings.HttpUrl);
            EditorGUILayout.LabelField("宿主进程运行中", McpBridgeProcessManager.IsHostRunning() ? "是" : "否");
            EditorGUILayout.LabelField("Unity 已连接", McpBridgeConnection.IsConnected ? "是" : "否");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("工具开关", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "可单独启用或禁用 MCP 工具。变更后会向已连接客户端广播 notifications/tools/list_changed。",
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
