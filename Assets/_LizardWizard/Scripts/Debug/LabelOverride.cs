using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

//Xavier: This is a utility script to override the names of variables in the inspector
//Script credits: https://discussions.unity.com/t/can-i-change-variable-name-on-inspector/143568
public class LabelOverride : PropertyAttribute
{
    public string label;
    public LabelOverride(string label) //Unfortunately, this has to be double quotes: "". not single: ''
    {
        this.label = label;
    }

    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(LabelOverride))]
    public class ThisPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            try
            {
                var propertyAttribute = this.attribute as LabelOverride;
                if( IsArray(property) == false )
                {
                    label.text = propertyAttribute.label;
                    
                } else
                {
                    Debug.LogWarningFormat(
                        "{0}(\"{1}\") doesn't support arrays ",
                        typeof(LabelOverride).Name ,
                        propertyAttribute.label
                    );
                }
                EditorGUI.PropertyField( position , property , label );
            } catch ( System.Exception ex ) { Debug.LogException( ex ); }
        }
        
        bool IsArray (SerializedProperty property)
        {
            string path =  property.propertyPath;
            int idot = path.IndexOf('.');
            if( idot==-1 ) return false;
            string propName = path.Substring( 0 , idot );
            SerializedProperty p = property.serializedObject.FindProperty( propName );
            return p.isArray;
            //CREDITS: https://answers.unity.com/questions/603882/serializedproperty-isnt-being-detected-as-an-array.html
        }
    }
    #endif
}
