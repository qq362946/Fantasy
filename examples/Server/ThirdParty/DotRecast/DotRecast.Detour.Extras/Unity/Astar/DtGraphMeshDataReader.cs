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

using System;
using System.IO.Compression;
using DotRecast.Core;

namespace DotRecast.Detour.Extras.Unity.Astar
{
    using static DtDetour;

    public class DtGraphMeshDataReader : DtZipBinaryReader
    {
        public const float INT_PRECISION_FACTOR = 1000f;

        public DtGraphMeshData Read(ZipArchive file, string filename, DtGraphMeta meta, int maxVertPerPoly)
        {
            RcByteBuffer buffer = ToByteBuffer(file, filename);
            int tileXCount = buffer.GetInt();
            if (tileXCount < 0)
            {
                return null;
            }

            int tileZCount = buffer.GetInt();
            DtMeshData[] tiles = new DtMeshData[tileXCount * tileZCount];
            for (int z = 0; z < tileZCount; z++)
            {
                for (int x = 0; x < tileXCount; x++)
                {
                    int tileIndex = x + z * tileXCount;
                    int tx = buffer.GetInt();
                    int tz = buffer.GetInt();
                    if (tx != x || tz != z)
                    {
                        throw new ArgumentException("Inconsistent tile positions");
                    }

                    tiles[tileIndex] = new DtMeshData();
                    int width = buffer.GetInt();
                    int depth = buffer.GetInt();

                    int trisCount = buffer.GetInt();
                    int[] tris = new int[trisCount];
                    for (int i = 0; i < tris.Length; i++)
                    {
                        tris[i] = buffer.GetInt();
                    }

                    int vertsCount = buffer.GetInt();
                    float[] verts = new float[3 * vertsCount];
                    for (int i = 0; i < verts.Length; i++)
                    {
                        verts[i] = buffer.GetInt() / INT_PRECISION_FACTOR;
                    }

                    int[] vertsInGraphSpace = new int[3 * buffer.GetInt()];
                    for (int i = 0; i < vertsInGraphSpace.Length; i++)
                    {
                        vertsInGraphSpace[i] = buffer.GetInt();
                    }

                    int nodeCount = buffer.GetInt();
                    DtPoly[] nodes = new DtPoly[nodeCount];
                    DtPolyDetail[] detailNodes = new DtPolyDetail[nodeCount];
                    float[] detailVerts = Array.Empty<float>();
                    int[] detailTris = new int[4 * nodeCount];
                    int vertMask = GetVertMask(vertsCount);
                    float ymin = float.PositiveInfinity;
                    float ymax = float.NegativeInfinity;
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        nodes[i] = new DtPoly(i, maxVertPerPoly);
                        nodes[i].vertCount = 3;
                        // XXX: What can we do with the penalty?
                        int penalty = buffer.GetInt();
                        nodes[i].flags = buffer.GetInt();
                        nodes[i].verts[0] = buffer.GetInt() & vertMask;
                        nodes[i].verts[1] = buffer.GetInt() & vertMask;
                        nodes[i].verts[2] = buffer.GetInt() & vertMask;
                        ymin = Math.Min(ymin, verts[nodes[i].verts[0] * 3 + 1]);
                        ymin = Math.Min(ymin, verts[nodes[i].verts[1] * 3 + 1]);
                        ymin = Math.Min(ymin, verts[nodes[i].verts[2] * 3 + 1]);
                        ymax = Math.Max(ymax, verts[nodes[i].verts[0] * 3 + 1]);
                        ymax = Math.Max(ymax, verts[nodes[i].verts[1] * 3 + 1]);
                        ymax = Math.Max(ymax, verts[nodes[i].verts[2] * 3 + 1]);
                        int vertBase = 0;
                        byte vertCount = 0;
                        int triBase = i;
                        byte triCount = 1;
                        detailNodes[i] = new DtPolyDetail(vertBase, triBase, vertCount, triCount);
                        detailTris[4 * i] = 0;
                        detailTris[4 * i + 1] = 1;
                        detailTris[4 * i + 2] = 2;
                        // Bit for each edge that belongs to poly boundary, basically all edges marked as boundary as it is
                        // a triangle
                        detailTris[4 * i + 3] = (1 << 4) | (1 << 2) | 1;
                    }

                    tiles[tileIndex].verts = verts;
                    tiles[tileIndex].polys = nodes;
                    tiles[tileIndex].detailMeshes = detailNodes;
                    tiles[tileIndex].detailVerts = detailVerts;
                    tiles[tileIndex].detailTris = detailTris;
                    DtMeshHeader header = new DtMeshHeader();
                    header.magic = DT_NAVMESH_MAGIC;
                    header.version = DT_NAVMESH_VERSION;
                    header.x = x;
                    header.y = z;
                    header.polyCount = nodeCount;
                    header.vertCount = vertsCount;
                    header.detailMeshCount = nodeCount;
                    header.detailTriCount = nodeCount;
                    header.maxLinkCount = nodeCount * 3 * 2; // needed by Recast, not needed by recast4j, needed by DotRecast
                    header.bmin.X = meta.forcedBoundsCenter.x - 0.5f * meta.forcedBoundsSize.x +
                                    meta.cellSize * meta.tileSizeX * x;
                    header.bmin.Y = ymin;
                    header.bmin.Z = meta.forcedBoundsCenter.z - 0.5f * meta.forcedBoundsSize.z +
                                    meta.cellSize * meta.tileSizeZ * z;
                    header.bmax.X = meta.forcedBoundsCenter.x - 0.5f * meta.forcedBoundsSize.x +
                                    meta.cellSize * meta.tileSizeX * (x + 1);
                    header.bmax.Y = ymax;
                    header.bmax.Z = meta.forcedBoundsCenter.z - 0.5f * meta.forcedBoundsSize.z +
                                    meta.cellSize * meta.tileSizeZ * (z + 1);
                    header.bvQuantFactor = 1.0f / meta.cellSize;
                    header.offMeshBase = nodeCount;
                    header.walkableClimb = meta.walkableClimb;
                    header.walkableHeight = meta.walkableHeight;
                    header.walkableRadius = meta.characterRadius;
                    tiles[tileIndex].header = header;
                }
            }

            return new DtGraphMeshData(tileXCount, tileZCount, tiles);
        }

        public static int HighestOneBit(uint i)
        {
            i |= (i >> 1);
            i |= (i >> 2);
            i |= (i >> 4);
            i |= (i >> 8);
            i |= (i >> 16);
            return (int)(i - (i >> 1));
        }

        // See NavmeshBase.cs: ASTAR_RECAST_LARGER_TILES
        private int GetVertMask(int vertsCount)
        {
            int vertMask = HighestOneBit((uint)vertsCount);
            if (vertMask != vertsCount)
            {
                vertMask *= 2;
            }

            vertMask--;
            return vertMask;
        }
    }
}