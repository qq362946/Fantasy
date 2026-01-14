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

using DotRecast.Core.Numerics;

namespace DotRecast.Detour
{
    /// Configuration parameters used to define multi-tile navigation meshes.
    /// The values are used to allocate space during the initialization of a navigation mesh.
    /// @see dtNavMesh::init()
    /// @ingroup detour
    public struct DtNavMeshParams
    {
        public RcVec3f orig; //< The world space origin of the navigation mesh's tile space. [(x, y, z)]
        public float tileWidth; //< The width of each tile. (Along the x-axis.)
        public float tileHeight; //< The height of each tile. (Along the z-axis.)
        public int maxTiles; //< The maximum number of tiles the navigation mesh can contain. This and maxPolys are used to calculate how many bits are needed to identify tiles and polygons uniquely.
        public int maxPolys; //< The maximum number of polygons each tile can contain. This and maxTiles are used to calculate how many bits are needed to identify tiles and polygons uniquely.
    }
}