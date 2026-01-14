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


namespace DotRecast.Detour.Crowd
{
    public class DtLocalBoundary
    {
        public const int MAX_LOCAL_SEGS = 8;
        public const int MAX_LOCAL_POLYS = 16;

        private RcVec3f m_center = new RcVec3f();
        private List<DtSegment> m_segs = new List<DtSegment>();
        private long[] m_polys = new long[MAX_LOCAL_POLYS];
        private int m_npolys;

        public DtLocalBoundary()
        {
            m_center.X = m_center.Y = m_center.Z = float.MaxValue;
        }

        public void Reset()
        {
            m_center.X = m_center.Y = m_center.Z = float.MaxValue;
            m_npolys = 0;
            m_segs.Clear();
        }

        protected void AddSegment(float dist, RcSegmentVert s)
        {
            // Insert neighbour based on the distance.
            DtSegment seg = new DtSegment();
            seg.s[0] = s.vmin;
            seg.s[1] = s.vmax;
            //RcArrays.Copy(s, seg.s, 6);
            seg.d = dist;
            if (0 == m_segs.Count)
            {
                m_segs.Add(seg);
            }
            else if (dist >= m_segs[m_segs.Count - 1].d)
            {
                if (m_segs.Count >= MAX_LOCAL_SEGS)
                {
                    return;
                }

                m_segs.Add(seg);
            }
            else
            {
                // Insert inbetween.
                int i;
                for (i = 0; i < m_segs.Count; ++i)
                {
                    if (dist <= m_segs[i].d)
                    {
                        break;
                    }
                }

                m_segs.Insert(i, seg);
            }

            while (m_segs.Count > MAX_LOCAL_SEGS)
            {
                m_segs.RemoveAt(m_segs.Count - 1);
            }
        }

        public void Update(long startRef, RcVec3f pos, float collisionQueryRange, DtNavMeshQuery navquery, IDtQueryFilter filter)
        {
            const int MAX_SEGS_PER_POLY = DtDetour.DT_VERTS_PER_POLYGON * 3;

            if (startRef == 0)
            {
                Reset();
                return;
            }

            m_center = pos;

            // First query non-overlapping polygons.
            var status = navquery.FindLocalNeighbourhood(startRef, pos, collisionQueryRange, filter, m_polys, null, out m_npolys, MAX_LOCAL_POLYS);
            if (status.Succeeded())
            {
                // Secondly, store all polygon edges.
                m_segs.Clear();
                Span<RcSegmentVert> segs = stackalloc RcSegmentVert[MAX_SEGS_PER_POLY];
                int nsegs = 0;

                for (int j = 0; j < m_npolys; ++j)
                {
                    var result = navquery.GetPolyWallSegments(m_polys[j], filter, segs, null, ref nsegs, MAX_SEGS_PER_POLY);
                    if (result.Succeeded())
                    {
                        for (int k = 0; k < nsegs; ++k)
                        {
                            ref RcSegmentVert s = ref segs[k];
                            var s0 = s.vmin;
                            var s3 = s.vmax;

                            // Skip too distant segments.
                            var distSqr = DtUtils.DistancePtSegSqr2D(pos, s0, s3, out var tseg);
                            if (distSqr > RcMath.Sqr(collisionQueryRange))
                            {
                                continue;
                            }

                            AddSegment(distSqr, s);
                        }
                    }
                }
            }
        }

        public bool IsValid(DtNavMeshQuery navquery, IDtQueryFilter filter)
        {
            if (m_npolys <= 0)
            {
                return false;
            }

            // Check that all polygons still pass query filter.
            for (int i = 0; i < m_npolys; ++i)
            {
                if (!navquery.IsValidPolyRef(m_polys[i], filter))
                {
                    return false;
                }
            }

            return true;
        }

        public RcVec3f GetCenter()
        {
            return m_center;
        }

        public RcVec3f[] GetSegment(int j)
        {
            return m_segs[j].s;
        }

        public int GetSegmentCount()
        {
            return m_segs.Count;
        }
    }
}