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
using System.Collections.Generic;
using System.Diagnostics;
using DotRecast.Core;
using DotRecast.Core.Collections;
using DotRecast.Core.Numerics;

namespace DotRecast.Detour
{
    using static DtDetour;

    /// Provides the ability to perform pathfinding related queries against
    /// a navigation mesh.
    /// @ingroup detour
    public class DtNavMeshQuery
    {
        protected readonly DtNavMesh m_nav; //< Pointer to navmesh data.
        protected DtQueryData m_query; //< Sliced query state.

        protected readonly DtNodePool m_tinyNodePool; //< Pointer to small node pool. 
        protected readonly DtNodePool m_nodePool; //< Pointer to node pool. 
        protected readonly DtNodeQueue m_openList; //< Pointer to open list queue. 

        //////////////////////////////////////////////////////////////////////////////////////////

        /// @class dtNavMeshQuery
        ///
        /// For methods that support undersized buffers, if the buffer is too small 
        /// to hold the entire result set the return status of the method will include 
        /// the #DT_BUFFER_TOO_SMALL flag.
        ///
        /// Constant member functions can be used by multiple clients without side
        /// effects. (E.g. No change to the closed list. No impact on an in-progress
        /// sliced path query. Etc.)
        /// 
        /// Walls and portals: A @e wall is a polygon segment that is 
        /// considered impassable. A @e portal is a passable segment between polygons.
        /// A portal may be treated as a wall based on the dtQueryFilter used for a query.
        ///
        /// @see dtNavMesh, dtQueryFilter, #dtAllocNavMeshQuery(), #dtAllocNavMeshQuery()
        public DtNavMeshQuery(DtNavMesh nav)
        {
            m_nav = nav;
            m_nodePool = new DtNodePool();
            m_openList = new DtNodeQueue();
            m_tinyNodePool = new DtNodePool();
        }

        /// Returns random location on navmesh.
        /// Polygons are chosen weighted by area. The search runs in linear related to number of polygon.
        ///  @param[in]		filter			The polygon filter to apply to the query.
        ///  @param[in]		frand			Function returning a random number [0..1).
        ///  @param[out]	randomRef		The reference id of the random location.
        ///  @param[out]	randomPt		The random location. 
        /// @returns The status flags for the query.
        public DtStatus FindRandomPoint(IDtQueryFilter filter, IRcRand frand, out long randomRef, out RcVec3f randomPt)
        {
            randomRef = 0;
            randomPt = RcVec3f.Zero;

            if (null == filter || null == frand)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            // Randomly pick one tile. Assume that all tiles cover roughly the same area.
            DtMeshTile tile = null;
            float tsum = 0.0f;
            for (int i = 0; i < m_nav.GetMaxTiles(); i++)
            {
                DtMeshTile t = m_nav.GetTile(i);
                if (t == null || t.data == null || t.data.header == null)
                {
                    continue;
                }

                // Choose random tile using reservoir sampling.
                float area = 1.0f; // Could be tile area too.
                tsum += area;
                float u = frand.Next();
                if (u * tsum <= area)
                {
                    tile = t;
                }
            }

            if (tile == null)
            {
                return DtStatus.DT_FAILURE;
            }

            // Randomly pick one polygon weighted by polygon area.
            DtPoly poly = null;
            long polyRef = 0;
            long @base = m_nav.GetPolyRefBase(tile);

            float areaSum = 0.0f;
            for (int i = 0; i < tile.data.header.polyCount; ++i)
            {
                DtPoly p = tile.data.polys[i];
                // Do not return off-mesh connection polygons.
                if (p.GetPolyType() != DtPolyTypes.DT_POLYTYPE_GROUND)
                {
                    continue;
                }

                // Must pass filter
                long refs = @base | (long)i;
                if (!filter.PassFilter(refs, tile, p))
                {
                    continue;
                }

                // Calc area of the polygon.
                float polyArea = 0.0f;
                for (int j = 2; j < p.vertCount; ++j)
                {
                    int va = p.verts[0] * 3;
                    int vb = p.verts[j - 1] * 3;
                    int vc = p.verts[j] * 3;
                    polyArea += DtUtils.TriArea2D(tile.data.verts, va, vb, vc);
                }

                // Choose random polygon weighted by area, using reservoir sampling.
                areaSum += polyArea;
                float u = frand.Next();
                if (u * areaSum <= polyArea)
                {
                    poly = p;
                    polyRef = refs;
                }
            }

            if (poly == null)
            {
                return DtStatus.DT_FAILURE;
            }

            // Randomly pick point on polygon.
            Span<float> verts = stackalloc float[3 * m_nav.GetMaxVertsPerPoly()];
            Span<float> areas = stackalloc float[m_nav.GetMaxVertsPerPoly()];
            RcSpans.Copy(tile.data.verts, poly.verts[0] * 3, verts, 0, 3);
            for (int j = 1; j < poly.vertCount; ++j)
            {
                RcSpans.Copy(tile.data.verts, poly.verts[j] * 3, verts, j * 3, 3);
            }

            float s = frand.Next();
            float t0 = frand.Next();

            DtUtils.RandomPointInConvexPoly(verts, poly.vertCount, areas, s, t0, out var pt);
            ClosestPointOnPoly(polyRef, pt, out var closest, out var _);

            randomRef = polyRef;
            randomPt = closest;

            return DtStatus.DT_SUCCESS;
        }

        /// Returns random location on navmesh within the reach of specified location.
        /// Polygons are chosen weighted by area. The search runs in linear related to number of polygon.
        /// The location is not exactly constrained by the circle, but it limits the visited polygons.
        ///  @param[in]		startRef		The reference id of the polygon where the search starts.
        ///  @param[in]		centerPos		The center of the search circle. [(x, y, z)]
        ///  @param[in]		maxRadius		The radius of the search circle. [Units: wu]
        ///  @param[in]		filter			The polygon filter to apply to the query.
        ///  @param[in]		frand			Function returning a random number [0..1).
        ///  @param[out]	randomRef		The reference id of the random location.
        ///  @param[out]	randomPt		The random location. [(x, y, z)]
        /// @returns The status flags for the query.
        public DtStatus FindRandomPointAroundCircle(long startRef, RcVec3f centerPos, float maxRadius,
            IDtQueryFilter filter, IRcRand frand, out long randomRef, out RcVec3f randomPt)
        {
            return FindRandomPointAroundCircle(startRef, centerPos, maxRadius, filter, frand, DtNoOpDtPolygonByCircleConstraint.Shared, out randomRef, out randomPt);
        }

        /**
     * Returns random location on navmesh within the reach of specified location. Polygons are chosen weighted by area.
     * The search runs in linear related to number of polygon. The location is strictly constrained by the circle.
     *
     * @param startRef
     *            The reference id of the polygon where the search starts.
     * @param centerPos
     *            The center of the search circle. [(x, y, z)]
     * @param maxRadius
     * @param filter
     *            The polygon filter to apply to the query.
     * @param frand
     *            Function returning a random number [0..1).
     * @return Random location
     */
        public DtStatus FindRandomPointWithinCircle(long startRef, RcVec3f centerPos, float maxRadius,
            IDtQueryFilter filter, IRcRand frand, out long randomRef, out RcVec3f randomPt)
        {
            return FindRandomPointAroundCircle(startRef, centerPos, maxRadius, filter, frand, DtStrictDtPolygonByCircleConstraint.Shared, out randomRef, out randomPt);
        }

        /// Returns random location on navmesh within the reach of specified location.
        /// Polygons are chosen weighted by area. The search runs in linear related to number of polygon.
        /// The location is not exactly constrained by the circle, but it limits the visited polygons.
        ///  @param[in]		startRef		The reference id of the polygon where the search starts.
        ///  @param[in]		centerPos		The center of the search circle. [(x, y, z)]
        ///  @param[in]		maxRadius		The radius of the search circle. [Units: wu]
        ///  @param[in]		filter			The polygon filter to apply to the query.
        ///  @param[in]		frand			Function returning a random number [0..1).
        ///  @param[in]		constraint      
        ///  @param[out]	randomRef		The reference id of the random location.
        ///  @param[out]	randomPt		The random location. [(x, y, z)]
        /// @returns The status flags for the query.
        public DtStatus FindRandomPointAroundCircle(long startRef, RcVec3f centerPos, float maxRadius,
            IDtQueryFilter filter, IRcRand frand, IDtPolygonByCircleConstraint constraint,
            out long randomRef, out RcVec3f randomPt)
        {
            randomRef = startRef;
            randomPt = centerPos;

            // Validate input
            if (!m_nav.IsValidPolyRef(startRef) || !centerPos.IsFinite() || maxRadius < 0
                || !float.IsFinite(maxRadius) || null == filter || null == frand)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            m_nav.GetTileAndPolyByRefUnsafe(startRef, out var startTile, out var startPoly);
            if (!filter.PassFilter(startRef, startTile, startPoly))
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            m_nodePool.Clear();
            m_openList.Clear();

            DtNode startNode = m_nodePool.GetNode(startRef);
            startNode.pos = centerPos;
            startNode.pidx = 0;
            startNode.cost = 0;
            startNode.total = 0;
            startNode.id = startRef;
            startNode.flags = DtNodeFlags.DT_NODE_OPEN;
            m_openList.Push(startNode);

            DtStatus status = DtStatus.DT_SUCCESS;

            float radiusSqr = maxRadius * maxRadius;
            float areaSum = 0.0f;

            DtPoly randomPoly = null;
            long randomPolyRef = 0;
            RcFixedArray256<float> randomPolyVerts = new RcFixedArray256<float>();
            int randomPolyVertCount = 0;

            RcFixedArray256<float> polyVertBuffer = new RcFixedArray256<float>();
            RcFixedArray256<float> constrainedVertBuffer = new RcFixedArray256<float>();

            while (!m_openList.IsEmpty())
            {
                DtNode bestNode = m_openList.Pop();
                bestNode.flags &= ~DtNodeFlags.DT_NODE_OPEN;
                bestNode.flags |= DtNodeFlags.DT_NODE_CLOSED;

                // Get poly and tile.
                // The API input has been checked already, skip checking internal data.
                long bestRef = bestNode.id;
                m_nav.GetTileAndPolyByRefUnsafe(bestRef, out var bestTile, out var bestPoly);

                // Place random locations on on ground.
                if (bestPoly.GetPolyType() == DtPolyTypes.DT_POLYTYPE_GROUND)
                {
                    // Calc area of the polygon.
                    float polyArea = 0.0f;
                    Span<float> polyVerts = polyVertBuffer.AsSpan().Slice(0, bestPoly.vertCount * 3);
                    for (int j = 0; j < bestPoly.vertCount; ++j)
                    {
                        RcSpans.Copy(bestTile.data.verts, bestPoly.verts[j] * 3, polyVerts, j * 3, 3);
                    }

                    Span<float> constrainedVerts = constrainedVertBuffer.AsSpan();
                    var result = constraint.Apply(polyVerts, centerPos, maxRadius, constrainedVerts, out var ncverts);
                    if (0 < ncverts)
                    {
                        int vertCount = ncverts / 3;
                        for (int j = 2; j < vertCount; ++j)
                        {
                            int va = 0;
                            int vb = (j - 1) * 3;
                            int vc = j * 3;
                            polyArea += DtUtils.TriArea2D(constrainedVerts, va, vb, vc);
                        }

                        // Choose random polygon weighted by area, using reservoir sampling.
                        areaSum += polyArea;
                        float u = frand.Next();
                        if (u * areaSum <= polyArea)
                        {
                            randomPoly = bestPoly;
                            randomPolyRef = bestRef;
                            randomPolyVerts.CopyFrom(constrainedVerts, ncverts);
                            randomPolyVertCount = ncverts;
                        }
                    }
                }

                // Get parent poly and tile.
                long parentRef = 0;
                if (bestNode.pidx != 0)
                {
                    parentRef = m_nodePool.GetNodeAtIdx(bestNode.pidx).id;
                }

                for (int i = bestPoly.firstLink; i != DT_NULL_LINK; i = bestTile.links[i].next)
                {
                    DtLink link = bestTile.links[i];
                    long neighbourRef = link.refs;
                    // Skip invalid neighbours and do not follow back to parent.
                    if (neighbourRef == 0 || neighbourRef == parentRef)
                    {
                        continue;
                    }

                    // Expand to neighbour
                    m_nav.GetTileAndPolyByRefUnsafe(neighbourRef, out var neighbourTile, out var neighbourPoly);

                    // Do not advance if the polygon is excluded by the filter.
                    if (!filter.PassFilter(neighbourRef, neighbourTile, neighbourPoly))
                    {
                        continue;
                    }

                    // Find edge and calc distance to the edge.
                    var ppStatus = GetPortalPoints(bestRef, bestPoly, bestTile, neighbourRef,
                        neighbourPoly, neighbourTile, out var va, out var vb);
                    if (ppStatus.Failed())
                    {
                        continue;
                    }

                    // If the circle is not touching the next polygon, skip it.
                    var distSqr = DtUtils.DistancePtSegSqr2D(centerPos, va, vb, out var tesg);
                    if (distSqr > radiusSqr)
                    {
                        continue;
                    }

                    DtNode neighbourNode = m_nodePool.GetNode(neighbourRef);
                    if (null == neighbourNode)
                    {
                        status |= DtStatus.DT_OUT_OF_NODES;
                        continue;
                    }

                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_CLOSED) != 0)
                    {
                        continue;
                    }

                    // Cost
                    if (neighbourNode.flags == 0)
                    {
                        neighbourNode.pos = RcVec3f.Lerp(va, vb, 0.5f);
                    }

                    float total = bestNode.total + RcVec3f.Distance(bestNode.pos, neighbourNode.pos);

                    // The node is already in open list and the new result is worse, skip.
                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_OPEN) != 0 && total >= neighbourNode.total)
                    {
                        continue;
                    }

                    neighbourNode.id = neighbourRef;
                    neighbourNode.flags = (neighbourNode.flags & ~DtNodeFlags.DT_NODE_CLOSED);
                    neighbourNode.pidx = m_nodePool.GetNodeIdx(bestNode);
                    neighbourNode.total = total;

                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_OPEN) != 0)
                    {
                        m_openList.Modify(neighbourNode);
                    }
                    else
                    {
                        neighbourNode.flags = DtNodeFlags.DT_NODE_OPEN;
                        m_openList.Push(neighbourNode);
                    }
                }
            }

            if (randomPoly == null)
            {
                return DtStatus.DT_FAILURE;
            }

            // Randomly pick point on polygon.
            float s = frand.Next();
            float t = frand.Next();

            Span<float> areas = stackalloc float[randomPolyVertCount / 3];
            DtUtils.RandomPointInConvexPoly(randomPolyVerts.AsSpan(), randomPolyVertCount / 3, areas, s, t, out var pt);
            ClosestPointOnPoly(randomPolyRef, pt, out var closest, out var _);

            randomRef = randomPolyRef;
            randomPt = closest;

            return status;
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        /// @par
        ///
        /// Uses the detail polygons to find the surface height. (Most accurate.)
        ///
        /// @p pos does not have to be within the bounds of the polygon or navigation mesh.
        ///
        /// See closestPointOnPolyBoundary() for a limited but faster option.
        ///
        /// Finds the closest point on the specified polygon.
        ///  @param[in]		ref			The reference id of the polygon.
        ///  @param[in]		pos			The position to check. [(x, y, z)]
        ///  @param[out]	closest		The closest point on the polygon. [(x, y, z)]
        ///  @param[out]	posOverPoly	True of the position is over the polygon.
        /// @returns The status flags for the query.
        public DtStatus ClosestPointOnPoly(long refs, RcVec3f pos, out RcVec3f closest, out bool posOverPoly)
        {
            closest = pos;
            posOverPoly = false;

            if (!m_nav.IsValidPolyRef(refs) || !pos.IsFinite())
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            m_nav.ClosestPointOnPoly(refs, pos, out closest, out posOverPoly);
            return DtStatus.DT_SUCCESS;
        }

        /// @par
        ///
        /// Much faster than ClosestPointOnPoly().
        ///
        /// If the provided position lies within the polygon's xz-bounds (above or below), 
        /// then @p pos and @p closest will be equal.
        ///
        /// The height of @p closest will be the polygon boundary.  The height detail is not used.
        /// 
        /// @p pos does not have to be within the bounds of the polybon or the navigation mesh.
        /// 
        /// Returns a point on the boundary closest to the source point if the source point is outside the 
        /// polygon's xz-bounds.
        ///  @param[in]		ref			The reference id to the polygon.
        ///  @param[in]		pos			The position to check. [(x, y, z)]
        ///  @param[out]	closest		The closest point. [(x, y, z)]
        /// @returns The status flags for the query.
        public DtStatus ClosestPointOnPolyBoundary(long refs, RcVec3f pos, out RcVec3f closest)
        {
            closest = pos;
            var status = m_nav.GetTileAndPolyByRef(refs, out var tile, out var poly);
            if (status.Failed())
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            if (tile == null || !pos.IsFinite())
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            // Collect vertices.
            Span<float> verts = stackalloc float[m_nav.GetMaxVertsPerPoly() * 3];
            Span<float> edged = stackalloc float[m_nav.GetMaxVertsPerPoly()];
            Span<float> edget = stackalloc float[m_nav.GetMaxVertsPerPoly()];
            int nv = poly.vertCount;
            for (int i = 0; i < nv; ++i)
            {
                RcSpans.Copy(tile.data.verts, poly.verts[i] * 3, verts, i * 3, 3);
            }

            if (DtUtils.DistancePtPolyEdgesSqr(pos, verts, nv, edged, edget))
            {
                closest = pos;
            }
            else
            {
                // Point is outside the polygon, dtClamp to nearest edge.
                float dmin = edged[0];
                int imin = 0;
                for (int i = 1; i < nv; ++i)
                {
                    if (edged[i] < dmin)
                    {
                        dmin = edged[i];
                        imin = i;
                    }
                }

                int va = imin * 3;
                int vb = ((imin + 1) % nv) * 3;
                closest = RcVec.Lerp(verts, va, vb, edget[imin]);
            }

            return DtStatus.DT_SUCCESS;
        }

        /// @par
        ///
        /// Will return #DT_FAILURE if the provided position is outside the xz-bounds
        /// of the polygon.
        ///
        /// Gets the height of the polygon at the provided position using the height detail. (Most accurate.)
        /// @param[in] ref The reference id of the polygon.
        /// @param[in] pos A position within the xz-bounds of the polygon. [(x, y, z)]
        /// @param[out] height The height at the surface of the polygon.
        /// @returns The status flags for the query.
        public DtStatus GetPolyHeight(long refs, RcVec3f pos, out float height)
        {
            height = default;

            var status = m_nav.GetTileAndPolyByRef(refs, out var tile, out var poly);
            if (status.Failed())
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            if (!pos.IsFinite2D())
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            // We used to return success for offmesh connections, but the
            // getPolyHeight in DetourNavMesh does not do this, so special
            // case it here.
            if (poly.GetPolyType() == DtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
            {
                int i = poly.verts[0] * 3;
                var v0 = new RcVec3f(tile.data.verts[i], tile.data.verts[i + 1], tile.data.verts[i + 2]);
                i = poly.verts[1] * 3;
                var v1 = new RcVec3f(tile.data.verts[i], tile.data.verts[i + 1], tile.data.verts[i + 2]);
                DtUtils.DistancePtSegSqr2D(pos, v0, v1, out var t);
                height = v0.Y + (v1.Y - v0.Y) * t;

                return DtStatus.DT_SUCCESS;
            }

            if (!m_nav.GetPolyHeight(tile, poly, pos, out var h))
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            height = h;
            return DtStatus.DT_SUCCESS;
        }

        /// Finds the polygon nearest to the specified center point.
        /// [opt] means the specified parameter can be a null pointer, in that case the output parameter will not be set.
        /// 
        ///  @param[in]		center		The center of the search box. [(x, y, z)]
        ///  @param[in]		halfExtents	The search distance along each axis. [(x, y, z)]
        ///  @param[in]		filter		The polygon filter to apply to the query.
        ///  @param[out]	nearestRef	The reference id of the nearest polygon. Will be set to 0 if no polygon is found.
        ///  @param[out]	nearestPt	The nearest point on the polygon. Unchanged if no polygon is found. [opt] [(x, y, z)]
        ///  @param[out]	isOverPoly 	Set to true if the point's X/Z coordinate lies inside the polygon, false otherwise. Unchanged if no polygon is found. [opt]
        /// @returns The status flags for the query.
        public DtStatus FindNearestPoly(RcVec3f center, RcVec3f halfExtents, IDtQueryFilter filter,
            out long nearestRef, out RcVec3f nearestPt, out bool isOverPoly)
        {
            nearestRef = 0;
            nearestPt = center;
            isOverPoly = false;

            // Get nearby polygons from proximity grid.
            DtFindNearestPolyQuery query = new DtFindNearestPolyQuery(this, center);
            DtStatus status = QueryPolygons(center, halfExtents, filter, query);
            if (status.Failed())
            {
                return status;
            }

            nearestRef = query.NearestRef();
            nearestPt = query.NearestPt();
            isOverPoly = query.OverPoly();

            return DtStatus.DT_SUCCESS;
        }

        /// Queries polygons within a tile.
        protected void QueryPolygonsInTile(DtMeshTile tile, RcVec3f qmin, RcVec3f qmax, IDtQueryFilter filter, IDtPolyQuery query)
        {
            const int batchSize = 32;
            Span<long> polyRefs = stackalloc long[batchSize];
            Span<int> polys = stackalloc int[batchSize];
            int n = 0;

            if (tile.data.bvTree != null)
            {
                int nodeIndex = 0;
                int end = tile.data.header.bvNodeCount;
                var tbmin = tile.data.header.bmin;
                var tbmax = tile.data.header.bmax;
                float qfac = tile.data.header.bvQuantFactor;

                // Calculate quantized box
                RcVec3i bmin;
                RcVec3i bmax;
                // dtClamp query box to world box.
                float minx = Math.Clamp(qmin.X, tbmin.X, tbmax.X) - tbmin.X;
                float miny = Math.Clamp(qmin.Y, tbmin.Y, tbmax.Y) - tbmin.Y;
                float minz = Math.Clamp(qmin.Z, tbmin.Z, tbmax.Z) - tbmin.Z;
                float maxx = Math.Clamp(qmax.X, tbmin.X, tbmax.X) - tbmin.X;
                float maxy = Math.Clamp(qmax.Y, tbmin.Y, tbmax.Y) - tbmin.Y;
                float maxz = Math.Clamp(qmax.Z, tbmin.Z, tbmax.Z) - tbmin.Z;
                // Quantize
                bmin.X = (int)(qfac * minx) & 0x7ffffffe;
                bmin.Y = (int)(qfac * miny) & 0x7ffffffe;
                bmin.Z = (int)(qfac * minz) & 0x7ffffffe;
                bmax.X = (int)(qfac * maxx + 1) | 1;
                bmax.Y = (int)(qfac * maxy + 1) | 1;
                bmax.Z = (int)(qfac * maxz + 1) | 1;

                // Traverse tree
                long @base = m_nav.GetPolyRefBase(tile);
                while (nodeIndex < end)
                {
                    DtBVNode node = tile.data.bvTree[nodeIndex];
                    bool overlap = DtUtils.OverlapQuantBounds(ref bmin, ref bmax, ref node.bmin, ref node.bmax);
                    bool isLeafNode = node.i >= 0;

                    if (isLeafNode && overlap)
                    {
                        long refs = @base | (long)node.i;
                        if (filter.PassFilter(refs, tile, tile.data.polys[node.i]))
                        {
                            polyRefs[n] = refs;
                            polys[n] = tile.data.polys[node.i].index;

                            if (n == batchSize - 1)
                            {
                                query.Process(tile, polys, polyRefs, batchSize);
                                n = 0;
                            }
                            else
                            {
                                n++;
                            }
                        }
                    }

                    if (overlap || isLeafNode)
                    {
                        nodeIndex++;
                    }
                    else
                    {
                        int escapeIndex = -node.i;
                        nodeIndex += escapeIndex;
                    }
                }
            }
            else
            {
                RcVec3f bmin = new RcVec3f();
                RcVec3f bmax = new RcVec3f();
                long @base = m_nav.GetPolyRefBase(tile);
                for (int i = 0; i < tile.data.header.polyCount; ++i)
                {
                    DtPoly p = tile.data.polys[i];
                    // Do not return off-mesh connection polygons.
                    if (p.GetPolyType() == DtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                    {
                        continue;
                    }

                    // Must pass filter
                    long refs = @base | (long)i;
                    if (!filter.PassFilter(refs, tile, p))
                    {
                        continue;
                    }

                    // Calc polygon bounds.
                    int v = p.verts[0] * 3;
                    bmin = tile.data.verts.ToVec3(v);
                    bmax = tile.data.verts.ToVec3(v);
                    for (int j = 1; j < p.vertCount; ++j)
                    {
                        v = p.verts[j] * 3;
                        bmin = RcVec3f.Min(bmin, tile.data.verts.ToVec3(v));
                        bmax = RcVec3f.Max(bmax, tile.data.verts.ToVec3(v));
                    }

                    if (DtUtils.OverlapBounds(qmin, qmax, bmin, bmax))
                    {
                        polyRefs[n] = refs;
                        polys[n] = p.index;

                        if (n == batchSize - 1)
                        {
                            query.Process(tile, polys, polyRefs, batchSize);
                            n = 0;
                        }
                        else
                        {
                            n++;
                        }
                    }
                }
            }

            // Process the last polygons that didn't make a full batch.
            if (n > 0)
            {
                query.Process(tile, polys, polyRefs, n);
            }
        }

        /// @par 
        ///
        /// If no polygons are found, the function will return #DT_SUCCESS with a
        /// @p polyCount of zero.
        ///
        /// If @p polys is too small to hold the entire result set, then the array will 
        /// be filled to capacity. The method of choosing which polygons from the 
        /// full set are included in the partial result set is undefined.
        ///
        /// Finds polygons that overlap the search box.
        ///  @param[in]		center		The center of the search box. [(x, y, z)]
        ///  @param[in]		halfExtents		The search distance along each axis. [(x, y, z)]
        ///  @param[in]		filter		The polygon filter to apply to the query.
        ///  @param[out]	polys		The reference ids of the polygons that overlap the query box.
        ///  @param[out]	polyCount	The number of polygons in the search result.
        ///  @param[in]		maxPolys	The maximum number of polygons the search result can hold.
        /// @returns The status flags for the query.
        public DtStatus QueryPolygons(RcVec3f center, RcVec3f halfExtents,
            IDtQueryFilter filter,
            long[] polys, out int polyCount, int maxPolys)
        {
            polyCount = 0;
            if (null == polys || maxPolys < 0)
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;

            DtCollectPolysQuery collector = new DtCollectPolysQuery(polys, maxPolys);
            DtStatus status = QueryPolygons(center, halfExtents, filter, collector);
            if (status.Failed())
                return status;

            polyCount = collector.NumCollected();
            return collector.Overflowed()
                ? DtStatus.DT_SUCCESS | DtStatus.DT_BUFFER_TOO_SMALL
                : DtStatus.DT_SUCCESS;
        }

        /// @par 
        ///
        /// The query will be invoked with batches of polygons. Polygons passed
        /// to the query have bounding boxes that overlap with the center and halfExtents
        /// passed to this function. The dtPolyQuery::process function is invoked multiple
        /// times until all overlapping polygons have been processed.
        ///
        /// Finds polygons that overlap the search box.
        ///  @param[in]		center		The center of the search box. [(x, y, z)]
        ///  @param[in]		halfExtents		The search distance along each axis. [(x, y, z)]
        ///  @param[in]		filter		The polygon filter to apply to the query.
        ///  @param[in]		query		The query. Polygons found will be batched together and passed to this query.
        public DtStatus QueryPolygons(RcVec3f center, RcVec3f halfExtents, IDtQueryFilter filter, IDtPolyQuery query)
        {
            if (!center.IsFinite() || !halfExtents.IsFinite() || null == filter)
            {
                return DtStatus.DT_INVALID_PARAM;
            }

            // Find tiles the query touches.
            RcVec3f bmin = RcVec3f.Subtract(center, halfExtents);
            RcVec3f bmax = RcVec3f.Add(center, halfExtents);

            // Find tiles the query touches.
            m_nav.CalcTileLoc(bmin, out var minx, out var miny);
            m_nav.CalcTileLoc(bmax, out var maxx, out var maxy);

            const int MAX_NEIS = 32;
            Span<int> neis = stackalloc int[MAX_NEIS];

            for (int y = miny; y <= maxy; ++y)
            {
                for (int x = minx; x <= maxx; ++x)
                {
                    int nneis = m_nav.GetTilesAt(x, y, neis, MAX_NEIS);
                    for (int j = 0; j < nneis; ++j)
                    {
                        var nei = m_nav.GetTile(neis[j]);
                        QueryPolygonsInTile(nei, bmin, bmax, filter, query);
                    }
                }
            }

            return DtStatus.DT_SUCCESS;
        }

        /// @par
        ///
        /// If the end polygon cannot be reached through the navigation graph,
        /// the last polygon in the path will be the nearest the end polygon.
        ///
        /// If the path array is to small to hold the full result, it will be filled as 
        /// far as possible from the start polygon toward the end polygon.
        ///
        /// The start and end positions are used to calculate traversal costs. 
        /// (The y-values impact the result.)
        ///
        /// @name Standard Pathfinding Functions
        /// @{
        /// Finds a path from the start polygon to the end polygon.
        ///  @param[in]		startRef	The reference id of the start polygon.
        ///  @param[in]		endRef		The reference id of the end polygon.
        ///  @param[in]		startPos	A position within the start polygon. [(x, y, z)]
        ///  @param[in]		endPos		A position within the end polygon. [(x, y, z)]
        ///  @param[in]		filter		The polygon filter to apply to the query.
        ///  @param[out]	path		An ordered list of polygon references representing the path. (Start to end.) 
        ///  							[(polyRef) * @p pathCount]
        ///  @param[out]	pathCount	The number of polygons returned in the @p path array.
        ///  @param[in]		maxPath		The maximum number of polygons the @p path array can hold. [Limit: >= 1]
        /// @par
        ///
        /// If the end polygon cannot be reached through the navigation graph,
        /// the last polygon in the path will be the nearest the end polygon.
        ///
        /// If the path array is to small to hold the full result, it will be filled as 
        /// far as possible from the start polygon toward the end polygon.
        ///
        /// The start and end positions are used to calculate traversal costs. 
        /// (The y-values impact the result.)
        ///
        public DtStatus FindPath(long startRef, long endRef, RcVec3f startPos, RcVec3f endPos, IDtQueryFilter filter, Span<long> path, out int pathCount, int maxPath)
        {
            pathCount = 0;

            // Validate input
            if (!m_nav.IsValidPolyRef(startRef) || !m_nav.IsValidPolyRef(endRef) || !startPos.IsFinite() || !endPos.IsFinite() || 
                null == filter || path.IsEmpty || maxPath <= 0)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            if (startRef == endRef)
            {
                path[0] = startRef;
                pathCount = 1;
                return DtStatus.DT_SUCCESS;
            }

            m_nodePool.Clear();
            m_openList.Clear();

            DtNode startNode = m_nodePool.GetNode(startRef);
            startNode.pos = startPos;
            startNode.pidx = 0;
            startNode.cost = 0;
            startNode.total = RcVec3f.Distance(startPos, endPos) * DtDefaultQueryHeuristic.H_SCALE;
            startNode.id = startRef;
            startNode.flags = DtNodeFlags.DT_NODE_OPEN;
            m_openList.Push(startNode);

            DtNode lastBestNode = startNode;
            float lastBestNodeCost = startNode.total;

            bool outOfNodes = false;

            while (!m_openList.IsEmpty())
            {
                // Remove node from open list and put it in closed list.
                DtNode bestNode = m_openList.Pop();
                bestNode.flags &= ~DtNodeFlags.DT_NODE_OPEN;
                bestNode.flags |= DtNodeFlags.DT_NODE_CLOSED;

                // Reached the goal, stop searching.
                if (bestNode.id == endRef)
                {
                    lastBestNode = bestNode;
                    break;
                }

                // Get current poly and tile.
                // The API input has been checked already, skip checking internal data.
                long bestRef = bestNode.id;
                m_nav.GetTileAndPolyByRefUnsafe(bestRef, out var bestTile, out var bestPoly);

                // Get parent poly and tile.
                long parentRef = 0;
                DtMeshTile parentTile = null;
                DtPoly parentPoly = null;
                if (bestNode.pidx != 0)
                {
                    parentRef = m_nodePool.GetNodeAtIdx(bestNode.pidx).id;
                }

                if (parentRef != 0)
                {
                    m_nav.GetTileAndPolyByRefUnsafe(parentRef, out parentTile, out parentPoly);
                }

                for (int i = bestPoly.firstLink; i != DT_NULL_LINK; i = bestTile.links[i].next)
                {
                    long neighbourRef = bestTile.links[i].refs;

                    // Skip invalid ids and do not expand back to where we came from.
                    if (neighbourRef == 0 || neighbourRef == parentRef)
                    {
                        continue;
                    }

                    // Get neighbour poly and tile.
                    // The API input has been checked already, skip checking internal data.
                    m_nav.GetTileAndPolyByRefUnsafe(neighbourRef, out var neighbourTile, out var neighbourPoly);

                    if (!filter.PassFilter(neighbourRef, neighbourTile, neighbourPoly))
                    {
                        continue;
                    }

                    // deal explicitly with crossing tile boundaries
                    byte crossSide = 0;
                    if (bestTile.links[i].side != 0xff)
                        crossSide = (byte)(bestTile.links[i].side >> 1);

                    // get the node
                    DtNode neighbourNode = m_nodePool.GetNode(neighbourRef, crossSide);
                    if (null == neighbourNode)
                    {
                        outOfNodes = true;
                        continue;
                    }

                    // If the node is visited the first time, calculate node position.
                    if (neighbourNode.flags == 0)
                    {
                        GetEdgeMidPoint(bestRef, bestPoly, bestTile,
                            neighbourRef, neighbourPoly, neighbourTile,
                            ref neighbourNode.pos);
                    }

                    // Calculate cost and heuristic.
                    float cost = 0;
                    float heuristic = 0;

                    // Special case for last node.
                    if (neighbourRef == endRef)
                    {
                        // Cost
                        float curCost = filter.GetCost(bestNode.pos, neighbourNode.pos,
                            parentRef, parentTile, parentPoly,
                            bestRef, bestTile, bestPoly,
                            neighbourRef, neighbourTile, neighbourPoly);
                        float endCost = filter.GetCost(neighbourNode.pos, endPos,
                            bestRef, bestTile, bestPoly,
                            neighbourRef, neighbourTile, neighbourPoly,
                            0, null, null);

                        cost = bestNode.cost + curCost + endCost;
                        heuristic = 0;
                    }
                    else
                    {
                        // Cost
                        float curCost = filter.GetCost(bestNode.pos, neighbourNode.pos,
                            parentRef, parentTile, parentPoly,
                            bestRef, bestTile, bestPoly,
                            neighbourRef, neighbourTile, neighbourPoly);
                        cost = bestNode.cost + curCost;
                        heuristic = RcVec3f.Distance(neighbourNode.pos, endPos) * DtDefaultQueryHeuristic.H_SCALE;
                    }

                    float total = cost + heuristic;

                    // The node is already in open list and the new result is worse, skip.
                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_OPEN) != 0 && total >= neighbourNode.total)
                    {
                        continue;
                    }

                    // The node is already visited and process, and the new result is worse, skip.
                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_CLOSED) != 0 && total >= neighbourNode.total)
                    {
                        continue;
                    }

                    // Add or update the node.
                    neighbourNode.pidx = m_nodePool.GetNodeIdx(bestNode);
                    neighbourNode.id = neighbourRef;
                    neighbourNode.flags = (neighbourNode.flags & ~DtNodeFlags.DT_NODE_CLOSED);
                    neighbourNode.cost = cost;
                    neighbourNode.total = total;

                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_OPEN) != 0)
                    {
                        // Already in open, update node location.
                        m_openList.Modify(neighbourNode);
                    }
                    else
                    {
                        // Put the node in open list.
                        neighbourNode.flags |= DtNodeFlags.DT_NODE_OPEN;
                        m_openList.Push(neighbourNode);
                    }

                    // Update nearest node to target so far.
                    if (heuristic < lastBestNodeCost)
                    {
                        lastBestNodeCost = heuristic;
                        lastBestNode = neighbourNode;
                    }
                }
            }

            var status = GetPathToNode(lastBestNode, path, out pathCount, maxPath);
            if (lastBestNode.id != endRef)
            {
                status |= DtStatus.DT_PARTIAL_RESULT;
            }

            if (outOfNodes)
                status |= DtStatus.DT_OUT_OF_NODES;

            return status;
        }

        ///@}
        /// @name Sliced Pathfinding Functions
        /// Common use case:
        ///	-# Call initSlicedFindPath() to initialize the sliced path query.
        ///	-# Call updateSlicedFindPath() until it returns complete.
        ///	-# Call finalizeSlicedFindPath() to get the path.
        ///@{ 
        /// Initializes a sliced path query.
        ///  @param[in]		startRef	The reference id of the start polygon.
        ///  @param[in]		endRef		The reference id of the end polygon.
        ///  @param[in]		startPos	A position within the start polygon. [(x, y, z)]
        ///  @param[in]		endPos		A position within the end polygon. [(x, y, z)]
        ///  @param[in]		filter		The polygon filter to apply to the query.
        ///  @param[in]		options		query options (see: #dtFindPathOptions)
        /// @returns The status flags for the query.
        public DtStatus InitSlicedFindPath(long startRef, long endRef, RcVec3f startPos, RcVec3f endPos, IDtQueryFilter filter, int options)
        {
            // Init path state.
            m_query = new DtQueryData();
            m_query.status = DtStatus.DT_FAILURE;
            m_query.startRef = startRef;
            m_query.endRef = endRef;
            m_query.startPos = startPos;
            m_query.endPos = endPos;
            m_query.filter = filter;
            m_query.options = options;
            m_query.raycastLimitSqr = float.MaxValue;

            // Validate input
            if (!m_nav.IsValidPolyRef(startRef) || !m_nav.IsValidPolyRef(endRef) || !startPos.IsFinite() || !endPos.IsFinite() || null == filter)
            {
                return DtStatus.DT_INVALID_PARAM;
            }

            // trade quality with performance?
            if ((options & DtFindPathOptions.DT_FINDPATH_ANY_ANGLE) != 0)
            {
                // limiting to several times the character radius yields nice results. It is not sensitive
                // so it is enough to compute it from the first tile.
                DtMeshTile tile = m_nav.GetTileByRef(startRef);
                float agentRadius = tile.data.header.walkableRadius;
                m_query.raycastLimitSqr = RcMath.Sqr(agentRadius * DT_RAY_CAST_LIMIT_PROPORTIONS);
            }

            if (startRef == endRef)
            {
                m_query.status = DtStatus.DT_SUCCESS;
                return DtStatus.DT_SUCCESS;
            }

            m_nodePool.Clear();
            m_openList.Clear();

            DtNode startNode = m_nodePool.GetNode(startRef);
            startNode.pos = startPos;
            startNode.pidx = 0;
            startNode.cost = 0;
            startNode.total = RcVec3f.Distance(startPos, endPos) * DtDefaultQueryHeuristic.H_SCALE;
            startNode.id = startRef;
            startNode.flags = DtNodeFlags.DT_NODE_OPEN;
            m_openList.Push(startNode);

            m_query.status = DtStatus.DT_IN_PROGRESS;
            m_query.lastBestNode = startNode;
            m_query.lastBestNodeCost = startNode.total;

            return m_query.status;
        }

        /// Updates an in-progress sliced path query.
        ///  @param[in]		maxIter		The maximum number of iterations to perform.
        ///  @param[out]	doneIters	The actual number of iterations completed. [opt]
        /// @returns The status flags for the query.
        public virtual DtStatus UpdateSlicedFindPath(int maxIter, out int doneIters)
        {
            doneIters = 0;
            if (!m_query.status.InProgress())
            {
                return m_query.status;
            }

            // Make sure the request is still valid.
            if (!m_nav.IsValidPolyRef(m_query.startRef) || !m_nav.IsValidPolyRef(m_query.endRef))
            {
                m_query.status = DtStatus.DT_FAILURE;
                return DtStatus.DT_FAILURE;
            }

            var rayHit = new DtRaycastHit();
            rayHit.maxPath = 0;

            int iter = 0;
            while (iter < maxIter && !m_openList.IsEmpty())
            {
                iter++;

                // Remove node from open list and put it in closed list.
                DtNode bestNode = m_openList.Pop();
                bestNode.flags &= ~DtNodeFlags.DT_NODE_OPEN;
                bestNode.flags |= DtNodeFlags.DT_NODE_CLOSED;

                // Reached the goal, stop searching.
                if (bestNode.id == m_query.endRef)
                {
                    m_query.lastBestNode = bestNode;
                    var details = m_query.status & DtStatus.DT_STATUS_DETAIL_MASK;
                    m_query.status = DtStatus.DT_SUCCESS | details;
                    doneIters = iter;
                    return m_query.status;
                }

                // Get current poly and tile.
                // The API input has been checked already, skip checking internal data.
                long bestRef = bestNode.id;
                var status = m_nav.GetTileAndPolyByRef(bestRef, out var bestTile, out var bestPoly);
                if (status.Failed())
                {
                    // The polygon has disappeared during the sliced query, fail.
                    m_query.status = DtStatus.DT_FAILURE;
                    doneIters = iter;
                    return m_query.status;
                }

                // Get parent and grand parent poly and tile.
                long parentRef = 0, grandpaRef = 0;
                DtMeshTile parentTile = null;
                DtPoly parentPoly = null;
                DtNode parentNode = null;
                if (bestNode.pidx != 0)
                {
                    parentNode = m_nodePool.GetNodeAtIdx(bestNode.pidx);
                    parentRef = parentNode.id;
                    if (parentNode.pidx != 0)
                    {
                        grandpaRef = m_nodePool.GetNodeAtIdx(parentNode.pidx).id;
                    }
                }

                if (parentRef != 0)
                {
                    status = m_nav.GetTileAndPolyByRef(parentRef, out parentTile, out parentPoly);
                    bool invalidParent = status.Failed();
                    if (invalidParent || (grandpaRef != 0 && !m_nav.IsValidPolyRef(grandpaRef)))
                    {
                        // The polygon has disappeared during the sliced query fail.
                        m_query.status = DtStatus.DT_FAILURE;
                        doneIters = iter;
                        return m_query.status;
                    }
                }

                // decide whether to test raycast to previous nodes
                bool tryLOS = false;
                if ((m_query.options & DtFindPathOptions.DT_FINDPATH_ANY_ANGLE) != 0)
                {
                    if ((parentRef != 0) && (RcVec3f.DistanceSquared(parentNode.pos, bestNode.pos) < m_query.raycastLimitSqr))
                    {
                        tryLOS = true;
                    }
                }

                for (int i = bestPoly.firstLink; i != DT_NULL_LINK; i = bestTile.links[i].next)
                {
                    long neighbourRef = bestTile.links[i].refs;

                    // Skip invalid ids and do not expand back to where we came from.
                    if (neighbourRef == 0 || neighbourRef == parentRef)
                    {
                        continue;
                    }

                    // Get neighbour poly and tile.
                    // The API input has been checked already, skip checking internal data.
                    m_nav.GetTileAndPolyByRefUnsafe(neighbourRef, out var neighbourTile, out var neighbourPoly);

                    if (!m_query.filter.PassFilter(neighbourRef, neighbourTile, neighbourPoly))
                    {
                        continue;
                    }

                    // get the neighbor node
                    DtNode neighbourNode = m_nodePool.GetNode(neighbourRef, 0);
                    if (null == neighbourNode)
                    {
                        m_query.status |= DtStatus.DT_OUT_OF_NODES;
                        continue;
                    }

                    // do not expand to nodes that were already visited from the
                    // same parent
                    if (neighbourNode.pidx != 0 && neighbourNode.pidx == bestNode.pidx)
                    {
                        continue;
                    }

                    // If the node is visited the first time, calculate node position.
                    if (neighbourNode.flags == 0)
                    {
                        GetEdgeMidPoint(bestRef, bestPoly, bestTile,
                            neighbourRef, neighbourPoly, neighbourTile,
                            ref neighbourNode.pos);
                    }

                    // Calculate cost and heuristic.
                    float cost = 0;
                    float heuristic = 0;

                    // raycast parent
                    bool foundShortCut = false;
                    rayHit.pathCost = rayHit.t = 0;
                    if (tryLOS)
                    {
                        Raycast(parentRef, parentNode.pos, neighbourNode.pos, m_query.filter, DtRaycastOptions.DT_RAYCAST_USE_COSTS, ref rayHit, grandpaRef);
                        foundShortCut = rayHit.t >= 1.0f;
                    }

                    // update move cost
                    if (foundShortCut)
                    {
                        // shortcut found using raycast. Using shorter cost instead
                        cost = parentNode.cost + rayHit.pathCost;
                    }
                    else
                    {
                        // No shortcut found.
                        float curCost = m_query.filter.GetCost(bestNode.pos, neighbourNode.pos,
                            parentRef, parentTile, parentPoly,
                            bestRef, bestTile, bestPoly,
                            neighbourRef, neighbourTile, neighbourPoly);
                        cost = bestNode.cost + curCost;
                    }

                    // Special case for last node.
                    if (neighbourRef == m_query.endRef)
                    {
                        float endCost = m_query.filter.GetCost(neighbourNode.pos, m_query.endPos,
                            bestRef, bestTile, bestPoly,
                            neighbourRef, neighbourTile, neighbourPoly,
                            0, null, null);

                        cost = cost + endCost;
                        heuristic = 0;
                    }
                    else
                    {
                        heuristic = RcVec3f.Distance(neighbourNode.pos, m_query.endPos) * DtDefaultQueryHeuristic.H_SCALE;
                    }

                    float total = cost + heuristic;

                    // The node is already in open list and the new result is worse, skip.
                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_OPEN) != 0 && total >= neighbourNode.total)
                    {
                        continue;
                    }

                    // The node is already visited and process, and the new result is worse, skip.
                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_CLOSED) != 0 && total >= neighbourNode.total)
                    {
                        continue;
                    }

                    // Add or update the node.
                    neighbourNode.pidx = foundShortCut ? bestNode.pidx : m_nodePool.GetNodeIdx(bestNode);
                    neighbourNode.id = neighbourRef;
                    neighbourNode.flags = (neighbourNode.flags & ~(DtNodeFlags.DT_NODE_CLOSED | DtNodeFlags.DT_NODE_PARENT_DETACHED));
                    neighbourNode.cost = cost;
                    neighbourNode.total = total;
                    
                    if (foundShortCut)
                        neighbourNode.flags = (neighbourNode.flags | DtNodeFlags.DT_NODE_PARENT_DETACHED);


                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_OPEN) != 0)
                    {
                        // Already in open, update node location.
                        m_openList.Modify(neighbourNode);
                    }
                    else
                    {
                        // Put the node in open list.
                        neighbourNode.flags |= DtNodeFlags.DT_NODE_OPEN;
                        m_openList.Push(neighbourNode);
                    }

                    // Update nearest node to target so far.
                    if (heuristic < m_query.lastBestNodeCost)
                    {
                        m_query.lastBestNodeCost = heuristic;
                        m_query.lastBestNode = neighbourNode;
                    }
                }
            }

            // Exhausted all nodes, but could not find path.
            if (m_openList.IsEmpty())
            {
                var details = m_query.status & DtStatus.DT_STATUS_DETAIL_MASK;
                m_query.status = DtStatus.DT_SUCCESS | details;
            }

            doneIters = iter;
            return m_query.status;
        }

        /// Finalizes and returns the results of a sliced path query.
        ///  @param[out]	path		An ordered list of polygon references representing the path. (Start to end.) 
        ///  							[(polyRef) * @p pathCount]
        ///  @param[out]	pathCount	The number of polygons returned in the @p path array.
        ///  @param[in]		maxPath		The max number of polygons the path array can hold. [Limit: >= 1]
        /// @returns The status flags for the query.
        public virtual DtStatus FinalizeSlicedFindPath(Span<long> path, out int pathCount, int maxPath)
        {
            pathCount = 0;

            if (path.IsEmpty || maxPath <= 0)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            if (m_query.status.Failed())
            {
                // Reset query.
                m_query = new DtQueryData();
                return DtStatus.DT_FAILURE;
            }

            int n = 0;

            if (m_query.startRef == m_query.endRef)
            {
                // Special case: the search starts and ends at same poly.
                path[n++] = m_query.startRef;
            }
            else
            {
                // Reverse the path.
                RcDebug.Assert(null != m_query.lastBestNode);
		
                if (m_query.lastBestNode.id != m_query.endRef)
                    m_query.status |= DtStatus.DT_PARTIAL_RESULT;

                DtNode prev = null;
                DtNode node = m_query.lastBestNode;
                int prevRay = 0;
                do
                {
                    DtNode next = m_nodePool.GetNodeAtIdx(node.pidx);
                    node.pidx = m_nodePool.GetNodeIdx(prev);
                    prev = node;
                    int nextRay = node.flags & DtNodeFlags.DT_NODE_PARENT_DETACHED; // keep track of whether parent is not adjacent (i.e. due to raycast shortcut)
                    node.flags = (node.flags & ~DtNodeFlags.DT_NODE_PARENT_DETACHED) | prevRay; // and store it in the reversed path's node
                    prevRay = nextRay;
                    node = next;
                }
                while (null != node);
		
                // Store path
                node = prev;
                do
                {
                    DtNode next = m_nodePool.GetNodeAtIdx(node.pidx);
                    DtStatus status = DtStatus.DT_STATUS_NOTHING;
                    if ((node.flags & DtNodeFlags.DT_NODE_PARENT_DETACHED) != 0)
                    {
                        float t;
                        int m;
                        status = Raycast(node.id, node.pos, next.pos, m_query.filter, out t, out var normal, path.Slice(n), out m, maxPath-n);
                        n += m;
                        // raycast ends on poly boundary and the path might include the next poly boundary.
                        if (path[n-1] == next.id)
                            n--; // remove to avoid duplicates
                    }
                    else
                    {
                        path[n++] = node.id;
                        if (n >= maxPath)
                            status = DtStatus.DT_BUFFER_TOO_SMALL;
                    }

                    if (status.Has(DtStatus.DT_STATUS_DETAIL_MASK))
                    {
                        m_query.status |= status & DtStatus.DT_STATUS_DETAIL_MASK;
                        break;
                    }
                    node = next;
                }
                while (null != node);
            }
	
            DtStatus details = m_query.status & DtStatus.DT_STATUS_DETAIL_MASK;

            // Reset query.
            m_query = new DtQueryData();
	
            pathCount = n;
	
            return DtStatus.DT_SUCCESS | details;
        }

        /// Finalizes and returns the results of an incomplete sliced path query, returning the path to the furthest
        /// polygon on the existing path that was visited during the search.
        ///  @param[in]		existing		An array of polygon references for the existing path.
        ///  @param[in]		existingSize	The number of polygon in the @p existing array.
        ///  @param[out]	path			An ordered list of polygon references representing the path. (Start to end.) 
        ///  								[(polyRef) * @p pathCount]
        ///  @param[out]	pathCount		The number of polygons returned in the @p path array.
        ///  @param[in]		maxPath			The max number of polygons the @p path array can hold. [Limit: >= 1]
        /// @returns The status flags for the query.
        public virtual DtStatus FinalizeSlicedFindPathPartial(Span<long> existing, int existingSize, 
            Span<long> path, out int pathCount, int maxPath)
        {
            pathCount = 0;

            if (existing.IsEmpty || existingSize <= 0 || path.IsEmpty || 0 >= maxPath)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            if (m_query.status.Failed())
            {
                // Reset query.
                m_query = new DtQueryData();
                return DtStatus.DT_FAILURE;
            }
            
            int n = 0;

            if (m_query.startRef == m_query.endRef)
            {
                // Special case: the search starts and ends at same poly.
                path[n++] = m_query.startRef;
            }
            else
            {
                // Find furthest existing node that was visited.
                DtNode prev = null;
                DtNode node = null;
                for (int i = existingSize-1; i >= 0; --i)
                {
                    node = m_nodePool.FindNode(existing[i]);
                    if (null != node)
                        break;
                }
		
                if (null == node)
                {
                    m_query.status |= DtStatus.DT_PARTIAL_RESULT;
                    RcDebug.Assert(null != m_query.lastBestNode);
                    node = m_query.lastBestNode;
                }
		
                // Reverse the path.
                int prevRay = 0;
                do
                {
                    DtNode next = m_nodePool.GetNodeAtIdx(node.pidx);
                    node.pidx = m_nodePool.GetNodeIdx(prev);
                    prev = node;
                    int nextRay = node.flags & DtNodeFlags.DT_NODE_PARENT_DETACHED; // keep track of whether parent is not adjacent (i.e. due to raycast shortcut)
                    node.flags = (node.flags & ~DtNodeFlags.DT_NODE_PARENT_DETACHED) | prevRay; // and store it in the reversed path's node
                    prevRay = nextRay;
                    node = next;
                }
                while (null != node);
		
                // Store path
                node = prev;
                do
                {
                    DtNode next = m_nodePool.GetNodeAtIdx(node.pidx);
                    DtStatus status = DtStatus.DT_STATUS_NOTHING;
                    if ((node.flags & DtNodeFlags.DT_NODE_PARENT_DETACHED) != 0)
                    {
                        float t;
                        int m;
                        status = Raycast(node.id, node.pos, next.pos, m_query.filter, out t, out var normal, path.Slice(n), out m, maxPath-n);
                        n += m;
                        // raycast ends on poly boundary and the path might include the next poly boundary.
                        if (path[n-1] == next.id)
                            n--; // remove to avoid duplicates
                    }
                    else
                    {
                        path[n++] = node.id;
                        if (n >= maxPath)
                            status = DtStatus.DT_BUFFER_TOO_SMALL;
                    }

                    if (status.Has(DtStatus.DT_STATUS_DETAIL_MASK))
                    {
                        m_query.status |= status & DtStatus.DT_STATUS_DETAIL_MASK;
                        break;
                    }
                    node = next;
                }
                while (null != node);
            }
	
            DtStatus details = m_query.status & DtStatus.DT_STATUS_DETAIL_MASK;

            // Reset query.
            m_query = new DtQueryData();
	
            pathCount = n;
	
            return DtStatus.DT_SUCCESS | details;
        }

        protected DtStatus AppendVertex(RcVec3f pos, byte flags, long refs, Span<DtStraightPath> straightPath, ref int straightPathCount, int maxStraightPath)
        {
            if (straightPathCount > 0 && RcVec.Equal(straightPath[straightPathCount - 1].pos, pos))
            {
                // The vertices are equal, update flags and poly.
                straightPath[straightPathCount - 1] = new DtStraightPath(straightPath[straightPathCount - 1].pos, flags, refs);
            }
            else
            {
                // Append new vertex.
                straightPath[straightPathCount] = new DtStraightPath(pos, flags, refs);
                straightPathCount++;

                // If there is no space to append more vertices, return.
                if (straightPathCount >= maxStraightPath)
                {
                    return DtStatus.DT_SUCCESS | DtStatus.DT_BUFFER_TOO_SMALL;
                }

                // If reached end of path, return.
                if (flags == DtStraightPathFlags.DT_STRAIGHTPATH_END)
                {
                    return DtStatus.DT_SUCCESS;
                }
            }

            return DtStatus.DT_IN_PROGRESS;
        }

        protected DtStatus AppendPortals(int startIdx, int endIdx, RcVec3f endPos, Span<long> path,
            Span<DtStraightPath> straightPath, ref int straightPathCount, int maxStraightPath, int options)
        {
            var startPos = straightPath[straightPathCount - 1].pos;
            // Append or update last vertex
            DtStatus stat;
            for (int i = startIdx; i < endIdx; i++)
            {
                // Calculate portal
                long from = path[i];
                var status = m_nav.GetTileAndPolyByRef(from, out var fromTile, out var fromPoly);
                if (status.Failed())
                {
                    return DtStatus.DT_FAILURE;
                }

                long to = path[i + 1];
                status = m_nav.GetTileAndPolyByRef(to, out var toTile, out var toPoly);
                if (status.Failed())
                {
                    return DtStatus.DT_FAILURE;
                }

                var ppStatus = GetPortalPoints(from, fromPoly, fromTile, to, toPoly, toTile, out var left, out var right);
                if (ppStatus.Failed())
                {
                    break;
                }

                if ((options & DtStraightPathOptions.DT_STRAIGHTPATH_AREA_CROSSINGS) != 0)
                {
                    // Skip intersection if only area crossings are requested.
                    if (fromPoly.GetArea() == toPoly.GetArea())
                    {
                        continue;
                    }
                }

                // Append intersection
                if (DtUtils.IntersectSegSeg2D(startPos, endPos, left, right, out var _, out var t))
                {
                    var pt = RcVec3f.Lerp(left, right, t);
                    stat = AppendVertex(pt, 0, path[i + 1], straightPath, ref straightPathCount, maxStraightPath);
                    if (!stat.InProgress())
                    {
                        return stat;
                    }
                }
            }

            return DtStatus.DT_IN_PROGRESS;
        }

        /// @par
        /// 
        /// This method peforms what is often called 'string pulling'.
        ///
        /// The start position is clamped to the first polygon in the path, and the 
        /// end position is clamped to the last. So the start and end positions should 
        /// normally be within or very near the first and last polygons respectively.
        ///
        /// The returned polygon references represent the reference id of the polygon 
        /// that is entered at the associated path position. The reference id associated 
        /// with the end point will always be zero.  This allows, for example, matching 
        /// off-mesh link points to their representative polygons.
        ///
        /// If the provided result buffers are too small for the entire result set, 
        /// they will be filled as far as possible from the start toward the end 
        /// position.
        ///
        /// Finds the straight path from the start to the end position within the polygon corridor.
        ///  @param[in]		startPos			Path start position. [(x, y, z)]
        ///  @param[in]		endPos				Path end position. [(x, y, z)]
        ///  @param[in]		path				An array of polygon references that represent the path corridor.
        ///  @param[in]		pathSize			The number of polygons in the @p path array.
        ///  @param[out]	straightPath		Points describing the straight path. [(x, y, z) * @p straightPathCount].
        ///  @param[in]		maxStraightPath		The maximum number of points the straight path arrays can hold.  [Limit: > 0]
        ///  @param[in]		options				Query options. (see: #dtStraightPathOptions)
        /// @returns The status flags for the query.
        public virtual DtStatus FindStraightPath(RcVec3f startPos, RcVec3f endPos,
            Span<long> path, int pathSize,
            Span<DtStraightPath> straightPath, out int straightPathCount, int maxStraightPath,
            int options)
        {
            straightPathCount = 0;

            if (!startPos.IsFinite() || !endPos.IsFinite() ||
                straightPath.IsEmpty ||
                path.IsEmpty || pathSize <= 0 || path[0] == 0
                || maxStraightPath <= 0)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            DtStatus stat = DtStatus.DT_STATUS_NOTHING;

            // TODO: Should this be callers responsibility?
            var closestStartPosRes = ClosestPointOnPolyBoundary(path[0], startPos, out var closestStartPos);
            if (closestStartPosRes.Failed())
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            var closestEndPosRes = ClosestPointOnPolyBoundary(path[pathSize - 1], endPos, out var closestEndPos);
            if (closestEndPosRes.Failed())
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            // Add start point.
            stat = AppendVertex(closestStartPos, DtStraightPathFlags.DT_STRAIGHTPATH_START, path[0], straightPath, ref straightPathCount, maxStraightPath);
            if (!stat.InProgress())
            {
                return stat;
            }

            if (pathSize > 1)
            {
                RcVec3f portalApex = closestStartPos;
                RcVec3f portalLeft = portalApex;
                RcVec3f portalRight = portalApex;
                int apexIndex = 0;
                int leftIndex = 0;
                int rightIndex = 0;

                int leftPolyType = 0;
                int rightPolyType = 0;

                long leftPolyRef = path[0];
                long rightPolyRef = path[0];

                for (int i = 0; i < pathSize; ++i)
                {
                    RcVec3f left;
                    RcVec3f right;
                    int toType;

                    if (i + 1 < pathSize)
                    {
                        int fromType; // // fromType is ignored.

                        // Next portal.
                        var ppStatus = GetPortalPoints(path[i], path[i + 1], out left, out right, out fromType, out toType);
                        if (ppStatus.Failed())
                        {
                            // Failed to get portal points, in practice this means that path[i+1] is invalid polygon.
                            // Clamp the end point to path[i], and return the path so far.
                            var cpStatus = ClosestPointOnPolyBoundary(path[i], endPos, out closestEndPos);
                            if (cpStatus.Failed())
                            {
                                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
                            }

                            // Append portals along the current straight path segment.
                            if ((options & (DtStraightPathOptions.DT_STRAIGHTPATH_AREA_CROSSINGS | DtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS)) != 0)
                            {
                                // Ignore status return value as we're just about to return anyway.
                                AppendPortals(apexIndex, i, closestEndPos, path, straightPath, ref straightPathCount, maxStraightPath, options);
                            }

                            // Ignore status return value as we're just about to return anyway.
                            AppendVertex(closestEndPos, 0, path[i], straightPath, ref straightPathCount, maxStraightPath);

                            return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM | (straightPathCount >= maxStraightPath ? DtStatus.DT_BUFFER_TOO_SMALL : DtStatus.DT_STATUS_NOTHING);
                        }

                        // If starting really close the portal, advance.
                        if (i == 0)
                        {
                            var distSqr = DtUtils.DistancePtSegSqr2D(portalApex, left, right, out var t);
                            if (distSqr < RcMath.Sqr(0.001f))
                            {
                                continue;
                            }
                        }
                    }
                    else
                    {
                        // End of the path.
                        left = closestEndPos;
                        right = closestEndPos;
                        toType = DtPolyTypes.DT_POLYTYPE_GROUND;
                    }

                    // Right vertex.
                    if (DtUtils.TriArea2D(portalApex, portalRight, right) <= 0.0f)
                    {
                        if (RcVec.Equal(portalApex, portalRight) || DtUtils.TriArea2D(portalApex, portalLeft, right) > 0.0f)
                        {
                            portalRight = right;
                            rightPolyRef = (i + 1 < pathSize) ? path[i + 1] : 0;
                            rightPolyType = toType;
                            rightIndex = i;
                        }
                        else
                        {
                            // Append portals along the current straight path segment.
                            if ((options & (DtStraightPathOptions.DT_STRAIGHTPATH_AREA_CROSSINGS | DtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS)) != 0)
                            {
                                stat = AppendPortals(apexIndex, leftIndex, portalLeft, path, straightPath, ref straightPathCount, maxStraightPath, options);
                                if (!stat.InProgress())
                                {
                                    return stat;
                                }
                            }

                            portalApex = portalLeft;
                            apexIndex = leftIndex;

                            byte flags = 0;
                            if (leftPolyRef == 0)
                            {
                                flags = DtStraightPathFlags.DT_STRAIGHTPATH_END;
                            }
                            else if (leftPolyType == DtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                            {
                                flags = DtStraightPathFlags.DT_STRAIGHTPATH_OFFMESH_CONNECTION;
                            }

                            long refs = leftPolyRef;

                            // Append or update vertex
                            stat = AppendVertex(portalApex, flags, refs, straightPath, ref straightPathCount, maxStraightPath);
                            if (!stat.InProgress())
                            {
                                return stat;
                            }

                            portalLeft = portalApex;
                            portalRight = portalApex;
                            leftIndex = apexIndex;
                            rightIndex = apexIndex;

                            // Restart
                            i = apexIndex;

                            continue;
                        }
                    }

                    // Left vertex.
                    if (DtUtils.TriArea2D(portalApex, portalLeft, left) >= 0.0f)
                    {
                        if (RcVec.Equal(portalApex, portalLeft) || DtUtils.TriArea2D(portalApex, portalRight, left) < 0.0f)
                        {
                            portalLeft = left;
                            leftPolyRef = (i + 1 < pathSize) ? path[i + 1] : 0;
                            leftPolyType = toType;
                            leftIndex = i;
                        }
                        else
                        {
                            // Append portals along the current straight path segment.
                            if ((options & (DtStraightPathOptions.DT_STRAIGHTPATH_AREA_CROSSINGS | DtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS)) != 0)
                            {
                                stat = AppendPortals(apexIndex, rightIndex, portalRight, path, straightPath, ref straightPathCount, maxStraightPath, options);
                                if (!stat.InProgress())
                                {
                                    return stat;
                                }
                            }

                            portalApex = portalRight;
                            apexIndex = rightIndex;

                            byte flags = 0;
                            if (rightPolyRef == 0)
                            {
                                flags = DtStraightPathFlags.DT_STRAIGHTPATH_END;
                            }
                            else if (rightPolyType == DtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                            {
                                flags = DtStraightPathFlags.DT_STRAIGHTPATH_OFFMESH_CONNECTION;
                            }

                            long refs = rightPolyRef;

                            // Append or update vertex
                            stat = AppendVertex(portalApex, flags, refs, straightPath, ref straightPathCount, maxStraightPath);
                            if (!stat.InProgress())
                            {
                                return stat;
                            }

                            portalLeft = portalApex;
                            portalRight = portalApex;
                            leftIndex = apexIndex;
                            rightIndex = apexIndex;

                            // Restart
                            i = apexIndex;

                            continue;
                        }
                    }
                }

                // Append portals along the current straight path segment.
                if ((options & (DtStraightPathOptions.DT_STRAIGHTPATH_AREA_CROSSINGS | DtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS)) != 0)
                {
                    stat = AppendPortals(apexIndex, pathSize - 1, closestEndPos, path, straightPath, ref straightPathCount, maxStraightPath, options);
                    if (!stat.InProgress())
                    {
                        return stat;
                    }
                }
            }

            // Ignore status return value as we're just about to return anyway.
            AppendVertex(closestEndPos, DtStraightPathFlags.DT_STRAIGHTPATH_END, 0, straightPath, ref straightPathCount, maxStraightPath);
            return DtStatus.DT_SUCCESS | (straightPathCount >= maxStraightPath ? DtStatus.DT_BUFFER_TOO_SMALL : DtStatus.DT_STATUS_NOTHING);
        }

        /// @par
        ///
        /// This method is optimized for small delta movement and a small number of 
        /// polygons. If used for too great a distance, the result set will form an 
        /// incomplete path.
        ///
        /// @p resultPos will equal the @p endPos if the end is reached. 
        /// Otherwise the closest reachable position will be returned.
        /// 
        /// @p resultPos is not projected onto the surface of the navigation 
        /// mesh. Use #getPolyHeight if this is needed.
        ///
        /// This method treats the end position in the same manner as 
        /// the #raycast method. (As a 2D point.) See that method's documentation 
        /// for details.
        /// 
        /// If the @p visited array is too small to hold the entire result set, it will 
        /// be filled as far as possible from the start position toward the end 
        /// position.
        ///
        /// Moves from the start to the end position constrained to the navigation mesh.
        ///  @param[in]		startRef		The reference id of the start polygon.
        ///  @param[in]		startPos		A position of the mover within the start polygon. [(x, y, x)]
        ///  @param[in]		endPos			The desired end position of the mover. [(x, y, z)]
        ///  @param[in]		filter			The polygon filter to apply to the query.
        ///  @param[out]	resultPos		The result position of the mover. [(x, y, z)]
        ///  @param[out]	visited			The reference ids of the polygons visited during the move.
        ///  @param[out]	visitedCount	The number of polygons visited during the move.
        ///  @param[in]		maxVisitedSize	The maximum number of polygons the @p visited array can hold.
        /// @returns The status flags for the query.
        public DtStatus MoveAlongSurface(long startRef, RcVec3f startPos, RcVec3f endPos,
            IDtQueryFilter filter,
            out RcVec3f resultPos, Span<long> visited, out int visitedCount, int maxVisitedSize)
        {
            resultPos = RcVec3f.Zero;

            visitedCount = 0;

            // Validate input
            if (!m_nav.IsValidPolyRef(startRef) || !startPos.IsFinite()
                                                || !endPos.IsFinite() || null == filter)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            DtStatus status = DtStatus.DT_SUCCESS;

            m_tinyNodePool.Clear();

            DtNode startNode = m_tinyNodePool.GetNode(startRef);
            startNode.pidx = 0;
            startNode.cost = 0;
            startNode.total = 0;
            startNode.id = startRef;
            startNode.flags = DtNodeFlags.DT_NODE_CLOSED;
            LinkedList<DtNode> stack = new LinkedList<DtNode>();
            stack.AddLast(startNode);

            RcVec3f bestPos = new RcVec3f();
            float bestDist = float.MaxValue;
            DtNode bestNode = null;
            bestPos = startPos;

            // Search constraints
            var searchPos = RcVec3f.Lerp(startPos, endPos, 0.5f);
            float searchRadSqr = RcMath.Sqr(RcVec3f.Distance(startPos, endPos) / 2.0f + 0.001f);

            Span<float> verts = stackalloc float[m_nav.GetMaxVertsPerPoly() * 3];

            const int MAX_NEIS = 8;
            Span<long> neis = stackalloc long[MAX_NEIS];

            while (0 < stack.Count)
            {
                // Pop front.
                DtNode curNode = stack.First?.Value;
                stack.RemoveFirst();

                // Get poly and tile.
                // The API input has been checked already, skip checking internal data.
                long curRef = curNode.id;
                m_nav.GetTileAndPolyByRefUnsafe(curRef, out var curTile, out var curPoly);

                // Collect vertices.
                int nverts = curPoly.vertCount;
                for (int i = 0; i < nverts; ++i)
                {
                    RcSpans.Copy(curTile.data.verts, curPoly.verts[i] * 3, verts, i * 3, 3);
                }

                // If target is inside the poly, stop search.
                if (DtUtils.PointInPolygon(endPos, verts, nverts))
                {
                    bestNode = curNode;
                    bestPos = endPos;
                    break;
                }

                // Find wall edges and find nearest point inside the walls.
                for (int i = 0, j = curPoly.vertCount - 1; i < curPoly.vertCount; j = i++)
                {
                    // Find links to neighbours.
                    int nneis = 0;

                    if ((curPoly.neis[j] & DT_EXT_LINK) != 0)
                    {
                        // Tile border.
                        for (int k = curPoly.firstLink; k != DT_NULL_LINK; k = curTile.links[k].next)
                        {
                            DtLink link = curTile.links[k];
                            if (link.edge == j)
                            {
                                if (link.refs != 0)
                                {
                                    m_nav.GetTileAndPolyByRefUnsafe(link.refs, out var neiTile, out var neiPoly);
                                    if (filter.PassFilter(link.refs, neiTile, neiPoly))
                                    {
                                        if (nneis < MAX_NEIS)
                                        {
                                            neis[nneis++] = link.refs;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (curPoly.neis[j] != 0)
                    {
                        int idx = curPoly.neis[j] - 1;
                        long refs = m_nav.GetPolyRefBase(curTile) | (long)idx;
                        if (filter.PassFilter(refs, curTile, curTile.data.polys[idx]))
                        {
                            // Internal edge, encode id.
                            neis[nneis++] = refs;
                        }
                    }

                    if (nneis == 0)
                    {
                        // Wall edge, calc distance.
                        int vj = j * 3;
                        int vi = i * 3;
                        var distSqr = DtUtils.DistancePtSegSqr2D(endPos, verts, vj, vi, out var tseg);
                        if (distSqr < bestDist)
                        {
                            // Update nearest distance.
                            bestPos = RcVec.Lerp(verts, vj, vi, tseg);
                            bestDist = distSqr;
                            bestNode = curNode;
                        }
                    }
                    else
                    {
                        for (int k = 0; k < nneis; ++k)
                        {
                            DtNode neighbourNode = m_tinyNodePool.GetNode(neis[k]);
                            // Skip if already visited.
                            if ((neighbourNode.flags & DtNodeFlags.DT_NODE_CLOSED) != 0)
                            {
                                continue;
                            }

                            // Skip the link if it is too far from search constraint.
                            // TODO: Maybe should use GetPortalPoints(), but this one is way faster.
                            int vj = j * 3;
                            int vi = i * 3;
                            var distSqr = DtUtils.DistancePtSegSqr2D(searchPos, verts, vj, vi, out var _);
                            if (distSqr > searchRadSqr)
                            {
                                continue;
                            }

                            // Mark as the node as visited and push to queue.
                            neighbourNode.pidx = m_tinyNodePool.GetNodeIdx(curNode);
                            neighbourNode.flags |= DtNodeFlags.DT_NODE_CLOSED;
                            stack.AddLast(neighbourNode);
                        }
                    }
                }
            }

            int n = 0;
            if (bestNode != null)
            {
                // Reverse the path.
                DtNode prev = null;
                DtNode node = bestNode;
                do
                {
                    DtNode next = m_tinyNodePool.GetNodeAtIdx(node.pidx);
                    node.pidx = m_tinyNodePool.GetNodeIdx(prev);
                    prev = node;
                    node = next;
                } while (node != null);

                // Store result
                node = prev;
                do
                {
                    visited[n++] = node.id;
                    if (n >= maxVisitedSize)
                    {
                        status |= DtStatus.DT_BUFFER_TOO_SMALL;
                        ;
                        break;
                    }

                    node = m_tinyNodePool.GetNodeAtIdx(node.pidx);
                } while (node != null);
            }

            resultPos = bestPos;
            visitedCount = n;

            return status;
        }

        protected DtStatus GetPortalPoints(long from, long to, out RcVec3f left, out RcVec3f right, out int fromType, out int toType)
        {
            left = RcVec3f.Zero;
            right = RcVec3f.Zero;
            fromType = 0;
            toType = 0;

            var status = m_nav.GetTileAndPolyByRef(from, out var fromTile, out var fromPoly);
            if (status.Failed())
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            fromType = fromPoly.GetPolyType();

            status = m_nav.GetTileAndPolyByRef(to, out var toTile, out var toPoly);
            if (status.Failed())
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            toType = toPoly.GetPolyType();

            return GetPortalPoints(from, fromPoly, fromTile, to, toPoly, toTile, out left, out right);
        }

        // Returns portal points between two polygons.
        protected DtStatus GetPortalPoints(long from, DtPoly fromPoly, DtMeshTile fromTile,
            long to, DtPoly toPoly, DtMeshTile toTile,
            out RcVec3f left, out RcVec3f right)
        {
            left = RcVec3f.Zero;
            right = RcVec3f.Zero;

            // Find the link that points to the 'to' polygon.
            DtLink link = null;
            for (int i = fromPoly.firstLink; i != DT_NULL_LINK; i = fromTile.links[i].next)
            {
                if (fromTile.links[i].refs == to)
                {
                    link = fromTile.links[i];
                    break;
                }
            }

            if (link == null)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            // Handle off-mesh connections.
            if (fromPoly.GetPolyType() == DtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
            {
                // Find link that points to first vertex.
                for (int i = fromPoly.firstLink; i != DT_NULL_LINK; i = fromTile.links[i].next)
                {
                    if (fromTile.links[i].refs == to)
                    {
                        int v = fromTile.links[i].edge;
                        left.X = fromTile.data.verts[fromPoly.verts[v] * 3];
                        left.Y = fromTile.data.verts[fromPoly.verts[v] * 3 + 1];
                        left.Z = fromTile.data.verts[fromPoly.verts[v] * 3 + 2];

                        right.X = fromTile.data.verts[fromPoly.verts[v] * 3];
                        right.Y = fromTile.data.verts[fromPoly.verts[v] * 3 + 1];
                        right.Z = fromTile.data.verts[fromPoly.verts[v] * 3 + 2];

                        return DtStatus.DT_SUCCESS;
                    }
                }

                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            if (toPoly.GetPolyType() == DtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
            {
                for (int i = toPoly.firstLink; i != DT_NULL_LINK; i = toTile.links[i].next)
                {
                    if (toTile.links[i].refs == from)
                    {
                        int v = toTile.links[i].edge;
                        left.X = toTile.data.verts[toPoly.verts[v] * 3];
                        left.Y = toTile.data.verts[toPoly.verts[v] * 3 + 1];
                        left.Z = toTile.data.verts[toPoly.verts[v] * 3 + 2];

                        right.X = toTile.data.verts[toPoly.verts[v] * 3];
                        right.Y = toTile.data.verts[toPoly.verts[v] * 3 + 1];
                        right.Z = toTile.data.verts[toPoly.verts[v] * 3 + 2];

                        return DtStatus.DT_SUCCESS;
                    }
                }

                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            // Find portal vertices.
            int v0 = fromPoly.verts[link.edge];
            int v1 = fromPoly.verts[(link.edge + 1) % fromPoly.vertCount];
            left.X = fromTile.data.verts[v0 * 3];
            left.Y = fromTile.data.verts[v0 * 3 + 1];
            left.Z = fromTile.data.verts[v0 * 3 + 2];

            right.X = fromTile.data.verts[v1 * 3];
            right.Y = fromTile.data.verts[v1 * 3 + 1];
            right.Z = fromTile.data.verts[v1 * 3 + 2];

            // If the link is at tile boundary, dtClamp the vertices to
            // the link width.
            if (link.side != 0xff)
            {
                // Unpack portal limits.
                if (link.bmin != 0 || link.bmax != 255)
                {
                    float s = 1.0f / 255.0f;
                    float tmin = link.bmin * s;
                    float tmax = link.bmax * s;
                    left = RcVec.Lerp(fromTile.data.verts, v0 * 3, v1 * 3, tmin);
                    right = RcVec.Lerp(fromTile.data.verts, v0 * 3, v1 * 3, tmax);
                }
            }

            return DtStatus.DT_SUCCESS;
        }

        protected DtStatus GetEdgeMidPoint(long from, DtPoly fromPoly, DtMeshTile fromTile, long to,
            DtPoly toPoly, DtMeshTile toTile, ref RcVec3f mid)
        {
            var ppStatus = GetPortalPoints(from, fromPoly, fromTile, to, toPoly, toTile, out var left, out var right);
            if (ppStatus.Failed())
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            mid.X = (left.X + right.X) * 0.5f;
            mid.Y = (left.Y + right.Y) * 0.5f;
            mid.Z = (left.Z + right.Z) * 0.5f;

            return DtStatus.DT_SUCCESS;
        }

        protected DtStatus GetEdgeIntersectionPoint(RcVec3f fromPos, long from, DtPoly fromPoly, DtMeshTile fromTile,
            RcVec3f toPos, long to, DtPoly toPoly, DtMeshTile toTile,
            ref RcVec3f pt)
        {
            var ppStatus = GetPortalPoints(from, fromPoly, fromTile, to, toPoly, toTile, out var left, out var right);
            if (ppStatus.Failed())
            {
                return DtStatus.DT_FAILURE;
            }

            float t = 0.5f;
            if (DtUtils.IntersectSegSeg2D(fromPos, toPos, left, right, out var _, out var t2))
            {
                t = Math.Clamp(t2, 0.1f, 0.9f);
            }

            pt = RcVec3f.Lerp(left, right, t);
            return DtStatus.DT_SUCCESS;
        }

        /// @par
        ///
        /// This method is meant to be used for quick, short distance checks.
        ///
        /// If the path array is too small to hold the result, it will be filled as 
        /// far as possible from the start postion toward the end position.
        ///
        /// <b>Using the Hit Parameter (t)</b>
        /// 
        /// If the hit parameter is a very high value (FLT_MAX), then the ray has hit 
        /// the end position. In this case the path represents a valid corridor to the 
        /// end position and the value of @p hitNormal is undefined.
        ///
        /// If the hit parameter is zero, then the start position is on the wall that 
        /// was hit and the value of @p hitNormal is undefined.
        ///
        /// If 0 < t < 1.0 then the following applies:
        ///
        /// @code
        /// distanceToHitBorder = distanceToEndPosition * t
        /// hitPoint = startPos + (endPos - startPos) * t
        /// @endcode
        ///
        /// <b>Use Case Restriction</b>
        ///
        /// The raycast ignores the y-value of the end position. (2D check.) This 
        /// places significant limits on how it can be used. For example:
        ///
        /// Consider a scene where there is a main floor with a second floor balcony 
        /// that hangs over the main floor. So the first floor mesh extends below the 
        /// balcony mesh. The start position is somewhere on the first floor. The end 
        /// position is on the balcony.
        ///
        /// The raycast will search toward the end position along the first floor mesh. 
        /// If it reaches the end position's xz-coordinates it will indicate FLT_MAX
        /// (no wall hit), meaning it reached the end position. This is one example of why
        /// this method is meant for short distance checks.
        ///
        public DtStatus Raycast(long startRef, RcVec3f startPos, RcVec3f endPos,
            IDtQueryFilter filter,
            out float t, out RcVec3f hitNormal, Span<long> path, out int pathCount, int maxPath)
        {
            DtRaycastHit hit = new DtRaycastHit();
            hit.path = path;
            hit.maxPath = maxPath;

            DtStatus status = Raycast(startRef, startPos, endPos, filter, 0, ref hit, 0);

            t = hit.t;
            hitNormal = hit.hitNormal;
            path = hit.path;
            pathCount = hit.pathCount;

            return status;
        }

        /// @par
        ///
        /// This method is meant to be used for quick, short distance checks.
        ///
        /// If the path array is too small to hold the result, it will be filled as 
        /// far as possible from the start postion toward the end position.
        ///
        /// <b>Using the Hit Parameter t of RaycastHit</b>
        /// 
        /// If the hit parameter is a very high value (FLT_MAX), then the ray has hit 
        /// the end position. In this case the path represents a valid corridor to the 
        /// end position and the value of @p hitNormal is undefined.
        ///
        /// If the hit parameter is zero, then the start position is on the wall that 
        /// was hit and the value of @p hitNormal is undefined.
        ///
        /// If 0 < t < 1.0 then the following applies:
        ///
        /// @code
        /// distanceToHitBorder = distanceToEndPosition * t
        /// hitPoint = startPos + (endPos - startPos) * t
        /// @endcode
        ///
        /// <b>Use Case Restriction</b>
        ///
        /// The raycast ignores the y-value of the end position. (2D check.) This 
        /// places significant limits on how it can be used. For example:
        ///
        /// Consider a scene where there is a main floor with a second floor balcony 
        /// that hangs over the main floor. So the first floor mesh extends below the 
        /// balcony mesh. The start position is somewhere on the first floor. The end 
        /// position is on the balcony.
        ///
        /// The raycast will search toward the end position along the first floor mesh. 
        /// If it reaches the end position's xz-coordinates it will indicate FLT_MAX
        /// (no wall hit), meaning it reached the end position. This is one example of why
        /// this method is meant for short distance checks.
        ///
        /// Casts a 'walkability' ray along the surface of the navigation mesh from 
        /// the start position toward the end position.
        ///  @param[in]		startRef	The reference id of the start polygon.
        ///  @param[in]		startPos	A position within the start polygon representing 
        ///  							the start of the ray. [(x, y, z)]
        ///  @param[in]		endPos		The position to cast the ray toward. [(x, y, z)]
        ///  @param[in]		filter		The polygon filter to apply to the query.
        ///  @param[in]		options		govern how the raycast behaves. See dtRaycastOptions
        ///  @param[out]	hit			Pointer to a raycast hit structure which will be filled by the results.
        ///  @param[in]		prevRef		parent of start ref. Used during for cost calculation [opt]
        /// @returns The status flags for the query.
        public DtStatus Raycast(long startRef, RcVec3f startPos, RcVec3f endPos,
            IDtQueryFilter filter, int options,
            ref DtRaycastHit hit, long prevRef)
        {
            // Validate input
            if (!m_nav.IsValidPolyRef(startRef) || !startPos.IsFinite() || !endPos.IsFinite()
                || null == filter || (prevRef != 0 && !m_nav.IsValidPolyRef(prevRef)))
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            hit.t = 0;
            hit.pathCount = 0;
            hit.pathCost = 0;

            RcVec3f curPos = RcVec3f.Zero;
            RcVec3f lastPos = RcVec3f.Zero;
            Span<RcVec3f> verts = stackalloc RcVec3f[m_nav.GetMaxVertsPerPoly() + 1];
            int n = 0;

            curPos = startPos;
            RcVec3f dir = RcVec3f.Subtract(endPos, startPos);
            hit.hitNormal = RcVec3f.Zero;

            DtStatus status = DtStatus.DT_SUCCESS;

            DtMeshTile prevTile, tile, nextTile;
            DtPoly prevPoly, poly, nextPoly;

            // The API input has been checked already, skip checking internal data.
            long curRef = startRef;
            m_nav.GetTileAndPolyByRefUnsafe(curRef, out tile, out poly);
            nextTile = prevTile = tile;
            nextPoly = prevPoly = poly;
            if (prevRef != 0)
            {
                m_nav.GetTileAndPolyByRefUnsafe(prevRef, out prevTile, out prevPoly);
            }

            while (curRef != 0)
            {
                // Cast ray against current polygon.

                // Collect vertices.
                int nv = 0;
                for (int i = 0; i < poly.vertCount; ++i)
                {
                    verts[nv] = tile.data.verts.ToVec3(poly.verts[i] * 3);
                    nv++;
                }

                bool intersects = DtUtils.IntersectSegmentPoly2D(startPos, endPos, verts, nv, out var tmin, out var tmax, out var segMin, out var segMax);
                if (!intersects)
                {
                    // Could not hit the polygon, keep the old t and report hit.
                    hit.pathCount = n;
                    return DtStatus.DT_SUCCESS;
                }

                hit.hitEdgeIndex = segMax;

                // Keep track of furthest t so far.
                if (tmax > hit.t)
                {
                    hit.t = tmax;
                }

                // Store visited polygons.
                if (n < hit.maxPath)
                {
                    hit.path[n++] = curRef;
                }
                else
                {
                    status |= DtStatus.DT_BUFFER_TOO_SMALL;
                }

                // Ray end is completely inside the polygon.
                if (segMax == -1)
                {
                    hit.t = float.MaxValue;
                    hit.pathCount = n;

                    // add the cost
                    if ((options & DtRaycastOptions.DT_RAYCAST_USE_COSTS) != 0)
                    {
                        hit.pathCost += filter.GetCost(curPos, endPos, prevRef, prevTile, prevPoly, curRef, tile, poly,
                            curRef, tile, poly);
                    }

                    return status;
                }

                // Follow neighbours.
                long nextRef = 0;

                for (int i = poly.firstLink; i != DT_NULL_LINK; i = tile.links[i].next)
                {
                    DtLink link = tile.links[i];

                    // Find link which contains this edge.
                    if (link.edge != segMax)
                    {
                        continue;
                    }

                    // Get pointer to the next polygon.
                    m_nav.GetTileAndPolyByRefUnsafe(link.refs, out nextTile, out nextPoly);

                    // Skip off-mesh connections.
                    if (nextPoly.GetPolyType() == DtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                    {
                        continue;
                    }

                    // Skip links based on filter.
                    if (!filter.PassFilter(link.refs, nextTile, nextPoly))
                    {
                        continue;
                    }

                    // If the link is internal, just return the ref.
                    if (link.side == 0xff)
                    {
                        nextRef = link.refs;
                        break;
                    }

                    // If the link is at tile boundary,

                    // Check if the link spans the whole edge, and accept.
                    if (link.bmin == 0 && link.bmax == 255)
                    {
                        nextRef = link.refs;
                        break;
                    }

                    // Check for partial edge links.
                    int v0 = poly.verts[link.edge];
                    int v1 = poly.verts[(link.edge + 1) % poly.vertCount];
                    int left = v0 * 3;
                    int right = v1 * 3;

                    // Check that the intersection lies inside the link portal.
                    if (link.side == 0 || link.side == 4)
                    {
                        // Calculate link size.
                        const float s = 1.0f / 255.0f;
                        float lmin = tile.data.verts[left + 2]
                                     + (tile.data.verts[right + 2] - tile.data.verts[left + 2]) * (link.bmin * s);
                        float lmax = tile.data.verts[left + 2]
                                     + (tile.data.verts[right + 2] - tile.data.verts[left + 2]) * (link.bmax * s);
                        if (lmin > lmax)
                        {
                            (lmin, lmax) = (lmax, lmin);
                        }

                        // Find Z intersection.
                        float z = startPos.Z + (endPos.Z - startPos.Z) * tmax;
                        if (z >= lmin && z <= lmax)
                        {
                            nextRef = link.refs;
                            break;
                        }
                    }
                    else if (link.side == 2 || link.side == 6)
                    {
                        // Calculate link size.
                        const float s = 1.0f / 255.0f;
                        float lmin = tile.data.verts[left]
                                     + (tile.data.verts[right] - tile.data.verts[left]) * (link.bmin * s);
                        float lmax = tile.data.verts[left]
                                     + (tile.data.verts[right] - tile.data.verts[left]) * (link.bmax * s);
                        if (lmin > lmax)
                        {
                            (lmin, lmax) = (lmax, lmin);
                        }

                        // Find X intersection.
                        float x = startPos.X + (endPos.X - startPos.X) * tmax;
                        if (x >= lmin && x <= lmax)
                        {
                            nextRef = link.refs;
                            break;
                        }
                    }
                }

                // add the cost
                if ((options & DtRaycastOptions.DT_RAYCAST_USE_COSTS) != 0)
                {
                    // compute the intersection point at the furthest end of the polygon
                    // and correct the height (since the raycast moves in 2d)
                    lastPos = curPos;
                    curPos = RcVec.Mad(startPos, dir, hit.t);
                    var e1 = verts[segMax];
                    var e2 = verts[(segMax + 1) % nv];
                    var eDir = RcVec3f.Subtract(e2, e1);
                    var diff = RcVec3f.Subtract(curPos, e1);
                    float s = RcMath.Sqr(eDir.X) > RcMath.Sqr(eDir.Z) ? diff.X / eDir.X : diff.Z / eDir.Z;
                    curPos.Y = e1.Y + eDir.Y * s;

                    hit.pathCost += filter.GetCost(lastPos, curPos, prevRef, prevTile, prevPoly, curRef, tile, poly,
                        nextRef, nextTile, nextPoly);
                }

                if (nextRef == 0)
                {
                    // No neighbour, we hit a wall.

                    // Calculate hit normal.
                    int a = segMax;
                    int b = segMax + 1 < nv ? segMax + 1 : 0;
                    // int va = a * 3;
                    // int vb = b * 3;
                    float dx = verts[b].X - verts[a].X;
                    float dz = verts[b].Z - verts[a].X;
                    hit.hitNormal = RcVec3f.Normalize(new RcVec3f(dz, 0, -dx));

                    hit.pathCount = n;
                    return status;
                }

                // No hit, advance to neighbour polygon.
                prevRef = curRef;
                curRef = nextRef;
                prevTile = tile;
                tile = nextTile;
                prevPoly = poly;
                poly = nextPoly;


                if (status.Has(DtStatus.DT_BUFFER_TOO_SMALL))
                {
                    status |= DtStatus.DT_PARTIAL_RESULT;
                    break;
                }
            }

            hit.pathCount = n;

            return status;
        }

        /// @par
        ///
        /// At least one result array must be provided.
        ///
        /// The order of the result set is from least to highest cost to reach the polygon.
        ///
        /// A common use case for this method is to perform Dijkstra searches.
        /// Candidate polygons are found by searching the graph beginning at the start polygon.
        ///
        /// If a polygon is not found via the graph search, even if it intersects the
        /// search circle, it will not be included in the result set. For example:
        ///
        /// polyA is the start polygon.
        /// polyB shares an edge with polyA. (Is adjacent.)
        /// polyC shares an edge with polyB, but not with polyA
        /// Even if the search circle overlaps polyC, it will not be included in the
        /// result set unless polyB is also in the set.
        ///
        /// The value of the center point is used as the start position for cost
        /// calculations. It is not projected onto the surface of the mesh, so its
        /// y-value will effect the costs.
        ///
        /// Intersection tests occur in 2D. All polygons and the search circle are
        /// projected onto the xz-plane. So the y-value of the center point does not
        /// effect intersection tests.
        ///
        /// If the result arrays are to small to hold the entire result set, they will be
        /// filled to capacity.
        ///
        ///@}
        /// @name Dijkstra Search Functions
        /// @{ 
        /// Finds the polygons along the navigation graph that touch the specified circle.
        ///  @param[in]		startRef		The reference id of the polygon where the search starts.
        ///  @param[in]		centerPos		The center of the search circle. [(x, y, z)]
        ///  @param[in]		radius			The radius of the search circle.
        ///  @param[in]		filter			The polygon filter to apply to the query.
        ///  @param[out]	resultRef		The reference ids of the polygons touched by the circle. [opt]
        ///  @param[out]	resultParent	The reference ids of the parent polygons for each result. 
        ///  								Zero if a result polygon has no parent. [opt]
        ///  @param[out]	resultCost		The search cost from @p centerPos to the polygon. [opt]
        ///  @param[out]	resultCount		The number of polygons found. [opt]
        ///  @param[in]		maxResult		The maximum number of polygons the result arrays can hold.
        /// @returns The status flags for the query.
        /// @par
        ///
        /// At least one result array must be provided.
        ///
        /// The order of the result set is from least to highest cost to reach the polygon.
        ///
        /// A common use case for this method is to perform Dijkstra searches. 
        /// Candidate polygons are found by searching the graph beginning at the start polygon.
        ///
        /// If a polygon is not found via the graph search, even if it intersects the 
        /// search circle, it will not be included in the result set. For example:
        ///
        /// polyA is the start polygon.
        /// polyB shares an edge with polyA. (Is adjacent.)
        /// polyC shares an edge with polyB, but not with polyA
        /// Even if the search circle overlaps polyC, it will not be included in the 
        /// result set unless polyB is also in the set.
        /// 
        /// The value of the center point is used as the start position for cost 
        /// calculations. It is not projected onto the surface of the mesh, so its 
        /// y-value will effect the costs.
        ///
        /// Intersection tests occur in 2D. All polygons and the search circle are 
        /// projected onto the xz-plane. So the y-value of the center point does not 
        /// effect intersection tests.
        ///
        /// If the result arrays are to small to hold the entire result set, they will be 
        /// filled to capacity.
        ///
        public DtStatus FindPolysAroundCircle(long startRef, RcVec3f centerPos, float radius, 
            IDtQueryFilter filter,
            Span<long> resultRef, Span<long> resultParent, Span<float> resultCost,
            out int resultCount, int maxResult)
        {
            resultCount = 0;
            
            if (!m_nav.IsValidPolyRef(startRef) || 
                !centerPos.IsFinite() 
                || radius < 0 || !float.IsFinite(radius) 
                || null == filter || maxResult < 0)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            m_nodePool.Clear();
            m_openList.Clear();

            DtNode startNode = m_nodePool.GetNode(startRef);
            startNode.pos = centerPos;
            startNode.pidx = 0;
            startNode.cost = 0;
            startNode.total = 0;
            startNode.id = startRef;
            startNode.flags = DtNodeFlags.DT_NODE_OPEN;
            m_openList.Push(startNode);


            DtStatus status = DtStatus.DT_SUCCESS;
            
            int n = 0;
            
            float radiusSqr = RcMath.Sqr(radius);

            while (!m_openList.IsEmpty())
            {
                DtNode bestNode = m_openList.Pop();
                bestNode.flags &= ~DtNodeFlags.DT_NODE_OPEN;
                bestNode.flags |= DtNodeFlags.DT_NODE_CLOSED;

                // Get poly and tile.
                // The API input has been checked already, skip checking internal data.
                long bestRef = bestNode.id;
                m_nav.GetTileAndPolyByRefUnsafe(bestRef, out var bestTile, out var bestPoly);

                // Get parent poly and tile.
                long parentRef = 0;
                DtMeshTile parentTile = null;
                DtPoly parentPoly = null;
                if (bestNode.pidx != 0)
                {
                    parentRef = m_nodePool.GetNodeAtIdx(bestNode.pidx).id;
                }

                if (parentRef != 0)
                {
                    m_nav.GetTileAndPolyByRefUnsafe(parentRef, out parentTile, out parentPoly);
                }

                if (n < maxResult)
                {
                    resultRef[n] = bestRef;
                    resultParent[n] = parentRef;
                    resultCost[n] = bestNode.total;
                    n++;
                }
                else
                {
                    status |= DtStatus.DT_BUFFER_TOO_SMALL;
                }

                for (int i = bestPoly.firstLink; i != DT_NULL_LINK; i = bestTile.links[i].next)
                {
                    DtLink link = bestTile.links[i];
                    long neighbourRef = link.refs;
                    // Skip invalid neighbours and do not follow back to parent.
                    if (neighbourRef == 0 || neighbourRef == parentRef)
                    {
                        continue;
                    }

                    // Expand to neighbour
                    m_nav.GetTileAndPolyByRefUnsafe(neighbourRef, out var neighbourTile, out var neighbourPoly);

                    // Do not advance if the polygon is excluded by the filter.
                    if (!filter.PassFilter(neighbourRef, neighbourTile, neighbourPoly))
                    {
                        continue;
                    }

                    // Find edge and calc distance to the edge.
                    var ppStatus = GetPortalPoints(bestRef, bestPoly, bestTile, neighbourRef, neighbourPoly,
                        neighbourTile, out var va, out var vb);
                    if (ppStatus.Failed())
                    {
                        continue;
                    }

                    // If the circle is not touching the next polygon, skip it.
                    var distSqr = DtUtils.DistancePtSegSqr2D(centerPos, va, vb, out var tseg);
                    if (distSqr > radiusSqr)
                    {
                        continue;
                    }

                    DtNode neighbourNode = m_nodePool.GetNode(neighbourRef);

                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_CLOSED) != 0)
                    {
                        continue;
                    }

                    // Cost
                    if (neighbourNode.flags == 0)
                    {
                        neighbourNode.pos = RcVec3f.Lerp(va, vb, 0.5f);
                    }

                    float cost = filter.GetCost(bestNode.pos, neighbourNode.pos, parentRef, parentTile, parentPoly, bestRef,
                        bestTile, bestPoly, neighbourRef, neighbourTile, neighbourPoly);

                    float total = bestNode.total + cost;
                    // The node is already in open list and the new result is worse, skip.
                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_OPEN) != 0 && total >= neighbourNode.total)
                    {
                        continue;
                    }

                    neighbourNode.id = neighbourRef;
                    neighbourNode.pidx = m_nodePool.GetNodeIdx(bestNode);
                    neighbourNode.total = total;

                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_OPEN) != 0)
                    {
                        m_openList.Modify(neighbourNode);
                    }
                    else
                    {
                        neighbourNode.flags = DtNodeFlags.DT_NODE_OPEN;
                        m_openList.Push(neighbourNode);
                    }
                }
            }

            resultCount = n;

            return status;
        }

        /// @par
        ///
        /// The order of the result set is from least to highest cost.
        /// 
        /// At least one result array must be provided.
        ///
        /// A common use case for this method is to perform Dijkstra searches. 
        /// Candidate polygons are found by searching the graph beginning at the start 
        /// polygon.
        /// 
        /// The same intersection test restrictions that apply to findPolysAroundCircle()
        /// method apply to this method.
        /// 
        /// The 3D centroid of the search polygon is used as the start position for cost 
        /// calculations.
        /// 
        /// Intersection tests occur in 2D. All polygons are projected onto the 
        /// xz-plane. So the y-values of the vertices do not effect intersection tests.
        /// 
        /// If the result arrays are is too small to hold the entire result set, they will 
        /// be filled to capacity.
        ///
        /// Finds the polygons along the naviation graph that touch the specified convex polygon.
        ///  @param[in]		startRef		The reference id of the polygon where the search starts.
        ///  @param[in]		verts			The vertices describing the convex polygon. (CCW) 
        ///  								[(x, y, z) * @p nverts]
        ///  @param[in]		nverts			The number of vertices in the polygon.
        ///  @param[in]		filter			The polygon filter to apply to the query.
        ///  @param[out]	resultRef		The reference ids of the polygons touched by the search polygon. [opt]
        ///  @param[out]	resultParent	The reference ids of the parent polygons for each result. Zero if a 
        ///  								result polygon has no parent. [opt]
        ///  @param[out]	resultCost		The search cost from the centroid point to the polygon. [opt]
        ///  @param[out]	resultCount		The number of polygons found.
        ///  @param[in]		maxResult		The maximum number of polygons the result arrays can hold.
        /// @returns The status flags for the query.
        public DtStatus FindPolysAroundShape(long startRef, Span<RcVec3f> verts, int nverts,
            IDtQueryFilter filter,
            Span<long> resultRef, Span<long> resultParent, Span<float> resultCost,
            out int resultCount, int maxResult)
        {
            resultCount = 0;

            // Validate input
            if (!m_nav.IsValidPolyRef(startRef) || 
                null == verts || nverts < 3 || 
                null == filter || maxResult < 0)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            m_nodePool.Clear();
            m_openList.Clear();

            RcVec3f centerPos = RcVec3f.Zero;
            for (int i = 0; i < nverts; ++i)
            {
                centerPos += verts[i];
            }

            float scale = 1.0f / nverts;
            centerPos.X *= scale;
            centerPos.Y *= scale;
            centerPos.Z *= scale;

            DtNode startNode = m_nodePool.GetNode(startRef);
            startNode.pos = centerPos;
            startNode.pidx = 0;
            startNode.cost = 0;
            startNode.total = 0;
            startNode.id = startRef;
            startNode.flags = DtNodeFlags.DT_NODE_OPEN;
            m_openList.Push(startNode);

            DtStatus status = DtStatus.DT_SUCCESS;

            int n = 0;
            
            while (!m_openList.IsEmpty())
            {
                DtNode bestNode = m_openList.Pop();
                bestNode.flags &= ~DtNodeFlags.DT_NODE_OPEN;
                bestNode.flags |= DtNodeFlags.DT_NODE_CLOSED;

                // Get poly and tile.
                // The API input has been checked already, skip checking internal data.
                long bestRef = bestNode.id;
                m_nav.GetTileAndPolyByRefUnsafe(bestRef, out var bestTile, out var bestPoly);

                // Get parent poly and tile.
                long parentRef = 0;
                DtMeshTile parentTile = null;
                DtPoly parentPoly = null;
                if (bestNode.pidx != 0)
                {
                    parentRef = m_nodePool.GetNodeAtIdx(bestNode.pidx).id;
                }

                if (parentRef != 0)
                {
                    m_nav.GetTileAndPolyByRefUnsafe(parentRef, out parentTile, out parentPoly);
                }

                if (n < maxResult)
                {
                    resultRef[n] = bestRef;
                    resultParent[n] = parentRef;
                    resultCost[n] = bestNode.total;
                    ++n;
                }
                else
                {
                    status |= DtStatus.DT_BUFFER_TOO_SMALL;
                }

                for (int i = bestPoly.firstLink; i != DT_NULL_LINK; i = bestTile.links[i].next)
                {
                    DtLink link = bestTile.links[i];
                    long neighbourRef = link.refs;
                    // Skip invalid neighbours and do not follow back to parent.
                    if (neighbourRef == 0 || neighbourRef == parentRef)
                    {
                        continue;
                    }

                    // Expand to neighbour
                    m_nav.GetTileAndPolyByRefUnsafe(neighbourRef, out var neighbourTile, out var neighbourPoly);

                    // Do not advance if the polygon is excluded by the filter.
                    if (!filter.PassFilter(neighbourRef, neighbourTile, neighbourPoly))
                    {
                        continue;
                    }

                    // Find edge and calc distance to the edge.
                    var ppStatus = GetPortalPoints(bestRef, bestPoly, bestTile, neighbourRef, neighbourPoly,
                        neighbourTile, out var va, out var vb);
                    if (ppStatus.Failed())
                    {
                        continue;
                    }

                    // If the poly is not touching the edge to the next polygon, skip the connection it.
                    bool intersects = DtUtils.IntersectSegmentPoly2D(va, vb, verts, nverts, out var tmin, out var tmax, out var segMin, out var segMax);
                    if (!intersects)
                    {
                        continue;
                    }

                    if (tmin > 1.0f || tmax < 0.0f)
                    {
                        continue;
                    }

                    DtNode neighbourNode = m_nodePool.GetNode(neighbourRef);

                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_CLOSED) != 0)
                    {
                        continue;
                    }

                    // Cost
                    if (neighbourNode.flags == 0)
                    {
                        neighbourNode.pos = RcVec3f.Lerp(va, vb, 0.5f);
                    }

                    float cost = filter.GetCost(bestNode.pos, neighbourNode.pos, parentRef, parentTile, parentPoly, bestRef,
                        bestTile, bestPoly, neighbourRef, neighbourTile, neighbourPoly);

                    float total = bestNode.total + cost;

                    // The node is already in open list and the new result is worse, skip.
                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_OPEN) != 0 && total >= neighbourNode.total)
                    {
                        continue;
                    }

                    neighbourNode.id = neighbourRef;
                    neighbourNode.pidx = m_nodePool.GetNodeIdx(bestNode);
                    neighbourNode.total = total;

                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_OPEN) != 0)
                    {
                        m_openList.Modify(neighbourNode);
                    }
                    else
                    {
                        neighbourNode.flags = DtNodeFlags.DT_NODE_OPEN;
                        m_openList.Push(neighbourNode);
                    }
                }
            }
            
            resultCount = n;
	
            return status;
        }

        /// @par
        ///
        /// This method is optimized for a small search radius and small number of result 
        /// polygons.
        ///
        /// Candidate polygons are found by searching the navigation graph beginning at 
        /// the start polygon.
        ///
        /// The same intersection test restrictions that apply to the findPolysAroundCircle 
        /// mehtod applies to this method.
        ///
        /// The value of the center point is used as the start point for cost calculations. 
        /// It is not projected onto the surface of the mesh, so its y-value will effect 
        /// the costs.
        /// 
        /// Intersection tests occur in 2D. All polygons and the search circle are 
        /// projected onto the xz-plane. So the y-value of the center point does not 
        /// effect intersection tests.
        /// 
        /// If the result arrays are is too small to hold the entire result set, they will 
        /// be filled to capacity.
        /// 
        /// Finds the non-overlapping navigation polygons in the local neighbourhood around the center position.
        ///  @param[in]		startRef		The reference id of the polygon where the search starts.
        ///  @param[in]		centerPos		The center of the query circle. [(x, y, z)]
        ///  @param[in]		radius			The radius of the query circle.
        ///  @param[in]		filter			The polygon filter to apply to the query.
        ///  @param[out]	resultRef		The reference ids of the polygons touched by the circle.
        ///  @param[out]	resultParent	The reference ids of the parent polygons for each result. 
        /// @returns The status flags for the query.
        /// @par
        ///
        /// This method is optimized for a small search radius and small number of result 
        /// polygons.
        ///
        /// Candidate polygons are found by searching the navigation graph beginning at 
        /// the start polygon.
        ///
        /// The same intersection test restrictions that apply to the findPolysAroundCircle 
        /// mehtod applies to this method.
        ///
        /// The value of the center point is used as the start point for cost calculations. 
        /// It is not projected onto the surface of the mesh, so its y-value will effect 
        /// the costs.
        /// 
        /// Intersection tests occur in 2D. All polygons and the search circle are 
        /// projected onto the xz-plane. So the y-value of the center point does not 
        /// effect intersection tests.
        /// 
        /// If the result arrays are is too small to hold the entire result set, they will 
        /// be filled to capacity.
        /// 
        public DtStatus FindLocalNeighbourhood(long startRef, RcVec3f centerPos, float radius,
            IDtQueryFilter filter,
            Span<long> resultRef, Span<long> resultParent,
            out int resultCount, int maxResult)
        {
            resultCount = 0;
            
            if (!m_nav.IsValidPolyRef(startRef) || 
                !centerPos.IsFinite() || 
                radius < 0 || !float.IsFinite(radius) || 
                null == filter || maxResult < 0)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }
            
            LinkedList<DtNode> stack = new LinkedList<DtNode>();
            
            m_tinyNodePool.Clear();

            DtNode startNode = m_tinyNodePool.GetNode(startRef);
            startNode.pidx = 0;
            startNode.id = startRef;
            startNode.flags = DtNodeFlags.DT_NODE_CLOSED;
            stack.AddLast(startNode);
            
            float radiusSqr = RcMath.Sqr(radius);

            Span<float> pa = stackalloc float[m_nav.GetMaxVertsPerPoly() * 3];
            Span<float> pb = stackalloc float[m_nav.GetMaxVertsPerPoly() * 3];

            DtStatus status = DtStatus.DT_SUCCESS;
	
            int n = 0;
            if (n < maxResult)
            {
                resultRef[n] = startNode.id;
                if (!resultParent.IsEmpty)
                    resultParent[n] = 0;
                ++n;
            }
            else
            {
                status |= DtStatus.DT_BUFFER_TOO_SMALL;
            }

            while (0 < stack.Count)
            {
                // Pop front.
                DtNode curNode = stack.First?.Value;
                stack.RemoveFirst();

                // Get poly and tile.
                // The API input has been checked already, skip checking internal data.
                long curRef = curNode.id;
                m_nav.GetTileAndPolyByRefUnsafe(curRef, out var curTile, out var curPoly);

                for (int i = curPoly.firstLink; i != DT_NULL_LINK; i = curTile.links[i].next)
                {
                    DtLink link = curTile.links[i];
                    long neighbourRef = link.refs;
                    // Skip invalid neighbours.
                    if (neighbourRef == 0)
                    {
                        continue;
                    }

                    DtNode neighbourNode = m_tinyNodePool.GetNode(neighbourRef);
                    // Skip visited.
                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_CLOSED) != 0)
                    {
                        continue;
                    }

                    // Expand to neighbour
                    m_nav.GetTileAndPolyByRefUnsafe(neighbourRef, out var neighbourTile, out var neighbourPoly);

                    // Skip off-mesh connections.
                    if (neighbourPoly.GetPolyType() == DtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                    {
                        continue;
                    }

                    // Do not advance if the polygon is excluded by the filter.
                    if (!filter.PassFilter(neighbourRef, neighbourTile, neighbourPoly))
                    {
                        continue;
                    }

                    // Find edge and calc distance to the edge.
                    var ppStatus = GetPortalPoints(curRef, curPoly, curTile, neighbourRef, neighbourPoly,
                        neighbourTile, out var va, out var vb);
                    if (ppStatus.Failed())
                    {
                        continue;
                    }

                    // If the circle is not touching the next polygon, skip it.
                    var distSqr = DtUtils.DistancePtSegSqr2D(centerPos, va, vb, out var _);
                    if (distSqr > radiusSqr)
                    {
                        continue;
                    }

                    // Mark node visited, this is done before the overlap test so that
                    // we will not visit the poly again if the test fails.
                    neighbourNode.flags |= DtNodeFlags.DT_NODE_CLOSED;
                    neighbourNode.pidx = m_tinyNodePool.GetNodeIdx(curNode);

                    // Check that the polygon does not collide with existing polygons.

                    // Collect vertices of the neighbour poly.
                    int npa = neighbourPoly.vertCount;
                    for (int k = 0; k < npa; ++k)
                    {
                        RcSpans.Copy(neighbourTile.data.verts, neighbourPoly.verts[k] * 3, pa, k * 3, 3);
                    }

                    bool overlap = false;
                    for (int j = 0; j < n; ++j)
                    {
                        long pastRef = resultRef[j];

                        // Connected polys do not overlap.
                        bool connected = false;
                        for (int k = curPoly.firstLink; k != DT_NULL_LINK; k = curTile.links[k].next)
                        {
                            if (curTile.links[k].refs == pastRef)
                            {
                                connected = true;
                                break;
                            }
                        }

                        if (connected)
                        {
                            continue;
                        }

                        // Potentially overlapping.
                        m_nav.GetTileAndPolyByRefUnsafe(pastRef, out var pastTile, out var pastPoly);

                        // Get vertices and test overlap
                        int npb = pastPoly.vertCount;
                        for (int k = 0; k < npb; ++k)
                        {
                            RcSpans.Copy(pastTile.data.verts, pastPoly.verts[k] * 3, pb, k * 3, 3);
                        }

                        if (DtUtils.OverlapPolyPoly2D(pa, npa, pb, npb))
                        {
                            overlap = true;
                            break;
                        }
                    }

                    if (overlap)
                    {
                        continue;
                    }
                    
                    // This poly is fine, store and advance to the poly.
                    if (n < maxResult)
                    {
                        resultRef[n] = neighbourRef;
                        if (!resultParent.IsEmpty)
                            resultParent[n] = curRef;
                        ++n;
                    }
                    else
                    {
                        status |= DtStatus.DT_BUFFER_TOO_SMALL;
                    }
			
                    stack.AddLast(neighbourNode);
                }
            }

            
            resultCount = n;
	
            return status;
        }


        protected void InsertInterval(Span<DtSegInterval> ints, ref int nints, int maxInts, int tmin, int tmax, long refs)
        {
            if (nints + 1 > maxInts)
                return;

            // Find insertion point.
            int idx = 0;
            while (idx < nints)
            {
                if (tmax <= ints[idx].tmin)
                {
                    break;
                }

                idx++;
            }

            // Move current results.
            if (0 != nints - idx)
            {
                RcSpans.Move(ints, idx, idx + 1, nints - idx);
            }

            // Store
            ints[idx] = new DtSegInterval(refs, tmin, tmax);
            nints++;
        }

        /// @par
        ///
        /// If the @p segmentRefs parameter is provided, then all polygon segments will be returned. 
        /// Otherwise only the wall segments are returned.
        /// 
        /// A segment that is normally a portal will be included in the result set as a 
        /// wall if the @p filter results in the neighbor polygon becoomming impassable.
        /// 
        /// The @p segmentVerts and @p segmentRefs buffers should normally be sized for the 
        /// maximum segments per polygon of the source navigation mesh.
        /// 
        /// Returns the segments for the specified polygon, optionally including portals.
        ///  @param[in]		ref				The reference id of the polygon.
        ///  @param[in]		filter			The polygon filter to apply to the query.
        ///  @param[out]	segmentVerts	The segments. [(ax, ay, az, bx, by, bz) * segmentCount]
        ///  @param[out]	segmentRefs		The reference ids of each segment's neighbor polygon. 
        ///  								Or zero if the segment is a wall. [opt] [(parentRef) * @p segmentCount] 
        ///  @param[out]	segmentCount	The number of segments returned.
        ///  @param[in]		maxSegments		The maximum number of segments the result arrays can hold.
        /// @returns The status flags for the query.
        public DtStatus GetPolyWallSegments(long refs, IDtQueryFilter filter,
            Span<RcSegmentVert> segmentVerts, Span<long> segmentRefs, ref int segmentCount,
            int maxSegments)
        {
            segmentCount = 0;

            DtStatus status = m_nav.GetTileAndPolyByRef(refs, out var tile, out var poly);
            if (status.Failed())
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            if (null == filter)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            int n = 0;
            const int MAX_INTERVAL = 16;
            Span<DtSegInterval> ints = stackalloc DtSegInterval[MAX_INTERVAL];
            int nints;

            bool storePortals = segmentRefs != null;

            status = DtStatus.DT_SUCCESS;

            for (int i = 0, j = poly.vertCount - 1; i < poly.vertCount; j = i++)
            {
                // Skip non-solid edges.
                nints = 0;
                if ((poly.neis[j] & DT_EXT_LINK) != 0)
                {
                    // Tile border.
                    for (int k = poly.firstLink; k != DT_NULL_LINK; k = tile.links[k].next)
                    {
                        DtLink link = tile.links[k];
                        if (link.edge == j)
                        {
                            if (link.refs != 0)
                            {
                                m_nav.GetTileAndPolyByRefUnsafe(link.refs, out var neiTile, out var neiPoly);
                                if (filter.PassFilter(link.refs, neiTile, neiPoly))
                                {
                                    InsertInterval(ints, ref nints, MAX_INTERVAL, link.bmin, link.bmax, link.refs);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Internal edge
                    long neiRef = 0;
                    if (poly.neis[j] != 0)
                    {
                        int idx = (poly.neis[j] - 1);
                        neiRef = m_nav.GetPolyRefBase(tile) | (long)idx;
                        if (!filter.PassFilter(neiRef, tile, tile.data.polys[idx]))
                        {
                            neiRef = 0;
                        }
                    }

                    // If the edge leads to another polygon and portals are not stored, skip.
                    if (neiRef != 0 && !storePortals)
                    {
                        continue;
                    }

                    if (n < maxSegments)
                    {
                        int ivj = poly.verts[j] * 3;
                        int ivi = poly.verts[i] * 3;
                        var seg = new RcSegmentVert();
                        seg.vmin = tile.data.verts.ToVec3(ivj);
                        seg.vmax = tile.data.verts.ToVec3(ivi);
                        segmentVerts[n] = seg;
                        if (null != segmentRefs)
                        {
                            segmentRefs[n] = neiRef;
                        }

                        n++;
                    }
                    else
                    {
                        status |= DtStatus.DT_BUFFER_TOO_SMALL;
                    }

                    continue;
                }

                // Add sentinels
                InsertInterval(ints, ref nints, MAX_INTERVAL, -1, 0, 0);
                InsertInterval(ints, ref nints, MAX_INTERVAL, 255, 256, 0);

                // Store segments.
                int vj = poly.verts[j] * 3;
                int vi = poly.verts[i] * 3;
                for (int k = 1; k < nints; ++k)
                {
                    // Portal segment.
                    if (storePortals && ints[k].refs != 0)
                    {
                        float tmin = ints[k].tmin / 255.0f;
                        float tmax = ints[k].tmax / 255.0f;

                        if (n < maxSegments)
                        {
                            var seg = new RcSegmentVert();
                            seg.vmin = RcVec.Lerp(tile.data.verts, vj, vi, tmin);
                            seg.vmax = RcVec.Lerp(tile.data.verts, vj, vi, tmax);
                            segmentVerts[n] = seg;

                            if (null != segmentRefs)
                            {
                                segmentRefs[n] = ints[k].refs;
                            }

                            n++;
                        }
                        else
                        {
                            status |= DtStatus.DT_BUFFER_TOO_SMALL;
                        }
                    }

                    // Wall segment.
                    int imin = ints[k - 1].tmax;
                    int imax = ints[k].tmin;
                    if (imin != imax)
                    {
                        float tmin = imin / 255.0f;
                        float tmax = imax / 255.0f;

                        if (n < maxSegments)
                        {
                            var seg = new RcSegmentVert();
                            seg.vmin = RcVec.Lerp(tile.data.verts, vj, vi, tmin);
                            seg.vmax = RcVec.Lerp(tile.data.verts, vj, vi, tmax);
                            segmentVerts[n] = seg;

                            if (null != segmentRefs)
                            {
                                segmentRefs[n] = 0;
                            }

                            n++;
                        }
                        else
                        {
                            status |= DtStatus.DT_BUFFER_TOO_SMALL;
                        }
                    }
                }
            }

            segmentCount = n;

            return status;
        }

        /// @par
        ///
        /// @p hitPos is not adjusted using the height detail data.
        ///
        /// @p hitDist will equal the search radius if there is no wall within the
        /// radius. In this case the values of @p hitPos and @p hitNormal are
        /// undefined.
        ///
        /// The normal will become unpredicable if @p hitDist is a very small number.
        ///
        /// Finds the distance from the specified position to the nearest polygon wall.
        ///  @param[in]		startRef		The reference id of the polygon containing @p centerPos.
        ///  @param[in]		centerPos		The center of the search circle. [(x, y, z)]
        ///  @param[in]		maxRadius		The radius of the search circle.
        ///  @param[in]		filter			The polygon filter to apply to the query.
        ///  @param[out]	hitDist			The distance to the nearest wall from @p centerPos.
        ///  @param[out]	hitPos			The nearest position on the wall that was hit. [(x, y, z)]
        ///  @param[out]	hitNormal		The normalized ray formed from the wall point to the 
        ///  								source point. [(x, y, z)]
        /// @returns The status flags for the query.
        public virtual DtStatus FindDistanceToWall(long startRef, RcVec3f centerPos, float maxRadius,
            IDtQueryFilter filter,
            out float hitDist, out RcVec3f hitPos, out RcVec3f hitNormal)
        {
            hitDist = 0;
            hitPos = RcVec3f.Zero;
            hitNormal = RcVec3f.Zero;

            // Validate input
            if (!m_nav.IsValidPolyRef(startRef) || !centerPos.IsFinite() || maxRadius < 0
                || !float.IsFinite(maxRadius) || null == filter)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            m_nodePool.Clear();
            m_openList.Clear();

            DtNode startNode = m_nodePool.GetNode(startRef);
            startNode.pos = centerPos;
            startNode.pidx = 0;
            startNode.cost = 0;
            startNode.total = 0;
            startNode.id = startRef;
            startNode.flags = DtNodeFlags.DT_NODE_OPEN;
            m_openList.Push(startNode);

            float radiusSqr = RcMath.Sqr(maxRadius);

            var hasBestV = false;
            var bestvj = RcVec3f.Zero;
            var bestvi = RcVec3f.Zero;

            var status = DtStatus.DT_SUCCESS;
            while (!m_openList.IsEmpty())
            {
                DtNode bestNode = m_openList.Pop();
                bestNode.flags &= ~DtNodeFlags.DT_NODE_OPEN;
                bestNode.flags |= DtNodeFlags.DT_NODE_CLOSED;

                // Get poly and tile.
                // The API input has been checked already, skip checking internal data.
                long bestRef = bestNode.id;
                m_nav.GetTileAndPolyByRefUnsafe(bestRef, out var bestTile, out var bestPoly);

                // Get parent poly and tile.
                long parentRef = 0;
                if (bestNode.pidx != 0)
                {
                    parentRef = m_nodePool.GetNodeAtIdx(bestNode.pidx).id;
                }

                // Hit test walls.
                for (int i = 0, j = bestPoly.vertCount - 1; i < bestPoly.vertCount; j = i++)
                {
                    // Skip non-solid edges.
                    if ((bestPoly.neis[j] & DT_EXT_LINK) != 0)
                    {
                        // Tile border.
                        bool solid = true;
                        for (int k = bestPoly.firstLink; k != DT_NULL_LINK; k = bestTile.links[k].next)
                        {
                            DtLink link = bestTile.links[k];
                            if (link.edge == j)
                            {
                                if (link.refs != 0)
                                {
                                    m_nav.GetTileAndPolyByRefUnsafe(link.refs, out var neiTile, out var neiPoly);
                                    if (filter.PassFilter(link.refs, neiTile, neiPoly))
                                    {
                                        solid = false;
                                    }
                                }

                                break;
                            }
                        }

                        if (!solid)
                        {
                            continue;
                        }
                    }
                    else if (bestPoly.neis[j] != 0)
                    {
                        // Internal edge
                        int idx = (bestPoly.neis[j] - 1);
                        long refs = m_nav.GetPolyRefBase(bestTile) | (long)idx;
                        if (filter.PassFilter(refs, bestTile, bestTile.data.polys[idx]))
                        {
                            continue;
                        }
                    }

                    // Calc distance to the edge.
                    int vj = bestPoly.verts[j] * 3;
                    int vi = bestPoly.verts[i] * 3;
                    var distSqr = DtUtils.DistancePtSegSqr2D(centerPos, bestTile.data.verts, vj, vi, out var tseg);

                    // Edge is too far, skip.
                    if (distSqr > radiusSqr)
                    {
                        continue;
                    }

                    // Hit wall, update radius.
                    radiusSqr = distSqr;
                    // Calculate hit pos.
                    hitPos.X = bestTile.data.verts[vj + 0] + (bestTile.data.verts[vi + 0] - bestTile.data.verts[vj + 0]) * tseg;
                    hitPos.Y = bestTile.data.verts[vj + 1] + (bestTile.data.verts[vi + 1] - bestTile.data.verts[vj + 1]) * tseg;
                    hitPos.Z = bestTile.data.verts[vj + 2] + (bestTile.data.verts[vi + 2] - bestTile.data.verts[vj + 2]) * tseg;
                    hasBestV = true;
                    bestvj = bestTile.data.verts.ToVec3(vj);
                    bestvi = bestTile.data.verts.ToVec3(vi);
                }

                for (int i = bestPoly.firstLink; i != DT_NULL_LINK; i = bestTile.links[i].next)
                {
                    DtLink link = bestTile.links[i];
                    long neighbourRef = link.refs;
                    // Skip invalid neighbours and do not follow back to parent.
                    if (neighbourRef == 0 || neighbourRef == parentRef)
                    {
                        continue;
                    }

                    // Expand to neighbour.
                    m_nav.GetTileAndPolyByRefUnsafe(neighbourRef, out var neighbourTile, out var neighbourPoly);

                    // Skip off-mesh connections.
                    if (neighbourPoly.GetPolyType() == DtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                    {
                        continue;
                    }

                    // Calc distance to the edge.
                    int va = bestPoly.verts[link.edge] * 3;
                    int vb = bestPoly.verts[(link.edge + 1) % bestPoly.vertCount] * 3;
                    var distSqr = DtUtils.DistancePtSegSqr2D(centerPos, bestTile.data.verts, va, vb, out var tseg);
                    // If the circle is not touching the next polygon, skip it.
                    if (distSqr > radiusSqr)
                    {
                        continue;
                    }

                    if (!filter.PassFilter(neighbourRef, neighbourTile, neighbourPoly))
                    {
                        continue;
                    }

                    DtNode neighbourNode = m_nodePool.GetNode(neighbourRef);
                    if (null == neighbourNode)
                    {
                        status |= DtStatus.DT_OUT_OF_NODES;
                        continue;
                    }

                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_CLOSED) != 0)
                    {
                        continue;
                    }

                    // Cost
                    if (neighbourNode.flags == 0)
                    {
                        GetEdgeMidPoint(bestRef, bestPoly, bestTile,
                            neighbourRef, neighbourPoly, neighbourTile,
                            ref neighbourNode.pos);
                    }

                    float total = bestNode.total + RcVec3f.Distance(bestNode.pos, neighbourNode.pos);

                    // The node is already in open list and the new result is worse, skip.
                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_OPEN) != 0 && total >= neighbourNode.total)
                    {
                        continue;
                    }

                    neighbourNode.id = neighbourRef;
                    neighbourNode.flags = (neighbourNode.flags & ~DtNodeFlags.DT_NODE_CLOSED);
                    neighbourNode.pidx = m_nodePool.GetNodeIdx(bestNode);
                    neighbourNode.total = total;

                    if ((neighbourNode.flags & DtNodeFlags.DT_NODE_OPEN) != 0)
                    {
                        m_openList.Modify(neighbourNode);
                    }
                    else
                    {
                        neighbourNode.flags |= DtNodeFlags.DT_NODE_OPEN;
                        m_openList.Push(neighbourNode);
                    }
                }
            }

            // Calc hit normal.
            if (hasBestV)
            {
                var tangent = RcVec3f.Subtract(bestvi, bestvj);
                hitNormal = RcVec3f.Normalize(new RcVec3f(tangent.Z, 0, -tangent.X));
            }

            hitDist = MathF.Sqrt(radiusSqr);

            return status;
        }

        /// Returns true if the polygon reference is valid and passes the filter restrictions.
        /// @param[in] ref The polygon reference to check.
        /// @param[in] filter The filter to apply.
        public bool IsValidPolyRef(long refs, IDtQueryFilter filter)
        {
            var status = m_nav.GetTileAndPolyByRef(refs, out var tile, out var poly);
            if (status.Failed())
            {
                return false;
            }

            // If cannot pass filter, assume flags has changed and boundary is invalid.
            if (!filter.PassFilter(refs, tile, poly))
            {
                return false;
            }

            return true;
        }

        /// Gets the navigation mesh the query object is using.
        /// @return The navigation mesh the query object is using.
        public DtNavMesh GetAttachedNavMesh()
        {
            return m_nav;
        }

        /// Gets a path from the explored nodes in the previous search.
        ///  @param[in]		endRef		The reference id of the end polygon.
        ///  @param[out]	path		An ordered list of polygon references representing the path. (Start to end.)
        ///  							[(polyRef) * @p pathCount]
        ///  @param[out]	pathCount	The number of polygons returned in the @p path array.
        ///  @param[in]		maxPath		The maximum number of polygons the @p path array can hold. [Limit: >= 0]
        ///  @returns		The status flags. Returns DT_FAILURE | DT_INVALID_PARAM if any parameter is wrong, or if
        ///  				@p endRef was not explored in the previous search. Returns DT_SUCCESS | DT_BUFFER_TOO_SMALL
        ///  				if @p path cannot contain the entire path. In this case it is filled to capacity with a partial path.
        ///  				Otherwise returns DT_SUCCESS.
        ///  @remarks		The result of this function depends on the state of the query object. For that reason it should only
        ///  				be used immediately after one of the two Dijkstra searches, findPolysAroundCircle or findPolysAroundShape.
        public DtStatus GetPathFromDijkstraSearch(long endRef, Span<long> path, out int pathCount, int maxPath)
        {
            pathCount = 0;
            if (!m_nav.IsValidPolyRef(endRef) || path.IsEmpty || maxPath <= 0)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            if (m_nodePool.FindNodes(endRef, out var endNodes) != 1
                || (endNodes[0].flags & DtNodeFlags.DT_NODE_CLOSED) == 0)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            DtNode endNode = endNodes[0];

            return GetPathToNode(endNode, path, out pathCount, maxPath);
        }

        // Gets the path leading to the specified end node.
        protected DtStatus GetPathToNode(DtNode endNode, Span<long> path, out int pathCount, int maxPath)
        {
            // Find the length of the entire path.
            DtNode curNode = endNode;
            int length = 0;
            do
            {
                length++;
                curNode = m_nodePool.GetNodeAtIdx(curNode.pidx);
            } while (null != curNode);

            // If the path cannot be fully stored then advance to the last node we will be able to store.
            curNode = endNode;
            int writeCount;
            for (writeCount = length; writeCount > maxPath; writeCount--)
            {
                RcDebug.Assert(null != curNode);

                curNode = m_nodePool.GetNodeAtIdx(curNode.pidx);
            }

            // Write path
            for (int i = writeCount - 1; i >= 0; i--)
            {
                RcDebug.Assert(null != curNode);

                path[i] = curNode.id;
                curNode = m_nodePool.GetNodeAtIdx(curNode.pidx);
            }

            RcDebug.Assert(null == curNode);

            pathCount = Math.Min(length, maxPath);

            if (length > maxPath)
                return DtStatus.DT_SUCCESS | DtStatus.DT_BUFFER_TOO_SMALL;

            return DtStatus.DT_SUCCESS;
        }

        /// @par
        ///
        /// The closed list is the list of polygons that were fully evaluated during 
        /// the last navigation graph search. (A* or Dijkstra)
        /// 
        public bool IsInClosedList(long refs)
        {
            if (m_nodePool == null)
            {
                return false;
            }

            int n = m_nodePool.FindNodes(refs, out var nodes);
            for (int i = 0; i < n; ++i)
            {
                if ((nodes[i].flags & DtNodeFlags.DT_NODE_CLOSED) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public DtNodePool GetNodePool()
        {
            return m_nodePool;
        }
    }
}