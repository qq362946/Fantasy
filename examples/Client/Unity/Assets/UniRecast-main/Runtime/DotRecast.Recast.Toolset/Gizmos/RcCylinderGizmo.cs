using System;
using DotRecast.Core.Numerics;
using static DotRecast.Recast.Toolset.Gizmos.RcGizmoHelper;


namespace DotRecast.Recast.Toolset.Gizmos
{
    public class RcCylinderGizmo : IRcGizmoMeshFilter
    {
        public readonly float[] vertices;
        public readonly int[] triangles;
        public readonly RcVec3f center;
        public readonly float[] gradient;

        public RcCylinderGizmo(RcVec3f start, RcVec3f end, float radius)
        {
            center = new RcVec3f(
                0.5f * (start.X + end.X), 0.5f * (start.Y + end.Y),
                0.5f * (start.Z + end.Z)
            );
            RcVec3f axis = new RcVec3f(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
            Span<RcVec3f> normals = stackalloc RcVec3f[3];
            normals[1] = new RcVec3f(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
            normals[1] = RcVec3f.Normalize(normals[1]);
            normals[0] = GetSideVector(axis);
            normals[2] = RcVec3f.Zero;
            normals[2] = RcVec3f.Cross(normals[0], normals[1]);
            normals[2] = RcVec3f.Normalize(normals[2]);
            triangles = GenerateCylindricalTriangles();
            RcVec3f trX = new RcVec3f(normals[0].X, normals[1].X, normals[2].X);
            RcVec3f trY = new RcVec3f(normals[0].Y, normals[1].Y, normals[2].Y);
            RcVec3f trZ = new RcVec3f(normals[0].Z, normals[1].Z, normals[2].Z);
            vertices = GenerateCylindricalVertices();
            float halfLength = 0.5f * axis.Length();
            gradient = new float[vertices.Length / 3];
            RcVec3f v = new RcVec3f();
            for (int i = 0; i < vertices.Length; i += 3)
            {
                float offset = (i >= vertices.Length / 2) ? -halfLength : halfLength;
                float x = radius * vertices[i];
                float y = vertices[i + 1] + offset;
                float z = radius * vertices[i + 2];
                vertices[i] = x * trX.X + y * trX.Y + z * trX.Z + center.X;
                vertices[i + 1] = x * trY.X + y * trY.Y + z * trY.Z + center.Y;
                vertices[i + 2] = x * trZ.X + y * trZ.Y + z * trZ.Z + center.Z;
                if (i < vertices.Length / 4 || i >= 3 * vertices.Length / 4)
                {
                    gradient[i / 3] = 1;
                }
                else
                {
                    v.X = vertices[i] - center.X;
                    v.Y = vertices[i + 1] - center.Y;
                    v.Z = vertices[i + 2] - center.Z;
                    v = RcVec3f.Normalize(v);
                    gradient[i / 3] = Math.Clamp(0.57735026f * (v.X + v.Y + v.Z), -1, 1);
                }
            }
        }

        private RcVec3f GetSideVector(RcVec3f axis)
        {
            RcVec3f side = new RcVec3f(1, 0, 0);
            if (axis.X > 0.8)
            {
                side = new RcVec3f(0, 0, 1);
            }

            var forward = RcVec3f.Cross(side, axis);
            side = RcVec3f.Cross(axis, forward);
            side = RcVec3f.Normalize(side);
            return side;
        }
    }
}