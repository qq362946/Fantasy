namespace DotRecast.Detour
{
    /// Vertex flags returned by dtNavMeshQuery::findStraightPath.
    public static class DtStraightPathFlags
    {
        public const byte DT_STRAIGHTPATH_START = 0x01; //< The vertex is the start position in the path.
        public const byte DT_STRAIGHTPATH_END = 0x02; //< The vertex is the end position in the path.
        public const byte DT_STRAIGHTPATH_OFFMESH_CONNECTION = 0x04; //< The vertex is the start of an off-mesh connection.
    }
}