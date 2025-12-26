namespace DotRecast.Detour.TileCache
{
    // TODO: @ikpil, better pooling system
    public class DtTileCacheAlloc
    {
        public virtual T[] Alloc<T>(long size)
        {
            return new T[size];
        }

        public virtual void Free<T>(T ptr)
        {
            // ..
        }

        public virtual void Reset()
        {
            // ..
        }
    }
}