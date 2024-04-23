using UnityEditor;
using UnityEngine.UI;

namespace Fantasy
{
    [CustomEditor(typeof(HorizontalOrVerticalLayoutGroup), true)]
    [CanEditMultipleObjects]
    public class HorizontalOrVerticalLayoutGroupEditor : UnityEditor.UI.HorizontalOrVerticalLayoutGroupEditor
    {
        private HorizontalOrVerticalLayoutGroup _target;
        private int _targetInstanceId;

        protected override void OnEnable()
        {
            _target = (HorizontalOrVerticalLayoutGroup)target;
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