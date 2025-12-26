using System;
using DotRecast.Core.Numerics;
using DotRecast.Recast;


namespace DotRecast.Detour.Extras.Jumplink
{
    public class TrajectorySampler
    {
        public void Sample(JumpLinkBuilderConfig acfg, RcHeightfield heightfield, EdgeSampler es)
        {
            int nsamples = es.start.gsamples.Length;
            for (int i = 0; i < nsamples; ++i)
            {
                GroundSample ssmp = es.start.gsamples[i];
                foreach (GroundSegment end in es.end)
                {
                    GroundSample esmp = end.gsamples[i];
                    if (!ssmp.validHeight || !esmp.validHeight)
                    {
                        continue;
                    }

                    if (!SampleTrajectory(acfg, heightfield, ssmp.p, esmp.p, es.trajectory))
                    {
                        continue;
                    }

                    ssmp.validTrajectory = true;
                    esmp.validTrajectory = true;
                }
            }
        }

        private bool SampleTrajectory(JumpLinkBuilderConfig acfg, RcHeightfield solid, RcVec3f pa, RcVec3f pb, ITrajectory tra)
        {
            float cs = Math.Min(acfg.cellSize, acfg.cellHeight);
            float d = RcVec.Dist2D(pa, pb) + MathF.Abs(pa.Y - pb.Y);
            int nsamples = Math.Max(2, (int)MathF.Ceiling(d / cs));
            for (int i = 0; i < nsamples; ++i)
            {
                float u = (float)i / (float)(nsamples - 1);
                RcVec3f p = tra.Apply(pa, pb, u);
                if (CheckHeightfieldCollision(solid, p.X, p.Y + acfg.groundTolerance, p.Y + acfg.agentHeight, p.Z))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CheckHeightfieldCollision(RcHeightfield solid, float x, float ymin, float ymax, float z)
        {
            int w = solid.width;
            int h = solid.height;
            float cs = solid.cs;
            float ch = solid.ch;
            RcVec3f orig = solid.bmin;
            int ix = (int)MathF.Floor((x - orig.X) / cs);
            int iz = (int)MathF.Floor((z - orig.Z) / cs);

            if (ix < 0 || iz < 0 || ix > w || iz > h)
            {
                return false;
            }

            RcSpan s = solid.spans[ix + iz * w];
            if (s == null)
            {
                return false;
            }

            while (s != null)
            {
                float symin = orig.Y + s.smin * ch;
                float symax = orig.Y + s.smax * ch;
                if (OverlapRange(ymin, ymax, symin, symax))
                {
                    return true;
                }

                s = s.next;
            }

            return false;
        }

        private bool OverlapRange(float amin, float amax, float bmin, float bmax)
        {
            return (amin > bmax || amax < bmin) ? false : true;
        }
    }
}