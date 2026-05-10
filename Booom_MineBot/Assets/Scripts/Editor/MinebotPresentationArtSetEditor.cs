using Minebot.Presentation;
using UnityEditor;
using UnityEngine;

namespace Minebot.Editor
{
    [CustomEditor(typeof(MinebotPresentationArtSet))]
    public sealed class MinebotPresentationArtSetEditor : UnityEditor.Editor
    {
        private static readonly string[] ExcludedProperties =
        {
            "m_Script",
            "dualGridTerrainProfile",
            "layoutSettings",
            "families",
            "legacyTopology",
            "allowLegacyArtSetFallback",
            "floorDualGridTiles",
            "soilDualGridTiles",
            "stoneDualGridTiles",
            "hardRockDualGridTiles",
            "ultraHardDualGridTiles",
            "boundaryDualGridTiles",
            "fogNearDualGridTiles",
            "fogDeepDualGridTiles",
            "wallContourTiles",
            "dangerContourTiles"
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DualGridAuthoringInspector.DrawArtSetConfigurationFields(serializedObject);

            EditorGUILayout.Space(12f);
            DrawPropertiesExcluding(serializedObject, ExcludedProperties);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            DualGridAuthoringInspector.DrawValidationMessages(((MinebotPresentationArtSet)target).GetDualGridValidationIssues());
        }
    }
}
