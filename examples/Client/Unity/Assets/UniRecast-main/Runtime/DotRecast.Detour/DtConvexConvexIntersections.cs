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
using DotRecast.Core.Numerics;

namespace DotRecast.Detour
{
    // Convex-convex intersection based on "Computational Geometry in C" by Joseph O'Rourke
    public static class DtConvexConvexIntersections
    {
        private const float EPSILON = 0.0001f;

        public static int CalculateIntersectionBufferSize(int n, int m)
        {
            return Math.Max(m, n) * 3 * 3;
        }

        public static bool Intersect(ReadOnlySpan<float> p, ReadOnlySpan<float> q, Span<float> intersection, out int nin)
        {
            nin = 0;
            int n = p.Length / 3;
            int m = q.Length / 3;
            int maxInters = CalculateIntersectionBufferSize(n, m);
            Span<float> inters = stackalloc float[maxInters];
            int ii = 0;
            
            /* Initialize variables. */
            RcVec3f a = new RcVec3f();
            RcVec3f b = new RcVec3f();
            RcVec3f a1 = new RcVec3f();
            RcVec3f b1 = new RcVec3f();

            int aa = 0;
            int ba = 0;
            int ai = 0;
            int bi = 0;

            DtConvexConvexInFlag f = DtConvexConvexInFlag.Unknown;
            bool firstPoint = true;
            RcVec3f ip = new RcVec3f();
            RcVec3f iq = new RcVec3f();

            do
            {
                a = p.ToVec3(3 * (ai % n));
                b = q.ToVec3(3 * (bi % m));
                a1 = p.ToVec3(3 * ((ai + n - 1) % n)); // prev a
                b1 = q.ToVec3(3 * ((bi + m - 1) % m)); // prev b

                RcVec3f A = RcVec3f.Subtract(a, a1);
                RcVec3f B = RcVec3f.Subtract(b, b1);

                float cross = B.X * A.Z - A.X * B.Z; // TriArea2D({0, 0}, A, B);
                float aHB = DtUtils.TriArea2D(b1, b, a);
                float bHA = DtUtils.TriArea2D(a1, a, b);
                if (MathF.Abs(cross) < EPSILON)
                {
                    cross = 0f;
                }

                bool parallel = cross == 0f;
                DtConvexConvexIntersection code = parallel ? ParallelInt(a1, a, b1, b, ref ip, ref iq) : SegSegInt(a1, a, b1, b, ref ip, ref iq);

                if (code == DtConvexConvexIntersection.Single)
                {
                    if (firstPoint)
                    {
                        firstPoint = false;
                        aa = ba = 0;
                    }

                    ii = AddVertex(inters, ii, ip);
                    f = InOut(f, aHB, bHA);
                }

                /*-----Advance rules-----*/

                /* Special case: A & B overlap and oppositely oriented. */
                if (code == DtConvexConvexIntersection.Overlap && A.Dot2D(B) < 0)
                {
                    ii = AddVertex(inters, ii, ip);
                    ii = AddVertex(inters, ii, iq);
                    break;
                }

                /* Special case: A & B parallel and separated. */
                if (parallel && aHB < 0f && bHA < 0f)
                {
                    return false;
                }
                /* Special case: A & B collinear. */
                else if (parallel && MathF.Abs(aHB) < EPSILON && MathF.Abs(bHA) < EPSILON)
                {
                    /* Advance but do not output point. */
                    if (f == DtConvexConvexInFlag.Pin)
                    {
                        ba++;
                        bi++;
                    }
                    else
                    {
                        aa++;
                        ai++;
                    }
                }
                /* Generic cases. */
                else if (cross >= 0)
                {
                    if (bHA > 0)
                    {
                        if (f == DtConvexConvexInFlag.Pin)
                        {
                            ii = AddVertex(inters, ii, a);
                        }

                        aa++;
                        ai++;
                    }
                    else
                    {
                        if (f == DtConvexConvexInFlag.Qin)
                        {
                            ii = AddVertex(inters, ii, b);
                        }

                        ba++;
                        bi++;
                    }
                }
                else
                {
                    if (aHB > 0)
                    {
                        if (f == DtConvexConvexInFlag.Qin)
                        {
                            ii = AddVertex(inters, ii, b);
                        }

                        ba++;
                        bi++;
                    }
                    else
                    {
                        if (f == DtConvexConvexInFlag.Pin)
                        {
                            ii = AddVertex(inters, ii, a);
                        }

                        aa++;
                        ai++;
                    }
                }
                /* Quit when both adv. indices have cycled, or one has cycled twice. */
            } while ((aa < n || ba < m) && aa < 2 * n && ba < 2 * m);

            /* Deal with special cases: not implemented. */
            if (f == DtConvexConvexInFlag.Unknown)
            {
                return false;
            }

            inters.Slice(0, ii).CopyTo(intersection);
            nin = ii;
            return true;
        }

        private static int AddVertex(Span<float> inters, int ii, RcVec3f p)
        {
            if (ii > 0)
            {
                if (inters[ii - 3] == p.X && inters[ii - 2] == p.Y && inters[ii - 1] == p.Z)
                {
                    return ii;
                }

                if (inters[0] == p.X && inters[1] == p.Y && inters[2] == p.Z)
                {
                    return ii;
                }
            }

            inters[ii] = p.X;
            inters[ii + 1] = p.Y;
            inters[ii + 2] = p.Z;
            return ii + 3;
        }


        private static DtConvexConvexInFlag InOut(DtConvexConvexInFlag inflag, float aHB, float bHA)
        {
            if (aHB > 0)
            {
                return DtConvexConvexInFlag.Pin;
            }
            else if (bHA > 0)
            {
                return DtConvexConvexInFlag.Qin;
            }

            return inflag;
        }

        private static DtConvexConvexIntersection SegSegInt(RcVec3f a, RcVec3f b, RcVec3f c, RcVec3f d, ref RcVec3f p, ref RcVec3f q)
        {
            if (DtUtils.IntersectSegSeg2D(a, b, c, d, out var s, out var t))
            {
                if (s >= 0.0f && s <= 1.0f && t >= 0.0f && t <= 1.0f)
                {
                    p.X = a.X + (b.X - a.X) * s;
                    p.Y = a.Y + (b.Y - a.Y) * s;
                    p.Z = a.Z + (b.Z - a.Z) * s;
                    return DtConvexConvexIntersection.Single;
                }
            }

            return DtConvexConvexIntersection.None;
        }

        private static DtConvexConvexIntersection ParallelInt(RcVec3f a, RcVec3f b, RcVec3f c, RcVec3f d, ref RcVec3f p, ref RcVec3f q)
        {
            if (Between(a, b, c) && Between(a, b, d))
            {
                p = c;
                q = d;
                return DtConvexConvexIntersection.Overlap;
            }

            if (Between(c, d, a) && Between(c, d, b))
            {
                p = a;
                q = b;
                return DtConvexConvexIntersection.Overlap;
            }

            if (Between(a, b, c) && Between(c, d, b))
            {
                p = c;
                q = b;
                return DtConvexConvexIntersection.Overlap;
            }

            if (Between(a, b, c) && Between(c, d, a))
            {
                p = c;
                q = a;
                return DtConvexConvexIntersection.Overlap;
            }

            if (Between(a, b, d) && Between(c, d, b))
            {
                p = d;
                q = b;
                return DtConvexConvexIntersection.Overlap;
            }

            if (Between(a, b, d) && Between(c, d, a))
            {
                p = d;
                q = a;
                return DtConvexConvexIntersection.Overlap;
            }

            return DtConvexConvexIntersection.None;
        }

        private static bool Between(RcVec3f a, RcVec3f b, RcVec3f c)
        {
            if (MathF.Abs(a.X - b.X) > MathF.Abs(a.Z - b.Z))
            {
                return ((a.X <= c.X) && (c.X <= b.X)) || ((a.X >= c.X) && (c.X >= b.X));
            }
            else
            {
                return ((a.Z <= c.Z) && (c.Z <= b.Z)) || ((a.Z >= c.Z) && (c.Z >= b.Z));
            }
        }
    }
}