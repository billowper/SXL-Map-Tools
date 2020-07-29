using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ConditionalAttribute), true)]
public class ConditionalAttributeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (attribute is ConditionalAttribute attr)
        {
            var path = property.propertyPath;
            var condition = property.serializedObject.FindProperty(path.Replace(property.name, attr.ConditionalPropertyName));
            if (condition == null || CheckField(condition, attr) == attr is ShowIfAttribute)
            {
                EditorGUI.BeginChangeCheck();

                EditorGUI.PropertyField(position, property, label, true);

                if (EditorGUI.EndChangeCheck())
                {
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }

    private bool CheckField(SerializedProperty condition, ConditionalAttribute attr)
    {
        if (condition.propertyType == SerializedPropertyType.ObjectReference)
        {
            return condition.objectReferenceValue != null;
        }

        if (condition.propertyType == SerializedPropertyType.Boolean)
        {
            return condition.boolValue;
        }

        if (condition.propertyType == SerializedPropertyType.Integer)
        {
            return condition.intValue == (int) attr.TestValue;
        }

        if (condition.propertyType == SerializedPropertyType.Enum)
        {
            return condition.enumValueIndex == (int) attr.TestValue;
        }

        return false;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (attribute is ConditionalAttribute attr)
        {
            var path = property.propertyPath;
            var condition = property.serializedObject.FindProperty(path.Replace(property.name, attr.ConditionalPropertyName));
            if (condition == null || CheckField(condition, attr) == attr is ShowIfAttribute)
            {
                return base.GetPropertyHeight(property, label);
            }

            return 0f;
        }

        return base.GetPropertyHeight(property, label);
    }
}