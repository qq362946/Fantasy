namespace DotRecast.Recast.Toolset.Tools
{
    public class RcCrowdAgentTrail
    {
        public const int AGENT_MAX_TRAIL = 64;
        public float[] trail = new float[AGENT_MAX_TRAIL * 3];
        public int htrail;
    }
}