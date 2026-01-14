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
using DotRecast.Core.Numerics;

namespace DotRecast.Detour.Extras
{
    public class BVTreeBuilder
    {
        public void Build(DtMeshData data)
        {
            data.bvTree = new DtBVNode[data.header.polyCount * 2];
            data.header.bvNodeCount = data.bvTree.Length == 0
                ? 0
                : CreateBVTree(data, data.bvTree, data.header.bvQuantFactor);
        }

        private static int CreateBVTree(DtMeshData data, DtBVNode[] nodes, float quantFactor)
        {
            BVItem[] items = new BVItem[data.header.polyCount];
            for (int i = 0; i < data.header.polyCount; i++)
            {
                BVItem it = new BVItem();
                items[i] = it;
                it.i = i;
                RcVec3f bmin = data.verts.ToVec3(data.polys[i].verts[0] * 3);
                RcVec3f bmax = data.verts.ToVec3(data.polys[i].verts[0] * 3);
                for (int j = 1; j < data.polys[i].vertCount; j++)
                {
                    bmin = RcVec3f.Min(bmin, data.verts.ToVec3(data.polys[i].verts[j] * 3));
                    bmax = RcVec3f.Max(bmax, data.verts.ToVec3(data.polys[i].verts[j] * 3));
                }

                it.bmin.X = Math.Clamp((int)((bmin.X - data.header.bmin.X) * quantFactor), 0, 0x7fffffff);
                it.bmin.Y = Math.Clamp((int)((bmin.Y - data.header.bmin.Y) * quantFactor), 0, 0x7fffffff);
                it.bmin.Z = Math.Clamp((int)((bmin.Z - data.header.bmin.Z) * quantFactor), 0, 0x7fffffff);
                it.bmax.X = Math.Clamp((int)((bmax.X - data.header.bmin.X) * quantFactor), 0, 0x7fffffff);
                it.bmax.Y = Math.Clamp((int)((bmax.Y - data.header.bmin.Y) * quantFactor), 0, 0x7fffffff);
                it.bmax.Z = Math.Clamp((int)((bmax.Z - data.header.bmin.Z) * quantFactor), 0, 0x7fffffff);
            }

            return DtNavMeshBuilder.Subdivide(items, data.header.polyCount, 0, data.header.polyCount, 0, nodes);
        }
    }
}