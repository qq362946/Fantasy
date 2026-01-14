using DotRecast.Core.Numerics;

namespace DotRecast.Recast.Toolset.Gizmos
{
    public static class RcGizmoFactory
    {
        public static RcBoxGizmo Box(RcVec3f center, RcVec3f[] halfEdges)
        {
            return new RcBoxGizmo(center, halfEdges);
        }

        public static RcSphereGizmo Sphere(RcVec3f center, float radius)
        {
            return new RcSphereGizmo(center, radius);
        }

        public static RcCapsuleGizmo Capsule(RcVec3f start, RcVec3f end, float radius)
        {
            return new RcCapsuleGizmo(start, end, radius);
        }

        public static RcCylinderGizmo Cylinder(RcVec3f start, RcVec3f end, float radius)
        {
            return new RcCylinderGizmo(start, end, radius);
        }

        public static RcTrimeshGizmo Trimesh(float[] verts, int[] faces)
        {
            return new RcTrimeshGizmo(verts, faces);
        }

        public static RcCompositeGizmo Composite(params IRcGizmoMeshFilter[] gizmos)
        {
            return new RcCompositeGizmo(gizmos);
        }
    }
}