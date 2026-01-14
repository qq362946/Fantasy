namespace DotRecast.Recast
{
    /// Contour build flags.
    /// @see rcBuildContours
    public static class RcBuildContoursFlags
    {
        public const int RC_CONTOUR_TESS_WALL_EDGES = 0x01; //< Tessellate solid (impassable) edges during contour simplification.
        public const int RC_CONTOUR_TESS_AREA_EDGES = 0x02; //< Tessellate edges between areas during contour simplification.
    }
}