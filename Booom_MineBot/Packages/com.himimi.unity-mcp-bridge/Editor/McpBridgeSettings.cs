using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace McpBridge.Editor
{
    internal sealed class McpBridgeSettings : ScriptableSingleton<McpBridgeSettings>
    {
        [SerializeField] private bool m_Enabled = true;
        [SerializeField] private int m_HttpPort = 63811;
        [SerializeField] private int m_IpcPort = 63812;
        [SerializeField] private bool m_AutoStartHost = true;
        [SerializeField] private bool m_AutoWriteCodexConfig = true;
        [SerializeField] private bool m_EnableReflectionTools = false;
        [SerializeField] private string m_CodexServerName = "unityProjectBubble";
        [SerializeField] private List<string> m_DisabledTools = new();

        public bool Enabled { get => m_Enabled; set => m_Enabled = value; }
        public int HttpPort { get => m_HttpPort; set => m_HttpPort = value; }
        public int IpcPort { get => m_IpcPort; set => m_IpcPort = value; }
        public bool AutoStartHost { get => m_AutoStartHost; set => m_AutoStartHost = value; }
        public bool AutoWriteCodexConfig { get => m_AutoWriteCodexConfig; set => m_AutoWriteCodexConfig = value; }
        public bool EnableReflectionTools { get => m_EnableReflectionTools; set => m_EnableReflectionTools = value; }
        public string CodexServerName { get => m_CodexServerName; set => m_CodexServerName = value; }

        public string HttpUrl => $"http://127.0.0.1:{m_HttpPort}/mcp";

        public bool IsToolEnabled(string toolName)
        {
            if (!m_EnableReflectionTools && McpToolConventions.IsReflectionTool(toolName))
            {
                return false;
            }

            return !m_DisabledTools.Contains(toolName);
        }

        public void SetToolEnabled(string toolName, bool enabled)
        {
            if (enabled) m_DisabledTools.Remove(toolName);
            else if (!m_DisabledTools.Contains(toolName)) m_DisabledTools.Add(toolName);
        }

        public void SaveSettings() => Save(true);
    }
}
