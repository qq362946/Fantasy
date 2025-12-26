using System;
using DotRecast.Core.Numerics;
using DotRecast.Recast;

namespace DotRecast.Detour.Extras.Jumplink
{
    public abstract class DtAbstractGroundSampler : IDtGroundSampler
    {
        public delegate bool ComputeNavMeshHeight(RcVec3f pt, float cellSize, out float height);

        protected void SampleGround(DtJumpLinkBuilderConfig acfg, DtEdgeSampler es, ComputeNavMeshHeight heightFunc)
        {
            float cs = acfg.cellSize;
            float dist = MathF.Sqrt(RcVec.Dist2DSqr(es.start.p, es.start.q));
            int ngsamples = Math.Max(2, (int)MathF.Ceiling(dist / cs));

            SampleGroundSegment(heightFunc, es.start, ngsamples);
            foreach (DtGroundSegment end in es.end)
            {
                SampleGroundSegment(heightFunc, end, ngsamples);
            }
        }

        public abstract void Sample(DtJumpLinkBuilderConfig acfg, RcBuilderResult result, DtEdgeSampler es);

        protected void SampleGroundSegment(ComputeNavMeshHeight heightFunc, DtGroundSegment seg, int nsamples)
        {
            seg.gsamples = new DtGroundSample[nsamples];

            for (int i = 0; i < nsamples; ++i)
            {
                float u = i / (float)(nsamples - 1);

                DtGroundSample s = new DtGroundSample();
                seg.gsamples[i] = s;
                RcVec3f pt = RcVec3f.Lerp(seg.p, seg.q, u);
                bool success = heightFunc.Invoke(pt, seg.height, out var height);
                s.p.X = pt.X;
                s.p.Y = height;
                s.p.Z = pt.Z;

                if (!success)
                {
                    continue;
                }

                s.validHeight = true;
            }
        }
    }
}