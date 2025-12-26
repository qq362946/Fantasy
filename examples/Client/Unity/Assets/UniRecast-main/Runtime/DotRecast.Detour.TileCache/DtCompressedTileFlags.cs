namespace DotRecast.Detour.TileCache
{
    /// Flags for addTile
    public class DtCompressedTileFlags
    {
        public const int DT_COMPRESSEDTILE_FREE_DATA = 0x01; //< Navmesh owns the tile memory and should free it. In C#, it is not used.
    }
}