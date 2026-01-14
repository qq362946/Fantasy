namespace DotRecast.Detour
{
    public readonly struct DtFindPathOption
    {
        public static readonly DtFindPathOption NoOption = new DtFindPathOption(DtDefaultQueryHeuristic.Default, 0, 0);

        public static readonly DtFindPathOption AnyAngle = new DtFindPathOption(DtDefaultQueryHeuristic.Default, DtFindPathOptions.DT_FINDPATH_ANY_ANGLE, float.MaxValue);
        public static readonly DtFindPathOption ZeroScale = new DtFindPathOption(new DtDefaultQueryHeuristic(0.0f), 0, 0);

        public readonly IDtQueryHeuristic heuristic;
        public readonly int options;
        public readonly float raycastLimit;

        public DtFindPathOption(IDtQueryHeuristic heuristic, int options, float raycastLimit)
        {
            this.heuristic = heuristic;
            this.options = options;
            this.raycastLimit = raycastLimit;
        }

        public DtFindPathOption(int options, float raycastLimit)
            : this(DtDefaultQueryHeuristic.Default, options, raycastLimit)
        {
        }
    }
}