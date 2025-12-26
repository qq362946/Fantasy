/*
recast4j copyright (c) 2021 Piotr Piastucki piotr@jtilia.org
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
using DotRecast.Core;
using DotRecast.Core.Numerics;

namespace DotRecast.Recast
{
    using static RcRecast;

    public static class RcFilledVolumeRasterization
    {
        private const float EPSILON = 0.00001f;
        private static readonly int[] BOX_EDGES = new[] { 0, 1, 0, 2, 0, 4, 1, 3, 1, 5, 2, 3, 2, 6, 3, 7, 4, 5, 4, 6, 5, 7, 6, 7 };

        public static void RasterizeSphere(RcHeightfield hf, RcVec3f center, float radius, int area, int flagMergeThr, RcContext ctx)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_RASTERIZE_SPHERE);
            Span<float> bounds = stackalloc float[]
            {
                center.X - radius, center.Y - radius, center.Z - radius, center.X + radius, center.Y + radius,
                center.Z + radius
            };
            RasterizationFilledShape(hf, bounds, area, flagMergeThr,
                rectangle => IntersectSphere(rectangle, center, radius * radius));
        }

        public static void RasterizeCapsule(RcHeightfield hf, RcVec3f start, RcVec3f end, float radius, int area, int flagMergeThr, RcContext ctx)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_RASTERIZE_CAPSULE);
            Span<float> bounds = stackalloc float[]
            {
                Math.Min(start.X, end.X) - radius, Math.Min(start.Y, end.Y) - radius,
                Math.Min(start.Z, end.Z) - radius, Math.Max(start.X, end.X) + radius, Math.Max(start.Y, end.Y) + radius,
                Math.Max(start.Z, end.Z) + radius
            };
            RcVec3f axis = new RcVec3f(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
            RasterizationFilledShape(hf, bounds, area, flagMergeThr,
                rectangle => IntersectCapsule(rectangle, start, end, axis, radius * radius));
        }

        public static void RasterizeCylinder(RcHeightfield hf, RcVec3f start, RcVec3f end, float radius, int area, int flagMergeThr, RcContext ctx)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_RASTERIZE_CYLINDER);
            Span<float> bounds = stackalloc float[]
            {
                Math.Min(start.X, end.X) - radius, Math.Min(start.Y, end.Y) - radius,
                Math.Min(start.Z, end.Z) - radius, Math.Max(start.X, end.X) + radius, Math.Max(start.Y, end.Y) + radius,
                Math.Max(start.Z, end.Z) + radius
            };
            RcVec3f axis = new RcVec3f(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
            RasterizationFilledShape(hf, bounds, area, flagMergeThr,
                rectangle => IntersectCylinder(rectangle, start, end, axis, radius * radius));
        }

        public static void RasterizeBox(RcHeightfield hf, RcVec3f center, RcVec3f[] halfEdges, int area, int flagMergeThr, RcContext ctx)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_RASTERIZE_BOX);
            Span<RcVec3f> normals = stackalloc RcVec3f[]
            {
                new RcVec3f(halfEdges[0].X, halfEdges[0].Y, halfEdges[0].Z),
                new RcVec3f(halfEdges[1].X, halfEdges[1].Y, halfEdges[1].Z),
                new RcVec3f(halfEdges[2].X, halfEdges[2].Y, halfEdges[2].Z),
            };
            normals[0] = RcVec3f.Normalize(normals[0]);
            normals[1] = RcVec3f.Normalize(normals[1]);
            normals[2] = RcVec3f.Normalize(normals[2]);

            float[] vertices = new float[8 * 3];
            Span<float> bounds = stackalloc float[]
            {
                float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity,
                float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity
            };
            for (int i = 0; i < 8; ++i)
            {
                float s0 = (i & 1) != 0 ? 1f : -1f;
                float s1 = (i & 2) != 0 ? 1f : -1f;
                float s2 = (i & 4) != 0 ? 1f : -1f;
                vertices[i * 3 + 0] = center.X + s0 * halfEdges[0].X + s1 * halfEdges[1].X + s2 * halfEdges[2].X;
                vertices[i * 3 + 1] = center.Y + s0 * halfEdges[0].Y + s1 * halfEdges[1].Y + s2 * halfEdges[2].Y;
                vertices[i * 3 + 2] = center.Z + s0 * halfEdges[0].Z + s1 * halfEdges[1].Z + s2 * halfEdges[2].Z;
                bounds[0] = Math.Min(bounds[0], vertices[i * 3 + 0]);
                bounds[1] = Math.Min(bounds[1], vertices[i * 3 + 1]);
                bounds[2] = Math.Min(bounds[2], vertices[i * 3 + 2]);
                bounds[3] = Math.Max(bounds[3], vertices[i * 3 + 0]);
                bounds[4] = Math.Max(bounds[4], vertices[i * 3 + 1]);
                bounds[5] = Math.Max(bounds[5], vertices[i * 3 + 2]);
            }

            float[][] planes = RcArrays.Create2D<float>(6, 4);
            for (int i = 0; i < 6; i++)
            {
                float m = i < 3 ? -1 : 1;
                int vi = i < 3 ? 0 : 7;
                planes[i][0] = m * normals[i % 3].X;
                planes[i][1] = m * normals[i % 3].Y;
                planes[i][2] = m * normals[i % 3].Z;
                planes[i][3] = vertices[vi * 3] * planes[i][0] + vertices[vi * 3 + 1] * planes[i][1]
                                                               + vertices[vi * 3 + 2] * planes[i][2];
            }

            RasterizationFilledShape(hf, bounds, area, flagMergeThr, rectangle => IntersectBox(rectangle, vertices, planes));
        }

        public static void RasterizeConvex(RcHeightfield hf, float[] vertices, int[] triangles, int area, int flagMergeThr, RcContext ctx)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_RASTERIZE_CONVEX);
            float[] bounds = new float[] { vertices[0], vertices[1], vertices[2], vertices[0], vertices[1], vertices[2] };
            for (int i = 0; i < vertices.Length; i += 3)
            {
                bounds[0] = Math.Min(bounds[0], vertices[i + 0]);
                bounds[1] = Math.Min(bounds[1], vertices[i + 1]);
                bounds[2] = Math.Min(bounds[2], vertices[i + 2]);
                bounds[3] = Math.Max(bounds[3], vertices[i + 0]);
                bounds[4] = Math.Max(bounds[4], vertices[i + 1]);
                bounds[5] = Math.Max(bounds[5], vertices[i + 2]);
            }


            float[][] planes = RcArrays.Create2D<float>(triangles.Length, 4);
            float[][] triBounds = RcArrays.Create2D<float>(triangles.Length / 3, 4);
            for (int i = 0, j = 0; i < triangles.Length; i += 3, j++)
            {
                int a = triangles[i] * 3;
                int b = triangles[i + 1] * 3;
                int c = triangles[i + 2] * 3;
                float[] ab = { vertices[b] - vertices[a], vertices[b + 1] - vertices[a + 1], vertices[b + 2] - vertices[a + 2] };
                float[] ac = { vertices[c] - vertices[a], vertices[c + 1] - vertices[a + 1], vertices[c + 2] - vertices[a + 2] };
                float[] bc = { vertices[c] - vertices[b], vertices[c + 1] - vertices[b + 1], vertices[c + 2] - vertices[b + 2] };
                float[] ca = { vertices[a] - vertices[c], vertices[a + 1] - vertices[c + 1], vertices[a + 2] - vertices[c + 2] };
                Plane(planes, i, ab, ac, vertices, a);
                Plane(planes, i + 1, planes[i], bc, vertices, b);
                Plane(planes, i + 2, planes[i], ca, vertices, c);

                float s = 1.0f / (vertices[a] * planes[i + 1][0] + vertices[a + 1] * planes[i + 1][1]
                                                                 + vertices[a + 2] * planes[i + 1][2] - planes[i + 1][3]);
                planes[i + 1][0] *= s;
                planes[i + 1][1] *= s;
                planes[i + 1][2] *= s;
                planes[i + 1][3] *= s;

                s = 1.0f / (vertices[b] * planes[i + 2][0] + vertices[b + 1] * planes[i + 2][1] + vertices[b + 2] * planes[i + 2][2]
                            - planes[i + 2][3]);
                planes[i + 2][0] *= s;
                planes[i + 2][1] *= s;
                planes[i + 2][2] *= s;
                planes[i + 2][3] *= s;

                triBounds[j][0] = Math.Min(Math.Min(vertices[a], vertices[b]), vertices[c]);
                triBounds[j][1] = Math.Min(Math.Min(vertices[a + 2], vertices[b + 2]), vertices[c + 2]);
                triBounds[j][2] = Math.Max(Math.Max(vertices[a], vertices[b]), vertices[c]);
                triBounds[j][3] = Math.Max(Math.Max(vertices[a + 2], vertices[b + 2]), vertices[c + 2]);
            }

            RasterizationFilledShape(hf, bounds, area, flagMergeThr,
                rectangle => IntersectConvex(rectangle, triangles, vertices, planes, triBounds));
        }

        private static void Plane(float[][] planes, int p, float[] v1, float[] v2, float[] vertices, int vert)
        {
            RcVec.Cross(planes[p], v1, v2);
            planes[p][3] = planes[p][0] * vertices[vert] + planes[p][1] * vertices[vert + 1] + planes[p][2] * vertices[vert + 2];
        }

        private static void RasterizationFilledShape(RcHeightfield hf, Span<float> bounds, int area, int flagMergeThr,
            Func<float[], float[]> intersection)
        {
            if (!OverlapBounds(hf.bmin, hf.bmax, bounds))
            {
                return;
            }

            bounds[3] = Math.Min(bounds[3], hf.bmax.X);
            bounds[5] = Math.Min(bounds[5], hf.bmax.Z);
            bounds[0] = Math.Max(bounds[0], hf.bmin.X);
            bounds[2] = Math.Max(bounds[2], hf.bmin.Z);

            if (bounds[3] <= bounds[0] || bounds[4] <= bounds[1] || bounds[5] <= bounds[2])
            {
                return;
            }

            float ics = 1.0f / hf.cs;
            float ich = 1.0f / hf.ch;
            int xMin = (int)((bounds[0] - hf.bmin.X) * ics);
            int zMin = (int)((bounds[2] - hf.bmin.Z) * ics);
            int xMax = Math.Min(hf.width - 1, (int)((bounds[3] - hf.bmin.X) * ics));
            int zMax = Math.Min(hf.height - 1, (int)((bounds[5] - hf.bmin.Z) * ics));
            float[] rectangle = new float[5];
            rectangle[4] = hf.bmin.Y;
            for (int x = xMin; x <= xMax; x++)
            {
                for (int z = zMin; z <= zMax; z++)
                {
                    rectangle[0] = x * hf.cs + hf.bmin.X;
                    rectangle[1] = z * hf.cs + hf.bmin.Z;
                    rectangle[2] = rectangle[0] + hf.cs;
                    rectangle[3] = rectangle[1] + hf.cs;
                    float[] h = intersection.Invoke(rectangle);
                    if (h != null)
                    {
                        int smin = (int)MathF.Floor((h[0] - hf.bmin.Y) * ich);
                        int smax = (int)MathF.Ceiling((h[1] - hf.bmin.Y) * ich);
                        if (smin != smax)
                        {
                            int ismin = Math.Clamp(smin, 0, RC_SPAN_MAX_HEIGHT);
                            int ismax = Math.Clamp(smax, ismin + 1, RC_SPAN_MAX_HEIGHT);
                            RcRasterizations.AddSpan(hf, x, z, ismin, ismax, area, flagMergeThr);
                        }
                    }
                }
            }
        }

        private static float[] IntersectSphere(float[] rectangle, RcVec3f center, float radiusSqr)
        {
            float x = Math.Max(rectangle[0], Math.Min(center.X, rectangle[2]));
            float y = rectangle[4];
            float z = Math.Max(rectangle[1], Math.Min(center.Z, rectangle[3]));

            float mx = x - center.X;
            float my = y - center.Y;
            float mz = z - center.Z;

            float b = my; // Dot(m, d) d = (0, 1, 0)
            float c = LenSqr(mx, my, mz) - radiusSqr;
            if (c > 0.0f && b > 0.0f)
            {
                return null;
            }

            float discr = b * b - c;
            if (discr < 0.0f)
            {
                return null;
            }

            float discrSqrt = MathF.Sqrt(discr);
            float tmin = -b - discrSqrt;
            float tmax = -b + discrSqrt;

            if (tmin < 0.0f)
            {
                tmin = 0.0f;
            }

            return new float[] { y + tmin, y + tmax };
        }

        private static float[] IntersectCapsule(float[] rectangle, RcVec3f start, RcVec3f end, RcVec3f axis, float radiusSqr)
        {
            float[] s = MergeIntersections(IntersectSphere(rectangle, start, radiusSqr), IntersectSphere(rectangle, end, radiusSqr));
            float axisLen2dSqr = axis.X * axis.X + axis.Z * axis.Z;
            if (axisLen2dSqr > EPSILON)
            {
                s = SlabsCylinderIntersection(rectangle, start, end, axis, radiusSqr, s);
            }

            return s;
        }

        private static float[] IntersectCylinder(float[] rectangle, RcVec3f start, RcVec3f end, RcVec3f axis, float radiusSqr)
        {
            float[] s = MergeIntersections(
                RayCylinderIntersection(new RcVec3f(
                    Math.Clamp(start.X, rectangle[0], rectangle[2]), rectangle[4],
                    Math.Clamp(start.Z, rectangle[1], rectangle[3])
                ), start, axis, radiusSqr),
                RayCylinderIntersection(new RcVec3f(
                    Math.Clamp(end.X, rectangle[0], rectangle[2]), rectangle[4],
                    Math.Clamp(end.Z, rectangle[1], rectangle[3])
                ), start, axis, radiusSqr));
            float axisLen2dSqr = axis.X * axis.X + axis.Z * axis.Z;
            if (axisLen2dSqr > EPSILON)
            {
                s = SlabsCylinderIntersection(rectangle, start, end, axis, radiusSqr, s);
            }

            if (axis.Y * axis.Y > EPSILON)
            {
                Span<RcVec3f> rectangleOnStartPlane = stackalloc RcVec3f[4];
                Span<RcVec3f> rectangleOnEndPlane = stackalloc RcVec3f[4];
                float ds = RcVec3f.Dot(axis, start);
                float de = RcVec3f.Dot(axis, end);
                for (int i = 0; i < 4; i++)
                {
                    float x = rectangle[(i + 1) & 2];
                    float z = rectangle[(i & 2) + 1];
                    RcVec3f a = new RcVec3f(x, rectangle[4], z);
                    float dotAxisA = RcVec3f.Dot(axis, a);
                    float t = (ds - dotAxisA) / axis.Y;
                    rectangleOnStartPlane[i].X = x;
                    rectangleOnStartPlane[i].Y = rectangle[4] + t;
                    rectangleOnStartPlane[i].Z = z;
                    t = (de - dotAxisA) / axis.Y;
                    rectangleOnEndPlane[i].X = x;
                    rectangleOnEndPlane[i].Y = rectangle[4] + t;
                    rectangleOnEndPlane[i].Z = z;
                }

                for (int i = 0; i < 4; i++)
                {
                    s = CylinderCapIntersection(start, radiusSqr, s, i, rectangleOnStartPlane);
                    s = CylinderCapIntersection(end, radiusSqr, s, i, rectangleOnEndPlane);
                }
            }

            return s;
        }

        private static float[] CylinderCapIntersection(RcVec3f start, float radiusSqr, float[] s, int i, Span<RcVec3f> rectangleOnPlane)
        {
            int j = (i + 1) % 4;
            // Ray against sphere intersection
            var m = new RcVec3f(
                rectangleOnPlane[i].X - start.X,
                rectangleOnPlane[i].Y - start.Y,
                rectangleOnPlane[i].Z - start.Z
            );
            var d = new RcVec3f(
                rectangleOnPlane[j].X - rectangleOnPlane[i].X,
                rectangleOnPlane[j].Y - rectangleOnPlane[i].Y,
                rectangleOnPlane[j].Z - rectangleOnPlane[i].Z
            );
            float dl = RcVec3f.Dot(d, d);
            float b = RcVec3f.Dot(m, d) / dl;
            float c = (RcVec3f.Dot(m, m) - radiusSqr) / dl;
            float discr = b * b - c;
            if (discr > EPSILON)
            {
                float discrSqrt = MathF.Sqrt(discr);
                float t1 = -b - discrSqrt;
                float t2 = -b + discrSqrt;
                if (t1 <= 1 && t2 >= 0)
                {
                    t1 = Math.Max(0, t1);
                    t2 = Math.Min(1, t2);
                    float y1 = rectangleOnPlane[i].Y + t1 * d.Y;
                    float y2 = rectangleOnPlane[i].Y + t2 * d.Y;
                    float[] y = { Math.Min(y1, y2), Math.Max(y1, y2) };
                    s = MergeIntersections(s, y);
                }
            }

            return s;
        }

        private static float[] SlabsCylinderIntersection(float[] rectangle, RcVec3f start, RcVec3f end, RcVec3f axis, float radiusSqr, float[] s)
        {
            if (Math.Min(start.X, end.X) < rectangle[0])
            {
                s = MergeIntersections(s, XSlabCylinderIntersection(rectangle, start, axis, radiusSqr, rectangle[0]));
            }

            if (Math.Max(start.X, end.X) > rectangle[2])
            {
                s = MergeIntersections(s, XSlabCylinderIntersection(rectangle, start, axis, radiusSqr, rectangle[2]));
            }

            if (Math.Min(start.Z, end.Z) < rectangle[1])
            {
                s = MergeIntersections(s, ZSlabCylinderIntersection(rectangle, start, axis, radiusSqr, rectangle[1]));
            }

            if (Math.Max(start.Z, end.Z) > rectangle[3])
            {
                s = MergeIntersections(s, ZSlabCylinderIntersection(rectangle, start, axis, radiusSqr, rectangle[3]));
            }

            return s;
        }

        private static float[] XSlabCylinderIntersection(float[] rectangle, RcVec3f start, RcVec3f axis, float radiusSqr, float x)
        {
            return RayCylinderIntersection(XSlabRayIntersection(rectangle, start, axis, x), start, axis, radiusSqr);
        }

        private static RcVec3f XSlabRayIntersection(float[] rectangle, RcVec3f start, RcVec3f direction, float x)
        {
            // 2d intersection of plane and segment
            float t = (x - start.X) / direction.X;
            float z = Math.Clamp(start.Z + t * direction.Z, rectangle[1], rectangle[3]);
            return new RcVec3f(x, rectangle[4], z);
        }

        private static float[] ZSlabCylinderIntersection(float[] rectangle, RcVec3f start, RcVec3f axis, float radiusSqr, float z)
        {
            return RayCylinderIntersection(ZSlabRayIntersection(rectangle, start, axis, z), start, axis, radiusSqr);
        }

        private static RcVec3f ZSlabRayIntersection(float[] rectangle, RcVec3f start, RcVec3f direction, float z)
        {
            // 2d intersection of plane and segment
            float t = (z - start.Z) / direction.Z;
            float x = Math.Clamp(start.X + t * direction.X, rectangle[0], rectangle[2]);
            return new RcVec3f(x, rectangle[4], z);
        }

        // Based on Christer Ericsons's "Real-Time Collision Detection"
        private static float[] RayCylinderIntersection(RcVec3f point, RcVec3f start, RcVec3f axis, float radiusSqr)
        {
            RcVec3f d = axis;
            RcVec3f m = new RcVec3f(point.X - start.X, point.Y - start.Y, point.Z - start.Z);
            // float[] n = { 0, 1, 0 };
            float md = RcVec3f.Dot(m, d);
            // float nd = Dot(n, d);
            float nd = axis.Y;
            float dd = RcVec3f.Dot(d, d);

            // float nn = Dot(n, n);
            float nn = 1;
            // float mn = Dot(m, n);
            float mn = m.Y;
            // float a = dd * nn - nd * nd;
            float a = dd - nd * nd;
            float k = RcVec3f.Dot(m, m) - radiusSqr;
            float c = dd * k - md * md;
            if (MathF.Abs(a) < EPSILON)
            {
                // Segment runs parallel to cylinder axis
                if (c > 0.0f)
                {
                    return null; // ’a’ and thus the segment lie outside cylinder
                }

                // Now known that segment intersects cylinder; figure out how it intersects
                float tt1 = -mn / nn; // Intersect segment against ’p’ endcap
                float tt2 = (nd - mn) / nn; // Intersect segment against ’q’ endcap
                return new float[] { point.Y + Math.Min(tt1, tt2), point.Y + Math.Max(tt1, tt2) };
            }

            float b = dd * mn - nd * md;
            float discr = b * b - a * c;
            if (discr < 0.0f)
            {
                return null; // No real roots; no intersection
            }

            float discSqrt = MathF.Sqrt(discr);
            float t1 = (-b - discSqrt) / a;
            float t2 = (-b + discSqrt) / a;

            if (md + t1 * nd < 0.0f)
            {
                // Intersection outside cylinder on ’p’ side
                t1 = -md / nd;
                if (k + t1 * (2 * mn + t1 * nn) > 0.0f)
                {
                    return null;
                }
            }
            else if (md + t1 * nd > dd)
            {
                // Intersection outside cylinder on ’q’ side
                t1 = (dd - md) / nd;
                if (k + dd - 2 * md + t1 * (2 * (mn - nd) + t1 * nn) > 0.0f)
                {
                    return null;
                }
            }

            if (md + t2 * nd < 0.0f)
            {
                // Intersection outside cylinder on ’p’ side
                t2 = -md / nd;
                if (k + t2 * (2 * mn + t2 * nn) > 0.0f)
                {
                    return null;
                }
            }
            else if (md + t2 * nd > dd)
            {
                // Intersection outside cylinder on ’q’ side
                t2 = (dd - md) / nd;
                if (k + dd - 2 * md + t2 * (2 * (mn - nd) + t2 * nn) > 0.0f)
                {
                    return null;
                }
            }

            return new float[] { point.Y + Math.Min(t1, t2), point.Y + Math.Max(t1, t2) };
        }

        private static float[] IntersectBox(float[] rectangle, Span<float> vertices, float[][] planes)
        {
            float yMin = float.PositiveInfinity;
            float yMax = float.NegativeInfinity;
            // check intersection with rays starting in box vertices first
            for (int i = 0; i < 8; i++)
            {
                int vi = i * 3;
                if (vertices[vi] >= rectangle[0] && vertices[vi] < rectangle[2] &&
                    vertices[vi + 2] >= rectangle[1] && vertices[vi + 2] < rectangle[3])
                {
                    yMin = Math.Min(yMin, vertices[vi + 1]);
                    yMax = Math.Max(yMax, vertices[vi + 1]);
                }
            }

            // check intersection with rays starting in rectangle vertices
            var point = new RcVec3f(0, rectangle[1], 0);
            for (int i = 0; i < 4; i++)
            {
                point.X = ((i & 1) == 0) ? rectangle[0] : rectangle[2];
                point.Z = ((i & 2) == 0) ? rectangle[1] : rectangle[3];
                for (int j = 0; j < 6; j++)
                {
                    if (MathF.Abs(planes[j][1]) > EPSILON)
                    {
                        float dotNormalPoint = RcVec3f.Dot(planes[j].ToVec3(), point);
                        float t = (planes[j][3] - dotNormalPoint) / planes[j][1];
                        float y = point.Y + t;
                        bool valid = true;
                        for (int k = 0; k < 6; k++)
                        {
                            if (k != j)
                            {
                                if (point.X * planes[k][0] + y * planes[k][1] + point.Z * planes[k][2] > planes[k][3])
                                {
                                    valid = false;
                                    break;
                                }
                            }
                        }

                        if (valid)
                        {
                            yMin = Math.Min(yMin, y);
                            yMax = Math.Max(yMax, y);
                        }
                    }
                }
            }

            // check intersection with box edges
            for (int i = 0; i < BOX_EDGES.Length; i += 2)
            {
                int vi = BOX_EDGES[i] * 3;
                int vj = BOX_EDGES[i + 1] * 3;
                float x = vertices[vi];
                float z = vertices[vi + 2];
                // edge slab intersection
                float y = vertices[vi + 1];
                float dx = vertices[vj] - x;
                float dy = vertices[vj + 1] - y;
                float dz = vertices[vj + 2] - z;
                if (MathF.Abs(dx) > EPSILON)
                {
                    if (XSlabSegmentIntersection(rectangle, x, y, z, dx, dy, dz, rectangle[0], out var iy))
                    {
                        yMin = Math.Min(yMin, iy);
                        yMax = Math.Max(yMax, iy);
                    }

                    if (XSlabSegmentIntersection(rectangle, x, y, z, dx, dy, dz, rectangle[2], out iy))
                    {
                        yMin = Math.Min(yMin, iy);
                        yMax = Math.Max(yMax, iy);
                    }
                }

                if (MathF.Abs(dz) > EPSILON)
                {
                    if (ZSlabSegmentIntersection(rectangle, x, y, z, dx, dy, dz, rectangle[1], out var iy))
                    {
                        yMin = Math.Min(yMin, iy);
                        yMax = Math.Max(yMax, iy);
                    }

                    if (ZSlabSegmentIntersection(rectangle, x, y, z, dx, dy, dz, rectangle[3], out iy))
                    {
                        yMin = Math.Min(yMin, iy);
                        yMax = Math.Max(yMax, iy);
                    }
                }
            }

            if (yMin <= yMax)
            {
                return new float[] { yMin, yMax };
            }

            return null;
        }

        private static float[] IntersectConvex(float[] rectangle, int[] triangles, float[] verts, float[][] planes,
            float[][] triBounds)
        {
            float imin = float.PositiveInfinity;
            float imax = float.NegativeInfinity;
            for (int tr = 0, tri = 0; tri < triangles.Length; tr++, tri += 3)
            {
                if (triBounds[tr][0] > rectangle[2] || triBounds[tr][2] < rectangle[0] || triBounds[tr][1] > rectangle[3]
                    || triBounds[tr][3] < rectangle[1])
                {
                    continue;
                }

                if (MathF.Abs(planes[tri][1]) < EPSILON)
                {
                    continue;
                }

                for (int i = 0; i < 3; i++)
                {
                    int vi = triangles[tri + i] * 3;
                    int vj = triangles[tri + (i + 1) % 3] * 3;
                    float x = verts[vi];
                    float z = verts[vi + 2];
                    // triangle vertex
                    if (x >= rectangle[0] && x <= rectangle[2] && z >= rectangle[1] && z <= rectangle[3])
                    {
                        imin = Math.Min(imin, verts[vi + 1]);
                        imax = Math.Max(imax, verts[vi + 1]);
                    }

                    // triangle slab intersection
                    float y = verts[vi + 1];
                    float dx = verts[vj] - x;
                    float dy = verts[vj + 1] - y;
                    float dz = verts[vj + 2] - z;
                    if (MathF.Abs(dx) > EPSILON)
                    {
                        if (XSlabSegmentIntersection(rectangle, x, y, z, dx, dy, dz, rectangle[0], out var iy))
                        {
                            imin = Math.Min(imin, iy);
                            imax = Math.Max(imax, iy);
                        }

                        if (XSlabSegmentIntersection(rectangle, x, y, z, dx, dy, dz, rectangle[2], out iy))
                        {
                            imin = Math.Min(imin, iy);
                            imax = Math.Max(imax, iy);
                        }
                    }

                    if (MathF.Abs(dz) > EPSILON)
                    {
                        if (ZSlabSegmentIntersection(rectangle, x, y, z, dx, dy, dz, rectangle[1], out var iy))
                        {
                            imin = Math.Min(imin, iy);
                            imax = Math.Max(imax, iy);
                        }

                        if (ZSlabSegmentIntersection(rectangle, x, y, z, dx, dy, dz, rectangle[3], out iy))
                        {
                            imin = Math.Min(imin, iy);
                            imax = Math.Max(imax, iy);
                        }
                    }
                }

                // rectangle vertex
                var point = new RcVec3f(0, rectangle[1], 0);
                for (int i = 0; i < 4; i++)
                {
                    point.X = ((i & 1) == 0) ? rectangle[0] : rectangle[2];
                    point.Z = ((i & 2) == 0) ? rectangle[1] : rectangle[3];
                    if (RayTriangleIntersection(point, tri, planes, out var y))
                    {
                        imin = Math.Min(imin, y);
                        imax = Math.Max(imax, y);
                    }
                }
            }

            if (imin < imax)
            {
                return new float[] { imin, imax };
            }

            return null;
        }

        private static bool XSlabSegmentIntersection(float[] rectangle, float x, float y, float z, float dx, float dy, float dz, float slabX, out float iy)
        {
            float x2 = x + dx;
            if ((x < slabX && x2 > slabX) || (x > slabX && x2 < slabX))
            {
                float t = (slabX - x) / dx;
                float iz = z + dz * t;
                if (iz >= rectangle[1] && iz <= rectangle[3])
                {
                    iy = y + dy * t;
                    return true;
                }
            }

            iy = 0.0f;
            return false;
        }

        private static bool ZSlabSegmentIntersection(float[] rectangle, float x, float y, float z, float dx, float dy, float dz, float slabZ, out float iy)
        {
            float z2 = z + dz;
            if ((z < slabZ && z2 > slabZ) || (z > slabZ && z2 < slabZ))
            {
                float t = (slabZ - z) / dz;
                float ix = x + dx * t;
                if (ix >= rectangle[0] && ix <= rectangle[2])
                {
                    iy = y + dy * t;
                    return true;
                }
            }

            iy = 0.0f;
            return false;
        }

        private static bool RayTriangleIntersection(RcVec3f point, int plane, float[][] planes, out float y)
        {
            y = 0.0f;
            float t = (planes[plane][3] - RcVec3f.Dot(planes[plane].ToVec3(), point)) / planes[plane][1];
            RcVec3f s = new RcVec3f(point.X, point.Y + t, point.Z);
            float u = RcVec3f.Dot(s, planes[plane + 1].ToVec3()) - planes[plane + 1][3];
            if (u < 0.0f || u > 1.0f)
            {
                return false;
            }

            float v = RcVec3f.Dot(s, planes[plane + 2].ToVec3()) - planes[plane + 2][3];
            if (v < 0.0f)
            {
                return false;
            }

            float w = 1f - u - v;
            if (w < 0.0f)
            {
                return false;
            }

            y = s.Y;
            return true;
        }

        private static float[] MergeIntersections(float[] s1, float[] s2)
        {
            if (s1 == null)
            {
                return s2;
            }

            if (s2 == null)
            {
                return s1;
            }

            return new float[] { Math.Min(s1[0], s2[0]), Math.Max(s1[1], s2[1]) };
        }

        private static float LenSqr(float dx, float dy, float dz)
        {
            return dx * dx + dy * dy + dz * dz;
        }

        private static bool OverlapBounds(RcVec3f amin, RcVec3f amax, ReadOnlySpan<float> bounds)
        {
            bool overlap = true;
            overlap = (amin.X > bounds[3] || amax.X < bounds[0]) ? false : overlap;
            overlap = (amin.Y > bounds[4]) ? false : overlap;
            overlap = (amin.Z > bounds[5] || amax.Z < bounds[2]) ? false : overlap;
            return overlap;
        }
    }
}