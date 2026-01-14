/*
recast4j Copyright (c) 2015-2019 Piotr Piastucki piotr@jtilia.org
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

using DotRecast.Core;
using DotRecast.Core.Numerics;

namespace DotRecast.Detour.Extras.Unity.Astar
{
    public class OffMeshLinkCreator
    {
        public void Build(GraphMeshData graphData, NodeLink2[] links, int nodeOffset)
        {
            if (links.Length > 0)
            {
                foreach (NodeLink2 l in links)
                {
                    DtMeshData startTile = graphData.GetTile(l.startNode - nodeOffset);
                    DtPoly startNode = graphData.GetNode(l.startNode - nodeOffset);
                    DtMeshData endTile = graphData.GetTile(l.endNode - nodeOffset);
                    DtPoly endNode = graphData.GetNode(l.endNode - nodeOffset);
                    if (startNode != null && endNode != null)
                    {
                        // FIXME: Optimise
                        startTile.polys = RcArrays.CopyOf(startTile.polys, startTile.polys.Length + 1);
                        int poly = startTile.header.polyCount;
                        startTile.polys[poly] = new DtPoly(poly, 2);
                        startTile.polys[poly].verts[0] = startTile.header.vertCount;
                        startTile.polys[poly].verts[1] = startTile.header.vertCount + 1;
                        startTile.polys[poly].SetPolyType(DtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION);
                        startTile.verts = RcArrays.CopyOf(startTile.verts, startTile.verts.Length + 6);
                        startTile.header.polyCount++;
                        startTile.header.vertCount += 2;
                        DtOffMeshConnection connection = new DtOffMeshConnection();
                        connection.poly = poly;
                        connection.pos = new RcVec3f[]
                        {
                            l.clamped1, l.clamped2
                        };
                        connection.rad = 0.1f;
                        connection.side = startTile == endTile
                            ? 0xFF
                            : DtNavMeshBuilder.ClassifyOffMeshPoint(connection.pos[1], startTile.header.bmin, startTile.header.bmax);
                        connection.userId = (int)l.linkID;
                        if (startTile.offMeshCons == null)
                        {
                            startTile.offMeshCons = new DtOffMeshConnection[1];
                        }
                        else
                        {
                            startTile.offMeshCons = RcArrays.CopyOf(startTile.offMeshCons, startTile.offMeshCons.Length + 1);
                        }

                        startTile.offMeshCons[startTile.offMeshCons.Length - 1] = connection;
                        startTile.header.offMeshConCount++;
                    }
                }
            }
        }
    }
}