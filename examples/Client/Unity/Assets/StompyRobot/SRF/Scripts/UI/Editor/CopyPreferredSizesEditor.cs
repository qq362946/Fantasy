using UnityEditor;

namespace SRF.UI.Editor
{
    [CustomEditor(typeof(CopyPreferredSizes))]
    [CanEditMultipleObjects]
    public class CopyPreferredSizesEditor : UnityEditor.Editor
    {
        private SerializedProperty _copySourcesProperty;
        private SerializedProperty _operationProperty;

        protected void OnEnable()
        {
            _copySourcesProperty = serializedObject.FindProperty("CopySources");
            _operationProperty = serializedObject.FindProperty("Operation");
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_operationProperty);
            EditorGUILayout.PropertyField(_copySourcesProperty);
            serializedObject.ApplyModifiedProperties();

            serializedObject.Update();
        }
    }
}