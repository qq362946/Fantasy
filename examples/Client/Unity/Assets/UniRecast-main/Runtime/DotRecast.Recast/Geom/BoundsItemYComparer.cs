using System.Collections.Generic;

namespace DotRecast.Recast.Geom
{
    public class BoundsItemYComparer : IComparer<BoundsItem>
    {
        public static readonly BoundsItemYComparer Shared = new BoundsItemYComparer();

        private BoundsItemYComparer()
        {
        }

        public int Compare(BoundsItem a, BoundsItem b)
        {
            return a.bmin.Y.CompareTo(b.bmin.Y);
        }
    }
}