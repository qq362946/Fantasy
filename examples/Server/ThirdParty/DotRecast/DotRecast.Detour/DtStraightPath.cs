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
    //TODO: (PP) Add comments
    public readonly struct DtStraightPath
    {
        /// The local path corridor corners for the agent. (Staight path.) [(x, y, z) * #ncorners]
        public readonly RcVec3f pos;

        /// The local path corridor corner flags. (See: #dtStraightPathFlags) [(flags) * #ncorners]
        public readonly byte flags;

        /// The reference id of the polygon being entered at the corner. [(polyRef) * #ncorners]
        public readonly long refs;

        public DtStraightPath(RcVec3f pos, byte flags, long refs)
        {
            this.pos = pos;
            this.flags = flags;
            this.refs = refs;
        }
    }
}