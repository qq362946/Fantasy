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

    public class DtMeshDataReader
    {
        public const int DT_POLY_DETAIL_SIZE = 10;
        public const int LINK_SIZEOF = 16;
        public const int LINK_SIZEOF32BIT = 12;

        public DtMeshData Read(BinaryReader stream, int maxVertPerPoly)
        {
            RcByteBuffer buf = RcIO.ToByteBuffer(stream);
            return Read(buf, maxVertPerPoly, false);
        }

        public DtMeshData Read(RcByteBuffer buf, int maxVertPerPoly)
        {
            return Read(buf, maxVertPerPoly, false);
        }

        public DtMeshData Read32Bit(BinaryReader stream, int maxVertPerPoly)
        {
            RcByteBuffer buf = RcIO.ToByteBuffer(stream);
            return Read(buf, maxVertPerPoly, true);
        }

        public DtMeshData Read32Bit(RcByteBuffer buf, int maxVertPerPoly)
        {
            return Read(buf, maxVertPerPoly, true);
        }

        public DtMeshData Read(RcByteBuffer buf, int maxVertPerPoly, bool is32Bit)
        {
            DtMeshData data = new DtMeshData();
            DtMeshHeader header = new DtMeshHeader();
            data.header = header;
            header.magic = buf.GetInt();
            if (header.magic != DT_NAVMESH_MAGIC)
            {
                header.magic = RcIO.SwapEndianness(header.magic);
                if (header.magic != DT_NAVMESH_MAGIC)
                {
                    throw new IOException("Invalid magic");
                }

                buf.Order(buf.Order() == RcByteOrder.BIG_ENDIAN ? RcByteOrder.LITTLE_ENDIAN : RcByteOrder.BIG_ENDIAN);
            }

            header.version = buf.GetInt();
            if (header.version != DT_NAVMESH_VERSION)
            {
                if (header.version < DT_NAVMESH_VERSION_RECAST4J_FIRST
                    || header.version > DT_NAVMESH_VERSION_RECAST4J_LAST)
                {
                    throw new IOException("Invalid version " + header.version);
                }
            }

            bool cCompatibility = header.version == DT_NAVMESH_VERSION;
            header.x = buf.GetInt();
            header.y = buf.GetInt();
            header.layer = buf.GetInt();
            header.userId = buf.GetInt();
            header.polyCount = buf.GetInt();
            header.vertCount = buf.GetInt();
            header.maxLinkCount = buf.GetInt();
            header.detailMeshCount = buf.GetInt();
            header.detailVertCount = buf.GetInt();
            header.detailTriCount = buf.GetInt();
            header.bvNodeCount = buf.GetInt();
            header.offMeshConCount = buf.GetInt();
            header.offMeshBase = buf.GetInt();
            header.walkableHeight = buf.GetFloat();
            header.walkableRadius = buf.GetFloat();
            header.walkableClimb = buf.GetFloat();

            header.bmin.X = buf.GetFloat();
            header.bmin.Y = buf.GetFloat();
            header.bmin.Z = buf.GetFloat();

            header.bmax.X = buf.GetFloat();
            header.bmax.Y = buf.GetFloat();
            header.bmax.Z = buf.GetFloat();

            header.bvQuantFactor = buf.GetFloat();
            data.verts = ReadVerts(buf, header.vertCount);
            data.polys = ReadPolys(buf, header, maxVertPerPoly);
            if (cCompatibility)
            {
                buf.Position(buf.Position() + header.maxLinkCount * GetSizeofLink(is32Bit));
            }

            data.detailMeshes = ReadPolyDetails(buf, header, cCompatibility);
            data.detailVerts = ReadVerts(buf, header.detailVertCount);
            data.detailTris = ReadDTris(buf, header);
            data.bvTree = ReadBVTree(buf, header);
            data.offMeshCons = ReadOffMeshCons(buf, header);
            return data;
        }


        public static int GetSizeofLink(bool is32Bit)
        {
            return is32Bit ? LINK_SIZEOF32BIT : LINK_SIZEOF;
        }

        private float[] ReadVerts(RcByteBuffer buf, int count)
        {
            float[] verts = new float[count * 3];
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = buf.GetFloat();
            }

            return verts;
        }

        private DtPoly[] ReadPolys(RcByteBuffer buf, DtMeshHeader header, int maxVertPerPoly)
        {
            DtPoly[] polys = new DtPoly[header.polyCount];
            for (int i = 0; i < polys.Length; i++)
            {
                polys[i] = new DtPoly(i, maxVertPerPoly);
                if (header.version < DT_NAVMESH_VERSION_RECAST4J_NO_POLY_FIRSTLINK)
                {
                    buf.GetInt(); // polys[i].firstLink
                }

                for (int j = 0; j < polys[i].verts.Length; j++)
                {
                    polys[i].verts[j] = buf.GetShort() & 0xFFFF;
                }

                for (int j = 0; j < polys[i].neis.Length; j++)
                {
                    polys[i].neis[j] = buf.GetShort() & 0xFFFF;
                }

                polys[i].flags = buf.GetShort() & 0xFFFF;
                polys[i].vertCount = buf.Get() & 0xFF;
                polys[i].areaAndtype = buf.Get() & 0xFF;
            }

            return polys;
        }

        private DtPolyDetail[] ReadPolyDetails(RcByteBuffer buf, DtMeshHeader header, bool cCompatibility)
        {
            DtPolyDetail[] polys = new DtPolyDetail[header.detailMeshCount];
            for (int i = 0; i < polys.Length; i++)
            {
                int vertBase = buf.GetInt();
                int triBase = buf.GetInt();
                byte vertCount = (byte)(buf.Get() & 0xFF);
                byte triCount = (byte)(buf.Get() & 0xFF);
                polys[i] = new DtPolyDetail(vertBase, triBase, vertCount, triCount);
                if (cCompatibility)
                {
                    buf.GetShort(); // C struct padding
                }
            }

            return polys;
        }

        private int[] ReadDTris(RcByteBuffer buf, DtMeshHeader header)
        {
            int[] tris = new int[4 * header.detailTriCount];
            for (int i = 0; i < tris.Length; i++)
            {
                tris[i] = buf.Get() & 0xFF;
            }

            return tris;
        }

        private DtBVNode[] ReadBVTree(RcByteBuffer buf, DtMeshHeader header)
        {
            DtBVNode[] nodes = new DtBVNode[header.bvNodeCount];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = new DtBVNode();
                if (header.version < DT_NAVMESH_VERSION_RECAST4J_32BIT_BVTREE)
                {
                    nodes[i].bmin.X = buf.GetShort() & 0xFFFF;
                    nodes[i].bmin.Y = buf.GetShort() & 0xFFFF;
                    nodes[i].bmin.Z = buf.GetShort() & 0xFFFF;

                    nodes[i].bmax.X = buf.GetShort() & 0xFFFF;
                    nodes[i].bmax.Y = buf.GetShort() & 0xFFFF;
                    nodes[i].bmax.Z = buf.GetShort() & 0xFFFF;
                }
                else
                {
                    nodes[i].bmin.X = buf.GetInt();
                    nodes[i].bmin.Y = buf.GetInt();
                    nodes[i].bmin.Z = buf.GetInt();

                    nodes[i].bmax.X = buf.GetInt();
                    nodes[i].bmax.Y = buf.GetInt();
                    nodes[i].bmax.Z = buf.GetInt();
                }

                nodes[i].i = buf.GetInt();
            }

            return nodes;
        }

        private DtOffMeshConnection[] ReadOffMeshCons(RcByteBuffer buf, DtMeshHeader header)
        {
            DtOffMeshConnection[] cons = new DtOffMeshConnection[header.offMeshConCount];
            for (int i = 0; i < cons.Length; i++)
            {
                cons[i] = new DtOffMeshConnection();
                for (int j = 0; j < 2; j++)
                {
                    cons[i].pos[j].X = buf.GetFloat();
                    cons[i].pos[j].Y = buf.GetFloat();
                    cons[i].pos[j].Z = buf.GetFloat();
                }

                cons[i].rad = buf.GetFloat();
                cons[i].poly = buf.GetShort() & 0xFFFF;
                cons[i].flags = buf.Get() & 0xFF;
                cons[i].side = buf.Get() & 0xFF;
                cons[i].userId = buf.GetInt();
            }

            return cons;
        }
    }
}