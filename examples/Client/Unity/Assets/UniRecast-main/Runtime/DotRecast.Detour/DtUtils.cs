using System;
using DotRecast.Core.Numerics;

namespace DotRecast.Detour
{
    public static class DtUtils
    {
        public static int NextPow2(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v;
        }

        public static int Ilog2(int v)
        {
            int r;
            int shift;
            r = (v > 0xffff ? 1 : 0) << 4;
            v >>= r;
            shift = (v > 0xff ? 1 : 0) << 3;
            v >>= shift;
            r |= shift;
            shift = (v > 0xf ? 1 : 0) << 2;
            v >>= shift;
            r |= shift;
            shift = (v > 0x3 ? 1 : 0) << 1;
            v >>= shift;
            r |= shift;
            r |= (v >> 1);
            return r;
        }

        /// Determines if two axis-aligned bounding boxes overlap.
        /// @param[in] amin Minimum bounds of box A. [(x, y, z)]
        /// @param[in] amax Maximum bounds of box A. [(x, y, z)]
        /// @param[in] bmin Minimum bounds of box B. [(x, y, z)]
        /// @param[in] bmax Maximum bounds of box B. [(x, y, z)]
        /// @return True if the two AABB's overlap.
        /// @see dtOverlapBounds
        public static bool OverlapQuantBounds(ref RcVec3i amin, ref RcVec3i amax, ref RcVec3i bmin, ref RcVec3i bmax)
        {
            bool overlap = true;
            overlap = (amin.X > bmax.X || amax.X < bmin.X) ? false : overlap;
            overlap = (amin.Y > bmax.Y || amax.Y < bmin.Y) ? false : overlap;
            overlap = (amin.Z > bmax.Z || amax.Z < bmin.Z) ? false : overlap;
            return overlap;
        }

        /// Determines if two axis-aligned bounding boxes overlap.
        /// @param[in] amin Minimum bounds of box A. [(x, y, z)]
        /// @param[in] amax Maximum bounds of box A. [(x, y, z)]
        /// @param[in] bmin Minimum bounds of box B. [(x, y, z)]
        /// @param[in] bmax Maximum bounds of box B. [(x, y, z)]
        /// @return True if the two AABB's overlap.
        /// @see dtOverlapQuantBounds
        public static bool OverlapBounds(RcVec3f amin, RcVec3f amax, RcVec3f bmin, RcVec3f bmax)
        {
            bool overlap = true;
            overlap = (amin.X > bmax.X || amax.X < bmin.X) ? false : overlap;
            overlap = (amin.Y > bmax.Y || amax.Y < bmin.Y) ? false : overlap;
            overlap = (amin.Z > bmax.Z || amax.Z < bmin.Z) ? false : overlap;
            return overlap;
        }

        public static bool OverlapRange(float amin, float amax, float bmin, float bmax, float eps)
        {
            return ((amin + eps) > bmax || (amax - eps) < bmin) ? false : true;
        }

        /// @par
        ///
        /// All vertices are projected onto the xz-plane, so the y-values are ignored.
        public static bool OverlapPolyPoly2D(Span<float> polya, int npolya, Span<float> polyb, int npolyb)
        {
            const float eps = 1e-4f;
            for (int i = 0, j = npolya - 1; i < npolya; j = i++)
            {
                int va = j * 3;
                int vb = i * 3;

                RcVec3f n = new RcVec3f(polya[vb + 2] - polya[va + 2], 0, -(polya[vb + 0] - polya[va + 0]));

                RcVec2f aminmax = ProjectPoly(n, polya, npolya);
                RcVec2f bminmax = ProjectPoly(n, polyb, npolyb);
                if (!OverlapRange(aminmax.X, aminmax.Y, bminmax.X, bminmax.Y, eps))
                {
                    // Found separating axis
                    return false;
                }
            }

            for (int i = 0, j = npolyb - 1; i < npolyb; j = i++)
            {
                int va = j * 3;
                int vb = i * 3;

                RcVec3f n = new RcVec3f(polyb[vb + 2] - polyb[va + 2], 0, -(polyb[vb + 0] - polyb[va + 0]));

                RcVec2f aminmax = ProjectPoly(n, polya, npolya);
                RcVec2f bminmax = ProjectPoly(n, polyb, npolyb);
                if (!OverlapRange(aminmax.X, aminmax.Y, bminmax.X, bminmax.Y, eps))
                {
                    // Found separating axis
                    return false;
                }
            }

            return true;
        }


        /// @}
        /// @name Computational geometry helper functions.
        /// @{
        /// Derives the signed xz-plane area of the triangle ABC, or the
        /// relationship of line AB to point C.
        /// @param[in] a Vertex A. [(x, y, z)]
        /// @param[in] b Vertex B. [(x, y, z)]
        /// @param[in] c Vertex C. [(x, y, z)]
        /// @return The signed xz-plane area of the triangle.
        public static float TriArea2D(Span<float> verts, int a, int b, int c)
        {
            float abx = verts[b] - verts[a];
            float abz = verts[b + 2] - verts[a + 2];
            float acx = verts[c] - verts[a];
            float acz = verts[c + 2] - verts[a + 2];
            return acx * abz - abx * acz;
        }

        public static float TriArea2D(RcVec3f a, RcVec3f b, RcVec3f c)
        {
            float abx = b.X - a.X;
            float abz = b.Z - a.Z;
            float acx = c.X - a.X;
            float acz = c.Z - a.Z;
            return acx * abz - abx * acz;
        }

        // Returns a random point in a convex polygon.
        // Adapted from Graphics Gems article.
        public static void RandomPointInConvexPoly(Span<float> pts, int npts, Span<float> areas, float s, float t, out RcVec3f @out)
        {
            // Calc triangle araes
            float areasum = 0.0f;
            for (int i = 2; i < npts; i++)
            {
                areas[i] = TriArea2D(pts, 0, (i - 1) * 3, i * 3);
                areasum += Math.Max(0.001f, areas[i]);
            }

            // Find sub triangle weighted by area.
            float thr = s * areasum;
            float acc = 0.0f;
            float u = 1.0f;
            int tri = npts - 1;
            for (int i = 2; i < npts; i++)
            {
                float dacc = areas[i];
                if (thr >= acc && thr < (acc + dacc))
                {
                    u = (thr - acc) / dacc;
                    tri = i;
                    break;
                }

                acc += dacc;
            }

            float v = MathF.Sqrt(t);

            float a = 1 - v;
            float b = (1 - u) * v;
            float c = u * v;
            int pa = 0;
            int pb = (tri - 1) * 3;
            int pc = tri * 3;

            @out = new RcVec3f()
            {
                X = a * pts[pa] + b * pts[pb] + c * pts[pc],
                Y = a * pts[pa + 1] + b * pts[pb + 1] + c * pts[pc + 1],
                Z = a * pts[pa + 2] + b * pts[pb + 2] + c * pts[pc + 2]
            };
        }

        public static bool ClosestHeightPointTriangle(RcVec3f p, RcVec3f a, RcVec3f b, RcVec3f c, out float h)
        {
            const float EPS = 1e-6f;

            h = 0;
            RcVec3f v0 = RcVec3f.Subtract(c, a);
            RcVec3f v1 = RcVec3f.Subtract(b, a);
            RcVec3f v2 = RcVec3f.Subtract(p, a);

            // Compute scaled barycentric coordinates
            float denom = v0.X * v1.Z - v0.Z * v1.X;
            if (MathF.Abs(denom) < EPS)
            {
                return false;
            }

            float u = v1.Z * v2.X - v1.X * v2.Z;
            float v = v0.X * v2.Z - v0.Z * v2.X;

            if (denom < 0)
            {
                denom = -denom;
                u = -u;
                v = -v;
            }

            // If point lies inside the triangle, return interpolated ycoord.
            if (u >= 0.0f && v >= 0.0f && (u + v) <= denom)
            {
                h = a.Y + (v0.Y * u + v1.Y * v) / denom;
                return true;
            }

            return false;
        }

        public static RcVec2f ProjectPoly(RcVec3f axis, Span<float> poly, int npoly)
        {
            float rmin, rmax;
            rmin = rmax = axis.Dot2D(poly.ToVec3());
            for (int i = 1; i < npoly; ++i)
            {
                float d = axis.Dot2D(poly.ToVec3(i * 3));
                rmin = Math.Min(rmin, d);
                rmax = Math.Max(rmax, d);
            }

            return new RcVec2f
            {
                X = rmin,
                Y = rmax,
            };
        }

        /// @par
        ///
        /// All points are projected onto the xz-plane, so the y-values are ignored.
        public static bool PointInPolygon(RcVec3f pt, Span<float> verts, int nverts)
        {
            // TODO: Replace pnpoly with triArea2D tests?
            int i, j;
            bool c = false;
            for (i = 0, j = nverts - 1; i < nverts; j = i++)
            {
                int vi = i * 3;
                int vj = j * 3;
                if (((verts[vi + 2] > pt.Z) != (verts[vj + 2] > pt.Z)) && (pt.X < (verts[vj + 0] - verts[vi + 0])
                        * (pt.Z - verts[vi + 2]) / (verts[vj + 2] - verts[vi + 2]) + verts[vi + 0]))
                {
                    c = !c;
                }
            }

            return c;
        }

        public static bool DistancePtPolyEdgesSqr(RcVec3f pt, Span<float> verts, int nverts, Span<float> ed, Span<float> et)
        {
            // TODO: Replace pnpoly with triArea2D tests?
            int i, j;
            bool c = false;
            for (i = 0, j = nverts - 1; i < nverts; j = i++)
            {
                int vi = i * 3;
                int vj = j * 3;
                if (((verts[vi + 2] > pt.Z) != (verts[vj + 2] > pt.Z)) &&
                    (pt.X < (verts[vj + 0] - verts[vi + 0]) * (pt.Z - verts[vi + 2]) / (verts[vj + 2] - verts[vi + 2]) + verts[vi + 0]))
                {
                    c = !c;
                }

                ed[j] = DistancePtSegSqr2D(pt, verts, vj, vi, out et[j]);
            }

            return c;
        }

        public static float DistancePtSegSqr2D(RcVec3f pt, Span<float> verts, int p, int q, out float t)
        {
            var vp = verts.ToVec3(p);
            var vq = verts.ToVec3(q);
            return DistancePtSegSqr2D(pt, vp, vq, out t);
        }

        public static float DistancePtSegSqr2D(RcVec3f pt, RcVec3f p, RcVec3f q, out float t)
        {
            float pqx = q.X - p.X;
            float pqz = q.Z - p.Z;
            float dx = pt.X - p.X;
            float dz = pt.Z - p.Z;
            float d = pqx * pqx + pqz * pqz;
            t = pqx * dx + pqz * dz;
            if (d > 0)
            {
                t /= d;
            }

            if (t < 0)
            {
                t = 0;
            }
            else if (t > 1)
            {
                t = 1;
            }

            dx = p.X + t * pqx - pt.X;
            dz = p.Z + t * pqz - pt.Z;
            return dx * dx + dz * dz;
        }

        public static bool IntersectSegmentPoly2D(RcVec3f p0, RcVec3f p1,
            Span<RcVec3f> verts, int nverts,
            out float tmin, out float tmax,
            out int segMin, out int segMax)
        {
            const float EPS = 0.000001f;

            tmin = 0;
            tmax = 1;
            segMin = -1;
            segMax = -1;

            var dir = RcVec3f.Subtract(p1, p0);

            var p0v = p0;
            for (int i = 0, j = nverts - 1; i < nverts; j = i++)
            {
                RcVec3f vpj = verts[j];
                RcVec3f vpi = verts[i];
                var edge = RcVec3f.Subtract(vpi, vpj);
                var diff = RcVec3f.Subtract(p0v, vpj);
                float n = RcVec.Perp2D(edge, diff);
                float d = RcVec.Perp2D(dir, edge);
                if (MathF.Abs(d) < EPS)
                {
                    // S is nearly parallel to this edge
                    if (n < 0)
                    {
                        return false;
                    }
                    else
                    {
                        continue;
                    }
                }

                float t = n / d;
                if (d < 0)
                {
                    // segment S is entering across this edge
                    if (t > tmin)
                    {
                        tmin = t;
                        segMin = j;
                        // S enters after leaving polygon
                        if (tmin > tmax)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    // segment S is leaving across this edge
                    if (t < tmax)
                    {
                        tmax = t;
                        segMax = j;
                        // S leaves before entering polygon
                        if (tmax < tmin)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public static int OppositeTile(int side)
        {
            return (side + 4) & 0x7;
        }


        public static bool IntersectSegSeg2D(RcVec3f ap, RcVec3f aq, RcVec3f bp, RcVec3f bq, out float s, out float t)
        {
            s = 0;
            t = 0;

            RcVec3f u = RcVec3f.Subtract(aq, ap);
            RcVec3f v = RcVec3f.Subtract(bq, bp);
            RcVec3f w = RcVec3f.Subtract(ap, bp);
            float d = RcVec.PerpXZ(u, v);
            if (MathF.Abs(d) < 1e-6f)
            {
                return false;
            }

            s = RcVec.PerpXZ(v, w) / d;
            t = RcVec.PerpXZ(u, w) / d;

            return true;
        }
    }
}