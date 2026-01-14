/*
Copyright (c) 2009-2010 Mikko Mononen memon@inside.org
recast4j copyright (c) 2015-2019 Piotr Piastucki piotr@jtilia.org
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
using System.Collections.Generic;
using DotRecast.Core;
using DotRecast.Core.Numerics;


namespace DotRecast.Recast
{
    using static RcRecast;
    using static RcVec;
    using static EdgeValues;

    public static class RcMeshDetails
    {
        public const int RC_UNSET_HEIGHT = RC_SPAN_MAX_HEIGHT;


        public static bool CircumCircle(RcVec3f p1, RcVec3f p2, RcVec3f p3, ref RcVec3f c, out float r)
        {
            const float EPS = 1e-6f;
            // Calculate the circle relative to p1, to avoid some precision issues.
            var v1 = new RcVec3f();
            var v2 = p2 - p1;
            var v3 = p3 - p1;

            float cp = Cross2(v1, v2, v3);
            if (MathF.Abs(cp) > EPS)
            {
                float v1Sq = Dot2(v1, v1);
                float v2Sq = Dot2(v2, v2);
                float v3Sq = Dot2(v3, v3);
                c.X = (v1Sq * (v2.Z - v3.Z) + v2Sq * (v3.Z - v1.Z) + v3Sq * (v1.Z - v2.Z)) / (2 * cp);
                c.Y = 0;
                c.Z = (v1Sq * (v3.X - v2.X) + v2Sq * (v1.X - v3.X) + v3Sq * (v2.X - v1.X)) / (2 * cp);
                r = Dist2(c, v1);
                c = c + p1;
                return true;
            }

            c = p1;
            r = 0f;
            return false;
        }

        public static float DistPtTri(RcVec3f p, RcVec3f a, RcVec3f b, RcVec3f c)
        {
            var v0 = c - a;
            var v1 = b - a;
            var v2 = p - a;

            float dot00 = Dot2(v0, v0);
            float dot01 = Dot2(v0, v1);
            float dot02 = Dot2(v0, v2);
            float dot11 = Dot2(v1, v1);
            float dot12 = Dot2(v1, v2);

            // Compute barycentric coordinates
            float invDenom = 1.0f / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            // If point lies inside the triangle, return interpolated y-coord.
            const float EPS = 1e-4f;
            if (u >= -EPS && v >= -EPS && (u + v) <= 1 + EPS)
            {
                float y = a.Y + v0.Y * u + v1.Y * v;
                return MathF.Abs(y - p.Y);
            }

            return float.MaxValue;
        }

        public static float DistancePtSeg(float[] verts, int pt, int p, int q)
        {
            float pqx = verts[q + 0] - verts[p + 0];
            float pqy = verts[q + 1] - verts[p + 1];
            float pqz = verts[q + 2] - verts[p + 2];
            float dx = verts[pt + 0] - verts[p + 0];
            float dy = verts[pt + 1] - verts[p + 1];
            float dz = verts[pt + 2] - verts[p + 2];
            float d = pqx * pqx + pqy * pqy + pqz * pqz;
            float t = pqx * dx + pqy * dy + pqz * dz;
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

            dx = verts[p + 0] + t * pqx - verts[pt + 0];
            dy = verts[p + 1] + t * pqy - verts[pt + 1];
            dz = verts[p + 2] + t * pqz - verts[pt + 2];

            return dx * dx + dy * dy + dz * dz;
        }

        public static float DistancePtSeg2d(RcVec3f verts, float[] poly, int p, int q)
        {
            float pqx = poly[q + 0] - poly[p + 0];
            float pqz = poly[q + 2] - poly[p + 2];
            float dx = verts.X - poly[p + 0];
            float dz = verts.Z - poly[p + 2];
            float d = pqx * pqx + pqz * pqz;
            float t = pqx * dx + pqz * dz;
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

            dx = poly[p + 0] + t * pqx - verts.X;
            dz = poly[p + 2] + t * pqz - verts.Z;

            return dx * dx + dz * dz;
        }

        public static float DistancePtSeg2d(float[] verts, int pt, float[] poly, int p, int q)
        {
            float pqx = poly[q + 0] - poly[p + 0];
            float pqz = poly[q + 2] - poly[p + 2];
            float dx = verts[pt + 0] - poly[p + 0];
            float dz = verts[pt + 2] - poly[p + 2];
            float d = pqx * pqx + pqz * pqz;
            float t = pqx * dx + pqz * dz;
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

            dx = poly[p + 0] + t * pqx - verts[pt + 0];
            dz = poly[p + 2] + t * pqz - verts[pt + 2];

            return dx * dx + dz * dz;
        }

        public static float DistToTriMesh(RcVec3f p, float[] verts, int nverts, List<int> tris, int ntris)
        {
            float dmin = float.MaxValue;
            for (int i = 0; i < ntris; ++i)
            {
                RcVec3f va = verts.ToVec3(tris[i * 4 + 0] * 3);
                RcVec3f vb = verts.ToVec3(tris[i * 4 + 1] * 3);
                RcVec3f vc = verts.ToVec3(tris[i * 4 + 2] * 3);
                float d = DistPtTri(p, va, vb, vc);
                if (d < dmin)
                {
                    dmin = d;
                }
            }

            if (dmin == float.MaxValue)
            {
                return -1;
            }

            return dmin;
        }

        public static float DistToPoly(int nvert, float[] verts, RcVec3f p)
        {
            float dmin = float.MaxValue;
            int i, j;
            bool c = false;
            for (i = 0, j = nvert - 1; i < nvert; j = i++)
            {
                int vi = i * 3;
                int vj = j * 3;
                if (((verts[vi + 2] > p.Z) != (verts[vj + 2] > p.Z)) && (p.X < (verts[vj + 0] - verts[vi + 0])
                        * (p.Z - verts[vi + 2]) / (verts[vj + 2] - verts[vi + 2]) + verts[vi + 0]))
                {
                    c = !c;
                }

                dmin = Math.Min(dmin, DistancePtSeg2d(p, verts, vj, vi));
            }

            return c ? -dmin : dmin;
        }

        public static int GetHeight(float fx, float fy, float fz, float cs, float ics, float ch, int radius,
            RcHeightPatch hp)
        {
            int ix = (int)MathF.Floor(fx * ics + 0.01f);
            int iz = (int)MathF.Floor(fz * ics + 0.01f);
            ix = Math.Clamp(ix - hp.xmin, 0, hp.width - 1);
            iz = Math.Clamp(iz - hp.ymin, 0, hp.height - 1);
            int h = hp.data[ix + iz * hp.width];
            if (h == RC_UNSET_HEIGHT)
            {
                // Special case when data might be bad.
                // Walk adjacent cells in a spiral up to 'radius', and look
                // for a pixel which has a valid height.
                int x = 1, z = 0, dx = 1, dz = 0;
                int maxSize = radius * 2 + 1;
                int maxIter = maxSize * maxSize - 1;

                int nextRingIterStart = 8;
                int nextRingIters = 16;

                float dmin = float.MaxValue;
                for (int i = 0; i < maxIter; ++i)
                {
                    int nx = ix + x;
                    int nz = iz + z;

                    if (nx >= 0 && nz >= 0 && nx < hp.width && nz < hp.height)
                    {
                        int nh = hp.data[nx + nz * hp.width];
                        if (nh != RC_UNSET_HEIGHT)
                        {
                            float d = MathF.Abs(nh * ch - fy);
                            if (d < dmin)
                            {
                                h = nh;
                                dmin = d;
                            }
                        }
                    }

                    // We are searching in a grid which looks approximately like this:
                    // __________
                    // |2 ______ 2|
                    // | |1 __ 1| |
                    // | | |__| | |
                    // | |______| |
                    // |__________|
                    // We want to find the best height as close to the center cell as possible. This means that
                    // if we find a height in one of the neighbor cells to the center, we don't want to
                    // expand further out than the 8 neighbors - we want to limit our search to the closest
                    // of these "rings", but the best height in the ring.
                    // For example, the center is just 1 cell. We checked that at the entrance to the function.
                    // The next "ring" contains 8 cells (marked 1 above). Those are all the neighbors to the center cell.
                    // The next one again contains 16 cells (marked 2). In general each ring has 8 additional cells, which
                    // can be thought of as adding 2 cells around the "center" of each side when we expand the ring.
                    // Here we detect if we are about to enter the next ring, and if we are and we have found
                    // a height, we abort the search.
                    if (i + 1 == nextRingIterStart)
                    {
                        if (h != RC_UNSET_HEIGHT)
                        {
                            break;
                        }

                        nextRingIterStart += nextRingIters;
                        nextRingIters += 8;
                    }

                    if ((x == z) || ((x < 0) && (x == -z)) || ((x > 0) && (x == 1 - z)))
                    {
                        int tmp = dx;
                        dx = -dz;
                        dz = tmp;
                    }

                    x += dx;
                    z += dz;
                }
            }

            return h;
        }

        public static int FindEdge(List<int> edges, int s, int t)
        {
            for (int i = 0; i < edges.Count / 4; i++)
            {
                int e = i * 4;
                if ((edges[e + 0] == s && edges[e + 1] == t) || (edges[e + 0] == t && edges[e + 1] == s))
                {
                    return i;
                }
            }

            return EV_UNDEF;
        }

        public static void AddEdge(RcContext ctx, List<int> edges, int maxEdges, int s, int t, int l, int r)
        {
            if (edges.Count / 4 >= maxEdges)
            {
                throw new Exception("addEdge: Too many edges (" + edges.Count / 4 + "/" + maxEdges + ").");
            }

            // Add edge if not already in the triangulation.
            int e = FindEdge(edges, s, t);
            if (e == EV_UNDEF)
            {
                edges.Add(s);
                edges.Add(t);
                edges.Add(l);
                edges.Add(r);
            }
        }

        public static void UpdateLeftFace(List<int> edges, int e, int s, int t, int f)
        {
            if (edges[e + 0] == s && edges[e + 1] == t && edges[e + 2] == EV_UNDEF)
            {
                edges[e + 2] = f;
            }
            else if (edges[e + 1] == s && edges[e + 0] == t && edges[e + 3] == EV_UNDEF)
            {
                edges[e + 3] = f;
            }
        }

        public static bool OverlapSegSeg2d(float[] verts, int a, int b, int c, int d)
        {
            float a1 = Cross2(verts, a, b, d);
            float a2 = Cross2(verts, a, b, c);
            if (a1 * a2 < 0.0f)
            {
                float a3 = Cross2(verts, c, d, a);
                float a4 = a3 + a2 - a1;
                if (a3 * a4 < 0.0f)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool OverlapEdges(float[] pts, List<int> edges, int s1, int t1)
        {
            for (int i = 0; i < edges.Count / 4; ++i)
            {
                int s0 = edges[i * 4 + 0];
                int t0 = edges[i * 4 + 1];
                // Same or connected edges do not overlap.
                if (s0 == s1 || s0 == t1 || t0 == s1 || t0 == t1)
                {
                    continue;
                }

                if (OverlapSegSeg2d(pts, s0 * 3, t0 * 3, s1 * 3, t1 * 3))
                {
                    return true;
                }
            }

            return false;
        }

        public static int CompleteFacet(RcContext ctx, float[] pts, int npts, List<int> edges, int maxEdges, int nfaces, int e)
        {
            const float EPS = 1e-5f;

            int edge = e * 4;

            // Cache s and t.
            int s, t;
            if (edges[edge + 2] == EV_UNDEF)
            {
                s = edges[edge + 0];
                t = edges[edge + 1];
            }
            else if (edges[edge + 3] == EV_UNDEF)
            {
                s = edges[edge + 1];
                t = edges[edge + 0];
            }
            else
            {
                // Edge already completed.
                return nfaces;
            }

            // Find best point on left of edge.
            int pt = npts;
            RcVec3f c = new RcVec3f();
            float r = -1f;
            for (int u = 0; u < npts; ++u)
            {
                if (u == s || u == t)
                {
                    continue;
                }

                RcVec3f vs = pts.ToVec3(s * 3);
                RcVec3f vt = pts.ToVec3(t * 3);
                RcVec3f vu = pts.ToVec3(u * 3);

                if (Cross2(vs, vt, vu) > EPS)
                {
                    if (r < 0)
                    {
                        // The circle is not updated yet, do it now.
                        pt = u;
                        CircumCircle(vs, vt, vu, ref c, out r);
                        continue;
                    }

                    float d = Dist2(c, vu);
                    float tol = 0.001f;
                    if (d > r * (1 + tol))
                    {
                        // Outside current circumcircle, skip.
                        continue;
                    }
                    else if (d < r * (1 - tol))
                    {
                        // Inside safe circumcircle, update circle.
                        pt = u;
                        CircumCircle(vs, vt, vu, ref c, out r);
                    }
                    else
                    {
                        // Inside epsilon circum circle, do extra tests to make sure the edge is valid.
                        // s-u and t-u cannot overlap with s-pt nor t-pt if they exists.
                        if (OverlapEdges(pts, edges, s, u))
                        {
                            continue;
                        }

                        if (OverlapEdges(pts, edges, t, u))
                        {
                            continue;
                        }

                        // Edge is valid.
                        pt = u;
                        CircumCircle(vs, vt, vu, ref c, out r);
                    }
                }
            }

            // Add new triangle or update edge info if s-t is on hull.
            if (pt < npts)
            {
                // Update face information of edge being completed.
                UpdateLeftFace(edges, e * 4, s, t, nfaces);

                // Add new edge or update face info of old edge.
                e = FindEdge(edges, pt, s);
                if (e == EV_UNDEF)
                {
                    AddEdge(ctx, edges, maxEdges, pt, s, nfaces, EV_UNDEF);
                }
                else
                {
                    UpdateLeftFace(edges, e * 4, pt, s, nfaces);
                }

                // Add new edge or update face info of old edge.
                e = FindEdge(edges, t, pt);
                if (e == EV_UNDEF)
                {
                    AddEdge(ctx, edges, maxEdges, t, pt, nfaces, EV_UNDEF);
                }
                else
                {
                    UpdateLeftFace(edges, e * 4, t, pt, nfaces);
                }

                nfaces++;
            }
            else
            {
                UpdateLeftFace(edges, e * 4, s, t, EV_HULL);
            }

            return nfaces;
        }

        public static void DelaunayHull(RcContext ctx, int npts, float[] pts, int nhull, int[] hull, List<int> tris)
        {
            int nfaces = 0;
            int maxEdges = npts * 10;
            List<int> edges = new List<int>(64);
            for (int i = 0, j = nhull - 1; i < nhull; j = i++)
            {
                AddEdge(ctx, edges, maxEdges, hull[j], hull[i], EV_HULL, EV_UNDEF);
            }

            int currentEdge = 0;
            while (currentEdge < edges.Count / 4)
            {
                if (edges[currentEdge * 4 + 2] == EV_UNDEF)
                {
                    nfaces = CompleteFacet(ctx, pts, npts, edges, maxEdges, nfaces, currentEdge);
                }

                if (edges[currentEdge * 4 + 3] == EV_UNDEF)
                {
                    nfaces = CompleteFacet(ctx, pts, npts, edges, maxEdges, nfaces, currentEdge);
                }

                currentEdge++;
            }

            // Create tris
            tris.Clear();
            for (int i = 0; i < nfaces * 4; ++i)
            {
                tris.Add(-1);
            }

            for (int i = 0; i < edges.Count / 4; ++i)
            {
                int e = i * 4;
                if (edges[e + 3] >= 0)
                {
                    // Left face
                    int t = edges[e + 3] * 4;
                    if (tris[t + 0] == -1)
                    {
                        tris[t + 0] = edges[e + 0];
                        tris[t + 1] = edges[e + 1];
                    }
                    else if (tris[t + 0] == edges[e + 1])
                    {
                        tris[t + 2] = edges[e + 0];
                    }
                    else if (tris[t + 1] == edges[e + 0])
                    {
                        tris[t + 2] = edges[e + 1];
                    }
                }

                if (edges[e + 2] >= 0)
                {
                    // Right
                    int t = edges[e + 2] * 4;
                    if (tris[t + 0] == -1)
                    {
                        tris[t + 0] = edges[e + 1];
                        tris[t + 1] = edges[e + 0];
                    }
                    else if (tris[t + 0] == edges[e + 0])
                    {
                        tris[t + 2] = edges[e + 1];
                    }
                    else if (tris[t + 1] == edges[e + 1])
                    {
                        tris[t + 2] = edges[e + 0];
                    }
                }
            }

            for (int i = 0; i < tris.Count / 4; ++i)
            {
                int t = i * 4;
                if (tris[t + 0] == -1 || tris[t + 1] == -1 || tris[t + 2] == -1)
                {
                    Console.Error.WriteLine($"delaunayHull: Removing dangling face {i} [{tris[t]},{tris[t + 1]},{tris[t + 2]}]");
                    tris[t + 0] = tris[tris.Count - 4];
                    tris[t + 1] = tris[tris.Count - 3];
                    tris[t + 2] = tris[tris.Count - 2];
                    tris[t + 3] = tris[tris.Count - 1];
                    tris.RemoveAt(tris.Count - 1);
                    tris.RemoveAt(tris.Count - 1);
                    tris.RemoveAt(tris.Count - 1);
                    tris.RemoveAt(tris.Count - 1);
                    --i;
                }
            }
        }

        // Calculate minimum extend of the polygon.
        public static float PolyMinExtent(float[] verts, int nverts)
        {
            float minDist = float.MaxValue;
            for (int i = 0; i < nverts; i++)
            {
                int ni = (i + 1) % nverts;
                int p1 = i * 3;
                int p2 = ni * 3;
                float maxEdgeDist = 0;
                for (int j = 0; j < nverts; j++)
                {
                    if (j == i || j == ni)
                    {
                        continue;
                    }

                    float d = DistancePtSeg2d(verts, j * 3, verts, p1, p2);
                    maxEdgeDist = Math.Max(maxEdgeDist, d);
                }

                minDist = Math.Min(minDist, maxEdgeDist);
            }

            return MathF.Sqrt(minDist);
        }

        public static void TriangulateHull(int nverts, float[] verts, int nhull, int[] hull, int nin, List<int> tris)
        {
            int start = 0, left = 1, right = nhull - 1;

            // Start from an ear with shortest perimeter.
            // This tends to favor well formed triangles as starting point.
            float dmin = float.MaxValue;
            for (int i = 0; i < nhull; i++)
            {
                if (hull[i] >= nin)
                {
                    continue; // Ears are triangles with original vertices as middle vertex while others are actually line
                }

                // segments on edges
                int pi = RcMeshs.Prev(i, nhull);
                int ni = RcMeshs.Next(i, nhull);
                int pv = hull[pi] * 3;
                int cv = hull[i] * 3;
                int nv = hull[ni] * 3;
                float d = Dist2(verts, pv, cv) + Dist2(verts, cv, nv) + Dist2(verts, nv, pv);
                if (d < dmin)
                {
                    start = i;
                    left = ni;
                    right = pi;
                    dmin = d;
                }
            }

            // Add first triangle
            tris.Add(hull[start]);
            tris.Add(hull[left]);
            tris.Add(hull[right]);
            tris.Add(0);

            // Triangulate the polygon by moving left or right,
            // depending on which triangle has shorter perimeter.
            // This heuristic was chose empirically, since it seems
            // handle tessellated straight edges well.
            while (RcMeshs.Next(left, nhull) != right)
            {
                // Check to see if se should advance left or right.
                int nleft = RcMeshs.Next(left, nhull);
                int nright = RcMeshs.Prev(right, nhull);

                int cvleft = hull[left] * 3;
                int nvleft = hull[nleft] * 3;
                int cvright = hull[right] * 3;
                int nvright = hull[nright] * 3;
                float dleft = Dist2(verts, cvleft, nvleft) + Dist2(verts, nvleft, cvright);
                float dright = Dist2(verts, cvright, nvright) + Dist2(verts, cvleft, nvright);

                if (dleft < dright)
                {
                    tris.Add(hull[left]);
                    tris.Add(hull[nleft]);
                    tris.Add(hull[right]);
                    tris.Add(0);
                    left = nleft;
                }
                else
                {
                    tris.Add(hull[left]);
                    tris.Add(hull[nright]);
                    tris.Add(hull[right]);
                    tris.Add(0);
                    right = nright;
                }
            }
        }

        public static float GetJitterX(int i)
        {
            return (((i * 0x8da6b343) & 0xffff) / 65535.0f * 2.0f) - 1.0f;
        }

        public static float GetJitterY(int i)
        {
            return (((i * 0xd8163841) & 0xffff) / 65535.0f * 2.0f) - 1.0f;
        }

        public static int BuildPolyDetail(RcContext ctx, float[] @in, int nin,
            float sampleDist, float sampleMaxError,
            int heightSearchRadius, RcCompactHeightfield chf,
            RcHeightPatch hp, float[] verts,
            ref List<int> tris, ref List<int> edges, ref List<int> samples)
        {
            const int MAX_VERTS = 127;
            const int MAX_TRIS = 255; // Max tris for delaunay is 2n-2-k (n=num verts, k=num hull verts).
            const int MAX_VERTS_PER_EDGE = 32;
            float[] edge = new float[(MAX_VERTS_PER_EDGE + 1) * 3];
            int[] hull = new int[MAX_VERTS];
            int nhull = 0;

            int nverts = nin;

            for (int i = 0; i < nin; ++i)
            {
                RcVec.Copy(verts, i * 3, @in, i * 3);
            }

            edges.Clear();
            tris.Clear();

            float cs = chf.cs;
            float ics = 1.0f / cs;

            // Calculate minimum extents of the polygon based on input data.
            float minExtent = PolyMinExtent(verts, nverts);

            // Tessellate outlines.
            // This is done in separate pass in order to ensure
            // seamless height values across the ply boundaries.
            if (sampleDist > 0)
            {
                for (int i = 0, j = nin - 1; i < nin; j = i++)
                {
                    int vj = j * 3;
                    int vi = i * 3;
                    bool swapped = false;
                    // Make sure the segments are always handled in same order
                    // using lexological sort or else there will be seams.
                    if (MathF.Abs(@in[vj + 0] - @in[vi + 0]) < 1e-6f)
                    {
                        if (@in[vj + 2] > @in[vi + 2])
                        {
                            (vi, vj) = (vj, vi);
                            swapped = true;
                        }
                    }
                    else
                    {
                        if (@in[vj + 0] > @in[vi + 0])
                        {
                            (vi, vj) = (vj, vi);
                            swapped = true;
                        }
                    }

                    // Create samples along the edge.
                    float dx = @in[vi + 0] - @in[vj + 0];
                    float dy = @in[vi + 1] - @in[vj + 1];
                    float dz = @in[vi + 2] - @in[vj + 2];
                    float d = MathF.Sqrt(dx * dx + dz * dz);
                    int nn = 1 + (int)MathF.Floor(d / sampleDist);
                    if (nn >= MAX_VERTS_PER_EDGE)
                    {
                        nn = MAX_VERTS_PER_EDGE - 1;
                    }

                    if (nverts + nn >= MAX_VERTS)
                    {
                        nn = MAX_VERTS - 1 - nverts;
                    }

                    for (int k = 0; k <= nn; ++k)
                    {
                        float u = (float)k / (float)nn;
                        int pos = k * 3;
                        edge[pos + 0] = @in[vj + 0] + dx * u;
                        edge[pos + 1] = @in[vj + 1] + dy * u;
                        edge[pos + 2] = @in[vj + 2] + dz * u;
                        edge[pos + 1] = GetHeight(edge[pos + 0], edge[pos + 1], edge[pos + 2], cs, ics, chf.ch,
                            heightSearchRadius, hp) * chf.ch;
                    }

                    // Simplify samples.
                    int[] idx = new int[MAX_VERTS_PER_EDGE];
                    idx[0] = 0;
                    idx[1] = nn;
                    int nidx = 2;
                    for (int k = 0; k < nidx - 1;)
                    {
                        int a = idx[k];
                        int b = idx[k + 1];
                        int va = a * 3;
                        int vb = b * 3;
                        // Find maximum deviation along the segment.
                        float maxd = 0;
                        int maxi = -1;
                        for (int m = a + 1; m < b; ++m)
                        {
                            float dev = DistancePtSeg(edge, m * 3, va, vb);
                            if (dev > maxd)
                            {
                                maxd = dev;
                                maxi = m;
                            }
                        }

                        // If the max deviation is larger than accepted error,
                        // add new point, else continue to next segment.
                        if (maxi != -1 && maxd > sampleMaxError * sampleMaxError)
                        {
                            for (int m = nidx; m > k; --m)
                            {
                                idx[m] = idx[m - 1];
                            }

                            idx[k + 1] = maxi;
                            nidx++;
                        }
                        else
                        {
                            ++k;
                        }
                    }

                    hull[nhull++] = j;
                    // Add new vertices.
                    if (swapped)
                    {
                        for (int k = nidx - 2; k > 0; --k)
                        {
                            RcVec.Copy(verts, nverts * 3, edge, idx[k] * 3);
                            hull[nhull++] = nverts;
                            nverts++;
                        }
                    }
                    else
                    {
                        for (int k = 1; k < nidx - 1; ++k)
                        {
                            RcVec.Copy(verts, nverts * 3, edge, idx[k] * 3);
                            hull[nhull++] = nverts;
                            nverts++;
                        }
                    }
                }
            }

            // If the polygon minimum extent is small (sliver or small triangle), do not try to add internal points.
            if (minExtent < sampleDist * 2)
            {
                TriangulateHull(nverts, verts, nhull, hull, nin, tris);
                SetTriFlags(tris, nhull, hull);
                return nverts;
            }

            // Tessellate the base mesh.
            // We're using the triangulateHull instead of delaunayHull as it tends to
            // create a bit better triangulation for long thin triangles when there
            // are no internal points.
            TriangulateHull(nverts, verts, nhull, hull, nin, tris);

            if (tris.Count == 0)
            {
                // Could not triangulate the poly, make sure there is some valid data there.
                throw new Exception("buildPolyDetail: Could not triangulate polygon (" + nverts + ") verts).");
            }

            if (sampleDist > 0)
            {
                // Create sample locations in a grid.
                RcVec3f bmin = @in.ToVec3();
                RcVec3f bmax = @in.ToVec3();
                for (int i = 1; i < nin; ++i)
                {
                    bmin = RcVec3f.Min(bmin, @in.ToVec3(i * 3));
                    bmax = RcVec3f.Max(bmax, @in.ToVec3(i * 3));
                }

                int x0 = (int)MathF.Floor(bmin.X / sampleDist);
                int x1 = (int)MathF.Ceiling(bmax.X / sampleDist);
                int z0 = (int)MathF.Floor(bmin.Z / sampleDist);
                int z1 = (int)MathF.Ceiling(bmax.Z / sampleDist);
                samples.Clear();
                for (int z = z0; z < z1; ++z)
                {
                    for (int x = x0; x < x1; ++x)
                    {
                        RcVec3f pt = new RcVec3f();
                        pt.X = x * sampleDist;
                        pt.Y = (bmax.Y + bmin.Y) * 0.5f;
                        pt.Z = z * sampleDist;
                        // Make sure the samples are not too close to the edges.
                        if (DistToPoly(nin, @in, pt) > -sampleDist / 2)
                        {
                            continue;
                        }

                        samples.Add(x);
                        samples.Add(GetHeight(pt.X, pt.Y, pt.Z, cs, ics, chf.ch, heightSearchRadius, hp));
                        samples.Add(z);
                        samples.Add(0); // Not added
                    }
                }

                // Add the samples starting from the one that has the most
                // error. The procedure stops when all samples are added
                // or when the max error is within treshold.
                int nsamples = samples.Count / 4;
                for (int iter = 0; iter < nsamples; ++iter)
                {
                    if (nverts >= MAX_VERTS)
                    {
                        break;
                    }

                    // Find sample with most error.
                    RcVec3f bestpt = new RcVec3f();
                    float bestd = 0;
                    int besti = -1;
                    for (int i = 0; i < nsamples; ++i)
                    {
                        int s = i * 4;
                        if (samples[s + 3] != 0)
                        {
                            continue; // skip added.
                        }

                        RcVec3f pt = new RcVec3f();
                        // The sample location is jittered to get rid of some bad triangulations
                        // which are cause by symmetrical data from the grid structure.
                        pt.X = samples[s + 0] * sampleDist + GetJitterX(i) * cs * 0.1f;
                        pt.Y = samples[s + 1] * chf.ch;
                        pt.Z = samples[s + 2] * sampleDist + GetJitterY(i) * cs * 0.1f;
                        float d = DistToTriMesh(pt, verts, nverts, tris, tris.Count / 4);
                        if (d < 0)
                        {
                            continue; // did not hit the mesh.
                        }

                        if (d > bestd)
                        {
                            bestd = d;
                            besti = i;
                            bestpt = pt;
                        }
                    }

                    // If the max error is within accepted threshold, stop tesselating.
                    if (bestd <= sampleMaxError || besti == -1)
                    {
                        break;
                    }

                    // Mark sample as added.
                    samples[besti * 4 + 3] = 1;
                    // Add the new sample point.
                    bestpt.CopyTo(verts, nverts * 3);
                    nverts++;

                    // Create new triangulation.
                    // TODO: Incremental add instead of full rebuild.
                    DelaunayHull(ctx, nverts, verts, nhull, hull, tris);
                }
            }

            int ntris = tris.Count / 4;
            if (ntris > MAX_TRIS)
            {
                List<int> subList = tris.GetRange(0, MAX_TRIS * 4);
                tris.Clear();
                tris.AddRange(subList);
                throw new Exception("rcBuildPolyMeshDetail: Shrinking triangle count from " + ntris + " to max " + MAX_TRIS);
            }

            SetTriFlags(tris, nhull, hull);
            return nverts;
        }

        public static bool OnHull(int a, int b, int nhull, int[] hull)
        {
            // All internal sampled points come after the hull so we can early out for those.
            if (a >= nhull || b >= nhull)
                return false;

            for (int j = nhull - 1, i = 0; i < nhull; j = i++)
            {
                if (a == hull[j] && b == hull[i])
                    return true;
            }

            return false;
        }

        // Find edges that lie on hull and mark them as such.
        public static void SetTriFlags(List<int> tris, int nhull, int[] hull)
        {
            // Matches DT_DETAIL_EDGE_BOUNDARY
            const int DETAIL_EDGE_BOUNDARY = 0x1;

            for (int i = 0; i < tris.Count; i += 4)
            {
                int a = tris[i];
                int b = tris[i + 1];
                int c = tris[i + 2];
                int flags = 0;
                flags |= (OnHull(a, b, nhull, hull) ? DETAIL_EDGE_BOUNDARY : 0) << 0;
                flags |= (OnHull(b, c, nhull, hull) ? DETAIL_EDGE_BOUNDARY : 0) << 2;
                flags |= (OnHull(c, a, nhull, hull) ? DETAIL_EDGE_BOUNDARY : 0) << 4;
                tris[i + 3] = flags;
            }
        }


        public static void SeedArrayWithPolyCenter(RcContext ctx, RcCompactHeightfield chf, int[] meshpoly, int poly, int npoly,
            int[] verts, int bs, RcHeightPatch hp, List<int> array)
        {
            // Note: Reads to the compact heightfield are offset by border size (bs)
            // since border size offset is already removed from the polymesh vertices.

            int[] offset = { 0, 0, -1, -1, 0, -1, 1, -1, 1, 0, 1, 1, 0, 1, -1, 1, -1, 0, };

            // Find cell closest to a poly vertex
            int startCellX = 0, startCellY = 0, startSpanIndex = -1;
            int dmin = RC_UNSET_HEIGHT;
            for (int j = 0; j < npoly && dmin > 0; ++j)
            {
                for (int k = 0; k < 9 && dmin > 0; ++k)
                {
                    int ax = verts[meshpoly[poly + j] * 3 + 0] + offset[k * 2 + 0];
                    int ay = verts[meshpoly[poly + j] * 3 + 1];
                    int az = verts[meshpoly[poly + j] * 3 + 2] + offset[k * 2 + 1];
                    if (ax < hp.xmin || ax >= hp.xmin + hp.width || az < hp.ymin || az >= hp.ymin + hp.height)
                    {
                        continue;
                    }

                    ref RcCompactCell c = ref chf.cells[(ax + bs) + (az + bs) * chf.width];
                    for (int i = c.index, ni = c.index + c.count; i < ni && dmin > 0; ++i)
                    {
                        ref RcCompactSpan s = ref chf.spans[i];
                        int d = Math.Abs(ay - s.y);
                        if (d < dmin)
                        {
                            startCellX = ax;
                            startCellY = az;
                            startSpanIndex = i;
                            dmin = d;
                        }
                    }
                }
            }

            // Find center of the polygon
            int pcx = 0, pcy = 0;
            for (int j = 0; j < npoly; ++j)
            {
                pcx += verts[meshpoly[poly + j] * 3 + 0];
                pcy += verts[meshpoly[poly + j] * 3 + 2];
            }

            pcx /= npoly;
            pcy /= npoly;

            array.Clear();
            array.Add(startCellX);
            array.Add(startCellY);
            array.Add(startSpanIndex);
            int[] dirs = { 0, 1, 2, 3 };
            Array.Fill(hp.data, 0, 0, (hp.width * hp.height) - (0));
            // DFS to move to the center. Note that we need a DFS here and can not just move
            // directly towards the center without recording intermediate nodes, even though the polygons
            // are convex. In very rare we can get stuck due to contour simplification if we do not
            // record nodes.
            int cx = -1, cy = -1, ci = -1;
            while (true)
            {
                if (array.Count < 3)
                {
                    ctx.Warn("Walk towards polygon center failed to reach center");
                    break;
                }

                ci = array[array.Count - 1];
                array.RemoveAt(array.Count - 1);

                cy = array[array.Count - 1];
                array.RemoveAt(array.Count - 1);

                cx = array[array.Count - 1];
                array.RemoveAt(array.Count - 1);

                // Check if close to center of the polygon.
                if (cx == pcx && cy == pcy)
                {
                    break;
                }

                // If we are already at the correct X-position, prefer direction
                // directly towards the center in the Y-axis; otherwise prefer
                // direction in the X-axis
                int directDir;
                if (cx == pcx)
                {
                    directDir = GetDirForOffset(0, pcy > cy ? 1 : -1);
                }
                else
                {
                    directDir = GetDirForOffset(pcx > cx ? 1 : -1, 0);
                }

                // Push the direct dir last so we start with this on next iteration
                int tmp = dirs[3];
                dirs[3] = dirs[directDir];
                dirs[directDir] = tmp;

                ref RcCompactSpan cs = ref chf.spans[ci];

                for (int i = 0; i < 4; ++i)
                {
                    int dir = dirs[i];
                    if (GetCon(ref cs, dir) == RC_NOT_CONNECTED)
                    {
                        continue;
                    }

                    int newX = cx + GetDirOffsetX(dir);
                    int newY = cy + GetDirOffsetY(dir);

                    int hpx = newX - hp.xmin;
                    int hpy = newY - hp.ymin;
                    if (hpx < 0 || hpx >= hp.width || hpy < 0 || hpy >= hp.height)
                    {
                        continue;
                    }

                    if (hp.data[hpx + hpy * hp.width] != 0)
                    {
                        continue;
                    }

                    hp.data[hpx + hpy * hp.width] = 1;

                    array.Add(newX);
                    array.Add(newY);
                    array.Add(chf.cells[(newX + bs) + (newY + bs) * chf.width].index + GetCon(ref cs, dir));
                }

                tmp = dirs[3];
                dirs[3] = dirs[directDir];
                dirs[directDir] = tmp;
            }

            array.Clear();
            // getHeightData seeds are given in coordinates with borders
            array.Add(cx + bs);
            array.Add(cy + bs);
            array.Add(ci);
            Array.Fill(hp.data, RC_UNSET_HEIGHT, 0, (hp.width * hp.height) - (0));
            ref RcCompactSpan cs2 = ref chf.spans[ci];
            hp.data[cx - hp.xmin + (cy - hp.ymin) * hp.width] = cs2.y;
        }


        public static void Push3(List<int> queue, int v1, int v2, int v3)
        {
            queue.Add(v1);
            queue.Add(v2);
            queue.Add(v3);
        }

        public static void GetHeightData(RcContext ctx, RcCompactHeightfield chf,
            int[] meshpolys, int poly, int npoly,
            int[] verts, int bs,
            ref RcHeightPatch hp, ref List<int> queue,
            int region)
        {
            // Note: Reads to the compact heightfield are offset by border size (bs)
            // since border size offset is already removed from the polymesh vertices.

            queue.Clear();
            // Set all heights to RC_UNSET_HEIGHT.
            Array.Fill(hp.data, RC_UNSET_HEIGHT, 0, (hp.width * hp.height) - (0));

            bool empty = true;

            // We cannot sample from this poly if it was created from polys
            // of different regions. If it was then it could potentially be overlapping
            // with polys of that region and the heights sampled here could be wrong.
            if (region != RC_MULTIPLE_REGS)
            {
                // Copy the height from the same region, and mark region borders
                // as seed points to fill the rest.
                for (int hy = 0; hy < hp.height; hy++)
                {
                    int y = hp.ymin + hy + bs;
                    for (int hx = 0; hx < hp.width; hx++)
                    {
                        int x = hp.xmin + hx + bs;
                        ref RcCompactCell c = ref chf.cells[x + y * chf.width];
                        for (int i = c.index, ni = c.index + c.count; i < ni; ++i)
                        {
                            ref RcCompactSpan s = ref chf.spans[i];
                            if (s.reg == region)
                            {
                                // Store height
                                hp.data[hx + hy * hp.width] = s.y;
                                empty = false;
                                // If any of the neighbours is not in same region,
                                // add the current location as flood fill start
                                bool border = false;
                                for (int dir = 0; dir < 4; ++dir)
                                {
                                    if (GetCon(ref s, dir) != RC_NOT_CONNECTED)
                                    {
                                        int ax = x + GetDirOffsetX(dir);
                                        int ay = y + GetDirOffsetY(dir);
                                        int ai = chf.cells[ax + ay * chf.width].index + GetCon(ref s, dir);
                                        ref RcCompactSpan @as = ref chf.spans[ai];
                                        if (@as.reg != region)
                                        {
                                            border = true;
                                            break;
                                        }
                                    }
                                }

                                if (border)
                                {
                                    Push3(queue, x, y, i);
                                }

                                break;
                            }
                        }
                    }
                }
            }

            // if the polygon does not contain any points from the current region (rare, but happens)
            // or if it could potentially be overlapping polygons of the same region,
            // then use the center as the seed point.
            if (empty)
            {
                SeedArrayWithPolyCenter(ctx, chf, meshpolys, poly, npoly, verts, bs, hp, queue);
            }

            const int RETRACT_SIZE = 256;
            int head = 0;

            // We assume the seed is centered in the polygon, so a BFS to collect
            // height data will ensure we do not move onto overlapping polygons and
            // sample wrong heights.
            while (head * 3 < queue.Count)
            {
                int cx = queue[head * 3 + 0];
                int cy = queue[head * 3 + 1];
                int ci = queue[head * 3 + 2];
                head++;
                if (head >= RETRACT_SIZE)
                {
                    head = 0;
                    queue = queue.GetRange(RETRACT_SIZE * 3, queue.Count - (RETRACT_SIZE * 3));
                }

                ref RcCompactSpan cs = ref chf.spans[ci];
                for (int dir = 0; dir < 4; ++dir)
                {
                    if (GetCon(ref cs, dir) == RC_NOT_CONNECTED)
                    {
                        continue;
                    }

                    int ax = cx + GetDirOffsetX(dir);
                    int ay = cy + GetDirOffsetY(dir);
                    int hx = ax - hp.xmin - bs;
                    int hy = ay - hp.ymin - bs;

                    if (hx < 0 || hx >= hp.width || hy < 0 || hy >= hp.height)
                    {
                        continue;
                    }

                    if (hp.data[hx + hy * hp.width] != RC_UNSET_HEIGHT)
                    {
                        continue;
                    }

                    int ai = chf.cells[ax + ay * chf.width].index + GetCon(ref cs, dir);
                    ref RcCompactSpan @as = ref chf.spans[ai];

                    hp.data[hx + hy * hp.width] = @as.y;
                    Push3(queue, ax, ay, ai);
                }
            }
        }

        /// @par
        ///
        /// See the #rcConfig documentation for more information on the configuration parameters.
        ///
        /// @see rcAllocPolyMeshDetail, rcPolyMesh, rcCompactHeightfield, rcPolyMeshDetail, rcConfig
        public static RcPolyMeshDetail BuildPolyMeshDetail(RcContext ctx, RcPolyMesh mesh, RcCompactHeightfield chf,
            float sampleDist, float sampleMaxError)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_BUILD_POLYMESHDETAIL);
            if (mesh.nverts == 0 || mesh.npolys == 0)
            {
                return null;
            }

            RcPolyMeshDetail dmesh = new RcPolyMeshDetail();
            int nvp = mesh.nvp;
            float cs = mesh.cs;
            float ch = mesh.ch;
            RcVec3f orig = mesh.bmin;
            int borderSize = mesh.borderSize;
            int heightSearchRadius = (int)Math.Max(1, MathF.Ceiling(mesh.maxEdgeError));

            List<int> edges = new List<int>(64);
            List<int> tris = new List<int>(512);
            List<int> arr = new List<int>(512);
            List<int> samples = new List<int>(512);
            float[] verts = new float[256 * 3];
            RcHeightPatch hp = new RcHeightPatch();
            int nPolyVerts = 0;
            int maxhw = 0, maxhh = 0;

            int[] bounds = new int[mesh.npolys * 4];
            float[] poly = new float[nvp * 3];

            // Find max size for a polygon area.
            for (int i = 0; i < mesh.npolys; ++i)
            {
                int p = i * nvp * 2;
                bounds[i * 4 + 0] = chf.width;
                bounds[i * 4 + 1] = 0;
                bounds[i * 4 + 2] = chf.height;
                bounds[i * 4 + 3] = 0;
                for (int j = 0; j < nvp; ++j)
                {
                    if (mesh.polys[p + j] == RC_MESH_NULL_IDX)
                    {
                        break;
                    }

                    int v = mesh.polys[p + j] * 3;
                    bounds[i * 4 + 0] = Math.Min(bounds[i * 4 + 0], mesh.verts[v + 0]);
                    bounds[i * 4 + 1] = Math.Max(bounds[i * 4 + 1], mesh.verts[v + 0]);
                    bounds[i * 4 + 2] = Math.Min(bounds[i * 4 + 2], mesh.verts[v + 2]);
                    bounds[i * 4 + 3] = Math.Max(bounds[i * 4 + 3], mesh.verts[v + 2]);
                    nPolyVerts++;
                }

                bounds[i * 4 + 0] = Math.Max(0, bounds[i * 4 + 0] - 1);
                bounds[i * 4 + 1] = Math.Min(chf.width, bounds[i * 4 + 1] + 1);
                bounds[i * 4 + 2] = Math.Max(0, bounds[i * 4 + 2] - 1);
                bounds[i * 4 + 3] = Math.Min(chf.height, bounds[i * 4 + 3] + 1);
                if (bounds[i * 4 + 0] >= bounds[i * 4 + 1] || bounds[i * 4 + 2] >= bounds[i * 4 + 3])
                {
                    continue;
                }

                maxhw = Math.Max(maxhw, bounds[i * 4 + 1] - bounds[i * 4 + 0]);
                maxhh = Math.Max(maxhh, bounds[i * 4 + 3] - bounds[i * 4 + 2]);
            }

            hp.data = new int[maxhw * maxhh];

            dmesh.nmeshes = mesh.npolys;
            dmesh.nverts = 0;
            dmesh.ntris = 0;
            dmesh.meshes = new int[dmesh.nmeshes * 4];

            int vcap = nPolyVerts + nPolyVerts / 2;
            int tcap = vcap * 2;

            dmesh.nverts = 0;
            dmesh.verts = new float[vcap * 3];
            dmesh.ntris = 0;
            dmesh.tris = new int[tcap * 4];

            for (int i = 0; i < mesh.npolys; ++i)
            {
                int p = i * nvp * 2;

                // Store polygon vertices for processing.
                int npoly = 0;
                for (int j = 0; j < nvp; ++j)
                {
                    if (mesh.polys[p + j] == RC_MESH_NULL_IDX)
                    {
                        break;
                    }

                    int v = mesh.polys[p + j] * 3;
                    poly[j * 3 + 0] = mesh.verts[v + 0] * cs;
                    poly[j * 3 + 1] = mesh.verts[v + 1] * ch;
                    poly[j * 3 + 2] = mesh.verts[v + 2] * cs;
                    npoly++;
                }

                // Get the height data from the area of the polygon.
                hp.xmin = bounds[i * 4 + 0];
                hp.ymin = bounds[i * 4 + 2];
                hp.width = bounds[i * 4 + 1] - bounds[i * 4 + 0];
                hp.height = bounds[i * 4 + 3] - bounds[i * 4 + 2];
                GetHeightData(ctx, chf, mesh.polys, p, npoly, mesh.verts, borderSize, ref hp, ref arr, mesh.regs[i]);

                // Build detail mesh.
                int nverts = BuildPolyDetail(ctx, poly, npoly,
                    sampleDist, sampleMaxError,
                    heightSearchRadius, chf, hp,
                    verts, ref tris,
                    ref edges, ref samples);

                // Move detail verts to world space.
                for (int j = 0; j < nverts; ++j)
                {
                    verts[j * 3 + 0] += orig.X;
                    verts[j * 3 + 1] += orig.Y + chf.ch; // Is this offset necessary? See
                    verts[j * 3 + 2] += orig.Z;
                }

                // Offset poly too, will be used to flag checking.
                for (int j = 0; j < npoly; ++j)
                {
                    poly[j * 3 + 0] += orig.X;
                    poly[j * 3 + 1] += orig.Y;
                    poly[j * 3 + 2] += orig.Z;
                }

                // Store detail submesh.
                int ntris = tris.Count / 4;

                dmesh.meshes[i * 4 + 0] = dmesh.nverts;
                dmesh.meshes[i * 4 + 1] = nverts;
                dmesh.meshes[i * 4 + 2] = dmesh.ntris;
                dmesh.meshes[i * 4 + 3] = ntris;

                // Store vertices, allocate more memory if necessary.
                if (dmesh.nverts + nverts > vcap)
                {
                    while (dmesh.nverts + nverts > vcap)
                    {
                        vcap += 256;
                    }

                    float[] newv = new float[vcap * 3];
                    if (dmesh.nverts != 0)
                    {
                        RcArrays.Copy(dmesh.verts, 0, newv, 0, 3 * dmesh.nverts);
                    }

                    dmesh.verts = newv;
                }

                for (int j = 0; j < nverts; ++j)
                {
                    dmesh.verts[dmesh.nverts * 3 + 0] = verts[j * 3 + 0];
                    dmesh.verts[dmesh.nverts * 3 + 1] = verts[j * 3 + 1];
                    dmesh.verts[dmesh.nverts * 3 + 2] = verts[j * 3 + 2];
                    dmesh.nverts++;
                }

                // Store triangles, allocate more memory if necessary.
                if (dmesh.ntris + ntris > tcap)
                {
                    while (dmesh.ntris + ntris > tcap)
                    {
                        tcap += 256;
                    }

                    int[] newt = new int[tcap * 4];
                    if (dmesh.ntris != 0)
                    {
                        RcArrays.Copy(dmesh.tris, 0, newt, 0, 4 * dmesh.ntris);
                    }

                    dmesh.tris = newt;
                }

                for (int j = 0; j < ntris; ++j)
                {
                    int t = j * 4;
                    dmesh.tris[dmesh.ntris * 4 + 0] = tris[t + 0];
                    dmesh.tris[dmesh.ntris * 4 + 1] = tris[t + 1];
                    dmesh.tris[dmesh.ntris * 4 + 2] = tris[t + 2];
                    dmesh.tris[dmesh.ntris * 4 + 3] = tris[t + 3];
                    dmesh.ntris++;
                }
            }

            return dmesh;
        }

        /// @see rcAllocPolyMeshDetail, rcPolyMeshDetail
        public static RcPolyMeshDetail MergePolyMeshDetails(RcContext ctx, RcPolyMeshDetail[] meshes, int nmeshes)
        {
            using var timer = ctx.ScopedTimer(RcTimerLabel.RC_TIMER_MERGE_POLYMESHDETAIL);

            RcPolyMeshDetail mesh = new RcPolyMeshDetail();

            int maxVerts = 0;
            int maxTris = 0;
            int maxMeshes = 0;

            for (int i = 0; i < nmeshes; ++i)
            {
                if (meshes[i] == null)
                {
                    continue;
                }

                maxVerts += meshes[i].nverts;
                maxTris += meshes[i].ntris;
                maxMeshes += meshes[i].nmeshes;
            }

            mesh.nmeshes = 0;
            mesh.meshes = new int[maxMeshes * 4];
            mesh.ntris = 0;
            mesh.tris = new int[maxTris * 4];
            mesh.nverts = 0;
            mesh.verts = new float[maxVerts * 3];

            // Merge datas.
            for (int i = 0; i < nmeshes; ++i)
            {
                RcPolyMeshDetail dm = meshes[i];
                if (dm == null)
                {
                    continue;
                }

                for (int j = 0; j < dm.nmeshes; ++j)
                {
                    int dst = mesh.nmeshes * 4;
                    int src = j * 4;
                    mesh.meshes[dst + 0] = mesh.nverts + dm.meshes[src + 0];
                    mesh.meshes[dst + 1] = dm.meshes[src + 1];
                    mesh.meshes[dst + 2] = mesh.ntris + dm.meshes[src + 2];
                    mesh.meshes[dst + 3] = dm.meshes[src + 3];
                    mesh.nmeshes++;
                }

                for (int k = 0; k < dm.nverts; ++k)
                {
                    RcVec.Copy(mesh.verts, mesh.nverts * 3, dm.verts, k * 3);
                    mesh.nverts++;
                }

                for (int k = 0; k < dm.ntris; ++k)
                {
                    mesh.tris[mesh.ntris * 4 + 0] = dm.tris[k * 4 + 0];
                    mesh.tris[mesh.ntris * 4 + 1] = dm.tris[k * 4 + 1];
                    mesh.tris[mesh.ntris * 4 + 2] = dm.tris[k * 4 + 2];
                    mesh.tris[mesh.ntris * 4 + 3] = dm.tris[k * 4 + 3];
                    mesh.ntris++;
                }
            }

            return mesh;
        }
    }
}