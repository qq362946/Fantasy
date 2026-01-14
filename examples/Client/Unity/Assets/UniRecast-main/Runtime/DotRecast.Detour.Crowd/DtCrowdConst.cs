namespace DotRecast.Detour.Crowd
{
    public static class DtCrowdConst
    {
        /// The maximum number of neighbors that a crowd agent can take into account
        /// for steering decisions.
        /// @ingroup crowd
        public const int DT_CROWDAGENT_MAX_NEIGHBOURS = 6;
        
        /// The maximum number of corners a crowd agent will look ahead in the path.
        /// This value is used for sizing the crowd agent corner buffers.
        /// Due to the behavior of the crowd manager, the actual number of useful
        /// corners will be one less than this number.
        /// @ingroup crowd
        public const int DT_CROWDAGENT_MAX_CORNERS = 4;

        /// The maximum number of crowd avoidance configurations supported by the
        /// crowd manager.
        /// @ingroup crowd
        /// @see dtObstacleAvoidanceParams, dtCrowd::SetObstacleAvoidanceParams(), dtCrowd::GetObstacleAvoidanceParams(),
        /// dtCrowdAgentParams::obstacleAvoidanceType
        public const int DT_CROWD_MAX_OBSTAVOIDANCE_PARAMS = 8;

        /// The maximum number of query filter types supported by the crowd manager.
        /// @ingroup crowd
        /// @see dtQueryFilter, dtCrowd::GetFilter() dtCrowd::GetEditableFilter(),
        /// dtCrowdAgentParams::queryFilterType
        public const int DT_CROWD_MAX_QUERY_FILTER_TYPE = 16;
        
        public const int MAX_ITERS_PER_UPDATE = 100;
        public const int MAX_PATHQUEUE_NODES = 4096;
        public const int MAX_COMMON_NODES = 512;
        public const int MAX_PATH_RESULT = 256;
    }
}