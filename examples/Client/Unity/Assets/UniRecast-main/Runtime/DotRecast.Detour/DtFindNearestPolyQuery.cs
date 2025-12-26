using System;
using DotRecast.Core.Numerics;

namespace DotRecast.Detour
{
    public class DtFindNearestPolyQuery : IDtPolyQuery
    {
        private readonly DtNavMeshQuery _query;
        private readonly RcVec3f _center;
        private float _nearestDistanceSqr;
        private long _nearestRef;
        private RcVec3f _nearestPoint;
        private bool _overPoly;

        public DtFindNearestPolyQuery(DtNavMeshQuery query, RcVec3f center)
        {
            _query = query;
            _center = center;
            _nearestDistanceSqr = float.MaxValue;
            _nearestPoint = center;
        }

        public void Process(DtMeshTile tile, ReadOnlySpan<int> polys, ReadOnlySpan<long> refs, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                long polyRef = refs[i];
                float d;
                
                // Find nearest polygon amongst the nearby polygons.
                _query.ClosestPointOnPoly(polyRef, _center, out var closestPtPoly, out var posOverPoly);

                // If a point is directly over a polygon and closer than
                // climb height, favor that instead of straight line nearest point.
                RcVec3f diff = RcVec3f.Subtract(_center, closestPtPoly);
                if (posOverPoly)
                {
                    d = MathF.Abs(diff.Y) - tile.data.header.walkableClimb;
                    d = d > 0 ? d * d : 0;
                }
                else
                {
                    d = diff.LengthSquared();
                }

                if (d < _nearestDistanceSqr)
                {
                    _nearestPoint = closestPtPoly;
                    _nearestDistanceSqr = d;
                    _nearestRef = polyRef;
                    _overPoly = posOverPoly;
                }
            }
        }

        public long NearestRef()
        {
            return _nearestRef;
        }

        public RcVec3f NearestPt()
        {
            return _nearestPoint;
        }

        public bool OverPoly()
        {
            return _overPoly;
        }
    }
}