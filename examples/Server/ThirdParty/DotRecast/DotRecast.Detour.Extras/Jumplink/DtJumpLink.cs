namespace DotRecast.Detour.Extras.Jumplink
{
    public class DtJumpLink
    {
        public const int MAX_SPINE = 8;
        public readonly int nspine = MAX_SPINE;
        public readonly float[] spine0 = new float[MAX_SPINE * 3];
        public readonly float[] spine1 = new float[MAX_SPINE * 3];
        public DtGroundSample[] startSamples;
        public DtGroundSample[] endSamples;
        public DtGroundSegment start;
        public DtGroundSegment end;
        public IDtTrajectory trajectory;
    }
}