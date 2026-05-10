using Minebot.Presentation;
using UnityEditor;
using UnityEngine;

namespace Minebot.Editor
{
    [CustomEditor(typeof(DualGridTerrainProfile))]
    public sealed class DualGridTerrainProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DualGridAuthoringInspector.DrawProfileConfigurationFields(serializedObject);
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            DualGridAuthoringInspector.DrawValidationMessages(((DualGridTerrainProfile)target).GetValidationIssues());
        }
    }
}
