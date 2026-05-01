using UnityEditor;
using UnityEngine;

namespace Minebot.Editor
{
    [CustomPropertyDrawer(typeof(InspectorLabelAttribute))]
    public sealed class InspectorLabelDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, BuildLabel(label), true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, BuildLabel(label), true);
        }

        private GUIContent BuildLabel(GUIContent original)
        {
            return new GUIContent(
                ResolveLabelText(original?.text),
                original?.image,
                original?.tooltip ?? string.Empty);
        }

        private string ResolveLabelText(string fallback)
        {
            var inspectorLabel = attribute as InspectorLabelAttribute;
            return string.IsNullOrWhiteSpace(inspectorLabel?.Label) ? fallback : inspectorLabel.Label;
        }
    }
}
