using System;
using UnityEditor;
using UnityEngine;

namespace Fantasy
{
    [Serializable]
    public class FantasyUIRefData
    {
        public string key;
        public string value;
    }

    [CustomPropertyDrawer(typeof(FantasyUIRefData))]
    public class FantasyUIRefDataDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 19f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // base.OnGUI(position, property, label);
            position.height = 18f;
            using var propertyScope = new EditorGUI.PropertyScope(position, label, property);

            // position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var keyRect = new Rect(position.x, position.y, position.width * 0.3f, position.height);
            var valueRect = new Rect(position.x + position.width * 0.3f + 10f, position.y, position.width - position.width * 0.3f - 10f, position.height);

            EditorGUI.PropertyField(keyRect, property.FindPropertyRelative("key"), GUIContent.none);
            EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("value"), GUIContent.none);
        }
    }
}