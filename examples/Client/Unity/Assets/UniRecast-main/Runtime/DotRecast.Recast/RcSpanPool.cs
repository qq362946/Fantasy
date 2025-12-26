/*
Copyright (c) 2009-2010 Mikko Mononen memon@inside.org
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

namespace DotRecast.Recast
{
    /// A memory pool used for quick allocation of spans within a heightfield.
    /// @see rcHeightfield
    public class RcSpanPool
    {
        public RcSpanPool next; //< The next span pool.
        public readonly RcSpan[] items; //< Array of spans in the pool.

        public RcSpanPool()
        {
            items = new RcSpan[RcRecast.RC_SPANS_PER_POOL];
            for (int i = 0; i < items.Length; ++i)
            {
                items[i] = new RcSpan();
            }
        }
    }
}