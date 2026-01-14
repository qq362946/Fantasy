namespace DotRecast.Recast.Toolset.Gizmos
{
    public class RcTrimeshGizmo : IRcGizmoMeshFilter
    {
        public readonly float[] vertices;
        public readonly int[] triangles;

        public RcTrimeshGizmo(float[] vertices, int[] triangles)
        {
            this.vertices = vertices;
            this.triangles = triangles;
        }
    }
}