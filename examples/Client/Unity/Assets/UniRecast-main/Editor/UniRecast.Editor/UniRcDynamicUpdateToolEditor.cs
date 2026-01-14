using UniRecast.Toolsets;
using UnityEditor;

namespace UniRecast.Editor
{
    [CustomEditor(typeof(UniRcDynamicUpdateTool))]
    public class UniRcDynamicUpdateToolEditor : UniRcToolEditor
    {
        protected override void Layout()
        {
            UniRcGui.Text(this.GetType().Name);
            UniRcGui.Separator();
        }
    }
}