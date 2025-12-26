using DotRecast.Core;

namespace DotRecast.Recast
{
    public class RcBuilderResult
    {
        public readonly int TileX;
        public readonly int TileZ;

        public readonly RcHeightfield SolidHeightfiled;
        public readonly RcCompactHeightfield CompactHeightfield;
        public readonly RcContourSet ContourSet;
        public readonly RcPolyMesh Mesh;
        public readonly RcPolyMeshDetail MeshDetail;
        public readonly RcContext Context;

        public RcBuilderResult(int tileX, int tileZ, RcHeightfield solidHeightfiled, RcCompactHeightfield compactHeightfield, RcContourSet contourSet, RcPolyMesh mesh, RcPolyMeshDetail meshDetail, RcContext ctx)
        {
            TileX = tileX;
            TileZ = tileZ;
            SolidHeightfiled = solidHeightfiled;
            CompactHeightfield = compactHeightfield;
            ContourSet = contourSet;
            Mesh = mesh;
            MeshDetail = meshDetail;
            Context = ctx;
        }
    }
}