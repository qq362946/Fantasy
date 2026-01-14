namespace DotRecast.Detour
{
    /// Options for dtNavMeshQuery::initSlicedFindPath and updateSlicedFindPath
    public static class DtFindPathOptions
    {
        public const int DT_FINDPATH_ANY_ANGLE = 0x02; //< use raycasts during pathfind to "shortcut" (raycast still consider costs)
    }
}