using UnityEditor;
using UnityEngine.UI;

namespace Fantasy
{
    [CustomEditor(typeof(ContentSizeFitter), true)]
    [CanEditMultipleObjects]
    public class ContentSizeFitterEditor: UnityEditor.UI.ContentSizeFitterEditor
    {
        private ContentSizeFitter _target;
        private int _targetInstanceId;

        protected override void OnEnable()
        {
            _target = (ContentSizeFitter)target;
            _targetInstanceId = _target.GetInstanceID();
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                FantasyEditorEventHelper.Trigger(_targetInstanceId);
            }
        }
    }
}