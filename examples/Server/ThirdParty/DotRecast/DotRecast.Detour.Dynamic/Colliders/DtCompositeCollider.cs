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
using System.Collections.Generic;
using System.Linq;
using DotRecast.Core;
using DotRecast.Recast;

namespace DotRecast.Detour.Dynamic.Colliders
{
    public class DtCompositeCollider : IDtCollider
    {
        private readonly List<IDtCollider> colliders;
        private readonly float[] _bounds;

        public DtCompositeCollider(List<IDtCollider> colliders)
        {
            this.colliders = colliders;
            _bounds = Bounds(colliders);
        }

        public DtCompositeCollider(params IDtCollider[] colliders)
        {
            this.colliders = colliders.ToList();
            _bounds = Bounds(this.colliders);
        }

        public float[] Bounds()
        {
            return _bounds;
        }

        private static float[] Bounds(List<IDtCollider> colliders)
        {
            float[] bounds = new float[]
            {
                float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity,
                float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity
            };
            foreach (IDtCollider collider in colliders)
            {
                float[] b = collider.Bounds();
                bounds[0] = Math.Min(bounds[0], b[0]);
                bounds[1] = Math.Min(bounds[1], b[1]);
                bounds[2] = Math.Min(bounds[2], b[2]);
                bounds[3] = Math.Max(bounds[3], b[3]);
                bounds[4] = Math.Max(bounds[4], b[4]);
                bounds[5] = Math.Max(bounds[5], b[5]);
            }

            return bounds;
        }

        public void Rasterize(RcHeightfield hf, RcContext context)
        {
            foreach (var c in colliders)
                c.Rasterize(hf, context);
        }
    }
}