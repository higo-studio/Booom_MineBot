using System.Collections.Generic;
using Minebot.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Minebot.Editor
{
    [CustomEditor(typeof(DualGridPreviewHost))]
    public sealed class DualGridPreviewHostEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            var host = (DualGridPreviewHost)target;
            EditorGUILayout.Space();
            DrawValidation(host.ValidateConfiguration());
            DrawActions(host);
        }

        private static void DrawValidation(IReadOnlyList<string> issues)
        {
            if (issues == null || issues.Count == 0)
            {
                EditorGUILayout.HelpBox("Dual-grid preview configuration is valid.", MessageType.Info);
                return;
            }

            for (int i = 0; i < issues.Count; i++)
            {
                EditorGUILayout.HelpBox(issues[i], MessageType.Warning);
            }
        }

        private static void DrawActions(DualGridPreviewHost host)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Preview"))
                {
                    bool rebuilt = host.RebuildPreview();
                    MarkDirty(host);
                    Debug.Log(rebuilt
                        ? $"Dual-grid preview refreshed for '{host.name}'."
                        : $"Dual-grid preview refresh skipped for '{host.name}'. Check the inspector warnings.",
                        host);
                }

                if (GUILayout.Button("Clear Preview"))
                {
                    host.ClearPreview();
                    MarkDirty(host);
                }
            }

            if (GUILayout.Button("Validate Configuration"))
            {
                IReadOnlyList<string> issues = host.ValidateConfiguration();
                if (issues.Count == 0)
                {
                    Debug.Log($"Dual-grid preview configuration is valid for '{host.name}'.", host);
                    return;
                }

                Debug.LogWarning(
                    $"Dual-grid preview configuration has {issues.Count} issue(s) for '{host.name}':\n{string.Join("\n", issues)}",
                    host);
            }
        }

        private static void MarkDirty(DualGridPreviewHost host)
        {
            EditorUtility.SetDirty(host);
            if (host.PreviewRoot != null)
            {
                EditorUtility.SetDirty(host.PreviewRoot.gameObject);
            }

            if (host.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(host.gameObject.scene);
            }
        }
    }
}
