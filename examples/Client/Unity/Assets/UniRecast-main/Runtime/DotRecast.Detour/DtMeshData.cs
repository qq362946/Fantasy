/*
Copyright (c) 2009-2010 Mikko Mononen memon@inside.org
recast4j copyright (c) 2015-2019 Piotr Piastucki piotr@jtilia.org
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

namespace DotRecast.Detour
{
    public class DtMeshData
    {
        public DtMeshHeader header; //< The tile header.
        public DtPoly[] polys; //< The tile polygons. [Size: dtMeshHeader::polyCount]
        public float[] verts; //< The tile vertices. [(x, y, z) * dtMeshHeader::vertCount] 
        public DtPolyDetail[] detailMeshes; //< The tile's detail sub-meshes. [Size: dtMeshHeader::detailMeshCount]

        /// The detail mesh's unique vertices. [(x, y, z) * dtMeshHeader::detailVertCount]
        public float[] detailVerts;

        /// The detail mesh's triangles. [(vertA, vertB, vertC, triFlags) * dtMeshHeader::detailTriCount].
        /// See dtDetailTriEdgeFlags and dtGetDetailTriEdgeFlags.
        public int[] detailTris;

        /// The tile bounding volume nodes. [Size: dtMeshHeader::bvNodeCount]
        /// (Will be null if bounding volumes are disabled.)
        public DtBVNode[] bvTree;

        public DtOffMeshConnection[] offMeshCons; //< The tile off-mesh connections. [Size: dtMeshHeader::offMeshConCount]
    }
}