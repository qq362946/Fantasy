using UnityEditor;
using UnityEngine.UI;

namespace Fantasy
{
    [CustomEditor(typeof(GridLayoutGroup), true)]
    [CanEditMultipleObjects]
    public class GridLayoutGroupEditor : UnityEditor.UI.GridLayoutGroupEditor
    {
        private GridLayoutGroup _target;
        private int _targetInstanceId;

        protected override void OnEnable()
        {
            _target = (GridLayoutGroup)target;
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