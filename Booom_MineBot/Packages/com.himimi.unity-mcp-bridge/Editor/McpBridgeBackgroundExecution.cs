using UnityEditor;
using UnityEngine;

namespace McpBridge.Editor
{
    [InitializeOnLoad]
    internal static class McpBridgeBackgroundExecution
    {
        private const string ActiveSessionKey = "McpBridge.BackgroundExecution.Active";
        private const string OriginalRunInBackgroundSessionKey = "McpBridge.BackgroundExecution.OriginalRunInBackground";

        static McpBridgeBackgroundExecution()
        {
            RefreshNow();
            EditorApplication.delayCall += RefreshNow;
            EditorApplication.quitting += RestoreIfNeeded;
        }

        public static void RefreshNow()
        {
            var settings = McpBridgeSettings.instance;
            settings.EnsureProjectScopedDefaults();

            if (settings.Enabled)
            {
                ApplyIfNeeded();
            }
            else
            {
                RestoreIfNeeded();
            }
        }

        private static void ApplyIfNeeded()
        {
            if (!SessionState.GetBool(ActiveSessionKey, false))
            {
                SessionState.SetBool(ActiveSessionKey, true);
                SessionState.SetBool(OriginalRunInBackgroundSessionKey, Application.runInBackground);
            }

            if (Application.runInBackground)
            {
                return;
            }

            Application.runInBackground = true;
            Debug.Log("[McpBridge] Enabled Application.runInBackground so MCP keeps working while the Unity window is unfocused.");
        }

        private static void RestoreIfNeeded()
        {
            if (!SessionState.GetBool(ActiveSessionKey, false))
            {
                return;
            }

            var originalRunInBackground = SessionState.GetBool(OriginalRunInBackgroundSessionKey, false);
            SessionState.EraseBool(ActiveSessionKey);
            SessionState.EraseBool(OriginalRunInBackgroundSessionKey);

            if (Application.runInBackground == originalRunInBackground)
            {
                return;
            }

            Application.runInBackground = originalRunInBackground;
            Debug.Log($"[McpBridge] Restored Application.runInBackground to {originalRunInBackground}.");
        }
    }
}
