namespace DotRecast.Detour
{
    /// Flags representing the type of a navigation mesh polygon.
    public static class DtPolyTypes
    {
        public const int DT_POLYTYPE_GROUND = 0; // The polygon is a standard convex polygon that is part of the surface of the mesh.
        public const int DT_POLYTYPE_OFFMESH_CONNECTION = 1; // The polygon is an off-mesh connection consisting of two vertices.
    }
}