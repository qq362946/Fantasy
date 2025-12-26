using UniRecast.Toolsets;
using UnityEditor;

namespace UniRecast.Editor
{
    [CustomEditor(typeof(UniRcTileTool))]
    public class UniRcTileToolEditor : UniRcToolEditor
    {
        protected override void Layout()
        {
            UniRcGui.Text(this.GetType().Name);
            UniRcGui.Separator();
        }
    }
}