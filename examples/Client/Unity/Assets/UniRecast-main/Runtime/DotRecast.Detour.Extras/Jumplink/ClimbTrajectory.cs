using System;
using DotRecast.Core;
using DotRecast.Core.Numerics;

namespace DotRecast.Detour.Extras.Jumplink
{
    public class ClimbTrajectory : ITrajectory
    {
        public RcVec3f Apply(RcVec3f start, RcVec3f end, float u)
        {
            return new RcVec3f()
            {
                X = RcMath.Lerp(start.X, end.X, Math.Min(2f * u, 1f)),
                Y = RcMath.Lerp(start.Y, end.Y, Math.Max(0f, 2f * u - 1f)),
                Z = RcMath.Lerp(start.Z, end.Z, Math.Min(2f * u, 1f))
            };
        }
    }
}