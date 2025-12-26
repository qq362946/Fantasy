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

using System.Collections.Generic;
using DotRecast.Core.Numerics;

namespace DotRecast.Detour
{
    public class DtNode
    {
        public readonly int ptr;

        public RcVec3f pos; // Position of the node.
        public float cost; // Cost from previous node to current node.
        public float total; // Cost up to the node.
        public int pidx; // Index to parent node.
        public int state; // extra state information. A polyRef can have multiple nodes with different extra info. see DT_MAX_STATES_PER_NODE
        public int flags; // Node flags. A combination of dtNodeFlags.
        public long id; // Polygon ref the node corresponds to.

        public DtNode(int ptr)
        {
            this.ptr = ptr;
        }
        
        public static int ComparisonNodeTotal(DtNode a, DtNode b)
        {
            int compare = a.total.CompareTo(b.total);
            if (0 != compare)
                return compare;

            return a.ptr.CompareTo(b.ptr);
        }

        public override string ToString()
        {
            return $"Node [ptr={ptr} id={id} cost={cost} total={total}]";
        }
    }
}