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

using System;
using DotRecast.Core.Numerics;
using DotRecast.Recast;

namespace DotRecast.Detour.Dynamic
{
    /**
 * Voxel raycast based on the algorithm described in
 *
 * "A Fast Voxel Traversal Algorithm for Ray Tracing" by John Amanatides and Andrew Woo
 */
    public class DtVoxelQuery
    {
        private readonly RcVec3f origin;
        private readonly float tileWidth;
        private readonly float tileDepth;
        private readonly Func<int, int, RcHeightfield> heightfieldProvider;

        public DtVoxelQuery(RcVec3f origin, float tileWidth, float tileDepth, Func<int, int, RcHeightfield> heightfieldProvider)
        {
            this.origin = origin;
            this.tileWidth = tileWidth;
            this.tileDepth = tileDepth;
            this.heightfieldProvider = heightfieldProvider;
        }

        /**
     * Perform raycast using voxels heightfields.
     *
     * @return Optional with hit parameter (t) or empty if no hit found
     */
        public bool Raycast(RcVec3f start, RcVec3f end, out float hit)
        {
            return TraverseTiles(start, end, out hit);
        }

        private bool TraverseTiles(RcVec3f start, RcVec3f end, out float hit)
        {
            float relStartX = start.X - origin.X;
            float relStartZ = start.Z - origin.Z;
            int sx = (int)MathF.Floor(relStartX / tileWidth);
            int sz = (int)MathF.Floor(relStartZ / tileDepth);
            int ex = (int)MathF.Floor((end.X - origin.X) / tileWidth);
            int ez = (int)MathF.Floor((end.Z - origin.Z) / tileDepth);
            int dx = ex - sx;
            int dz = ez - sz;
            int stepX = dx < 0 ? -1 : 1;
            int stepZ = dz < 0 ? -1 : 1;
            float xRem = (tileWidth + (relStartX % tileWidth)) % tileWidth;
            float zRem = (tileDepth + (relStartZ % tileDepth)) % tileDepth;
            float tx = end.X - start.X;
            float tz = end.Z - start.Z;
            float xOffest = MathF.Abs(tx < 0 ? xRem : tileWidth - xRem);
            float zOffest = MathF.Abs(tz < 0 ? zRem : tileDepth - zRem);
            tx = MathF.Abs(tx);
            tz = MathF.Abs(tz);
            float tMaxX = xOffest / tx;
            float tMaxZ = zOffest / tz;
            float tDeltaX = tileWidth / tx;
            float tDeltaZ = tileDepth / tz;
            float t = 0;
            while (true)
            {
                bool isHit = TraversHeightfield(sx, sz, start, end, t, Math.Min(1, Math.Min(tMaxX, tMaxZ)), out hit);
                if (isHit)
                {
                    return true;
                }

                if ((dx > 0 ? sx >= ex : sx <= ex) && (dz > 0 ? sz >= ez : sz <= ez))
                {
                    break;
                }

                if (tMaxX < tMaxZ)
                {
                    t = tMaxX;
                    tMaxX += tDeltaX;
                    sx += stepX;
                }
                else
                {
                    t = tMaxZ;
                    tMaxZ += tDeltaZ;
                    sz += stepZ;
                }
            }

            return false;
        }

        private bool TraversHeightfield(int x, int z, RcVec3f start, RcVec3f end, float tMin, float tMax, out float hit)
        {
            RcHeightfield hf = heightfieldProvider.Invoke(x, z);
            if (null != hf)
            {
                float tx = end.X - start.X;
                float ty = end.Y - start.Y;
                float tz = end.Z - start.Z;
                float[] entry = { start.X + tMin * tx, start.Y + tMin * ty, start.Z + tMin * tz };
                float[] exit = { start.X + tMax * tx, start.Y + tMax * ty, start.Z + tMax * tz };
                float relStartX = entry[0] - hf.bmin.X;
                float relStartZ = entry[2] - hf.bmin.Z;
                int sx = (int)MathF.Floor(relStartX / hf.cs);
                int sz = (int)MathF.Floor(relStartZ / hf.cs);
                int ex = (int)MathF.Floor((exit[0] - hf.bmin.X) / hf.cs);
                int ez = (int)MathF.Floor((exit[2] - hf.bmin.Z) / hf.cs);
                int dx = ex - sx;
                int dz = ez - sz;
                int stepX = dx < 0 ? -1 : 1;
                int stepZ = dz < 0 ? -1 : 1;
                float xRem = (hf.cs + (relStartX % hf.cs)) % hf.cs;
                float zRem = (hf.cs + (relStartZ % hf.cs)) % hf.cs;
                float xOffest = MathF.Abs(tx < 0 ? xRem : hf.cs - xRem);
                float zOffest = MathF.Abs(tz < 0 ? zRem : hf.cs - zRem);
                tx = MathF.Abs(tx);
                tz = MathF.Abs(tz);
                float tMaxX = xOffest / tx;
                float tMaxZ = zOffest / tz;
                float tDeltaX = hf.cs / tx;
                float tDeltaZ = hf.cs / tz;
                float t = 0;
                while (true)
                {
                    if (sx >= 0 && sx < hf.width && sz >= 0 && sz < hf.height)
                    {
                        float y1 = start.Y + ty * (tMin + t) - hf.bmin.Y;
                        float y2 = start.Y + ty * (tMin + Math.Min(tMaxX, tMaxZ)) - hf.bmin.Y;
                        float ymin = Math.Min(y1, y2) / hf.ch;
                        float ymax = Math.Max(y1, y2) / hf.ch;
                        RcSpan span = hf.spans[sx + sz * hf.width];
                        while (span != null)
                        {
                            if (span.smin <= ymin && span.smax >= ymax)
                            {
                                hit = Math.Min(1, tMin + t);
                                return true;
                            }

                            span = span.next;
                        }
                    }

                    if ((dx > 0 ? sx >= ex : sx <= ex) && (dz > 0 ? sz >= ez : sz <= ez))
                    {
                        break;
                    }

                    if (tMaxX < tMaxZ)
                    {
                        t = tMaxX;
                        tMaxX += tDeltaX;
                        sx += stepX;
                    }
                    else
                    {
                        t = tMaxZ;
                        tMaxZ += tDeltaZ;
                        sz += stepZ;
                    }
                }
            }

            hit = 0.0f;
            return false;
        }
    }
}