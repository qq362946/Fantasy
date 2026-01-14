using System;
using System.Collections.Generic;
using DotRecast.Detour;

namespace DotRecast.Recast.Toolset.Builder
{
    public class NavMeshBuildResult
    {
        public readonly bool Success;
        public readonly RcConfig Cfg;
        public readonly IList<RcBuilderResult> RecastBuilderResults;
        public readonly DtNavMesh NavMesh;

        public NavMeshBuildResult()
        {
            Success = false;
            RecastBuilderResults = Array.Empty<RcBuilderResult>();
            NavMesh = null;
        }
        
        // for solo
        public NavMeshBuildResult(RcConfig cfg, IList<RcBuilderResult> recastBuilderResults, DtNavMesh navMesh)
        {
            Success = true;
            Cfg = cfg;
            RecastBuilderResults = recastBuilderResults;
            NavMesh = navMesh;
        }
        
        // for tiles
        public NavMeshBuildResult(RcConfig cfg, IList<RcBuilderResult> recastBuilderResults)
        {
            Success = true;
            Cfg = cfg;
            RecastBuilderResults = recastBuilderResults;
            NavMesh = null;
        }
    }
}