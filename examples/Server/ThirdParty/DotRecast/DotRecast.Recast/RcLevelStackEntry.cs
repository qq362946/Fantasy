namespace DotRecast.Recast
{
    public readonly struct RcLevelStackEntry
    {
        public readonly int x;
        public readonly int y;
        public readonly int index;

        public RcLevelStackEntry(int tempX, int tempY, int tempIndex)
        {
            x = tempX;
            y = tempY;
            index = tempIndex;
        }
    }
}