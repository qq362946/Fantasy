namespace DotRecast.Detour
{
    public static class DtNodeFlags
    {
        public const int DT_NODE_OPEN = 0x01;
        public const int DT_NODE_CLOSED = 0x02;
        public const int DT_NODE_PARENT_DETACHED = 0x04; // parent of the node is not adjacent. Found using raycast.
    }
}