/*
recast4j copyright (c) 2021 Piotr Piastucki piotr@jtilia.org
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
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Recast;

namespace DotRecast.Detour.Dynamic.Colliders
{
    public class DtBoxCollider : DtCollider
    {
        private readonly RcVec3f center;
        private readonly RcVec3f[] halfEdges;

        public DtBoxCollider(RcVec3f center, RcVec3f[] halfEdges, int area, float flagMergeThreshold) :
            base(area, flagMergeThreshold, Bounds(center, halfEdges))
        {
            this.center = center;
            this.halfEdges = halfEdges;
        }

        private static float[] Bounds(RcVec3f center, RcVec3f[] halfEdges)
        {
            float[] bounds = new float[]
            {
                float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity,
                float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity
            };
            for (int i = 0; i < 8; ++i)
            {
                float s0 = (i & 1) != 0 ? 1f : -1f;
                float s1 = (i & 2) != 0 ? 1f : -1f;
                float s2 = (i & 4) != 0 ? 1f : -1f;
                float vx = center.X + s0 * halfEdges[0].X + s1 * halfEdges[1].X + s2 * halfEdges[2].X;
                float vy = center.Y + s0 * halfEdges[0].Y + s1 * halfEdges[1].Y + s2 * halfEdges[2].Y;
                float vz = center.Z + s0 * halfEdges[0].Z + s1 * halfEdges[1].Z + s2 * halfEdges[2].Z;
                bounds[0] = Math.Min(bounds[0], vx);
                bounds[1] = Math.Min(bounds[1], vy);
                bounds[2] = Math.Min(bounds[2], vz);
                bounds[3] = Math.Max(bounds[3], vx);
                bounds[4] = Math.Max(bounds[4], vy);
                bounds[5] = Math.Max(bounds[5], vz);
            }

            return bounds;
        }

        public override void Rasterize(RcHeightfield hf, RcContext context)
        {
            RcFilledVolumeRasterization.RasterizeBox(
                hf, center, halfEdges, area, (int)MathF.Floor(flagMergeThreshold / hf.ch), context);
        }

        public static RcVec3f[] GetHalfEdges(RcVec3f up, RcVec3f forward, RcVec3f extent)
        {
            RcVec3f[] halfEdges =
            {
                RcVec3f.Zero,
                new RcVec3f(up.X, up.Y, up.Z),
                RcVec3f.Zero
            };

            halfEdges[1] = RcVec3f.Normalize(halfEdges[1]);
            halfEdges[0] = RcVec3f.Cross(up, forward);
            halfEdges[0] = RcVec3f.Normalize(halfEdges[0]);
            halfEdges[2] = RcVec3f.Cross(halfEdges[0], up);
            halfEdges[2] = RcVec3f.Normalize(halfEdges[2]);
            halfEdges[0].X *= extent.X;
            halfEdges[0].Y *= extent.X;
            halfEdges[0].Z *= extent.X;
            halfEdges[1].X *= extent.Y;
            halfEdges[1].Y *= extent.Y;
            halfEdges[1].Z *= extent.Y;
            halfEdges[2].X *= extent.Z;
            halfEdges[2].Y *= extent.Z;
            halfEdges[2].Z *= extent.Z;
            return halfEdges;
        }
    }
}