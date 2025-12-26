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

using DotRecast.Core.Collections;
using DotRecast.Detour;
using DotRecast.Recast.Toolset.Geom;

namespace DotRecast.Recast.Toolset.Builder
{
    public class SoloNavMeshBuilder
    {
        public NavMeshBuildResult Build(DemoInputGeomProvider geom, RcNavMeshBuildSettings settings)
        {
            return Build(geom,
                RcPartitionType.OfValue(settings.partitioning),
                settings.cellSize, settings.cellHeight,
                settings.agentMaxSlope, settings.agentHeight, settings.agentRadius, settings.agentMaxClimb,
                settings.minRegionSize, settings.mergedRegionSize,
                settings.edgeMaxLen, settings.edgeMaxError,
                settings.vertsPerPoly,
                settings.detailSampleDist, settings.detailSampleMaxError,
                settings.filterLowHangingObstacles, settings.filterLedgeSpans, settings.filterWalkableLowHeightSpans,
                settings.keepInterResults);
        }

        public NavMeshBuildResult Build(DemoInputGeomProvider geom,
            RcPartition partitionType,
            float cellSize, float cellHeight,
            float agentMaxSlope, float agentHeight, float agentRadius, float agentMaxClimb,
            int regionMinSize, int regionMergeSize,
            float edgeMaxLen, float edgeMaxError,
            int vertsPerPoly,
            float detailSampleDist, float detailSampleMaxError,
            bool filterLowHangingObstacles, bool filterLedgeSpans, bool filterWalkableLowHeightSpans,
            bool keepInterResults)
        {
            RcConfig cfg = new RcConfig(
                partitionType,
                cellSize, cellHeight,
                agentMaxSlope, agentHeight, agentRadius, agentMaxClimb,
                regionMinSize, regionMergeSize,
                edgeMaxLen, edgeMaxError,
                vertsPerPoly,
                detailSampleDist, detailSampleMaxError,
                filterLowHangingObstacles, filterLedgeSpans, filterWalkableLowHeightSpans,
                SampleAreaModifications.SAMPLE_AREAMOD_WALKABLE, true);

            RcBuilderResult rcResult = BuildRecastResult(geom, cfg, keepInterResults);
            var meshData = BuildMeshData(geom, cellSize, cellHeight, agentHeight, agentRadius, agentMaxClimb, rcResult);
            if (null == meshData)
            {
                return new NavMeshBuildResult();
            }

            var navMesh = BuildNavMesh(meshData, vertsPerPoly);
            return new NavMeshBuildResult(cfg, RcImmutableArray.Create(rcResult), navMesh);
        }

        private DtNavMesh BuildNavMesh(DtMeshData meshData, int vertsPerPoly)
        {
            var mesh = new DtNavMesh();
            var status = mesh.Init(meshData, vertsPerPoly, 0);
            if (status.Failed())
            {
                return null;
            }
            
            return mesh;
        }

        private RcBuilderResult BuildRecastResult(DemoInputGeomProvider geom, RcConfig cfg, bool keepInterResults)
        {
            RcBuilderConfig bcfg = new RcBuilderConfig(cfg, geom.GetMeshBoundsMin(), geom.GetMeshBoundsMax());
            RcBuilder rcBuilder = new RcBuilder();
            return rcBuilder.Build(geom, bcfg, keepInterResults);
        }

        public DtMeshData BuildMeshData(DemoInputGeomProvider geom,
            float cellSize, float cellHeight,
            float agentHeight, float agentRadius, float agentMaxClimb,
            RcBuilderResult result)
        {
            DtNavMeshCreateParams option = DemoNavMeshBuilder
                .GetNavMeshCreateParams(geom, cellSize, cellHeight, agentHeight, agentRadius, agentMaxClimb, result);
            var meshData = DtNavMeshBuilder.CreateNavMeshData(option);
            if (null == meshData)
            {
                return null;
            }

            return DemoNavMeshBuilder.UpdateAreaAndFlags(meshData);
        }
    }
}