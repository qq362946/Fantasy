using System;
using DotRecast.Core.Numerics;
using DotRecast.Recast.Geom;
using DotRecast.Recast.Toolset.Builder;

namespace DotRecast.Recast.Toolset.Tools
{
    public class RcOffMeshConnectionTool : IRcToolable
    {
        public RcOffMeshConnectionTool()
        {
        }

        public string GetName()
        {
            return "Off-Mesh Links";
        }

        public void Add(IInputGeomProvider geom, RcNavMeshBuildSettings settings, RcVec3f start, RcVec3f end, bool bidir)
        {
            if (null == geom)
                return;

            int area = SampleAreaModifications.SAMPLE_POLYAREA_TYPE_JUMP;
            int flags = SampleAreaModifications.SAMPLE_POLYFLAGS_JUMP;
            geom.AddOffMeshConnection(start, end, settings.agentRadius, bidir, area, flags);
        }

        public void Remove(IInputGeomProvider geom, RcNavMeshBuildSettings settings, RcVec3f p)
        {
            // Delete
            // Find nearest link end-point
            float nearestDist = float.MaxValue;
            RcOffMeshConnection nearestConnection = null;
            foreach (RcOffMeshConnection offMeshCon in geom.GetOffMeshConnections())
            {
                float d = Math.Min(RcVec.DistanceSquared(p, offMeshCon.verts, 0), RcVec.DistanceSquared(p, offMeshCon.verts, 3));
                if (d < nearestDist && Math.Sqrt(d) < settings.agentRadius)
                {
                    nearestDist = d;
                    nearestConnection = offMeshCon;
                }
            }

            if (nearestConnection != null)
            {
                geom.GetOffMeshConnections().Remove(nearestConnection);
            }
        }
    }
}