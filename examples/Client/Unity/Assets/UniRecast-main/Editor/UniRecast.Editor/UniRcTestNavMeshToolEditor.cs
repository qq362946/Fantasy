using System.Linq;
using DotRecast.Detour;
using DotRecast.Recast.Toolset.Builder;
using DotRecast.Recast.Toolset.Tools;
using UniRecast.Core;
using UniRecast.Toolsets;
using UnityEditor;

namespace UniRecast.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UniRcTestNavMeshTool))]
    public class UniRcTestNavMeshToolEditor : UniRcToolEditor
    {
        private SerializedProperty _selectedMode;
        private SerializedProperty _selectedStraightPathOption;
        private SerializedProperty _constrainByCircle;
        private SerializedProperty _includeFlags;
        private SerializedProperty _excludeFlags;

        private static readonly string[] ModeLabels = RcTestNavmeshToolMode.Values.Select(x => x.Label).ToArray();

        private void OnEnable()
        {
            _selectedMode = serializedObject.FindPropertySafe(nameof(_selectedMode));
            _selectedStraightPathOption = serializedObject.FindPropertySafe(nameof(_selectedStraightPathOption));
            _constrainByCircle = serializedObject.FindPropertySafe(nameof(_constrainByCircle));
            _includeFlags = serializedObject.FindPropertySafe(nameof(_includeFlags));
            _excludeFlags = serializedObject.FindPropertySafe(nameof(_excludeFlags));
        }

        protected override void Layout()
        {
            var surface = target as UniRcTestNavMeshTool;
            if (surface is null)
                return;

            UniRcGui.Text("Mode");
            UniRcGui.Separator();
            UniRcGui.RadioButton(RcTestNavmeshToolMode.PATHFIND_FOLLOW.Label, _selectedMode, RcTestNavmeshToolMode.PATHFIND_FOLLOW.Idx);
            UniRcGui.RadioButton(RcTestNavmeshToolMode.PATHFIND_STRAIGHT.Label, _selectedMode, RcTestNavmeshToolMode.PATHFIND_STRAIGHT.Idx);
            UniRcGui.RadioButton(RcTestNavmeshToolMode.PATHFIND_SLICED.Label, _selectedMode, RcTestNavmeshToolMode.PATHFIND_SLICED.Idx);
            UniRcGui.RadioButton(RcTestNavmeshToolMode.DISTANCE_TO_WALL.Label, _selectedMode, RcTestNavmeshToolMode.DISTANCE_TO_WALL.Idx);
            UniRcGui.RadioButton(RcTestNavmeshToolMode.RAYCAST.Label, _selectedMode, RcTestNavmeshToolMode.RAYCAST.Idx);
            UniRcGui.RadioButton(RcTestNavmeshToolMode.FIND_POLYS_IN_CIRCLE.Label, _selectedMode, RcTestNavmeshToolMode.FIND_POLYS_IN_CIRCLE.Idx);
            UniRcGui.RadioButton(RcTestNavmeshToolMode.FIND_POLYS_IN_SHAPE.Label, _selectedMode, RcTestNavmeshToolMode.FIND_POLYS_IN_SHAPE.Idx);
            UniRcGui.RadioButton(RcTestNavmeshToolMode.FIND_LOCAL_NEIGHBOURHOOD.Label, _selectedMode, RcTestNavmeshToolMode.FIND_LOCAL_NEIGHBOURHOOD.Idx);
            UniRcGui.RadioButton(RcTestNavmeshToolMode.RANDOM_POINTS_IN_CIRCLE.Label, _selectedMode, RcTestNavmeshToolMode.RANDOM_POINTS_IN_CIRCLE.Idx);
            UniRcGui.NewLine();

            // selecting mode
            var mode = RcTestNavmeshToolMode.Values[_selectedMode.intValue];
            UniRcGui.Text(mode.Label);
            UniRcGui.Separator();

            if (RcTestNavmeshToolMode.PATHFIND_FOLLOW == mode)
            {
            }

            if (RcTestNavmeshToolMode.PATHFIND_STRAIGHT == mode)
            {
                UniRcGui.Text("Vertices at crossings");
                UniRcGui.Separator();
                UniRcGui.RadioButton(DtStraightPathOption.None.Label, _selectedStraightPathOption, DtStraightPathOption.None.Value);
                UniRcGui.RadioButton(DtStraightPathOption.AreaCrossings.Label, _selectedStraightPathOption, DtStraightPathOption.AreaCrossings.Value);
                UniRcGui.RadioButton(DtStraightPathOption.AllCrossings.Label, _selectedStraightPathOption, DtStraightPathOption.AllCrossings.Value);
                //var straightPathOption = DtStraightPathOption.Values[_selectedStraightPathOption.intValue];
                UniRcGui.NewLine();
            }

            if (RcTestNavmeshToolMode.PATHFIND_SLICED == mode)
            {
            }

            if (RcTestNavmeshToolMode.DISTANCE_TO_WALL == mode)
            {
            }

            if (RcTestNavmeshToolMode.RAYCAST == mode)
            {
            }

            if (RcTestNavmeshToolMode.FIND_POLYS_IN_CIRCLE == mode)
            {
                UniRcGui.Checkbox("Constrained", _constrainByCircle);
                UniRcGui.NewLine();
            }

            if (RcTestNavmeshToolMode.FIND_POLYS_IN_SHAPE == mode)
            {
            }

            if (RcTestNavmeshToolMode.FIND_LOCAL_NEIGHBOURHOOD == mode)
            {
            }

            if (RcTestNavmeshToolMode.RANDOM_POINTS_IN_CIRCLE == mode)
            {
            }

            UniRcGui.Text("Common");
            UniRcGui.Separator();

            UniRcGui.Text("Include Flags");
            UniRcGui.Separator();
            UniRcGui.CheckboxFlags("Walk", _includeFlags, SampleAreaModifications.SAMPLE_POLYFLAGS_WALK);
            UniRcGui.CheckboxFlags("Swim", _includeFlags, SampleAreaModifications.SAMPLE_POLYFLAGS_SWIM);
            UniRcGui.CheckboxFlags("Door", _includeFlags, SampleAreaModifications.SAMPLE_POLYFLAGS_DOOR);
            UniRcGui.CheckboxFlags("Jump", _includeFlags, SampleAreaModifications.SAMPLE_POLYFLAGS_JUMP);
            UniRcGui.NewLine();
            //
            // m_filter.SetIncludeFlags(_option.includeFlags);
            //
            UniRcGui.Text("Exclude Flags");
            UniRcGui.Separator();
            UniRcGui.CheckboxFlags("Walk", _excludeFlags, SampleAreaModifications.SAMPLE_POLYFLAGS_WALK);
            UniRcGui.CheckboxFlags("Swim", _excludeFlags, SampleAreaModifications.SAMPLE_POLYFLAGS_SWIM);
            UniRcGui.CheckboxFlags("Door", _excludeFlags, SampleAreaModifications.SAMPLE_POLYFLAGS_DOOR);
            UniRcGui.CheckboxFlags("Jump", _excludeFlags, SampleAreaModifications.SAMPLE_POLYFLAGS_JUMP);
            UniRcGui.NewLine();
            //
            // m_filter.SetExcludeFlags(_option.excludeFlags);
            //
            // bool previousEnableRaycast = _option.enableRaycast;
            // ImGui.Checkbox("Raycast shortcuts", ref _option.enableRaycast);
            //

            serializedObject.ApplyModifiedProperties();
            // if (previousToolMode != _option.mode || _option.straightPathOptions != previousStraightPathOptions
            //                                      || previousIncludeFlags != _option.includeFlags || previousExcludeFlags != _option.excludeFlags
            //                                      || previousEnableRaycast != _option.enableRaycast || previousConstrainByCircle != _option.constrainByCircle)
            // {
            //     Recalc();
            // }
        }
    }
}