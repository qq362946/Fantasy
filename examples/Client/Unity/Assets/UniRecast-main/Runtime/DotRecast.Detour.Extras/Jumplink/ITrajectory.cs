using DotRecast.Core.Numerics;

namespace DotRecast.Detour.Extras.Jumplink
{
    public interface ITrajectory
    {
        RcVec3f Apply(RcVec3f start, RcVec3f end, float u);
    }
}