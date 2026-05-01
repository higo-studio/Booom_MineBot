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
                EditorGUILayout.HelpBox("双网格预览配置有效。", MessageType.Info);
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
                if (GUILayout.Button("刷新预览"))
                {
                    bool rebuilt = host.RebuildPreview();
                    MarkDirty(host);
                    Debug.Log(rebuilt
                        ? $"已刷新 '{host.name}' 的双网格预览。"
                        : $"已跳过 '{host.name}' 的双网格预览刷新，请先检查 Inspector 中的警告。",
                        host);
                }

                if (GUILayout.Button("清空预览"))
                {
                    host.ClearPreview();
                    MarkDirty(host);
                }
            }

            if (GUILayout.Button("校验配置"))
            {
                IReadOnlyList<string> issues = host.ValidateConfiguration();
                if (issues.Count == 0)
                {
                    Debug.Log($"'{host.name}' 的双网格预览配置有效。", host);
                    return;
                }

                Debug.LogWarning(
                    $"'{host.name}' 的双网格预览配置有 {issues.Count} 个问题：\n{string.Join("\n", issues)}",
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
