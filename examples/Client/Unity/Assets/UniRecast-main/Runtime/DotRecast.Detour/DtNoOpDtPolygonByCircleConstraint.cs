using System;
using DotRecast.Core.Numerics;

namespace DotRecast.Detour
{
    public class DtNoOpDtPolygonByCircleConstraint : IDtPolygonByCircleConstraint
    {
        public static readonly DtNoOpDtPolygonByCircleConstraint Shared = new DtNoOpDtPolygonByCircleConstraint();

        private DtNoOpDtPolygonByCircleConstraint()
        {
        }

        public bool Apply(Span<float> polyVerts, RcVec3f circleCenter, float radius, Span<float> constrainedVerts, out int constrainedVertCount)
        {
            polyVerts.CopyTo(constrainedVerts);
            constrainedVertCount = polyVerts.Length;
            return true;
        }
    }
}