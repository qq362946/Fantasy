using UnityEditor;

namespace Fantasy
{
    [CustomEditor(typeof(FantasyHorizontalScrollView))]
    public class FantasyHorizontalScrollViewEditor : Editor
    {
        private FantasyHorizontalScrollView _target;

        private SerializedProperty _scrollRect;
        private SerializedProperty _horizontalLayoutGroup;
        private SerializedProperty _contentSizeFitter;
        private SerializedProperty _itemTemplate;

        private void OnEnable()
        {
            _target = (FantasyHorizontalScrollView)target;
            _scrollRect = serializedObject.FindProperty("scrollRect");
            _horizontalLayoutGroup = serializedObject.FindProperty("horizontalLayoutGroup");
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

                if (!_target.horizontalLayoutGroup)
                    EditorGUILayout.HelpBox("Can't Be Null", MessageType.Error);
                EditorGUILayout.PropertyField(_horizontalLayoutGroup);

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