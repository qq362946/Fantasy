namespace DotRecast.Detour.Extras.Jumplink
{
    public class DtJumpLinkType
    {
        public const int EDGE_JUMP_BIT = 1 << 0;
        public const int EDGE_CLIMB_DOWN_BIT = 1 << 1;
        public const int EDGE_JUMP_OVER_BIT = 1 << 2;

        public static readonly DtJumpLinkType EDGE_JUMP = new DtJumpLinkType(EDGE_JUMP_BIT);
        public static readonly DtJumpLinkType EDGE_CLIMB_DOWN = new DtJumpLinkType(EDGE_CLIMB_DOWN_BIT);
        public static readonly DtJumpLinkType EDGE_JUMP_OVER = new DtJumpLinkType(EDGE_JUMP_OVER_BIT);

        public readonly int Bit;

        private DtJumpLinkType(int bit)
        {
            Bit = bit;
        }
    }
}