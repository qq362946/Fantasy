using UnityEditor;
using UnityEditor.UI;

namespace SRF.UI.Editor
{
    [CustomEditor(typeof (SRNumberButton))]
    [CanEditMultipleObjects]
    public class SRNumberButtonEditor : ButtonEditor
    {
        private SerializedProperty _amountProperty;
        private SerializedProperty _targetFieldProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            _targetFieldProperty = serializedObject.FindProperty("TargetField");
            _amountProperty = serializedObject.FindProperty("Amount");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            serializedObject.Update();
            EditorGUILayout.PropertyField(_targetFieldProperty);
            EditorGUILayout.PropertyField(_amountProperty);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
