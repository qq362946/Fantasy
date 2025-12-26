namespace DotRecast.Recast.Toolset.Gizmos
{
    public class RcCompositeGizmo : IRcGizmoMeshFilter
    {
        public readonly IRcGizmoMeshFilter[] gizmoMeshes;

        public RcCompositeGizmo(params IRcGizmoMeshFilter[] gizmoMeshes)
        {
            this.gizmoMeshes = gizmoMeshes;
        }
    }
}