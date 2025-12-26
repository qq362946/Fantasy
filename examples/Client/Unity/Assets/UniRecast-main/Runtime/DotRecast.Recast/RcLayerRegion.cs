using System.Collections.Generic;

namespace DotRecast.Recast
{
    public class RcLayerRegion
    {
        public readonly int index;
        public List<int> layers;
        public List<int> neis;
        public int ymin, ymax;
        public byte layerId; // Layer ID
        public bool @base; // Flag indicating if the region is the base of merged regions.

        public RcLayerRegion(int i)
        {
            index = i;
            layers = new List<int>();
            neis = new List<int>();
            ymin = 0xFFFF;
            layerId = 0xff;
        }
    };
}