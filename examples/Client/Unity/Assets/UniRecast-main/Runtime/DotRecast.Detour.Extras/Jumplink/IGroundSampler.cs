using DotRecast.Recast;

namespace DotRecast.Detour.Extras.Jumplink
{
    public interface IGroundSampler
    {
        void Sample(JumpLinkBuilderConfig acfg, RcBuilderResult result, EdgeSampler es);
    }
}