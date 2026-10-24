using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

// --// Custom drawer references:
// --// https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CustomPropertyDrawer.html
// --// https://docs.unity3d.com/ScriptReference/PropertyDrawer.html
[CustomPropertyDrawer(typeof(DialogueLine))]
public class DialogueLineDrawer : PropertyDrawer
{
    private const int CharLimit = 110;
    private const float SentenceHeight = 60f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // --// Wrap the whole block so prefab overrides and multi-object editing still behave properly.
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty charProp = property.FindPropertyRelative("character");
        SerializedProperty emotionProp = property.FindPropertyRelative("emotion");
        SerializedProperty nameOverProp = property.FindPropertyRelative("characterNameOverride");
        SerializedProperty portOverProp = property.FindPropertyRelative("portraitOverride");
        SerializedProperty sentenceProp = property.FindPropertyRelative("sentence");
        SerializedProperty textEffectProp = property.FindPropertyRelative("textEffect");
        SerializedProperty targetWordProp = property.FindPropertyRelative("targetWord");
        SerializedProperty useTextTintProp = property.FindPropertyRelative("useTextTint");
        SerializedProperty textTintProp = property.FindPropertyRelative("textTint");
        SerializedProperty useCustomOpacityProp = property.FindPropertyRelative("useCustomOpacity");
        SerializedProperty opacityMultiplierProp = property.FindPropertyRelative("opacityMultiplier");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        int charCount = string.IsNullOrEmpty(sentenceProp.stringValue) ? 0 : sentenceProp.stringValue.Length;

        GUIStyle counterStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight
        };
        counterStyle.normal.textColor = charCount > CharLimit ? Color.red : Color.gray;

        Rect headerRect = new Rect(position.x, position.y, position.width, lineHeight);

        property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, label, true);
        EditorGUI.LabelField(headerRect, $"{charCount}/{CharLimit}", counterStyle);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            Rect contentRect = new Rect(
                position.x,
                position.y + lineHeight + spacing,
                position.width,
                0f
            );

            contentRect = EditorGUI.IndentedRect(contentRect);

            // --// Character first so the emotion popup can filter against that profile's valid portraits.
            DrawProperty(ref contentRect, charProp);
            DrawEmotionField(ref contentRect, charProp, emotionProp);

            DrawProperty(ref contentRect, nameOverProp);
            DrawProperty(ref contentRect, portOverProp);

            Rect sentenceLabelRect = new Rect(contentRect.x, contentRect.y, contentRect.width, lineHeight);
            EditorGUI.LabelField(sentenceLabelRect, "Sentence:");
            contentRect.y += lineHeight + spacing;

            Rect sentenceRect = new Rect(contentRect.x, contentRect.y, contentRect.width, SentenceHeight);
            sentenceProp.stringValue = EditorGUI.TextArea(sentenceRect, sentenceProp.stringValue, EditorStyles.textArea);
            contentRect.y += SentenceHeight + spacing;

            Rect fxLabelRect = new Rect(contentRect.x, contentRect.y, contentRect.width, lineHeight);
            EditorGUI.LabelField(fxLabelRect, "Text Effects:");
            contentRect.y += lineHeight + spacing;

            DrawProperty(ref contentRect, textEffectProp);
            DrawProperty(ref contentRect, targetWordProp, "Target Text");

            Rect styleLabelRect = new Rect(contentRect.x, contentRect.y, contentRect.width, lineHeight);
            EditorGUI.LabelField(styleLabelRect, "Style Overrides:");
            contentRect.y += lineHeight + spacing;

            DrawProperty(ref contentRect, useTextTintProp);

            if (useTextTintProp.boolValue)
            {
                DrawProperty(ref contentRect, textTintProp);
            }

            DrawProperty(ref contentRect, useCustomOpacityProp);

            if (useCustomOpacityProp.boolValue)
            {
                DrawProperty(ref contentRect, opacityMultiplierProp);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        if (!property.isExpanded)
            return lineHeight;

        SerializedProperty charProp = property.FindPropertyRelative("character");
        SerializedProperty nameOverProp = property.FindPropertyRelative("characterNameOverride");
        SerializedProperty portOverProp = property.FindPropertyRelative("portraitOverride");
        SerializedProperty textEffectProp = property.FindPropertyRelative("textEffect");
        SerializedProperty targetWordProp = property.FindPropertyRelative("targetWord");
        SerializedProperty useTextTintProp = property.FindPropertyRelative("useTextTint");
        SerializedProperty textTintProp = property.FindPropertyRelative("textTint");
        SerializedProperty useCustomOpacityProp = property.FindPropertyRelative("useCustomOpacity");
        SerializedProperty opacityMultiplierProp = property.FindPropertyRelative("opacityMultiplier");

        // --// Height has to mirror OnGUI exactly or the drawer will overlap itself in the Inspector.
        float total = 0f;

        total += lineHeight + spacing;

        total += EditorGUI.GetPropertyHeight(charProp, true) + spacing;

        total += lineHeight + spacing;

        total += EditorGUI.GetPropertyHeight(nameOverProp, true) + spacing;
        total += EditorGUI.GetPropertyHeight(portOverProp, true) + spacing;

        total += lineHeight + spacing;
        total += SentenceHeight + spacing;

        total += lineHeight + spacing;
        total += EditorGUI.GetPropertyHeight(textEffectProp, true) + spacing;
        total += EditorGUI.GetPropertyHeight(targetWordProp, true) + spacing;

        total += lineHeight + spacing;
        total += EditorGUI.GetPropertyHeight(useTextTintProp, true) + spacing;

        if (useTextTintProp.boolValue)
        {
            total += EditorGUI.GetPropertyHeight(textTintProp, true) + spacing;
        }

        total += EditorGUI.GetPropertyHeight(useCustomOpacityProp, true) + spacing;

        if (useCustomOpacityProp.boolValue)
        {
            total += EditorGUI.GetPropertyHeight(opacityMultiplierProp, true) + spacing;
        }

        return total;
    }

    private void DrawProperty(ref Rect rect, SerializedProperty prop, string customLabel = null)
    {
        // --// GetPropertyHeight accounts for headers, spacing attributes, and multi-line fields.
        float height = EditorGUI.GetPropertyHeight(prop, true);

        Rect fieldRect = new Rect(rect.x, rect.y, rect.width, height);

        if (string.IsNullOrEmpty(customLabel))
        {
            EditorGUI.PropertyField(fieldRect, prop, true);
        }
        else
        {
            EditorGUI.PropertyField(fieldRect, prop, new GUIContent(customLabel), true);
        }

        rect.y += height + EditorGUIUtility.standardVerticalSpacing;
    }

    private void DrawEmotionField(ref Rect rect, SerializedProperty charProp, SerializedProperty emotionProp)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        Rect emotionRect = new Rect(rect.x, rect.y, rect.width, lineHeight);

        CharacterProfile profile = charProp.objectReferenceValue as CharacterProfile;

        using (new EditorGUI.DisabledScope(profile == null))
        {
            if (profile != null)
            {
                // --// Build a clean popup from the portraits that actually exist on this profile.
                List<string> validEmotions = new List<string> { "Neutral" };

                foreach (var portrait in profile.portraits)
                {
                    string emotionName = portrait.emotion.ToString();

                    if (!validEmotions.Contains(emotionName))
                        validEmotions.Add(emotionName);
                }

                string currentEmotion = ((Emotion)emotionProp.enumValueIndex).ToString();
                int currentIndex = Mathf.Max(0, validEmotions.IndexOf(currentEmotion));

                int newIndex = EditorGUI.Popup(emotionRect, "Emotion", currentIndex, validEmotions.ToArray());

                if (newIndex != currentIndex)
                {
                    emotionProp.enumValueIndex =
                        (int)Enum.Parse(typeof(Emotion), validEmotions[newIndex]);
                }
            }
            else
            {
                EditorGUI.PropertyField(emotionRect, emotionProp);
            }
        }

        rect.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
    }
}
