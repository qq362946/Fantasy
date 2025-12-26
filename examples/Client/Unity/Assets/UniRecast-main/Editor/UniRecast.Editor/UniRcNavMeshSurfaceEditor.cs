using UniRecast.Core;
using DotRecast.Detour;
using DotRecast.Recast.Toolset;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UniRecast.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UniRcNavMeshSurface))]
    public class UniRcNavMeshSurfaceEditor : UniRcToolEditor
    {
        // Rasterization
        private SerializedProperty _cellSize;
        private SerializedProperty _cellHeight;

        // Agent
        private SerializedProperty _agentHeight;
        private SerializedProperty _agentRadius;
        private SerializedProperty _agentMaxClimb;
        private SerializedProperty _agentMaxSlope;
        private SerializedProperty _agentMaxAcceleration;
        private SerializedProperty _agentMaxSpeed;

        // Region
        private SerializedProperty _minRegionSize;
        private SerializedProperty _mergedRegionSize;

        // Filtering
        private SerializedProperty _filterLowHangingObstacles;
        private SerializedProperty _filterLedgeSpans;
        private SerializedProperty _filterWalkableLowHeightSpans;

        // Polygonization
        private SerializedProperty _edgeMaxLen;
        private SerializedProperty _edgeMaxError;
        private SerializedProperty _vertsPerPoly;

        // Detail Mesh
        private SerializedProperty _detailSampleDist;
        private SerializedProperty _detailSampleMaxError;

        // Tiles
        private SerializedProperty _tileSize;

        private SerializedProperty _navMeshData;

        private static readonly Color s_HandleColor = new Color(127f, 214f, 244f, 100f) / 255;

        //private static readonly Color s_HandleColorSelected = new Color(127f, 63.0f, 244f, 210f) / 255;
        private static readonly Color s_HandleColorSelected = new Color(0.0f, 0.75f, 1f, 0.5f);
        private static readonly Color s_HandleColorDisabled = new Color(127f * 0.75f, 214f * 0.75f, 244f * 0.75f, 100f) / 255;

        private void OnDisable()
        {
        }

        private void OnEnable()
        {
            // Rasterization
            _cellSize = serializedObject.FindProperty(nameof(_cellSize));
            _cellHeight = serializedObject.FindProperty(nameof(_cellHeight));

            // Agent
            _agentHeight = serializedObject.FindProperty(nameof(_agentHeight));
            _agentRadius = serializedObject.FindProperty(nameof(_agentRadius));
            _agentMaxClimb = serializedObject.FindProperty(nameof(_agentMaxClimb));
            _agentMaxSlope = serializedObject.FindProperty(nameof(_agentMaxSlope));
            _agentMaxAcceleration = serializedObject.FindProperty(nameof(_agentMaxAcceleration));
            _agentMaxSpeed = serializedObject.FindProperty(nameof(_agentMaxSpeed));

            // Region
            _minRegionSize = serializedObject.FindProperty(nameof(_minRegionSize));
            _mergedRegionSize = serializedObject.FindProperty(nameof(_mergedRegionSize));

            // Filtering
            _filterLowHangingObstacles = serializedObject.FindProperty(nameof(_filterLowHangingObstacles));
            _filterLedgeSpans = serializedObject.FindProperty(nameof(_filterLedgeSpans));
            _filterWalkableLowHeightSpans = serializedObject.FindProperty(nameof(_filterWalkableLowHeightSpans));

            // Polygonization
            _edgeMaxLen = serializedObject.FindProperty(nameof(_edgeMaxLen));
            _edgeMaxError = serializedObject.FindProperty(nameof(_edgeMaxError));
            _vertsPerPoly = serializedObject.FindProperty(nameof(_vertsPerPoly));

            // Detail Mesh
            _detailSampleDist = serializedObject.FindProperty(nameof(_detailSampleDist));
            _detailSampleMaxError = serializedObject.FindProperty(nameof(_detailSampleMaxError));

            // Tiles
            _tileSize = serializedObject.FindProperty(nameof(_tileSize));

            // Data
            _navMeshData = serializedObject.FindProperty(nameof(_navMeshData));
        }

        private void Clear()
        {
            var surface = target as UniRcNavMeshSurface;
            if (null == surface)
            {
                Debug.LogError($"not found UniRc NavMesh Surface");
                return;
            }

            surface.Clear();

            // 기본값 초기화
            var bs = new RcNavMeshBuildSettings();

            // Rasterization
            _cellSize.floatValue = bs.cellSize;
            _cellHeight.floatValue = bs.cellHeight;

            // Agent
            _agentHeight.floatValue = bs.agentHeight;
            _agentRadius.floatValue = bs.agentRadius;
            _agentMaxClimb.floatValue = bs.agentMaxClimb;
            _agentMaxSlope.floatValue = bs.agentMaxSlope;
            _agentMaxAcceleration.floatValue = bs.agentMaxAcceleration;
            _agentMaxSpeed.floatValue = bs.agentMaxSpeed;

            // Region
            _minRegionSize.intValue = bs.minRegionSize;
            _mergedRegionSize.intValue = bs.mergedRegionSize;

            // Filtering
            _filterLowHangingObstacles.boolValue = bs.filterLowHangingObstacles;
            _filterLedgeSpans.boolValue = bs.filterLedgeSpans;
            _filterWalkableLowHeightSpans.boolValue = bs.filterWalkableLowHeightSpans;

            // Polygonization
            _edgeMaxLen.floatValue = bs.edgeMaxLen;
            _edgeMaxError.floatValue = bs.edgeMaxError;
            _vertsPerPoly.intValue = bs.vertsPerPoly;

            // Detail Mesh
            _detailSampleDist.floatValue = bs.detailSampleDist;
            _detailSampleMaxError.floatValue = bs.detailSampleMaxError;

            // Tiles
            _tileSize.intValue = bs.tileSize;
        }

        private void Bake()
        {
            var surface = target as UniRcNavMeshSurface;
            if (null == surface)
            {
                Debug.LogError($"not found UniRc NavMesh Surface");
                return;
            }

            surface.Bake();
        }

        protected override void Layout()
        {
            var surface = target as UniRcNavMeshSurface;
            if (surface is null)
                return;

            // Draw image
            const float diagramHeight = 80.0f;
            Rect agentDiagramRect = EditorGUILayout.GetControlRect(false, diagramHeight);
            UniRcGui.DrawAgentDiagram(agentDiagramRect, _agentRadius.floatValue, _agentHeight.floatValue, _agentMaxClimb.floatValue, _agentMaxSlope.floatValue);

            UniRcGui.Text("Rasterization");
            UniRcGui.Separator();
            UniRcGui.SliderFloat("Cell Size", _cellSize, 0.01f, 1f, 0.01f);
            UniRcGui.SliderFloat("Cell Height", _cellHeight, 0.01f, 1f, 0.01f);
            //UniRcEditorHelpers.Text($"Voxels {voxels[0]} x {voxels[1]}");
            UniRcGui.NewLine();

            UniRcGui.Text("Agent");
            UniRcGui.Separator();
            UniRcGui.SliderFloat("Height", _agentHeight, 0.1f, 5f, 0.1f);
            UniRcGui.SliderFloat("Radius", _agentRadius, 0.1f, 5f, 0.1f);
            UniRcGui.SliderFloat("Max Climb", _agentMaxClimb, 0.1f, 5f, 0.1f);
            UniRcGui.SliderFloat("Max Slope", _agentMaxSlope, 1f, 90f, 1);
            UniRcGui.SliderFloat("Max Acceleration", _agentMaxAcceleration, 8f, 999f, 0.1f);
            UniRcGui.SliderFloat("Max Speed", _agentMaxSpeed, 1f, 10f, 0.1f);
            UniRcGui.NewLine();

            UniRcGui.Text("Region");
            UniRcGui.Separator();
            UniRcGui.SliderInt("Min Region Size", _minRegionSize, 1, 150);
            UniRcGui.SliderInt("Merged Region Size", _mergedRegionSize, 1, 150);
            UniRcGui.NewLine();

            UniRcGui.Text("Filtering");
            UniRcGui.Separator();
            UniRcGui.Checkbox("Low Hanging Obstacles", _filterLowHangingObstacles);
            UniRcGui.Checkbox("Ledge Spans", _filterLedgeSpans);
            UniRcGui.Checkbox("Walkable Low Height Spans", _filterWalkableLowHeightSpans);
            UniRcGui.NewLine();

            UniRcGui.Text("Polygonization");
            UniRcGui.Separator();
            UniRcGui.SliderFloat("Max Edge Length", _edgeMaxLen, 0f, 50f, 0.1f);
            UniRcGui.SliderFloat("Max Edge Error", _edgeMaxError, 0.1f, 3f, 0.1f);
            UniRcGui.SliderInt("Vert Per Poly", _vertsPerPoly, 3, 12);
            UniRcGui.NewLine();

            UniRcGui.Text("Detail Mesh");
            UniRcGui.Separator();
            UniRcGui.SliderFloat("Sample Distance", _detailSampleDist, 0f, 16f, 0.1f);
            UniRcGui.SliderFloat("Max Sample Error", _detailSampleMaxError, 0f, 16f, 0.1f);
            UniRcGui.NewLine();

            UniRcGui.Text("Tiling");
            UniRcGui.Separator();
            UniRcGui.SliderInt("Tile Size", _tileSize, 16, 1024, 16);
            UniRcGui.NewLine();

            // UniRcEditorHelpers.Text($"Tiles {tiles[0]} x {tiles[1]}");
            // UniRcEditorHelpers.Text($"Max Tiles {maxTiles}");
            // UniRcEditorHelpers.Text($"Max Polys {maxPolys}");
            //}

            var nmdRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginProperty(nmdRect, GUIContent.none, _navMeshData);
            var rectLabel = EditorGUI.PrefixLabel(nmdRect, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(_navMeshData.displayName));
            EditorGUI.EndProperty();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.BeginProperty(nmdRect, GUIContent.none, _navMeshData);
                EditorGUI.ObjectField(rectLabel, _navMeshData, GUIContent.none);
                EditorGUI.EndProperty();
            }

            UniRcGui.NewLine();

            serializedObject.ApplyModifiedProperties();

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (UniRcGui.Button("Clear"))
                {
                    Clear();
                    serializedObject.ApplyModifiedProperties();
                }

                if (UniRcGui.Button("Bake"))
                {
                    Bake();
                }
            }
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.Active | GizmoType.Pickable)]
        static void RenderGizmoSelected(UniRcNavMeshSurface navSurface, GizmoType gizmoType)
        {
            var zTestOld = Handles.zTest;
            Handles.zTest = CompareFunction.LessEqual;

            //navSurface.navMeshDataInstance.FlagAsInSelectionHierarchy();
            DrawBoundingBoxGizmoAndIcon(navSurface, true);
            Handles.zTest = zTestOld;
        }

        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Pickable)]
        static void RenderGizmoNotSelected(UniRcNavMeshSurface navSurface, GizmoType gizmoType)
        {
            var zTestOld = Handles.zTest;
            Handles.zTest = CompareFunction.LessEqual;

            DrawBoundingBoxGizmoAndIcon(navSurface, false);

            Handles.zTest = zTestOld;
        }

        static void DrawPoly(DtMeshTile tile, int index, Vector3 offset)
        {
            //Handles.color = new Color(64, 64, 64, 64);
            Handles.color = s_HandleColorSelected;
            var polygonVertices = new Vector3[3];

            DtPoly p = tile.data.polys[index];
            if (tile.data.detailMeshes != null)
            {
                DtPolyDetail pd = tile.data.detailMeshes[index];
                for (int j = 0; j < pd.triCount; ++j)
                {
                    int t = (pd.triBase + j) * 4;
                    for (int k = 0; k < 3; ++k)
                    {
                        int v = tile.data.detailTris[t + k];
                        if (v < p.vertCount)
                        {
                            polygonVertices[k] = new Vector3(
                                -tile.data.verts[p.verts[v] * 3],
                                tile.data.verts[p.verts[v] * 3 + 1],
                                tile.data.verts[p.verts[v] * 3 + 2]);
                        }
                        else
                        {
                            polygonVertices[k] = new Vector3(
                                -tile.data.detailVerts[(pd.vertBase + v - p.vertCount) * 3],
                                tile.data.detailVerts[(pd.vertBase + v - p.vertCount) * 3 + 1],
                                tile.data.detailVerts[(pd.vertBase + v - p.vertCount) * 3 + 2]);
                        }
                    }

                    Handles.DrawAAConvexPolygon(polygonVertices);
                }
            }
            else
            {
                for (int j = 1; j < p.vertCount - 1; ++j)
                {
                    var v0 = new Vector3(
                        -tile.data.verts[p.verts[0] * 3],
                        tile.data.verts[p.verts[0] * 3 + 1],
                        tile.data.verts[p.verts[0] * 3 + 2]
                    );
                    polygonVertices[0] = v0;

                    for (int k = 0; k < 2; ++k)
                    {
                        var vn = new Vector3(
                            -tile.data.verts[p.verts[j + k] * 3],
                            tile.data.verts[p.verts[j + k] * 3 + 1],
                            tile.data.verts[p.verts[j + k] * 3 + 2]
                        );
                        polygonVertices[k + 1] = vn;
                    }

                    Handles.DrawAAConvexPolygon(polygonVertices);
                }
            }
        }

        static void DrawBoundingBoxGizmoAndIcon(UniRcNavMeshSurface navSurface, bool selected)
        {
            // var color = selected ? s_HandleColorSelected : s_HandleColor;
            // if (!navSurface.enabled)
            //     color = s_HandleColorDisabled;

            var oldColor = Gizmos.color;
            var oldMatrix = Gizmos.matrix;

            // Use the unscaled matrix for the NavMeshSurface
            var localToWorld = Matrix4x4.TRS(navSurface.transform.position, navSurface.transform.rotation, Vector3.one);
            Gizmos.matrix = localToWorld;


            if (!navSurface.HasNavMeshData())
            {
                return;
            }

            var handleOldColor = Handles.color;
            // Handles의 Z 테스트 방식 설정

            // 폴리 그리기
            int count = navSurface.GetNavMeshData().GetMaxTiles();
            for (int i = 0; i < count; ++i)
            {
                var tile = navSurface.GetNavMeshData().GetTile(i);
                if (null == tile.data)
                    continue;

                for (int ii = 0; ii < tile.data.header.polyCount; ++ii)
                {
                    DtPoly p = tile.data.polys[ii];
                    if (p.GetPolyType() == DtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                    {
                        continue;
                    }

                    DrawPoly(tile, ii, navSurface.transform.position);
                }
            }


            // 폴리곤 점들의 배열
            //Vector3[] polygonVertices = GetPolygonVertices(navSurface.transform.position);

            // 폴리곤 그리기
            //Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 폴리곤 색상
            //Handles.DrawAAConvexPolygon(polygonVertices);

            Handles.color = Color.black;
            // 검은색 큐브 그리기
            for (int i = 0; i < count; ++i)
            {
                var tile = navSurface.GetNavMeshData().GetTile(i);
                if (null == tile.data)
                    continue;

                for (int ii = 0; ii < tile.data.header.vertCount; ++ii)
                {
                    int v = ii * 3;
                    var pt = new Vector3(-tile.data.verts[v + 0], tile.data.verts[v + 1], tile.data.verts[v + 2]);

                    Handles.DotHandleCap(
                        0,
                        pt + navSurface.transform.position,
                        Quaternion.identity,
                        HandleUtility.GetHandleSize(navSurface.transform.position + pt) * 0.015f,
                        EventType.Repaint
                    );
                }
            }

            Handles.color = handleOldColor;

            // if (navSurface.collectObjects == CollectObjects.Volume)
            // {
            //     Gizmos.color = color;
            //     Gizmos.DrawWireCube(navSurface.center, navSurface.size);
            //
            //     if (selected && navSurface.enabled)
            //     {
            //         var colorTrans = new Color(color.r * 0.75f, color.g * 0.75f, color.b * 0.75f, color.a * 0.15f);
            //         Gizmos.color = colorTrans;
            //         Gizmos.DrawCube(navSurface.center, navSurface.size);
            //     }
            // }
            // else
            // {
            //     if (navSurface.NavMesh != null)
            //     {
            //         var bounds = navSurface.NavMesh.sourceBounds;
            //         Gizmos.color = Color.grey;
            //         Gizmos.DrawWireCube(bounds.center, bounds.size);
            //     }
            // }

            Gizmos.matrix = oldMatrix;
            Gizmos.color = oldColor;

            //var startSize = HandleUtility.GetHandleSize(startPt);
            //Handles.CubeHandleCap(0, navSurface.transform.position + Vector3.up * 30, navSurface.transform.rotation, 10, EventType.Ignore);
            // Handles.DrawWireCube(navSurface.transform.position, Vector3.one * 100);
            // Handles.DrawDottedLine(navSurface.transform.position, navSurface.transform.position + Vector3.forward * 100, 1.0f);
            // Handles.Label(navSurface.transform.position, "Custom Label");

            Gizmos.DrawIcon(navSurface.transform.position, "NavMeshSurface Icon", true);
        }


        [MenuItem("GameObject/UniRecast/UniRc NavMesh Surface", false, 2000)]
        public static void CreateNavMeshSurface(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = UniRcGuiUtility.CreateAndSelectGameObject("UniRc NavMesh Surface", parent);
            go.AddComponent<UniRcNavMeshSurface>();
            var view = SceneView.lastActiveSceneView;
            if (view != null)
                view.MoveToView(go.transform);
        }
    }
}