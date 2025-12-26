using UnityEditor;
using UniRecast.Toolsets;

namespace UniRecast.Editor
{
    [CustomEditor(typeof(UniRcCrowdAgentProfilingTool))]
    public class UniRcCrowdAgentProfilingToolEditor : UniRcToolEditor
    {
        protected override void Layout()
        {
            UniRcGui.Text(this.GetType().Name);
            UniRcGui.Separator();
        }
    }
}