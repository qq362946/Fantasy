using System;
using System.Linq;
using DotRecast.Core;
using DotRecast.Core.Collections;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Detour.TileCache;
using DotRecast.Detour.TileCache.Io.Compress;
using DotRecast.Recast.Geom;
using DotRecast.Recast.Toolset.Builder;
using DotRecast.Recast.Toolset.Geom;

namespace DotRecast.Recast.Toolset.Tools
{
    public class RcObstacleTool : IRcToolable
    {
        private readonly IDtTileCacheCompressorFactory _comp;
        private readonly DemoDtTileCacheMeshProcess _proc;
        private DtTileCache _tc;

        public RcObstacleTool(IDtTileCacheCompressorFactory comp)
        {
            _comp = comp;
            _proc = new DemoDtTileCacheMeshProcess();
        }

        public string GetName()
        {
            return "Temp Obstacles";
        }

        public NavMeshBuildResult Build(IInputGeomProvider geom, RcNavMeshBuildSettings setting, RcByteOrder order, bool cCompatibility)
        {
            if (null == geom || null == geom.GetMesh())
            {
                //m_ctx->log(RC_LOG_ERROR, "buildTiledNavigation: No vertices and triangles.");
                return new NavMeshBuildResult();
            }

            _proc.Init(geom);

            // Init cache
            var bmin = geom.GetMeshBoundsMin();
            var bmax = geom.GetMeshBoundsMax();
            RcRecast.CalcGridSize(bmin, bmax, setting.cellSize, out var gw, out var gh);
            int ts = setting.tileSize;
            int tw = (gw + ts - 1) / ts;
            int th = (gh + ts - 1) / ts;

            // Generation params.
            var walkableRadius = (int)MathF.Ceiling(setting.agentRadius / setting.cellSize); // Reserve enough padding.
            RcConfig cfg = new RcConfig(
                true, setting.tileSize, setting.tileSize,
                walkableRadius + 3,
                RcPartitionType.OfValue(setting.partitioning),
                setting.cellSize, setting.cellHeight,
                setting.agentMaxSlope, setting.agentHeight, setting.agentRadius, setting.agentMaxClimb,
                (int)RcMath.Sqr(setting.minRegionSize), (int)RcMath.Sqr(setting.mergedRegionSize), // Note: area = size*size
                (int)(setting.edgeMaxLen / setting.cellSize), setting.edgeMaxError,
                setting.vertsPerPoly,
                setting.detailSampleDist, setting.detailSampleMaxError,
                true, true, true,
                SampleAreaModifications.SAMPLE_AREAMOD_WALKABLE, true);

            var builder = new DtTileCacheLayerBuilder(DtTileCacheCompressorFactory.Shared);
            var storageParams = new DtTileCacheStorageParams(order, cCompatibility);
            var results = builder.Build(geom, cfg, storageParams, 8, tw, th);
            var layers = results
                .SelectMany(x => x.layers)
                .ToList();

            _tc = CreateTileCache(geom, setting, tw, th, order, cCompatibility);

            for (int i = 0; i < layers.Count; ++i)
            {
                var layer = layers[i];
                var refs = _tc.AddTile(layer, 0);
                _tc.BuildNavMeshTile(refs);
            }

            return new NavMeshBuildResult(cfg, RcImmutableArray<RcBuilderResult>.Empty, _tc.GetNavMesh());
        }

        public void ClearAllTempObstacles()
        {
            if (null == _tc)
                return;

            for (int i = 0; i < _tc.GetObstacleCount(); ++i)
            {
                DtTileCacheObstacle ob = _tc.GetObstacle(i);
                if (ob.state == DtObstacleState.DT_OBSTACLE_EMPTY)
                    continue;

                _tc.RemoveObstacle(_tc.GetObstacleRef(ob));
            }
        }

        public void RemoveTempObstacle(RcVec3f sp, RcVec3f sq)
        {
            if (null == _tc)
                return;

            long refs = HitTestObstacle(sp, sq);
            _tc.RemoveObstacle(refs);
        }

        public long AddTempObstacle(RcVec3f p)
        {
            if (null == _tc)
                return 0;

            p.Y -= 0.5f;
            return _tc.AddObstacle(p, 1.0f, 2.0f);
        }

        public DtTileCache GetTileCache()
        {
            return _tc;
        }

        public DtTileCache CreateTileCache(IInputGeomProvider geom, RcNavMeshBuildSettings setting, int tw, int th, RcByteOrder order, bool cCompatibility)
        {
            DtTileCacheParams option = new DtTileCacheParams();
            option.ch = setting.cellHeight;
            option.cs = setting.cellSize;
            option.orig = geom.GetMeshBoundsMin();
            option.height = setting.tileSize;
            option.width = setting.tileSize;
            option.walkableHeight = setting.agentHeight;
            option.walkableRadius = setting.agentRadius;
            option.walkableClimb = setting.agentMaxClimb;
            option.maxSimplificationError = setting.edgeMaxError;
            option.maxTiles = tw * th * DtTileCacheLayer.EXPECTED_LAYERS_PER_TILE; // for test EXPECTED_LAYERS_PER_TILE;
            option.maxObstacles = 128;

            DtNavMeshParams navMeshParams = new DtNavMeshParams();
            navMeshParams.orig = geom.GetMeshBoundsMin();
            navMeshParams.tileWidth = setting.tileSize * setting.cellSize;
            navMeshParams.tileHeight = setting.tileSize * setting.cellSize;
            navMeshParams.maxTiles = 256; // ..
            navMeshParams.maxPolys = 16384;

            var navMesh = new DtNavMesh();
            navMesh.Init(navMeshParams, 6);
            var comp = _comp.Create(cCompatibility ? 0 : 1);
            var storageParams = new DtTileCacheStorageParams(order, cCompatibility);
            DtTileCache tc = new DtTileCache(option, storageParams, navMesh, comp, _proc);
            return tc;
        }

        public long HitTestObstacle(RcVec3f sp, RcVec3f sq)
        {
            float tmin = float.MaxValue;
            DtTileCacheObstacle obmin = null;

            for (int i = 0; i < _tc.GetObstacleCount(); ++i)
            {
                DtTileCacheObstacle ob = _tc.GetObstacle(i);
                if (ob.state == DtObstacleState.DT_OBSTACLE_EMPTY)
                    continue;

                RcVec3f bmin = RcVec3f.Zero;
                RcVec3f bmax = RcVec3f.Zero;
                _tc.GetObstacleBounds(ob, ref bmin, ref bmax);

                if (RcIntersections.IsectSegAABB(sp, sq, bmin, bmax, out var t0, out var t1))
                {
                    if (t0 < tmin)
                    {
                        tmin = t0;
                        obmin = ob;
                    }
                }
            }

            return _tc.GetObstacleRef(obmin);
        }
    }
}