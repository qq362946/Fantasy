using DotRecast.Core.Numerics;

namespace DotRecast.Detour.TileCache
{
    public class DtObstacleOrientedBox
    {
        public RcVec3f center;
        public RcVec3f extents;
        public readonly float[] rotAux = new float[2]; // { Cos(0.5f*angle)*Sin(-0.5f*angle); Cos(0.5f*angle)*Cos(0.5f*angle) - 0.5 } 
    }
}