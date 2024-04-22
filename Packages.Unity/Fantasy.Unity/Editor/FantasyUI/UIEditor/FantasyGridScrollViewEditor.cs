using UnityEditor;

namespace Fantasy
{
    [CustomEditor(typeof(FantasyGridScrollView))]
    public class FantasyGridScrollViewEditor : Editor
    {
        private FantasyGridScrollView _target;

        private SerializedProperty _scrollRect;
        private SerializedProperty _gridLayoutGroup;
        private SerializedProperty _contentSizeFitter;
        private SerializedProperty _itemTemplate;

        private void OnEnable()
        {
            _target = (FantasyGridScrollView)target;
            _scrollRect = serializedObject.FindProperty("scrollRect");
            _gridLayoutGroup = serializedObject.FindProperty("gridLayoutGroup");
            _contentSizeFitter = serializedObject.FindProperty("contentSizeFitter");
            _itemTemplate = serializedObject.FindProperty("itemTemplate");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (!_target.scrollRect)
                    EditorGUILayout.HelpBox("Can't Be Null", MessageType.Error);
                EditorGUILayout.PropertyField(_scrollRect);

                if (!_target.gridLayoutGroup)
                    EditorGUILayout.HelpBox("Can't Be Null", MessageType.Error);
                EditorGUILayout.PropertyField(_gridLayoutGroup);

                if (!_target.contentSizeFitter)
                    EditorGUILayout.HelpBox("Can't Be Null", MessageType.Error);
                EditorGUILayout.PropertyField(_contentSizeFitter);

                EditorGUILayout.Space();
                if (!_target.itemTemplate)
                    EditorGUILayout.HelpBox("Can't Be Null", MessageType.Error);
                EditorGUILayout.PropertyField(_itemTemplate);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}