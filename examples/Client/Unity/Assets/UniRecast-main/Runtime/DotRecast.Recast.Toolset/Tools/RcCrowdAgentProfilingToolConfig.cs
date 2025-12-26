namespace DotRecast.Recast.Toolset.Tools
{
    public class RcCrowdAgentProfilingToolConfig
    {
        public int expandSimOptions = 1;
        public int expandCrowdOptions = 1;
        public int agents = 1000;
        public int randomSeed = 270;
        public int numberOfZones = 4;
        public float zoneRadius = 20f;
        public float percentMobs = 80f;
        public float percentTravellers = 15f;
        public int pathQueueSize = 32;
        public int maxIterations = 300;

        public bool showAgents = true;
    }
}