namespace DotRecast.Detour.TileCache
{
    public class DtLayerMonotoneRegion
    {
        public const int DT_LAYER_MAX_NEIS = 16;

        public int area;
        public byte[] neis = new byte[DT_LAYER_MAX_NEIS];
        public byte nneis;
        public byte regId;
        public byte areaId;
    };
}