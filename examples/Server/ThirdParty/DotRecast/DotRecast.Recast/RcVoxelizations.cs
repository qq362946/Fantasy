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

using System.Collections.Generic;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Recast.Geom;

namespace DotRecast.Recast
{
    public static class RcVoxelizations
    {
        public static RcHeightfield BuildSolidHeightfield(RcContext ctx, IRcInputGeomProvider geomProvider, RcBuilderConfig builderCfg)
        {
            RcConfig cfg = builderCfg.cfg;

            // Allocate voxel heightfield where we rasterize our input data to.
            RcHeightfield solid = new RcHeightfield(builderCfg.width, builderCfg.height, builderCfg.bmin, builderCfg.bmax, cfg.Cs, cfg.Ch, cfg.BorderSize);

            // Allocate array that can hold triangle area types.
            // If you have multiple meshes you need to process, allocate
            // and array which can hold the max number of triangles you need to process.

            // Find triangles which are walkable based on their slope and rasterize them.
            // If your input data is multiple meshes, you can transform them here, calculate
            // the are type for each of the meshes and rasterize them.
            foreach (RcTriMesh geom in geomProvider.Meshes())
            {
                float[] verts = geom.GetVerts();
                if (cfg.UseTiles)
                {
                    RcVec2f tbmin;
                    RcVec2f tbmax;
                    tbmin.X = builderCfg.bmin.X;
                    tbmin.Y = builderCfg.bmin.Z;
                    tbmax.X = builderCfg.bmax.X;
                    tbmax.Y = builderCfg.bmax.Z;
                    List<RcChunkyTriMeshNode> nodes = geom.GetChunksOverlappingRect(tbmin, tbmax);
                    foreach (RcChunkyTriMeshNode node in nodes)
                    {
                        int[] tris = node.tris;
                        int ntris = tris.Length / 3;
                        int[] m_triareas = RcRecast.MarkWalkableTriangles(ctx, cfg.WalkableSlopeAngle, verts, tris, ntris, cfg.WalkableAreaMod);
                        RcRasterizations.RasterizeTriangles(ctx, verts, tris, m_triareas, ntris, solid, cfg.WalkableClimb);
                    }
                }
                else
                {
                    int[] tris = geom.GetTris();
                    int ntris = tris.Length / 3;
                    int[] m_triareas = RcRecast.MarkWalkableTriangles(ctx, cfg.WalkableSlopeAngle, verts, tris, ntris, cfg.WalkableAreaMod);
                    RcRasterizations.RasterizeTriangles(ctx, verts, tris, m_triareas, ntris, solid, cfg.WalkableClimb);
                }
            }

            return solid;
        }
    }
}
