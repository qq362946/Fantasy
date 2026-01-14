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
using System.IO;
using System.IO.Compression;

namespace DotRecast.Detour.Extras.Unity.Astar
{
    public class DtUnityAStarPathfindingReader
    {
        private const string META_FILE_NAME = "meta.json";
        private const string NODE_INDEX_FILE_NAME = "graph_references.binary";
        private const string NODE_LINK_2_FILE_NAME = "node_link2.binary";
        private const string GRAPH_META_FILE_NAME_PATTERN = "graph{0}.json";
        private const string GRAPH_DATA_FILE_NAME_PATTERN = "graph{0}_extra.binary";
        private const string GRAPH_CONNECTION_FILE_NAME_PATTERN = "graph{0}_references.binary";
        private const int MAX_VERTS_PER_POLY = 3;
        private readonly DtPathfindingRecastGraphMetaReader metaReader = new DtPathfindingRecastGraphMetaReader();
        private readonly DtNodeIndexReader nodeIndexReader = new DtNodeIndexReader();
        private readonly DtGraphMetaReader graphMetaReader = new DtGraphMetaReader();
        private readonly DtGraphMeshDataReader graphDataReader = new DtGraphMeshDataReader();
        private readonly DtGraphConnectionReader graphConnectionReader = new DtGraphConnectionReader();
        private readonly DtNodeLink2Reader nodeLink2Reader = new DtNodeLink2Reader();

        public DtGraphData Read(FileStream zipFile)
        {
            using ZipArchive file = new ZipArchive(zipFile);
            // Read meta file and check version and graph type
            DtPathfindingRecastGraphMeta meta = metaReader.Read(file, META_FILE_NAME);
            // Read index to node mapping
            int[] indexToNode = nodeIndexReader.Read(file, NODE_INDEX_FILE_NAME);
            // Read NodeLink2 data (off-mesh links)
            DtNodeLink2[] nodeLinks2 = nodeLink2Reader.Read(file, NODE_LINK_2_FILE_NAME, indexToNode);
            // Read graph by graph
            List<DtGraphMeta> metaList = new List<DtGraphMeta>();
            List<DtGraphMeshData> meshDataList = new List<DtGraphMeshData>();
            List<List<int[]>> connectionsList = new List<List<int[]>>();
            for (int graphIndex = 0; graphIndex < meta.graphs; graphIndex++)
            {
                DtGraphMeta graphMeta = graphMetaReader.Read(file, string.Format(GRAPH_META_FILE_NAME_PATTERN, graphIndex));
                // First graph mesh data - vertices and polygons
                DtGraphMeshData graphData = graphDataReader.Read(file,
                    string.Format(GRAPH_DATA_FILE_NAME_PATTERN, graphIndex), graphMeta, MAX_VERTS_PER_POLY);
                // Then graph connection data - links between nodes located in both the same tile and other tiles
                List<int[]> connections = graphConnectionReader.Read(file,
                    string.Format(GRAPH_CONNECTION_FILE_NAME_PATTERN, graphIndex), meta, indexToNode);
                metaList.Add(graphMeta);
                meshDataList.Add(graphData);
                connectionsList.Add(connections);
            }

            return new DtGraphData(meta, indexToNode, nodeLinks2, metaList, meshDataList, connectionsList);
        }
    }
}