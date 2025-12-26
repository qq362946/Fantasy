using UniRecast.Core;
using UniRecast.Toolsets;
using UnityEditor;

namespace UniRecast.Editor
{
    [CustomEditor(typeof(UniRcOffMeshConnectionTool))]
    public class UniRcOffMeshConnectionToolEditor : UniRcToolEditor
    {
        private SerializedProperty _bidir;

        private void OnEnable()
        {
            _bidir = serializedObject.FindPropertySafe(nameof(_bidir));
        }

        protected override void Layout()
        {
            UniRcGui.RadioButton("One Way", _bidir, 0);
            UniRcGui.RadioButton("Bidirectional", _bidir, 1);

            serializedObject.ApplyModifiedProperties();
        }
    }
}