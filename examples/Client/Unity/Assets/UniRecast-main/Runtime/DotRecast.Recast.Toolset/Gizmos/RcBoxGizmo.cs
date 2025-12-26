using DotRecast.Core.Numerics;
using DotRecast.Detour.Dynamic.Colliders;

namespace DotRecast.Recast.Toolset.Gizmos
{
    public class RcBoxGizmo : IRcGizmoMeshFilter
    {
        public static readonly int[] TRIANLGES =
        {
            0, 1, 2, 0, 2, 3, 4, 7, 6, 4, 6, 5, 0, 4, 5, 0, 5, 1, 1, 5, 6, 1, 6, 2,
            2, 6, 7, 2, 7, 3, 4, 0, 3, 4, 3, 7
        };

        public static readonly RcVec3f[] VERTS =
        {
            new RcVec3f(-1f, -1f, -1f),
            new RcVec3f(1f, -1f, -1f),
            new RcVec3f(1f, -1f, 1f),
            new RcVec3f(-1f, -1f, 1f),
            new RcVec3f(-1f, 1f, -1f),
            new RcVec3f(1f, 1f, -1f),
            new RcVec3f(1f, 1f, 1f),
            new RcVec3f(-1f, 1f, 1f),
        };

        public readonly float[] vertices = new float[8 * 3];
        public readonly RcVec3f center;
        public readonly RcVec3f[] halfEdges;

        public RcBoxGizmo(RcVec3f center, RcVec3f extent, RcVec3f forward, RcVec3f up) :
            this(center, DtBoxCollider.GetHalfEdges(up, forward, extent))
        {
        }

        public RcBoxGizmo(RcVec3f center, RcVec3f[] halfEdges)
        {
            this.center = center;
            this.halfEdges = halfEdges;
            for (int i = 0; i < 8; ++i)
            {
                float s0 = (i & 1) != 0 ? 1f : -1f;
                float s1 = (i & 2) != 0 ? 1f : -1f;
                float s2 = (i & 4) != 0 ? 1f : -1f;
                vertices[i * 3 + 0] = center.X + s0 * halfEdges[0].X + s1 * halfEdges[1].X + s2 * halfEdges[2].X;
                vertices[i * 3 + 1] = center.Y + s0 * halfEdges[0].Y + s1 * halfEdges[1].Y + s2 * halfEdges[2].Y;
                vertices[i * 3 + 2] = center.Z + s0 * halfEdges[0].Z + s1 * halfEdges[1].Z + s2 * halfEdges[2].Z;
            }
        }
    }
}