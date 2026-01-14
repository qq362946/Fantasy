using System.Linq;
using DotRecast.Core;
using DotRecast.Core.Collections;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Recast.Geom;
using DotRecast.Recast.Toolset.Builder;

namespace DotRecast.Recast.Toolset.Tools
{
    public class RcTileTool : IRcToolable
    {
        public string GetName()
        {
            return "Tiles";
        }

        public void RemoveAllTiles(IInputGeomProvider geom, RcNavMeshBuildSettings settings, DtNavMesh navMesh)
        {
            if (null == settings || null == geom || navMesh == null)
                return;

            var bmin = geom.GetMeshBoundsMin();
            var bmax = geom.GetMeshBoundsMax();
            int gw = 0, gh = 0;
            RcRecast.CalcGridSize(bmin, bmax, settings.cellSize, out gw, out gh);

            int ts = settings.tileSize;
            int tw = (gw + ts - 1) / ts;
            int th = (gh + ts - 1) / ts;

            for (int y = 0; y < th; ++y)
            {
                for (int x = 0; x < tw; ++x)
                {
                    var tileRef = navMesh.GetTileRefAt(x, y, 0);
                    navMesh.RemoveTile(tileRef);
                }
            }
        }

        public void BuildAllTiles(IInputGeomProvider geom, RcNavMeshBuildSettings settings, DtNavMesh navMesh)
        {
            if (null == settings || null == geom || navMesh == null)
                return;

            var bmin = geom.GetMeshBoundsMin();
            var bmax = geom.GetMeshBoundsMax();
            int gw = 0, gh = 0;
            RcRecast.CalcGridSize(bmin, bmax, settings.cellSize, out gw, out gh);

            int ts = settings.tileSize;
            int tw = (gw + ts - 1) / ts;
            int th = (gh + ts - 1) / ts;

            for (int y = 0; y < th; ++y)
            {
                for (int x = 0; x < tw; ++x)
                {
                    BuildTile(geom, settings, navMesh, x, y, out var tileBuildTicks, out var tileTriCount, out var tileMemUsage);
                }
            }
        }

        public bool BuildTile(IInputGeomProvider geom, RcNavMeshBuildSettings settings, DtNavMesh navMesh, int tx, int ty, out long tileBuildTicks, out int tileTriCount, out int tileMemUsage)
        {
            tileBuildTicks = 0;
            tileTriCount = 0; // ...
            tileMemUsage = 0; // ...

            var availableTile = navMesh.IsAvailableTileCount();
            if (!availableTile)
            {
                return false;
            }

            RcVec3f bmin = geom.GetMeshBoundsMin();
            RcVec3f bmax = geom.GetMeshBoundsMax();

            RcConfig cfg = new RcConfig(
                true, settings.tileSize, settings.tileSize,
                RcConfig.CalcBorder(settings.agentRadius, settings.cellSize),
                RcPartitionType.OfValue(settings.partitioning),
                settings.cellSize, settings.cellHeight,
                settings.agentMaxSlope, settings.agentHeight, settings.agentRadius, settings.agentMaxClimb,
                settings.minRegionSize * settings.minRegionSize * settings.cellSize * settings.cellSize,
                settings.mergedRegionSize * settings.mergedRegionSize * settings.cellSize * settings.cellSize,
                settings.edgeMaxLen, settings.edgeMaxError,
                settings.vertsPerPoly,
                settings.detailSampleDist, settings.detailSampleMaxError,
                settings.filterLowHangingObstacles, settings.filterLedgeSpans, settings.filterWalkableLowHeightSpans,
                SampleAreaModifications.SAMPLE_AREAMOD_WALKABLE, true
            );

            var beginTick = RcFrequency.Ticks;
            var rb = new RcBuilder();
            var result = rb.BuildTile(geom, cfg, bmin, bmax, tx, ty, new RcAtomicInteger(0), 1, settings.keepInterResults);

            var tb = new TileNavMeshBuilder();
            var meshData = tb.BuildMeshData(geom, settings.cellSize, settings.cellHeight, settings.agentHeight, settings.agentRadius, settings.agentMaxClimb, RcImmutableArray.Create(result)
            ).FirstOrDefault();

            if (null == meshData)
                return false;

            navMesh.UpdateTile(meshData, 0);

            tileBuildTicks = RcFrequency.Ticks - beginTick;
            tileTriCount = 0; // ...
            tileMemUsage = 0; // ...

            return true;
        }

        public bool BuildTile(IInputGeomProvider geom, RcNavMeshBuildSettings settings, DtNavMesh navMesh, RcVec3f pos, out long tileBuildTicks, out int tileTriCount, out int tileMemUsage)
        {
            tileBuildTicks = 0;
            tileTriCount = 0;
            tileMemUsage = 0;

            if (null == settings || null == geom || navMesh == null)
                return false;

            float ts = settings.tileSize * settings.cellSize;

            RcVec3f bmin = geom.GetMeshBoundsMin();
            RcVec3f bmax = geom.GetMeshBoundsMax();

            int tx = (int)((pos.X - bmin.X) / ts);
            int ty = (int)((pos.Z - bmin.Z) / ts);

            return BuildTile(geom, settings, navMesh, tx, ty, out tileBuildTicks, out tileTriCount, out tileMemUsage);
        }

        public bool RemoveTile(IInputGeomProvider geom, RcNavMeshBuildSettings settings, DtNavMesh navMesh, RcVec3f pos)
        {
            if (null == settings || null == geom || navMesh == null)
                return false;

            float ts = settings.tileSize * settings.cellSize;

            var bmin = geom.GetMeshBoundsMin();

            int tx = (int)((pos.X - bmin.X) / ts);
            int ty = (int)((pos.Z - bmin.Z) / ts);

            var tileRef = navMesh.GetTileRefAt(tx, ty, 0);
            navMesh.RemoveTile(tileRef);

            return true;
        }
    }
}