using UnityEngine;

[System.AttributeUsage(System.AttributeTargets.Field)]
public sealed class InspectorLabelAttribute : PropertyAttribute
{
    public InspectorLabelAttribute(string label)
    {
        Label = label ?? string.Empty;
    }

    public string Label { get; }
}
