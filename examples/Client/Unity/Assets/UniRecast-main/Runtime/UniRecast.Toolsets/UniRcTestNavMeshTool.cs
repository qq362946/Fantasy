namespace UniRecast.Toolsets
{
    using DotRecast.Recast.Toolset.Builder;
    using DotRecast.Recast.Toolset.Tools;
    using UnityEngine;

    public class UniRcTestNavMeshTool : MonoBehaviour
    {
        private RcTestNavMeshTool _tool;

        [SerializeField]
        private int selectedMode = 0;

        [SerializeField]
        private int selectedStraightPathOption = 0;

        [SerializeField]
        private bool constrainByCircle = true;

        [SerializeField]
        private int includeFlags = SampleAreaModifications.SAMPLE_POLYFLAGS_ALL;
        
        [SerializeField]
        private int excludeFlags = 0;
    }
}