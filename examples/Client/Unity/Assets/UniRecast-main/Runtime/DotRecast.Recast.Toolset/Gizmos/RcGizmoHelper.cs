using System;

namespace DotRecast.Recast.Toolset.Gizmos
{
    public static class RcGizmoHelper
    {
        private const int SEGMENTS = 16;
        private const int RINGS = 8;

        private static float[] sphericalVertices;

        public static float[] GenerateSphericalVertices()
        {
            if (sphericalVertices == null)
            {
                sphericalVertices = GenerateSphericalVertices(SEGMENTS, RINGS);
            }

            return sphericalVertices;
        }

        private static float[] GenerateSphericalVertices(int segments, int rings)
        {
            float[] vertices = new float[6 + 3 * (segments + 1) * (rings + 1)];
            // top
            int vi = 0;
            vertices[vi++] = 0;
            vertices[vi++] = 1;
            vertices[vi++] = 0;
            for (int r = 0; r <= rings; r++)
            {
                double theta = MathF.PI * (r + 1) / (rings + 2);
                vi = GenerateRingVertices(segments, vertices, vi, theta);
            }

            // bottom
            vertices[vi++] = 0;
            vertices[vi++] = -1;
            vertices[vi++] = 0;
            return vertices;
        }

        public static float[] GenerateCylindricalVertices()
        {
            return GenerateCylindricalVertices(SEGMENTS);
        }

        private static float[] GenerateCylindricalVertices(int segments)
        {
            float[] vertices = new float[3 * (segments + 1) * 4];
            int vi = 0;
            for (int r = 0; r < 4; r++)
            {
                vi = GenerateRingVertices(segments, vertices, vi, MathF.PI * 0.5);
            }

            return vertices;
        }

        private static int GenerateRingVertices(int segments, float[] vertices, int vi, double theta)
        {
            double cosTheta = Math.Cos(theta);
            double sinTheta = Math.Sin(theta);
            for (int p = 0; p <= segments; p++)
            {
                double phi = 2 * MathF.PI * p / segments;
                double cosPhi = Math.Cos(phi);
                double sinPhi = Math.Sin(phi);
                vertices[vi++] = (float)(sinTheta * cosPhi);
                vertices[vi++] = (float)cosTheta;
                vertices[vi++] = (float)(sinTheta * sinPhi);
            }

            return vi;
        }

        public static int[] GenerateSphericalTriangles()
        {
            return GenerateSphericalTriangles(SEGMENTS, RINGS);
        }

        private static int[] GenerateSphericalTriangles(int segments, int rings)
        {
            int[] triangles = new int[6 * (segments + rings * (segments + 1))];
            int ti = GenerateSphereUpperCapTriangles(segments, triangles, 0);
            ti = GenerateRingTriangles(segments, rings, triangles, 1, ti);
            GenerateSphereLowerCapTriangles(segments, rings, triangles, ti);
            return triangles;
        }

        public static int GenerateRingTriangles(int segments, int rings, int[] triangles, int vertexOffset, int ti)
        {
            for (int r = 0; r < rings; r++)
            {
                for (int p = 0; p < segments; p++)
                {
                    int current = p + r * (segments + 1) + vertexOffset;
                    int next = p + 1 + r * (segments + 1) + vertexOffset;
                    int currentBottom = p + (r + 1) * (segments + 1) + vertexOffset;
                    int nextBottom = p + 1 + (r + 1) * (segments + 1) + vertexOffset;
                    triangles[ti++] = current;
                    triangles[ti++] = next;
                    triangles[ti++] = nextBottom;
                    triangles[ti++] = current;
                    triangles[ti++] = nextBottom;
                    triangles[ti++] = currentBottom;
                }
            }

            return ti;
        }

        private static int GenerateSphereUpperCapTriangles(int segments, int[] triangles, int ti)
        {
            for (int p = 0; p < segments; p++)
            {
                triangles[ti++] = p + 2;
                triangles[ti++] = p + 1;
                triangles[ti++] = 0;
            }

            return ti;
        }

        private static void GenerateSphereLowerCapTriangles(int segments, int rings, int[] triangles, int ti)
        {
            int lastVertex = 1 + (segments + 1) * (rings + 1);
            for (int p = 0; p < segments; p++)
            {
                triangles[ti++] = lastVertex;
                triangles[ti++] = lastVertex - (p + 2);
                triangles[ti++] = lastVertex - (p + 1);
            }
        }

        public static int[] GenerateCylindricalTriangles()
        {
            return GenerateCylindricalTriangles(SEGMENTS);
        }

        private static int[] GenerateCylindricalTriangles(int segments)
        {
            int circleTriangles = segments - 2;
            int[] triangles = new int[6 * (circleTriangles + (segments + 1))];
            int vi = 0;
            int ti = GenerateCircleTriangles(segments, triangles, vi, 0, false);
            ti = GenerateRingTriangles(segments, 1, triangles, segments + 1, ti);
            int vertexCount = (segments + 1) * 4;
            ti = GenerateCircleTriangles(segments, triangles, vertexCount - segments, ti, true);
            return triangles;
        }

        private static int GenerateCircleTriangles(int segments, int[] triangles, int vi, int ti, bool invert)
        {
            for (int p = 0; p < segments - 2; p++)
            {
                if (invert)
                {
                    triangles[ti++] = vi;
                    triangles[ti++] = vi + p + 1;
                    triangles[ti++] = vi + p + 2;
                }
                else
                {
                    triangles[ti++] = vi + p + 2;
                    triangles[ti++] = vi + p + 1;
                    triangles[ti++] = vi;
                }
            }

            return ti;
        }
    }
}