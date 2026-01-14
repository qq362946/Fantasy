/*
Copyright (c) 2009-2010 Mikko Mononen memon@inside.org
recast4j copyright (c) 2015-2019 Piotr Piastucki piotr@jtilia.org
DotRecast Copyright (c) 2023-2024 Choi Ikpil ikpil@naver.com

This software is provided 'as-is', without any express or implied
warranty.  In no event will the authors be held liable for any damages
arising from the use of this software.
Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:
1. The origin of this software must not be misrepresented; you must not
 claim that you wrote the original software. If you use this software
 in a product, an acknowledgment in the product documentation would be
 appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be
 misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Recast.Geom;

namespace DotRecast.Recast
{
    using static RcRecast;
    using static RcAreas;

    public class RcBuilder
    {
        private readonly IRcBuilderProgressListener _progressListener;

        public RcBuilder()
        {
            _progressListener = null;
        }

        public RcBuilder(IRcBuilderProgressListener progressListener)
        {
            _progressListener = progressListener;
        }

        public List<RcBuilderResult> BuildTiles(IRcInputGeomProvider geom, RcConfig cfg, bool keepInterResults, bool buildAll,
            int threads = 0, TaskFactory taskFactory = null, CancellationToken cancellation = default)
        {
            RcVec3f bmin = geom.GetMeshBoundsMin();
            RcVec3f bmax = geom.GetMeshBoundsMax();
            CalcTileCount(bmin, bmax, cfg.Cs, cfg.TileSizeX, cfg.TileSizeZ, out var tw, out var th);

            if (1 < threads)
            {
                return BuildMultiThread(geom, cfg, bmin, bmax, tw, th, threads, taskFactory ?? Task.Factory, cancellation, keepInterResults, buildAll);
            }

            return BuildSingleThread(geom, cfg, bmin, bmax, tw, th, keepInterResults, buildAll);
        }

        private List<RcBuilderResult> BuildSingleThread(IRcInputGeomProvider geom, RcConfig cfg, RcVec3f bmin, RcVec3f bmax, int tw, int th,
            bool keepInterResults, bool buildAll)
        {
            var results = new List<RcBuilderResult>(th * tw);
            RcAtomicInteger counter = new RcAtomicInteger(0);

            for (int y = 0; y < th; ++y)
            {
                for (int x = 0; x < tw; ++x)
                {
                    var result = BuildTile(geom, cfg, bmin, bmax, x, y, counter, tw * th, keepInterResults);
                    results.Add(result);
                }
            }

            return results;
        }

        private List<RcBuilderResult> BuildMultiThread(IRcInputGeomProvider geom, RcConfig cfg, RcVec3f bmin, RcVec3f bmax, int tw, int th,
            int threads, TaskFactory taskFactory, CancellationToken cancellation,
            bool keepInterResults, bool buildAll)
        {
            var results = new ConcurrentQueue<RcBuilderResult>();
            RcAtomicInteger progress = new RcAtomicInteger(0);

            List<Task> limits = new List<Task>(threads);
            for (int x = 0; x < tw; ++x)
            {
                for (int y = 0; y < th; ++y)
                {
                    int tx = x;
                    int ty = y;
                    var task = taskFactory.StartNew(state =>
                    {
                        if (cancellation.IsCancellationRequested)
                            return;

                        try
                        {
                            RcBuilderResult result = BuildTile(geom, cfg, bmin, bmax, tx, ty, progress, tw * th, keepInterResults);
                            results.Enqueue(result);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }, null, cancellation);

                    limits.Add(task);
                    if (threads <= limits.Count)
                    {
                        Task.WaitAll(limits.ToArray());
                        limits.Clear();
                    }
                }
            }

            if (0 < limits.Count)
            {
                Task.WaitAll(limits.ToArray());
                limits.Clear();
            }

            var list = results.ToList();
            return list;
        }

        public RcBuilderResult BuildTile(IRcInputGeomProvider geom, RcConfig cfg, RcVec3f bmin, RcVec3f bmax, int tx, int ty, RcAtomicInteger progress, int total, bool keepInterResults)
        {
            var bcfg = new RcBuilderConfig(cfg, bmin, bmax, tx, ty);
            RcBuilderResult result = Build(geom, bcfg, keepInterResults);
            if (_progressListener != null)
            {
                _progressListener.OnProgress(progress.IncrementAndGet(), total);
            }


            return result;
        }

        public RcBuilderResult Build(IRcInputGeomProvider geom, RcBuilderConfig bcfg, bool keepInterResults)
        {
            RcConfig cfg = bcfg.cfg;
            RcContext ctx = new RcContext();
            //
            // Step 1. Rasterize input polygon soup.
            //
            RcHeightfield solid = RcVoxelizations.BuildSolidHeightfield(ctx, geom, bcfg);
            return Build(ctx, bcfg.tileX, bcfg.tileZ, geom, cfg, solid, keepInterResults);
        }

        public RcBuilderResult Build(RcContext ctx, int tileX, int tileZ, IRcInputGeomProvider geom, RcConfig cfg, RcHeightfield solid, bool keepInterResults)
        {
            FilterHeightfield(ctx, solid, cfg);
            RcCompactHeightfield chf = BuildCompactHeightfield(ctx, geom, cfg, solid);

            // Partition the heightfield so that we can use simple algorithm later to triangulate the walkable areas.
            // There are 3 partitioning methods, each with some pros and cons:
            // 1) Watershed partitioning
            //   - the classic Recast partitioning
            //   - creates the nicest tessellation
            //   - usually slowest
            //   - partitions the heightfield into nice regions without holes or overlaps
            //   - the are some corner cases where this method creates produces holes and overlaps
            //      - holes may appear when a small obstacles is close to large open area (triangulation can handle this)
            //      - overlaps may occur if you have narrow spiral corridors (i.e stairs), this make triangulation to fail
            //   * generally the best choice if you precompute the navmesh, use this if you have large open areas
            // 2) Monotone partitioning
            //   - fastest
            //   - partitions the heightfield into regions without holes and overlaps (guaranteed)
            //   - creates long thin polygons, which sometimes causes paths with detours
            //   * use this if you want fast navmesh generation
            // 3) Layer partitoining
            //   - quite fast
            //   - partitions the heighfield into non-overlapping regions
            //   - relies on the triangulation code to cope with holes (thus slower than monotone partitioning)
            //   - produces better triangles than monotone partitioning
            //   - does not have the corner cases of watershed partitioning
            //   - can be slow and create a bit ugly tessellation (still better than monotone)
            //     if you have large open areas with small obstacles (not a problem if you use tiles)
            //   * good choice to use for tiled navmesh with medium and small sized tiles

            if (cfg.Partition == RcPartitionType.WATERSHED.Value)
            {
                // Prepare for region partitioning, by calculating distance field along the walkable surface.
                RcRegions.BuildDistanceField(ctx, chf);

                // Partition the walkable surface into simple regions without holes.
                RcRegions.BuildRegions(ctx, chf, cfg.MinRegionArea, cfg.MergeRegionArea);
            }
            else if (cfg.Partition == RcPartitionType.MONOTONE.Value)
            {
                // Partition the walkable surface into simple regions without holes.
                // Monotone partitioning does not need distancefield.
                RcRegions.BuildRegionsMonotone(ctx, chf, cfg.MinRegionArea, cfg.MergeRegionArea);
            }
            else
            {
                // Partition the walkable surface into simple regions without holes.
                RcRegions.BuildLayerRegions(ctx, chf, cfg.MinRegionArea);
            }

            //
            // Step 5. Trace and simplify region contours.
            //

            // Create contours.
            RcContourSet cset = RcContours.BuildContours(ctx, chf, cfg.MaxSimplificationError, cfg.MaxEdgeLen, RcBuildContoursFlags.RC_CONTOUR_TESS_WALL_EDGES);

            //
            // Step 6. Build polygons mesh from contours.
            //

            RcPolyMesh pmesh = RcMeshs.BuildPolyMesh(ctx, cset, cfg.MaxVertsPerPoly);

            //
            // Step 7. Create detail mesh which allows to access approximate height
            // on each polygon.
            //
            RcPolyMeshDetail dmesh = cfg.BuildMeshDetail
                ? RcMeshDetails.BuildPolyMeshDetail(ctx, pmesh, chf, cfg.DetailSampleDist, cfg.DetailSampleMaxError)
                : null;

            return new RcBuilderResult(
                tileX,
                tileZ,
                keepInterResults ? solid : null,
                keepInterResults ? chf : null,
                keepInterResults ? cset : null,
                pmesh,
                dmesh,
                ctx
            );
        }

        /*
         * Step 2. Filter walkable surfaces.
         */
        private void FilterHeightfield(RcContext ctx, RcHeightfield solid, RcConfig cfg)
        {
            // Once all geometry is rasterized, we do initial pass of filtering to
            // remove unwanted overhangs caused by the conservative rasterization
            // as well as filter spans where the character cannot possibly stand.
            if (cfg.FilterLowHangingObstacles)
            {
                RcFilters.FilterLowHangingWalkableObstacles(ctx, cfg.WalkableClimb, solid);
            }

            if (cfg.FilterLedgeSpans)
            {
                RcFilters.FilterLedgeSpans(ctx, cfg.WalkableHeight, cfg.WalkableClimb, solid);
            }

            if (cfg.FilterWalkableLowHeightSpans)
            {
                RcFilters.FilterWalkableLowHeightSpans(ctx, cfg.WalkableHeight, solid);
            }
        }

        /*
         * Step 3. Partition walkable surface to simple regions.
         */
        private RcCompactHeightfield BuildCompactHeightfield(RcContext ctx, IRcInputGeomProvider geom, RcConfig cfg, RcHeightfield solid)
        {
            // Compact the heightfield so that it is faster to handle from now on.
            // This will result more cache coherent data as well as the neighbours
            // between walkable cells will be calculated.
            RcCompactHeightfield chf = RcCompacts.BuildCompactHeightfield(ctx, cfg.WalkableHeight, cfg.WalkableClimb, solid);

            // Erode the walkable area by agent radius.
            ErodeWalkableArea(ctx, cfg.WalkableRadius, chf);
            // (Optional) Mark areas.
            if (geom != null)
            {
                foreach (RcConvexVolume vol in geom.ConvexVolumes())
                {
                    MarkConvexPolyArea(ctx, vol.verts, vol.hmin, vol.hmax, vol.areaMod, chf);
                }
            }

            return chf;
        }

        public RcHeightfieldLayerSet BuildLayers(IRcInputGeomProvider geom, RcBuilderConfig builderCfg)
        {
            RcContext ctx = new RcContext();
            RcHeightfield solid = RcVoxelizations.BuildSolidHeightfield(ctx, geom, builderCfg);
            FilterHeightfield(ctx, solid, builderCfg.cfg);
            RcCompactHeightfield chf = BuildCompactHeightfield(ctx, geom, builderCfg.cfg, solid);

            RcLayers.BuildHeightfieldLayers(ctx, chf, builderCfg.cfg.BorderSize, builderCfg.cfg.WalkableHeight, out var lset);
            return lset;
        }
    }
}