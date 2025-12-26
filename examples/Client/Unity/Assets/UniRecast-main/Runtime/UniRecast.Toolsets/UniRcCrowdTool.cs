namespace UniRecast.Toolsets
{
    using UnityEngine;

    public class UniRcCrowdTool : MonoBehaviour
    {
        [SerializeField]
        private int mode;

        [SerializeField]
        private bool optimizeVis;

        [SerializeField]
        private bool optimizeTopo;

        [SerializeField]
        private bool anticipateTurns;

        [SerializeField]
        private bool obstacleAvoidance;

        [SerializeField]
        private bool separation;

        [SerializeField]
        private int obstacleAvoidanceType;

        [SerializeField]
        private float separationWeight;

        // ...
        [SerializeField]
        private bool showCorners;

        [SerializeField]
        private bool showCollisionSegments;

        [SerializeField]
        private bool showPath;

        [SerializeField]
        private bool showVO;

        [SerializeField]
        private bool showOpt;

        [SerializeField]
        private bool showNeis;

        [SerializeField]
        private bool showGrid;

        [SerializeField]
        private bool showNodes;
    }
}