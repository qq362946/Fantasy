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
    public class DtSphereCollider : DtCollider
    {
        private readonly RcVec3f center;
        private readonly float radius;

        public DtSphereCollider(RcVec3f center, float radius, int area, float flagMergeThreshold)
            : base(area, flagMergeThreshold, Bounds(center, radius))
        {
            this.center = center;
            this.radius = radius;
        }

        public override void Rasterize(RcHeightfield hf, RcContext context)
        {
            RcFilledVolumeRasterization.RasterizeSphere(hf, center, radius, area, (int)MathF.Floor(flagMergeThreshold / hf.ch),
                context);
        }

        private static float[] Bounds(RcVec3f center, float radius)
        {
            return new float[]
            {
                center.X - radius,
                center.Y - radius,
                center.Z - radius,
                center.X + radius,
                center.Y + radius,
                center.Z + radius
            };
        }
    }
}