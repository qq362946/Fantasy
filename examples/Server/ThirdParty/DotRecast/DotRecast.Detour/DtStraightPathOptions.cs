namespace DotRecast.Detour
{
    /// Options for dtNavMeshQuery::findStraightPath.
    public static class DtStraightPathOptions
    {
        public const int DT_STRAIGHTPATH_AREA_CROSSINGS = 0x01; //< Add a vertex at every polygon edge crossing where area changes.
        public const int DT_STRAIGHTPATH_ALL_CROSSINGS = 0x02; //< Add a vertex at every polygon edge crossing. 
    }
}