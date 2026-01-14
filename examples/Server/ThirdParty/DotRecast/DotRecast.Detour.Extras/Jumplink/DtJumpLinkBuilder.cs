using System;
using System.Collections.Generic;
using System.Linq;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Recast;

namespace DotRecast.Detour.Extras.Jumplink
{
    public class DtJumpLinkBuilder
    {
        private readonly DtEdgeExtractor edgeExtractor = new DtEdgeExtractor();
        private readonly DtEdgeSamplerFactory edgeSamplerFactory = new DtEdgeSamplerFactory();
        private readonly IDtGroundSampler groundSampler = new DtNavMeshGroundSampler();
        private readonly DtTrajectorySampler trajectorySampler = new DtTrajectorySampler();
        private readonly DtJumpSegmentBuilder jumpSegmentBuilder = new DtJumpSegmentBuilder();

        private readonly List<DtJumpEdge[]> edges;
        private readonly IList<RcBuilderResult> results;

        public DtJumpLinkBuilder(IList<RcBuilderResult> results)
        {
            this.results = results;
            edges = results.Select(r => edgeExtractor.ExtractEdges(r.Mesh)).ToList();
        }

        public List<DtJumpLink> Build(DtJumpLinkBuilderConfig acfg, DtJumpLinkType type)
        {
            List<DtJumpLink> links = new List<DtJumpLink>();
            for (int tile = 0; tile < results.Count; tile++)
            {
                DtJumpEdge[] edges = this.edges[tile];
                foreach (DtJumpEdge edge in edges)
                {
                    links.AddRange(ProcessEdge(acfg, results[tile], type, edge));
                }
            }

            return links;
        }

        private List<DtJumpLink> ProcessEdge(DtJumpLinkBuilderConfig acfg, RcBuilderResult result, DtJumpLinkType type, DtJumpEdge edge)
        {
            DtEdgeSampler es = edgeSamplerFactory.Get(acfg, type, edge);
            groundSampler.Sample(acfg, result, es);
            trajectorySampler.Sample(acfg, result.SolidHeightfiled, es);
            DtJumpSegment[] jumpSegments = jumpSegmentBuilder.Build(acfg, es);
            return BuildJumpLinks(acfg, es, jumpSegments);
        }


        private List<DtJumpLink> BuildJumpLinks(DtJumpLinkBuilderConfig acfg, DtEdgeSampler es, DtJumpSegment[] jumpSegments)
        {
            List<DtJumpLink> links = new List<DtJumpLink>();
            foreach (DtJumpSegment js in jumpSegments)
            {
                RcVec3f sp = es.start.gsamples[js.startSample].p;
                RcVec3f sq = es.start.gsamples[js.startSample + js.samples - 1].p;
                DtGroundSegment end = es.end[js.groundSegment];
                RcVec3f ep = end.gsamples[js.startSample].p;
                RcVec3f eq = end.gsamples[js.startSample + js.samples - 1].p;
                float d = Math.Min(RcVec.Dist2DSqr(sp, sq), RcVec.Dist2DSqr(ep, eq));
                if (d >= 4 * acfg.agentRadius * acfg.agentRadius)
                {
                    DtJumpLink link = new DtJumpLink();
                    links.Add(link);
                    link.startSamples = RcArrays.CopyOf(es.start.gsamples, js.startSample, js.samples);
                    link.endSamples = RcArrays.CopyOf(end.gsamples, js.startSample, js.samples);
                    link.start = es.start;
                    link.end = end;
                    link.trajectory = es.trajectory;
                    for (int j = 0; j < link.nspine; ++j)
                    {
                        float u = ((float)j) / (link.nspine - 1);
                        RcVec3f p = es.trajectory.Apply(sp, ep, u);
                        link.spine0[j * 3] = p.X;
                        link.spine0[j * 3 + 1] = p.Y;
                        link.spine0[j * 3 + 2] = p.Z;

                        p = es.trajectory.Apply(sq, eq, u);
                        link.spine1[j * 3] = p.X;
                        link.spine1[j * 3 + 1] = p.Y;
                        link.spine1[j * 3 + 2] = p.Z;
                    }
                }
            }

            return links;
        }

        public List<DtJumpEdge[]> GetEdges()
        {
            return edges;
        }
    }
}