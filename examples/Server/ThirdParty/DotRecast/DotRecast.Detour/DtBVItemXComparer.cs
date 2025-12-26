using System.Collections.Generic;

namespace DotRecast.Detour
{
    public class DtBVItemXComparer : IComparer<DtBVItem>
    {
        public static readonly DtBVItemXComparer Shared = new DtBVItemXComparer();

        private DtBVItemXComparer()
        {
        }

        public int Compare(DtBVItem a, DtBVItem b)
        {
            return a.bmin.X.CompareTo(b.bmin.X);
        }
    }
}