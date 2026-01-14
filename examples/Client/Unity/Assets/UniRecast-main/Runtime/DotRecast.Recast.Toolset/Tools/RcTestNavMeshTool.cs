using System;
using System.Collections.Generic;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Detour;

namespace DotRecast.Recast.Toolset.Tools
{
    public class RcTestNavMeshTool : IRcToolable
    {
        public RcTestNavMeshTool()
        {
        }

        public string GetName()
        {
            return "Test Navmesh";
        }

        public DtStatus FindFollowPath(DtNavMesh navMesh, DtNavMeshQuery navQuery, long startRef, long endRef, RcVec3f startPt, RcVec3f endPt, IDtQueryFilter filter, bool enableRaycast,
            Span<long> polys, out int npolys, Span<RcVec3f> smoothPath, out int nsmoothPath)
        {
            npolys = 0;
            nsmoothPath = 0;
            
            if (startRef == 0 || endRef == 0)
            {
                return DtStatus.DT_FAILURE;
            }

            navQuery.FindPath(startRef, endRef, startPt, endPt, filter, polys, out npolys, polys.Length);
            if (0 >= npolys)
                return DtStatus.DT_FAILURE;

            // Iterate over the path to find smooth path on the detail mesh surface.
            navQuery.ClosestPointOnPoly(startRef, startPt, out var iterPos, out var _);
            navQuery.ClosestPointOnPoly(polys[npolys - 1], endPt, out var targetPos, out var _);

            const float STEP_SIZE = 0.5f;
            const float SLOP = 0.01f;

            int n = 0;

            smoothPath[n++] = iterPos;

            Span<long> visited = stackalloc long[16];
            int nvisited = 0;


            // Move towards target a small advancement at a time until target reached or
            // when ran out of memory to store the path.
            while (0 < npolys && n < smoothPath.Length)
            {
                // Find location to steer towards.
                if (!DtPathUtils.GetSteerTarget(navQuery, iterPos, targetPos, SLOP,
                        polys, npolys, out var steerPos, out var steerPosFlag, out var steerPosRef))
                {
                    break;
                }

                bool endOfPath = (steerPosFlag & DtStraightPathFlags.DT_STRAIGHTPATH_END) != 0
                    ? true
                    : false;
                bool offMeshConnection = (steerPosFlag & DtStraightPathFlags.DT_STRAIGHTPATH_OFFMESH_CONNECTION) != 0
                    ? true
                    : false;

                // Find movement delta.
                RcVec3f delta = RcVec3f.Subtract(steerPos, iterPos);
                float len = MathF.Sqrt(RcVec3f.Dot(delta, delta));
                // If the steer target is end of path or off-mesh link, do not move past the location.
                if ((endOfPath || offMeshConnection) && len < STEP_SIZE)
                {
                    len = 1;
                }
                else
                {
                    len = STEP_SIZE / len;
                }

                RcVec3f moveTgt = RcVec.Mad(iterPos, delta, len);

                // Move
                navQuery.MoveAlongSurface(polys[0], iterPos, moveTgt, filter, out var result, visited, out nvisited, visited.Length);

                iterPos = result;

                npolys = DtPathUtils.MergeCorridorStartMoved(polys, npolys, polys.Length, visited, nvisited);
                npolys = DtPathUtils.FixupShortcuts(polys, npolys, navQuery);

                var status = navQuery.GetPolyHeight(polys[0], result, out var h);
                if (status.Succeeded())
                {
                    iterPos.Y = h;
                }

                // Handle end of path and off-mesh links when close enough.
                if (endOfPath && DtPathUtils.InRange(iterPos, steerPos, SLOP, 1.0f))
                {
                    // Reached end of path.
                    iterPos = targetPos;
                    if (n < smoothPath.Length)
                    {
                        smoothPath[n++] = iterPos;
                    }

                    break;
                }
                else if (offMeshConnection && DtPathUtils.InRange(iterPos, steerPos, SLOP, 1.0f))
                {
                    // Reached off-mesh connection.
                    RcVec3f startPos = RcVec3f.Zero;
                    RcVec3f endPos = RcVec3f.Zero;

                    // Advance the path up to and over the off-mesh connection.
                    long prevRef = 0;
                    long polyRef = polys[0];
                    int npos = 0;
                    while (npos < npolys && polyRef != steerPosRef)
                    {
                        prevRef = polyRef;
                        polyRef = polys[npos];
                        npos++;
                    }

                    for (int i = npos; i < npolys; ++i)
                        polys[i-npos] = polys[i];
                    npolys -= npos;

                    // Handle the connection.
                    var status2 = navMesh.GetOffMeshConnectionPolyEndPoints(prevRef, polyRef, ref startPos, ref endPos);
                    if (status2.Succeeded())
                    {
                        if (n < smoothPath.Length)
                        {
                            smoothPath[n++] = startPos;
                            // Hack to make the dotted path not visible during off-mesh connection.
                            if ((n & 1) != 0)
                            {
                                smoothPath[n++] = startPos;
                            }
                        }

                        // Move position at the other side of the off-mesh link.
                        iterPos = endPos;
                        navQuery.GetPolyHeight(polys[0], iterPos, out var eh);
                        iterPos.Y = eh;
                    }
                }

                // Store results.
                if (n < smoothPath.Length)
                {
                    smoothPath[n++] = iterPos;
                }
            }
            
            nsmoothPath = n;

            return DtStatus.DT_SUCCESS;
        }

        public DtStatus FindStraightPath(DtNavMeshQuery navQuery, long startRef, long endRef, RcVec3f startPt, RcVec3f endPt, IDtQueryFilter filter, bool enableRaycast,
            Span<long> polys, out int npolys, Span<DtStraightPath> straightPath, out int straightPathCount, int maxStraightPath, int straightPathOptions)
        {
            npolys = 0;
            straightPathCount = 0;
            if (startRef == 0 || endRef == 0)
            {
                return DtStatus.DT_FAILURE;
            }

            navQuery.FindPath(startRef, endRef, startPt, endPt, filter, polys, out npolys, polys.Length);

            if (0 >= npolys)
                return DtStatus.DT_FAILURE;

            // In case of partial path, make sure the end point is clamped to the last polygon.
            var epos = new RcVec3f(endPt.X, endPt.Y, endPt.Z);
            if (polys[npolys - 1] != endRef)
            {
                var result = navQuery.ClosestPointOnPoly(polys[npolys - 1], endPt, out var closest, out var _);
                if (result.Succeeded())
                {
                    epos = closest;
                }
            }

            navQuery.FindStraightPath(startPt, epos, polys, npolys, straightPath, out straightPathCount, maxStraightPath, straightPathOptions);

            return DtStatus.DT_SUCCESS;
        }

        public DtStatus InitSlicedFindPath(DtNavMeshQuery navQuery, long startRef, long endRef, RcVec3f startPos, RcVec3f endPos, IDtQueryFilter filter, bool enableRaycast)
        {
            if (startRef == 0 || endRef == 0)
            {
                return DtStatus.DT_FAILURE;
            }

            return navQuery.InitSlicedFindPath(startRef, endRef, startPos, endPos, filter, enableRaycast ? DtFindPathOptions.DT_FINDPATH_ANY_ANGLE : 0);
        }

        public DtStatus UpdateSlicedFindPath(DtNavMeshQuery navQuery, int maxIter, long endRef, RcVec3f startPos, RcVec3f endPos,
            Span<long> polys, out int npolys, int maxPolys, Span<DtStraightPath> straightPath, out int straightPathCount, int maxStraightPath)
        {
            npolys = 0;
            straightPathCount = 0;
            var status = navQuery.UpdateSlicedFindPath(maxIter, out _);

            if (!status.Succeeded())
            {
                return status;
            }

            navQuery.FinalizeSlicedFindPath(polys, out npolys, maxPolys);

            if (0 < npolys)
            {
                // In case of partial path, make sure the end point is clamped to the last polygon.
                RcVec3f epos = endPos;
                if (polys[maxPolys - 1] != endRef)
                {
                    var result = navQuery.ClosestPointOnPoly(polys[maxPolys - 1], endPos, out var closest, out var _);
                    if (result.Succeeded())
                    {
                        epos = closest;
                    }
                }

                navQuery.FindStraightPath(startPos, epos, polys, maxPolys, straightPath, out straightPathCount, maxStraightPath, DtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS);
            }

            return DtStatus.DT_SUCCESS;
        }


        public DtStatus Raycast(DtNavMeshQuery navQuery, long startRef, long endRef, RcVec3f startPos, RcVec3f endPos, IDtQueryFilter filter,
            Span<long> path, out int npath, Span<DtStraightPath> straightPath, out int straightPathCount, int maxStraightPath, ref RcVec3f hitPos, ref RcVec3f hitNormal, ref bool hitResult)
        {
            npath = 0;
            straightPathCount = 0;
            if (startRef == 0 || endRef == 0)
            {
                return DtStatus.DT_FAILURE;
            }

            var status = navQuery.Raycast(startRef, startPos, endPos, filter, out var t, out var hitNormal2, path, out npath, path.Length);
            if (!status.Succeeded())
            {
                return status;
            }

            // results ...

            if (t >= 1)
            {
                // No hit
                hitPos = endPos;
                hitResult = false;
            }
            else
            {
                // Hit
                hitPos = RcVec3f.Lerp(startPos, endPos, t);
                hitNormal = hitNormal2;
                hitResult = true;
            }

            // Adjust height.
            if (npath > 0)
            {
                var result = navQuery.GetPolyHeight(path[npath - 1], hitPos, out var h);
                if (result.Succeeded())
                {
                    hitPos.Y = h;
                }
            }

            straightPath[straightPathCount++] = new DtStraightPath(startPos, 0, 0);
            straightPath[straightPathCount++] = new DtStraightPath(hitPos, 0, 0);

            return status;
        }

        public DtStatus FindDistanceToWall(DtNavMeshQuery navQuery, long startRef, RcVec3f spos, float maxRadius, IDtQueryFilter filter,
            ref float hitDist, ref RcVec3f hitPos, ref RcVec3f hitNormal)
        {
            if (0 == startRef)
            {
                return DtStatus.DT_FAILURE;
            }

            var status = navQuery.FindDistanceToWall(startRef, spos, maxRadius, filter,
                out var tempHitDist, out var tempHitPos, out var tempHitNormal);

            if (status.Succeeded())
            {
                hitDist = tempHitDist;
                hitPos = tempHitPos;
                hitNormal = tempHitNormal;
            }

            return status;
        }


        public DtStatus FindPolysAroundCircle(DtNavMeshQuery navQuery, long startRef, long endRef, RcVec3f spos, RcVec3f epos, IDtQueryFilter filter, Span<long> resultRef, Span<long> resultParent, out int resultCount)
        {
            resultCount = 0;
            if (startRef == 0 || endRef == 0)
            {
                return DtStatus.DT_FAILURE;
            }

            float dx = epos.X - spos.X;
            float dz = epos.Z - spos.Z;
            float dist = MathF.Sqrt(dx * dx + dz * dz);

            Span<float> tempCosts = stackalloc float[resultRef.Length];
            var status = navQuery.FindPolysAroundCircle(startRef, spos, dist, filter, resultRef, resultParent, tempCosts, out resultCount, resultRef.Length);

            return status;
        }

        public DtStatus FindLocalNeighbourhood(DtNavMeshQuery navQuery, long startRef, RcVec3f spos, float radius, IDtQueryFilter filter,
            Span<long> resultRef, Span<long> resultParent, out int resultRefCount)
        {
            resultRefCount = 0;
            if (startRef == 0)
            {
                return DtStatus.DT_FAILURE;
            }

            Span<float> cost = stackalloc float[resultRef.Length];
            var status = navQuery.FindLocalNeighbourhood(startRef, spos, radius, filter, resultRef, resultParent, out resultRefCount, resultRef.Length);
            return status;
        }


        public DtStatus FindPolysAroundShape(DtNavMeshQuery navQuery, float agentHeight, long startRef, long endRef, RcVec3f spos, RcVec3f epos, IDtQueryFilter filter,
            Span<long> resultRefs, Span<long> resultParents, Span<RcVec3f> queryPoly, out int resultCount)
        {
            resultCount = 0;
            if (startRef == 0 || endRef == 0)
            {
                return DtStatus.DT_FAILURE;
            }

            float nx = (epos.Z - spos.Z) * 0.25f;
            float nz = -(epos.X - spos.X) * 0.25f;

            Span<RcVec3f> tempQueryPoly = stackalloc RcVec3f[4];
            tempQueryPoly[0].X = spos.X + nx * 1.2f;
            tempQueryPoly[0].Y = spos.Y + agentHeight / 2;
            tempQueryPoly[0].Z = spos.Z + nz * 1.2f;

            tempQueryPoly[1].X = spos.X - nx * 1.3f;
            tempQueryPoly[1].Y = spos.Y + agentHeight / 2;
            tempQueryPoly[1].Z = spos.Z - nz * 1.3f;

            tempQueryPoly[2].X = epos.X - nx * 0.8f;
            tempQueryPoly[2].Y = epos.Y + agentHeight / 2;
            tempQueryPoly[2].Z = epos.Z - nz * 0.8f;

            tempQueryPoly[3].X = epos.X + nx;
            tempQueryPoly[3].Y = epos.Y + agentHeight / 2;
            tempQueryPoly[3].Z = epos.Z + nz;

            Span<float> tempCosts = stackalloc float[resultRefs.Length];
            var status = navQuery.FindPolysAroundShape(startRef, tempQueryPoly, tempQueryPoly.Length, filter, resultRefs, resultParents, tempCosts, out resultCount, resultRefs.Length);
            if (status.Succeeded())
            {
                for (int i = 0; i < tempQueryPoly.Length; ++i)
                {
                    queryPoly[i] = tempQueryPoly[i];
                }
            }

            return status;
        }

        public DtStatus FindRandomPointAroundCircle(DtNavMeshQuery navQuery, long startRef, long endRef, RcVec3f spos, RcVec3f epos, IDtQueryFilter filter, bool constrainByCircle, int count,
            ref List<RcVec3f> points)
        {
            if (startRef == 0 || endRef == 0)
            {
                return DtStatus.DT_FAILURE;
            }

            float dx = epos.X - spos.X;
            float dz = epos.Z - spos.Z;
            float dist = MathF.Sqrt(dx * dx + dz * dz);

            IDtPolygonByCircleConstraint constraint = constrainByCircle
                ? DtStrictDtPolygonByCircleConstraint.Shared
                : DtNoOpDtPolygonByCircleConstraint.Shared;

            var frand = new RcRand();
            int prevCnt = points.Count;

            points = new List<RcVec3f>();
            while (0 < count && points.Count < prevCnt + count)
            {
                var status = navQuery.FindRandomPointAroundCircle(startRef, spos, dist, filter, frand, constraint,
                    out var randomRef, out var randomPt);

                if (status.Succeeded())
                {
                    points.Add(randomPt);
                }
            }

            return DtStatus.DT_SUCCESS;
        }
    }
}