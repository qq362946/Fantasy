/*
recast4j copyright (c) 2021 Piotr Piastucki piotr@jtilia.org
DotRecast Copyright (c) 2023-2024 Choi Ikpil ikpil@naver.com

This software is provided 'as-is', without any express or implied
warranty.  In no event will the authors be held liable for any damages
arising from the use of this software.
Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:
1. The origin of this software must not be misrepresented; you must not
 claim that you wrote the original software. If you use this software
 in a product, an acknowledgment in the product documentation would be
 appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be
 misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
*/

using System.Collections.Generic;

namespace DotRecast.Detour.Extras.Unity.Astar
{
    public class DtGraphData
    {
        public readonly DtPathfindingRecastGraphMeta meta;
        public readonly int[] indexToNode;
        public readonly DtNodeLink2[] nodeLinks2;
        public readonly List<DtGraphMeta> graphMeta;
        public readonly List<DtGraphMeshData> graphMeshData;
        public readonly List<List<int[]>> graphConnections;

        public DtGraphData(DtPathfindingRecastGraphMeta meta, int[] indexToNode, DtNodeLink2[] nodeLinks2, List<DtGraphMeta> graphMeta,
            List<DtGraphMeshData> graphMeshData, List<List<int[]>> graphConnections)
        {
            this.meta = meta;
            this.indexToNode = indexToNode;
            this.nodeLinks2 = nodeLinks2;
            this.graphMeta = graphMeta;
            this.graphMeshData = graphMeshData;
            this.graphConnections = graphConnections;
        }
    }
}