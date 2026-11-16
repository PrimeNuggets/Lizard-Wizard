using UnityEditor;

[CustomEditor(typeof(SpellData), true)]
[CanEditMultipleObjects]
public class SpellsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        }

        DrawPropertiesExcluding(serializedObject, "m_Script", nameof(AttackData.facingOffset));

        serializedObject.ApplyModifiedProperties();
    }
}
