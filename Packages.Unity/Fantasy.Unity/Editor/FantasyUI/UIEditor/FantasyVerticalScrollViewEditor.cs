using UnityEditor;

namespace Fantasy
{
    [CustomEditor(typeof(FantasyVerticalScrollView))]
    public class FantasyVerticalScrollViewEditor :Editor
    {
        private FantasyVerticalScrollView _target;

        private SerializedProperty _scrollRect;
        private SerializedProperty _verticalLayoutGroup;
        private SerializedProperty _contentSizeFitter;
        private SerializedProperty _itemTemplate;

        private void OnEnable()
        {
            _target = (FantasyVerticalScrollView)target;
            _scrollRect = serializedObject.FindProperty("scrollRect");
            _verticalLayoutGroup = serializedObject.FindProperty("verticalLayoutGroup");
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

                if (!_target.verticalLayoutGroup)
                    EditorGUILayout.HelpBox("Can't Be Null", MessageType.Error);
                EditorGUILayout.PropertyField(_verticalLayoutGroup);

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