using SRDebugger.Internal;
using SRDebugger.Profiler;
using UnityEditor;

namespace SRDebugger.Editor
{
    [CustomEditor(typeof (ProfilerCameraListener))]
    public class ProfilerCameraListenerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(SRDebugStrings.Current.ProfilerCameraListenerHelp, MessageType.Info, true);
        }
    }
}
