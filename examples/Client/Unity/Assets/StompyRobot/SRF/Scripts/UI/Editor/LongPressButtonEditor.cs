using UnityEditor;
using UnityEditor.UI;

namespace SRF.UI.Editor
{
    [CustomEditor(typeof (LongPressButton), true)]
    [CanEditMultipleObjects]
    public class LongPressButtonEditor : ButtonEditor
    {
        private SerializedProperty _onLongPressProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            _onLongPressProperty = serializedObject.FindProperty("_onLongPress");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            serializedObject.Update();
            EditorGUILayout.PropertyField(_onLongPressProperty);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
