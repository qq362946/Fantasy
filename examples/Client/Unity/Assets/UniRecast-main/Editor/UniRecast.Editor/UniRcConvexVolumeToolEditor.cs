using DotRecast.Recast.Toolset.Builder;
using UnityEditor;
using UniRecast.Core;
using UniRecast.Toolsets;

namespace UniRecast.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UniRcConvexVolumeTool))]
    public class UniRcConvexVolumeToolEditor : UniRcToolEditor
    {
        private SerializedProperty _boxHeight;
        private SerializedProperty _boxDescent;
        private SerializedProperty _polyOffset;

        private SerializedProperty _areaTypeValue;

        private void OnEnable()
        {
            _boxHeight = serializedObject.FindPropertySafe(nameof(_boxHeight));
            _boxDescent = serializedObject.FindPropertySafe(nameof(_boxDescent));
            _polyOffset = serializedObject.FindPropertySafe(nameof(_polyOffset));

            _areaTypeValue = serializedObject.FindPropertySafe(nameof(_areaTypeValue));
        }

        protected override void Layout()
        {
            UniRcGui.SliderFloat("Shape Height", _boxHeight, 0.1f, 20f, 0.1f);
            UniRcGui.SliderFloat("Shape Descent", _boxDescent, 0.1f, 20f, 0.1f);
            UniRcGui.SliderFloat("Poly Offset", _polyOffset, 0.1f, 10f, 0.1f);
            UniRcGui.NewLine();

            UniRcGui.Text("Area Type");
            UniRcGui.Separator();
            // int prevAreaTypeValue = areaTypeValue;
            UniRcGui.RadioButton("Ground", _areaTypeValue, SampleAreaModifications.SAMPLE_AREAMOD_GROUND.Value);
            UniRcGui.RadioButton("Water", _areaTypeValue, SampleAreaModifications.SAMPLE_AREAMOD_WATER.Value);
            UniRcGui.RadioButton("Road", _areaTypeValue, SampleAreaModifications.SAMPLE_AREAMOD_ROAD.Value);
            UniRcGui.RadioButton("Door", _areaTypeValue, SampleAreaModifications.SAMPLE_AREAMOD_DOOR.Value);
            UniRcGui.RadioButton("Grass", _areaTypeValue, SampleAreaModifications.SAMPLE_AREAMOD_GRASS.Value);
            UniRcGui.RadioButton("Jump", _areaTypeValue, SampleAreaModifications.SAMPLE_AREAMOD_JUMP.Value);
            UniRcGui.NewLine();

            // if (prevAreaTypeValue != areaTypeValue)
            // {
            //     areaType = SampleAreaModifications.OfValue(areaTypeValue);
            // }

            if (UniRcGui.Button("Clear Shape"))
            {
                // hull.Clear();
                // pts.Clear();
            }

            if (UniRcGui.Button("Remove All"))
            {
                // hull.Clear();
                // pts.Clear();
                //
                // var geom = _sample.GetInputGeom();
                // if (geom != null)
                // {
                //     geom.ClearConvexVolumes();
                // }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}