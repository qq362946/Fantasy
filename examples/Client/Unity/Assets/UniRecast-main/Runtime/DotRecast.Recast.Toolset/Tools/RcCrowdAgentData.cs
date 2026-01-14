using DotRecast.Core.Numerics;

namespace DotRecast.Recast.Toolset.Tools
{
    public class RcCrowdAgentData
    {
        public readonly RcCrowdAgentType type;
        public readonly RcVec3f home = new RcVec3f();

        public RcCrowdAgentData(RcCrowdAgentType type, RcVec3f home)
        {
            this.type = type;
            this.home = home;
        }
    }
}