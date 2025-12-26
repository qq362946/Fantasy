using DotRecast.Recast.Toolset.Tools;
using UnityEditor;
using UniRecast.Core;
using UniRecast.Toolsets;

namespace UniRecast.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UniRcCrowdTool))]
    public class UniRcCrowdToolEditor : UniRcToolEditor
    {
        private SerializedProperty _mode;
        private SerializedProperty _optimizeVis;
        private SerializedProperty _optimizeTopo;
        private SerializedProperty _anticipateTurns;
        private SerializedProperty _obstacleAvoidance;
        private SerializedProperty _separation;
        private SerializedProperty _obstacleAvoidanceType;
        private SerializedProperty _separationWeight;
        private SerializedProperty _showCorners;
        private SerializedProperty _showCollisionSegments;
        private SerializedProperty _showPath;
        private SerializedProperty _showVO;
        private SerializedProperty _showOpt;
        private SerializedProperty _showNeis;

        // debug
        private SerializedProperty _showGrid;
        private SerializedProperty _showNodes;

        private void OnEnable()
        {
            _mode = serializedObject.FindPropertySafe(nameof(_mode));

            // ...
            _optimizeVis = serializedObject.FindPropertySafe(nameof(_optimizeVis));
            _optimizeTopo = serializedObject.FindPropertySafe(nameof(_optimizeTopo));
            _anticipateTurns = serializedObject.FindPropertySafe(nameof(_anticipateTurns));
            _obstacleAvoidance = serializedObject.FindPropertySafe(nameof(_obstacleAvoidance));
            _separation = serializedObject.FindPropertySafe(nameof(_separation));
            _obstacleAvoidanceType = serializedObject.FindPropertySafe(nameof(_obstacleAvoidanceType));
            _separationWeight = serializedObject.FindPropertySafe(nameof(_separationWeight));

            // ...
            _showCorners = serializedObject.FindPropertySafe(nameof(_showCorners));
            _showCollisionSegments = serializedObject.FindPropertySafe(nameof(_showCollisionSegments));
            _showPath = serializedObject.FindPropertySafe(nameof(_showPath));
            _showVO = serializedObject.FindPropertySafe(nameof(_showVO));
            _showOpt = serializedObject.FindPropertySafe(nameof(_showOpt));
            _showNeis = serializedObject.FindPropertySafe(nameof(_showNeis));

            // debug
            _showGrid = serializedObject.FindPropertySafe(nameof(_showGrid));
            _showNodes = serializedObject.FindPropertySafe(nameof(_showNodes));
        }

        protected override void Layout()
        {
            UniRcGui.Text($"Crowd Tool Mode");
            UniRcGui.Separator();
            UniRcGui.RadioButton(RcCrowdToolMode.CREATE.Label, _mode, RcCrowdToolMode.CREATE.Idx);
            UniRcGui.RadioButton(RcCrowdToolMode.MOVE_TARGET.Label, _mode, RcCrowdToolMode.MOVE_TARGET.Idx);
            UniRcGui.RadioButton(RcCrowdToolMode.SELECT.Label, _mode, RcCrowdToolMode.SELECT.Idx);
            UniRcGui.RadioButton(RcCrowdToolMode.TOGGLE_POLYS.Label, _mode, RcCrowdToolMode.TOGGLE_POLYS.Idx);
            UniRcGui.NewLine();

            RcCrowdToolMode mode = RcCrowdToolMode.Values[_mode.intValue];

            UniRcGui.Text("Options");
            UniRcGui.Separator();
            UniRcGui.Checkbox("Optimize Visibility", _optimizeVis);
            UniRcGui.Checkbox("Optimize Topology", _optimizeTopo);
            UniRcGui.Checkbox("Anticipate Turns", _anticipateTurns);
            UniRcGui.Checkbox("Obstacle Avoidance", _obstacleAvoidance);
            UniRcGui.SliderInt("Avoidance Quality", _obstacleAvoidanceType, 0, 3);
            UniRcGui.Checkbox("Separation", _separation);
            UniRcGui.SliderFloat("Separation Weight", _separationWeight, 0f, 20f, 0.2f);
            UniRcGui.NewLine();

            // if (m_optimizeVis != toolParams.m_optimizeVis || m_optimizeTopo != toolParams.m_optimizeTopo
            //                                               || m_anticipateTurns != toolParams.m_anticipateTurns || m_obstacleAvoidance != toolParams.m_obstacleAvoidance
            //                                               || m_separation != toolParams.m_separation
            //                                               || m_obstacleAvoidanceType != toolParams.m_obstacleAvoidanceType
            //                                               || m_separationWeight != toolParams.m_separationWeight)
            // {
            //     UpdateAgentParams();
            // }


            UniRcGui.Text("Selected Debug Draw");
            UniRcGui.Separator();
            UniRcGui.Checkbox("Show Corners", _showCorners);
            UniRcGui.Checkbox("Show Collision Segs", _showCollisionSegments);
            UniRcGui.Checkbox("Show Path", _showPath);
            UniRcGui.Checkbox("Show VO", _showVO);
            UniRcGui.Checkbox("Show Path Optimization", _showOpt);
            UniRcGui.Checkbox("Show Neighbours", _showNeis);
            UniRcGui.NewLine();

            UniRcGui.Text("Debug Draw");
            UniRcGui.Separator();
            UniRcGui.Checkbox("Show Prox Grid", _showGrid);
            UniRcGui.Checkbox("Show Nodes", _showNodes);
            //UniRcGui.Text($"Update Time: {crowdUpdateTime} ms");

            serializedObject.ApplyModifiedProperties();
        }
    }
}