using DotRecast.Detour;
using DotRecast.Recast.Geom;

namespace DotRecast.Recast.Toolset.Builder
{
    public static class DemoNavMeshBuilder
    {
        public static DtNavMeshCreateParams GetNavMeshCreateParams(IInputGeomProvider geom, float cellSize,
            float cellHeight, float agentHeight, float agentRadius, float agentMaxClimb,
            RcBuilderResult rcResult)
        {
            RcPolyMesh pmesh = rcResult.Mesh;
            RcPolyMeshDetail dmesh = rcResult.MeshDetail;
            DtNavMeshCreateParams option = new DtNavMeshCreateParams();
            for (int i = 0; i < pmesh.npolys; ++i)
            {
                pmesh.flags[i] = 1;
            }

            option.verts = pmesh.verts;
            option.vertCount = pmesh.nverts;
            option.polys = pmesh.polys;
            option.polyAreas = pmesh.areas;
            option.polyFlags = pmesh.flags;
            option.polyCount = pmesh.npolys;
            option.nvp = pmesh.nvp;
            if (dmesh != null)
            {
                option.detailMeshes = dmesh.meshes;
                option.detailVerts = dmesh.verts;
                option.detailVertsCount = dmesh.nverts;
                option.detailTris = dmesh.tris;
                option.detailTriCount = dmesh.ntris;
            }

            option.walkableHeight = agentHeight;
            option.walkableRadius = agentRadius;
            option.walkableClimb = agentMaxClimb;
            option.bmin = pmesh.bmin;
            option.bmax = pmesh.bmax;
            option.cs = cellSize;
            option.ch = cellHeight;
            option.buildBvTree = true;

            var offMeshConnections = geom.GetOffMeshConnections();
            option.offMeshConCount = offMeshConnections.Count;
            option.offMeshConVerts = new float[option.offMeshConCount * 6];
            option.offMeshConRad = new float[option.offMeshConCount];
            option.offMeshConDir = new int[option.offMeshConCount];
            option.offMeshConAreas = new int[option.offMeshConCount];
            option.offMeshConFlags = new int[option.offMeshConCount];
            option.offMeshConUserID = new int[option.offMeshConCount];
            for (int i = 0; i < option.offMeshConCount; i++)
            {
                RcOffMeshConnection offMeshCon = offMeshConnections[i];
                for (int j = 0; j < 6; j++)
                {
                    option.offMeshConVerts[6 * i + j] = offMeshCon.verts[j];
                }

                option.offMeshConRad[i] = offMeshCon.radius;
                option.offMeshConDir[i] = offMeshCon.bidir ? 1 : 0;
                option.offMeshConAreas[i] = offMeshCon.area;
                option.offMeshConFlags[i] = offMeshCon.flags;
                // option.offMeshConUserID[i] = offMeshCon.userId;
            }

            return option;
        }

        public static DtMeshData UpdateAreaAndFlags(DtMeshData meshData)
        {
            // Update poly flags from areas.
            for (int i = 0; i < meshData.polys.Length; ++i)
            {
                if (meshData.polys[i].GetArea() == SampleAreaModifications.SAMPLE_POLYAREA_TYPE_WALKABLE)
                {
                    meshData.polys[i].SetArea(SampleAreaModifications.SAMPLE_POLYAREA_TYPE_GROUND);
                }

                if (meshData.polys[i].GetArea() == SampleAreaModifications.SAMPLE_POLYAREA_TYPE_GROUND
                    || meshData.polys[i].GetArea() == SampleAreaModifications.SAMPLE_POLYAREA_TYPE_GRASS
                    || meshData.polys[i].GetArea() == SampleAreaModifications.SAMPLE_POLYAREA_TYPE_ROAD)
                {
                    meshData.polys[i].flags = SampleAreaModifications.SAMPLE_POLYFLAGS_WALK;
                }
                else if (meshData.polys[i].GetArea() == SampleAreaModifications.SAMPLE_POLYAREA_TYPE_WATER)
                {
                    meshData.polys[i].flags = SampleAreaModifications.SAMPLE_POLYFLAGS_SWIM;
                }
                else if (meshData.polys[i].GetArea() == SampleAreaModifications.SAMPLE_POLYAREA_TYPE_DOOR)
                {
                    meshData.polys[i].flags = SampleAreaModifications.SAMPLE_POLYFLAGS_DOOR;
                }
            }

            return meshData;
        }
    }
}