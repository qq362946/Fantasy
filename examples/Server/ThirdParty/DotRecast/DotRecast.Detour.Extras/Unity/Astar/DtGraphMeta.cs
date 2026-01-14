/*
recast4j Copyright (c) 2015-2019 Piotr Piastucki piotr@jtilia.org
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

namespace DotRecast.Detour.Extras.Unity.Astar
{
    public class DtGraphMeta
    {
        public float characterRadius { get; set; }
        public float contourMaxError { get; set; }
        public float cellSize { get; set; }
        public float walkableHeight { get; set; }
        public float walkableClimb { get; set; }
        public float maxSlope { get; set; }
        public float maxEdgeLength { get; set; }
        public float minRegionSize { get; set; }

        /** Size of tile along X axis in voxels */
        public float tileSizeX { get; set; }

        /** Size of tile along Z axis in voxels */
        public float tileSizeZ { get; set; }

        public bool useTiles { get; set; }
        public DtUnityVector3f rotation { get; set; }
        public DtUnityVector3f forcedBoundsCenter { get; set; }
        public DtUnityVector3f forcedBoundsSize { get; set; }
    }
}