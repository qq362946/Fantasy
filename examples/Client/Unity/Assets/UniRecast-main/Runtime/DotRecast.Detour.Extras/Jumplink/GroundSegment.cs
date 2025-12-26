using DotRecast.Core.Numerics;

namespace DotRecast.Detour.Extras.Jumplink
{
    public class GroundSegment
    {
        public RcVec3f p;
        public RcVec3f q;
        public GroundSample[] gsamples;
        public float height;
    }
}