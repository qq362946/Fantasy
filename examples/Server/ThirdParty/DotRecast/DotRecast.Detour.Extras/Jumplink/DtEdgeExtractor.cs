using System.Collections.Generic;
using DotRecast.Core.Numerics;
using DotRecast.Recast;

namespace DotRecast.Detour.Extras.Jumplink
{
    using static RcRecast;

    public class DtEdgeExtractor
    {
        public DtJumpEdge[] ExtractEdges(RcPolyMesh mesh)
        {
            List<DtJumpEdge> edges = new List<DtJumpEdge>();
            if (mesh != null)
            {
                RcVec3f orig = mesh.bmin;
                float cs = mesh.cs;
                float ch = mesh.ch;
                for (int i = 0; i < mesh.npolys; i++)
                {
                    if (i > 41 || i < 41)
                    {
                        // continue;
                    }

                    int nvp = mesh.nvp;
                    int p = i * 2 * nvp;
                    for (int j = 0; j < nvp; ++j)
                    {
                        if (j != 1)
                        {
                            // continue;
                        }

                        if (mesh.polys[p + j] == RC_MESH_NULL_IDX)
                        {
                            break;
                        }

                        // Skip connected edges.
                        if ((mesh.polys[p + nvp + j] & 0x8000) != 0)
                        {
                            int dir = mesh.polys[p + nvp + j] & 0xf;
                            if (dir == 0xf)
                            {
                                // Border
                                if (mesh.polys[p + nvp + j] != RC_MESH_NULL_IDX)
                                {
                                    continue;
                                }

                                int nj = j + 1;
                                if (nj >= nvp || mesh.polys[p + nj] == RC_MESH_NULL_IDX)
                                {
                                    nj = 0;
                                }

                                int va = mesh.polys[p + j] * 3;
                                int vb = mesh.polys[p + nj] * 3;
                                DtJumpEdge e = new DtJumpEdge();
                                e.sp.X = orig.X + mesh.verts[vb] * cs;
                                e.sp.Y = orig.Y + mesh.verts[vb + 1] * ch;
                                e.sp.Z = orig.Z + mesh.verts[vb + 2] * cs;
                                e.sq.X = orig.X + mesh.verts[va] * cs;
                                e.sq.Y = orig.Y + mesh.verts[va + 1] * ch;
                                e.sq.Z = orig.Z + mesh.verts[va + 2] * cs;
                                edges.Add(e);
                            }
                        }
                    }
                }
            }

            return edges.ToArray();
        }
    }
}