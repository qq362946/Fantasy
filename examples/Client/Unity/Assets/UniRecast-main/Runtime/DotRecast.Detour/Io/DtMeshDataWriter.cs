/*
Recast4J Copyright (c) 2015 Piotr Piastucki piotr@jtilia.org

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

using System.IO;
using DotRecast.Core;

namespace DotRecast.Detour.Io
{
    using static DtDetour;

    public class DtMeshDataWriter
    {
        public void Write(BinaryWriter stream, DtMeshData data, RcByteOrder order, bool cCompatibility)
        {
            DtMeshHeader header = data.header;
            RcIO.Write(stream, header.magic, order);
            RcIO.Write(stream, cCompatibility ? DT_NAVMESH_VERSION : DT_NAVMESH_VERSION_RECAST4J_LAST, order);
            RcIO.Write(stream, header.x, order);
            RcIO.Write(stream, header.y, order);
            RcIO.Write(stream, header.layer, order);
            RcIO.Write(stream, header.userId, order);
            RcIO.Write(stream, header.polyCount, order);
            RcIO.Write(stream, header.vertCount, order);
            RcIO.Write(stream, header.maxLinkCount, order);
            RcIO.Write(stream, header.detailMeshCount, order);
            RcIO.Write(stream, header.detailVertCount, order);
            RcIO.Write(stream, header.detailTriCount, order);
            RcIO.Write(stream, header.bvNodeCount, order);
            RcIO.Write(stream, header.offMeshConCount, order);
            RcIO.Write(stream, header.offMeshBase, order);
            RcIO.Write(stream, header.walkableHeight, order);
            RcIO.Write(stream, header.walkableRadius, order);
            RcIO.Write(stream, header.walkableClimb, order);
            RcIO.Write(stream, header.bmin.X, order);
            RcIO.Write(stream, header.bmin.Y, order);
            RcIO.Write(stream, header.bmin.Z, order);
            RcIO.Write(stream, header.bmax.X, order);
            RcIO.Write(stream, header.bmax.Y, order);
            RcIO.Write(stream, header.bmax.Z, order);
            RcIO.Write(stream, header.bvQuantFactor, order);
            WriteVerts(stream, data.verts, header.vertCount, order);
            WritePolys(stream, data, order, cCompatibility);
            if (cCompatibility)
            {
                byte[] linkPlaceholder = new byte[header.maxLinkCount * DtMeshDataReader.GetSizeofLink(false)];
                stream.Write(linkPlaceholder);
            }

            WritePolyDetails(stream, data, order, cCompatibility);
            WriteVerts(stream, data.detailVerts, header.detailVertCount, order);
            WriteDTris(stream, data);
            WriteBVTree(stream, data, order, cCompatibility);
            WriteOffMeshCons(stream, data, order);
        }

        private void WriteVerts(BinaryWriter stream, float[] verts, int count, RcByteOrder order)
        {
            for (int i = 0; i < count * 3; i++)
            {
                RcIO.Write(stream, verts[i], order);
            }
        }

        private void WritePolys(BinaryWriter stream, DtMeshData data, RcByteOrder order, bool cCompatibility)
        {
            for (int i = 0; i < data.header.polyCount; i++)
            {
                if (cCompatibility)
                {
                    RcIO.Write(stream, 0xFFFF, order);
                }

                for (int j = 0; j < data.polys[i].verts.Length; j++)
                {
                    RcIO.Write(stream, (short)data.polys[i].verts[j], order);
                }

                for (int j = 0; j < data.polys[i].neis.Length; j++)
                {
                    RcIO.Write(stream, (short)data.polys[i].neis[j], order);
                }

                RcIO.Write(stream, (short)data.polys[i].flags, order);
                RcIO.Write(stream, (byte)data.polys[i].vertCount);
                RcIO.Write(stream, (byte)data.polys[i].areaAndtype);
            }
        }

        private void WritePolyDetails(BinaryWriter stream, DtMeshData data, RcByteOrder order, bool cCompatibility)
        {
            for (int i = 0; i < data.header.detailMeshCount; i++)
            {
                RcIO.Write(stream, data.detailMeshes[i].vertBase, order);
                RcIO.Write(stream, data.detailMeshes[i].triBase, order);
                RcIO.Write(stream, (byte)data.detailMeshes[i].vertCount);
                RcIO.Write(stream, (byte)data.detailMeshes[i].triCount);
                if (cCompatibility)
                {
                    RcIO.Write(stream, (short)0, order);
                }
            }
        }

        private void WriteDTris(BinaryWriter stream, DtMeshData data)
        {
            for (int i = 0; i < data.header.detailTriCount * 4; i++)
            {
                RcIO.Write(stream, (byte)data.detailTris[i]);
            }
        }

        private void WriteBVTree(BinaryWriter stream, DtMeshData data, RcByteOrder order, bool cCompatibility)
        {
            for (int i = 0; i < data.header.bvNodeCount; i++)
            {
                if (cCompatibility)
                {
                    RcIO.Write(stream, (short)data.bvTree[i].bmin.X, order);
                    RcIO.Write(stream, (short)data.bvTree[i].bmin.Y, order);
                    RcIO.Write(stream, (short)data.bvTree[i].bmin.Z, order);

                    RcIO.Write(stream, (short)data.bvTree[i].bmax.X, order);
                    RcIO.Write(stream, (short)data.bvTree[i].bmax.Y, order);
                    RcIO.Write(stream, (short)data.bvTree[i].bmax.Z, order);
                }
                else
                {
                    RcIO.Write(stream, data.bvTree[i].bmin.X, order);
                    RcIO.Write(stream, data.bvTree[i].bmin.Y, order);
                    RcIO.Write(stream, data.bvTree[i].bmin.Z, order);

                    RcIO.Write(stream, data.bvTree[i].bmax.X, order);
                    RcIO.Write(stream, data.bvTree[i].bmax.Y, order);
                    RcIO.Write(stream, data.bvTree[i].bmax.Z, order);
                }

                RcIO.Write(stream, data.bvTree[i].i, order);
            }
        }

        private void WriteOffMeshCons(BinaryWriter stream, DtMeshData data, RcByteOrder order)
        {
            for (int i = 0; i < data.header.offMeshConCount; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    RcIO.Write(stream, data.offMeshCons[i].pos[j].X, order);
                    RcIO.Write(stream, data.offMeshCons[i].pos[j].Y, order);
                    RcIO.Write(stream, data.offMeshCons[i].pos[j].Z, order);
                }

                RcIO.Write(stream, data.offMeshCons[i].rad, order);
                RcIO.Write(stream, (short)data.offMeshCons[i].poly, order);
                RcIO.Write(stream, (byte)data.offMeshCons[i].flags);
                RcIO.Write(stream, (byte)data.offMeshCons[i].side);
                RcIO.Write(stream, data.offMeshCons[i].userId, order);
            }
        }
    }
}