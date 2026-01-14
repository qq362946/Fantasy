namespace DotRecast.Detour
{
    /// Tile flags used for various functions and fields.
    /// For an example, see dtNavMesh::addTile().
    public static class DtTileFlags
    {
        /// The navigation mesh owns the tile memory and is responsible for freeing it.
        public const byte DT_TILE_FREE_DATA = 0x01;
    }
}