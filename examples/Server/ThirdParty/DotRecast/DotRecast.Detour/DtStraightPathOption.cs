using DotRecast.Core.Collections;

namespace DotRecast.Detour
{
    public class DtStraightPathOption
    {
        public static readonly DtStraightPathOption None = new DtStraightPathOption(0, "None");
        public static readonly DtStraightPathOption AreaCrossings = new DtStraightPathOption(DtStraightPathOptions.DT_STRAIGHTPATH_AREA_CROSSINGS, "Area");
        public static readonly DtStraightPathOption AllCrossings = new DtStraightPathOption(DtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS, "All");

        public static readonly RcImmutableArray<DtStraightPathOption> Values = RcImmutableArray.Create(
            None, AreaCrossings, AllCrossings
        );

        public readonly int Value;
        public readonly string Label;

        private DtStraightPathOption(int value, string label)
        {
            Value = value;
            Label = label;
        }
    }
}