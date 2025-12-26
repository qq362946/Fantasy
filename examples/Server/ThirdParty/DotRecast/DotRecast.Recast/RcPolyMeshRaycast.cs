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
using DotRecast.Core;
using DotRecast.Core.Numerics;

namespace DotRecast.Recast
{
    public static class RcPolyMeshRaycast
    {
        public static bool Raycast(IList<RcBuilderResult> results, RcVec3f src, RcVec3f dst, out float hitTime)
        {
            hitTime = 0.0f;
            foreach (RcBuilderResult result in results)
            {
                if (result.MeshDetail != null)
                {
                    if (Raycast(result.Mesh, result.MeshDetail, src, dst, out hitTime))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Raycast(RcPolyMesh poly, RcPolyMeshDetail meshDetail, RcVec3f sp, RcVec3f sq, out float hitTime)
        {
            hitTime = 0;
            Span<RcVec3f> tempVs = stackalloc RcVec3f[3];
            if (meshDetail != null)
            {
                for (int i = 0; i < meshDetail.nmeshes; ++i)
                {
                    int m = i * 4;
                    int bverts = meshDetail.meshes[m];
                    int btris = meshDetail.meshes[m + 2];
                    int ntris = meshDetail.meshes[m + 3];
                    int verts = bverts * 3;
                    int tris = btris * 4;
                    for (int j = 0; j < ntris; ++j)
                    {
                        Span<RcVec3f> vs = tempVs;
                        for (int k = 0; k < 3; ++k)
                        {
                            vs[k].X = meshDetail.verts[verts + meshDetail.tris[tris + j * 4 + k] * 3];
                            vs[k].Y = meshDetail.verts[verts + meshDetail.tris[tris + j * 4 + k] * 3 + 1];
                            vs[k].Z = meshDetail.verts[verts + meshDetail.tris[tris + j * 4 + k] * 3 + 2];
                        }

                        if (RcIntersections.IntersectSegmentTriangle(sp, sq, vs[0], vs[1], vs[2], out hitTime))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                // TODO: check PolyMesh instead
            }

            return false;
        }
    }
}