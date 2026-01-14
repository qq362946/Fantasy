using System.Collections.Generic;
using System.Linq;
using DotRecast.Detour;
using DotRecast.Recast.Toolset;
using UnityEngine;

namespace UniRecast.Core
{
    public class UniRcNavMeshSurface : MonoBehaviour
    {
        [SerializeField]
        private float _cellSize = 0.3f;

        [SerializeField]
        private float _cellHeight = 0.2f;

        // Agent
        [SerializeField]
        private float _agentHeight = 2.0f;

        [SerializeField]
        private float _agentRadius = 0.6f;

        [SerializeField]
        private float _agentMaxClimb = 0.9f;

        [SerializeField]
        private float _agentMaxSlope = 45f;

        [SerializeField]
        private float _agentMaxAcceleration = 8.0f;

        [SerializeField]
        private float _agentMaxSpeed = 3.5f;

        // Region
        [SerializeField]
        private int _minRegionSize = 8;

        [SerializeField]
        private int _mergedRegionSize = 20;

        // Filtering
        [SerializeField]
        private bool _filterLowHangingObstacles = true;

        [SerializeField]
        private bool _filterLedgeSpans = true;

        [SerializeField]
        private bool _filterWalkableLowHeightSpans = true;

        // Polygonization
        [SerializeField]
        private float _edgeMaxLen = 12f;

        [SerializeField]
        private float _edgeMaxError = 1.3f;

        [SerializeField]
        private int _vertsPerPoly = 6;

        // Detail Mesh
        [SerializeField]
        private float _detailSampleDist = 6f;

        [SerializeField]
        private float _detailSampleMaxError = 1f;

        // Tiles
        [SerializeField]
        private int _tileSize = 32;

        [SerializeField]
        private UniRcNavMeshData _navMeshData = new();

        private RcNavMeshBuildSettings ToBuildSettings()
        {
            var bs = new RcNavMeshBuildSettings();

            // Rasterization
            bs.cellSize = _cellSize;
            bs.cellHeight = _cellHeight;

            // Agent
            bs.agentHeight = _agentHeight;
            bs.agentHeight = _agentHeight;
            bs.agentRadius = _agentRadius;
            bs.agentMaxClimb = _agentMaxClimb;
            bs.agentMaxSlope = _agentMaxSlope;
            bs.agentMaxAcceleration = _agentMaxAcceleration;
            bs.agentMaxSpeed = _agentMaxSpeed;

            // Region
            bs.minRegionSize = _minRegionSize;
            bs.mergedRegionSize = _mergedRegionSize;

            // Filtering
            bs.filterLowHangingObstacles = _filterLowHangingObstacles;
            bs.filterLedgeSpans = _filterLedgeSpans;
            bs.filterWalkableLowHeightSpans = _filterWalkableLowHeightSpans;

            // Polygonization
            bs.edgeMaxLen = _edgeMaxLen;
            bs.edgeMaxError = _edgeMaxError;
            bs.vertsPerPoly = _vertsPerPoly;

            // Detail Mesh
            bs.detailSampleDist = _detailSampleDist;
            bs.detailSampleMaxError = _detailSampleMaxError;

            // Tiles
            bs.tiled = true;
            bs.tileSize = _tileSize;

            return bs;
        }

        public void Bake()
        {
            var setting = ToBuildSettings();
            BakeFrom(setting);
        }

        public void BakeFrom(RcNavMeshBuildSettings setting)
        {
            // 현재 씬에 속해 있다면 해당 씬을 가져옵니다.
            var currentScene = gameObject.scene;

            // 현재 씬의 모든 GameObject 배열을 가져옵니다.
            GameObject[] allGameObjects = currentScene.GetRootGameObjects();

            var targets = GetNavMeshSurfaceTargets(allGameObjects);
            if (0 >= targets.Count)
            {
                Debug.LogError($"not found navmesh targets");
                return;
            }

            var combinedTarget = targets.ToCombinedNavMeshSurfaceTarget(currentScene.name);
            var mesh = combinedTarget.ToMesh();
            mesh.SaveFile();

            var navMesh = mesh.Build(setting);
            _navMeshData.NavMesh = navMesh;

            navMesh.SaveNavMeshFile(combinedTarget.GetName());

            Debug.Log($"{null != _navMeshData}");
        }

        public List<UniRcNavMeshSurfaceTarget> GetNavMeshSurfaceTargets(IList<GameObject> gameObjects)
        {
            // force terrain
            var terrainTargets = gameObjects
                .SelectMany(x => x.GetComponentsInChildren<Terrain>())
                .Where(x => x.gameObject.activeSelf && x.isActiveAndEnabled)
                .Select(x => x.ToUniRcSurfaceSource());

            // all tag objects
            var navmeshTagObjects = gameObjects
                .SelectMany(x => x.ToHierarchyList())
                .Where(x => x.gameObject.activeSelf)
                .Where(x => x.CompareTag(UniRcConst.Tag))
                .ToList();

            var meshFilterTargets = navmeshTagObjects
                .SelectMany(x => x.GetComponentsInChildren<MeshFilter>())
                .Distinct()
                .Select(x => x.ToUniRcSurfaceSource());

            var targets = new List<UniRcNavMeshSurfaceTarget>();
            targets.AddRange(terrainTargets);
            targets.AddRange(meshFilterTargets);

            return targets;
        }

        public void Clear()
        {
            _navMeshData.NavMesh = null;
        }

        public bool HasNavMeshData()
        {
            return null != _navMeshData.NavMesh;
        }

        public DtNavMesh GetNavMeshData()
        {
            return _navMeshData.NavMesh;
        }
    }
}