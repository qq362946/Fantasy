namespace DotRecast.Detour
{
    /// Options for dtNavMeshQuery::raycast
    public static class DtRaycastOptions
    {
        public const int DT_RAYCAST_USE_COSTS = 0x01; //< Raycast should calculate movement cost along the ray and fill RaycastHit::cost
    }
}