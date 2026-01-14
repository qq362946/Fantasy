namespace DotRecast.Detour.Crowd
{
    /// Crowd agent update flags.
    /// @ingroup crowd
    /// @see dtCrowdAgentParams::updateFlags
    public static class DtCrowdAgentUpdateFlags
    {
        public const int DT_CROWD_ANTICIPATE_TURNS = 1;
        public const int DT_CROWD_OBSTACLE_AVOIDANCE = 2;
        public const int DT_CROWD_SEPARATION = 4;
        public const int DT_CROWD_OPTIMIZE_VIS = 8; //< Use #dtPathCorridor::optimizePathVisibility() to optimize the agent path.
        public const int DT_CROWD_OPTIMIZE_TOPO = 16; //< Use dtPathCorridor::optimizePathTopology() to optimize the agent path.
    }
}