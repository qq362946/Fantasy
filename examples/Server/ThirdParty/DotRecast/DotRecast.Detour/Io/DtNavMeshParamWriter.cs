using System.IO;
using DotRecast.Core;

namespace DotRecast.Detour.Io
{
    public class DtNavMeshParamWriter
    {
        public void Write(BinaryWriter stream, DtNavMeshParams option, RcByteOrder order)
        {
            RcIO.Write(stream, option.orig.X, order);
            RcIO.Write(stream, option.orig.Y, order);
            RcIO.Write(stream, option.orig.Z, order);
            RcIO.Write(stream, option.tileWidth, order);
            RcIO.Write(stream, option.tileHeight, order);
            RcIO.Write(stream, option.maxTiles, order);
            RcIO.Write(stream, option.maxPolys, order);
        }
    }
}