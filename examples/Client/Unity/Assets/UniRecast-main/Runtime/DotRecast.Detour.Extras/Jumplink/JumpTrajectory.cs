using System;
using DotRecast.Core;
using DotRecast.Core.Numerics;

namespace DotRecast.Detour.Extras.Jumplink
{
    public class JumpTrajectory : ITrajectory
    {
        private readonly float jumpHeight;

        public JumpTrajectory(float jumpHeight)
        {
            this.jumpHeight = jumpHeight;
        }

        public RcVec3f Apply(RcVec3f start, RcVec3f end, float u)
        {
            return new RcVec3f
            {
                X = RcMath.Lerp(start.X, end.X, u),
                Y = InterpolateHeight(start.Y, end.Y, u),
                Z = RcMath.Lerp(start.Z, end.Z, u)
            };
        }

        private float InterpolateHeight(float ys, float ye, float u)
        {
            if (u == 0f)
            {
                return ys;
            }
            else if (u == 1.0f)
            {
                return ye;
            }

            float h1, h2;
            if (ys >= ye)
            {
                // jump down
                h1 = jumpHeight;
                h2 = jumpHeight + ys - ye;
            }
            else
            {
                // jump up
                h1 = jumpHeight + ys - ye;
                h2 = jumpHeight;
            }

            float t = (float)(Math.Sqrt(h1) / (Math.Sqrt(h2) + Math.Sqrt(h1)));
            if (u <= t)
            {
                float v1 = 1.0f - (u / t);
                return ys + h1 - h1 * v1 * v1;
            }

            float v = (u - t) / (1.0f - t);
            return ys + h1 - h2 * v * v;
        }
    }
}