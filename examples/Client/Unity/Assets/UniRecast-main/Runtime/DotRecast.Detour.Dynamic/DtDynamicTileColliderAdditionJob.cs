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
using DotRecast.Detour.Dynamic.Colliders;

namespace DotRecast.Detour.Dynamic
{
    public class DtDynamicTileColliderAdditionJob : IDtDaynmicTileJob
    {
        private readonly long _colliderId;
        private readonly IDtCollider _collider;
        private readonly ICollection<DtDynamicTile> _affectedTiles;

        public DtDynamicTileColliderAdditionJob(long colliderId, IDtCollider collider, ICollection<DtDynamicTile> affectedTiles)
        {
            _colliderId = colliderId;
            _collider = collider;
            _affectedTiles = affectedTiles;
        }

        public ICollection<DtDynamicTile> AffectedTiles()
        {
            return _affectedTiles;
        }

        public void Process(DtDynamicTile tile)
        {
            tile.AddCollider(_colliderId, _collider);
        }
    }
}