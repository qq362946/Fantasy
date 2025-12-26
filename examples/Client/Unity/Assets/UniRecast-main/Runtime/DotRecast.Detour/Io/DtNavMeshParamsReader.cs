using DotRecast.Core;

namespace DotRecast.Detour.Io
{
    public class DtNavMeshParamsReader
    {
        public DtNavMeshParams Read(RcByteBuffer bb)
        {
            DtNavMeshParams option = new DtNavMeshParams();
            option.orig.X = bb.GetFloat();
            option.orig.Y = bb.GetFloat();
            option.orig.Z = bb.GetFloat();
            option.tileWidth = bb.GetFloat();
            option.tileHeight = bb.GetFloat();
            option.maxTiles = bb.GetInt();
            option.maxPolys = bb.GetInt();
            return option;
        }
    }
}