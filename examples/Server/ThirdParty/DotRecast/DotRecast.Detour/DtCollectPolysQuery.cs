using System;
using DotRecast.Core;

namespace DotRecast.Detour
{
    public class DtCollectPolysQuery : IDtPolyQuery
    {
        private long[] m_polys;
        private int m_maxPolys;
        private int m_numCollected;
        private bool m_overflow;

        public DtCollectPolysQuery(long[] polys, int maxPolys)
        {
            m_polys = polys;
            m_maxPolys = maxPolys;
        }

        public int NumCollected()
        {
            return m_numCollected;
        }

        public bool Overflowed()
        {
            return m_overflow;
        }

        public void Process(DtMeshTile tile, ReadOnlySpan<int> polys, ReadOnlySpan<long> polyRefs, int count)
        {
            int numLeft = m_maxPolys - m_numCollected;
            int toCopy = count;
            if (toCopy > numLeft)
            {
                m_overflow = true;
                toCopy = numLeft;
            }

            RcSpans.Copy<long>(polyRefs, 0, m_polys, m_numCollected, toCopy);
            m_numCollected += toCopy;
        }
    }
}