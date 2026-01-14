using DotRecast.Recast;

namespace DotRecast.Detour.Extras.Jumplink
{
    public interface IDtGroundSampler
    {
        void Sample(DtJumpLinkBuilderConfig acfg, RcBuilderResult result, DtEdgeSampler es);
    }
}