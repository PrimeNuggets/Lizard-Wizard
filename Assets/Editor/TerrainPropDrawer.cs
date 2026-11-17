using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(TerrainGenerator.Props))]
public class TerrainPropDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;
            foreach (string field in VisibleFields(property))
            {
                var child = property.FindPropertyRelative(field);
                line.y += line.height + EditorGUIUtility.standardVerticalSpacing;
                line.height = EditorGUI.GetPropertyHeight(child, true);
                if (field == nameof(TerrainGenerator.Props.genType))
                    EditorGUI.PropertyField(line, child, new GUIContent("Generation Type"), true);
                else
                    EditorGUI.PropertyField(line, child, true);
            }
            EditorGUI.indentLevel = previousIndent;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (property.isExpanded)
        {
            foreach (string field in VisibleFields(property))
                height += EditorGUIUtility.standardVerticalSpacing
                    + EditorGUI.GetPropertyHeight(property.FindPropertyRelative(field), true);
        }
        return height;
    }

    private static IEnumerable<string> VisibleFields(SerializedProperty property)
    {
        yield return nameof(TerrainGenerator.Props.item);
        yield return nameof(TerrainGenerator.Props.genType);

        var generation = property.FindPropertyRelative(nameof(TerrainGenerator.Props.genType));
        // For mixed selections, keep all settings accessible until a common mode is chosen.
        bool mixed = generation.hasMultipleDifferentValues;
        var mode = (TerrainGenerator.Props.GenType)generation.intValue;

        if (mixed || mode == TerrainGenerator.Props.GenType.Perlin
            || mode == TerrainGenerator.Props.GenType.Scattered)
            yield return nameof(TerrainGenerator.Props.scale);

        if (mixed || mode == TerrainGenerator.Props.GenType.Perlin)
            yield return nameof(TerrainGenerator.Props.spawnChance);

        if (mixed || mode == TerrainGenerator.Props.GenType.Scattered)
        {
            yield return nameof(TerrainGenerator.Props.rollLowerBound);
            yield return nameof(TerrainGenerator.Props.rollUpperBound);
        }

        if (mixed || mode == TerrainGenerator.Props.GenType.Randomized)
        {
            yield return nameof(TerrainGenerator.Props.minDist);
            yield return nameof(TerrainGenerator.Props.radius);
            yield return nameof(TerrainGenerator.Props.maxCount);
        }
    }
}
