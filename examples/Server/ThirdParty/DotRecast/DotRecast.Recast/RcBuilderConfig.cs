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

namespace DotRecast.Recast
{
    public class RcBuilderConfig
    {
        public readonly RcConfig cfg;

        public readonly int tileX;
        public readonly int tileZ;

        /** The width of the field along the x-axis. [Limit: >= 0] [Units: vx] **/
        public readonly int width;

        /** The height of the field along the z-axis. [Limit: >= 0] [Units: vx] **/
        public readonly int height;

        /** The minimum bounds of the field's AABB. [(x, y, z)] [Units: wu] **/
        public readonly RcVec3f bmin = new RcVec3f();

        /** The maximum bounds of the field's AABB. [(x, y, z)] [Units: wu] **/
        public readonly RcVec3f bmax = new RcVec3f();

        public RcBuilderConfig(RcConfig cfg, RcVec3f bmin, RcVec3f bmax) : this(cfg, bmin, bmax, 0, 0)
        {
        }

        public RcBuilderConfig(RcConfig cfg, RcVec3f bmin, RcVec3f bmax, int tileX, int tileZ)
        {
            this.tileX = tileX;
            this.tileZ = tileZ;
            this.cfg = cfg;
            this.bmin = bmin;
            this.bmax = bmax;
            if (cfg.UseTiles)
            {
                float tsx = cfg.TileSizeX * cfg.Cs;
                float tsz = cfg.TileSizeZ * cfg.Cs;
                this.bmin.X += tileX * tsx;
                this.bmin.Z += tileZ * tsz;
                this.bmax.X = this.bmin.X + tsx;
                this.bmax.Z = this.bmin.Z + tsz;
                
                // Expand the heighfield bounding box by border size to find the extents of geometry we need to build this tile.
                //
                // This is done in order to make sure that the navmesh tiles connect correctly at the borders,
                // and the obstacles close to the border work correctly with the dilation process.
                // No polygons (or contours) will be created on the border area.
                //
                // IMPORTANT!
                //
                //   :''''''''':
                //   : +-----+ :
                //   : |     | :
                //   : |     |<--- tile to build
                //   : |     | :  
                //   : +-----+ :<-- geometry needed
                //   :.........:
                //
                // You should use this bounding box to query your input geometry.
                //
                // For example if you build a navmesh for terrain, and want the navmesh tiles to match the terrain tile size
                // you will need to pass in data from neighbour terrain tiles too! In a simple case, just pass in all the 8 neighbours,
                // or use the bounding box below to only pass in a sliver of each of the 8 neighbours.
                
                this.bmin.X -= cfg.BorderSize * cfg.Cs;
                this.bmin.Z -= cfg.BorderSize * cfg.Cs;
                this.bmax.X += cfg.BorderSize * cfg.Cs;
                this.bmax.Z += cfg.BorderSize * cfg.Cs;
                width = cfg.TileSizeX + cfg.BorderSize * 2;
                height = cfg.TileSizeZ + cfg.BorderSize * 2;
            }
            else
            {
                RcRecast.CalcGridSize(this.bmin, this.bmax, cfg.Cs, out width, out height);
            }
        }
    }
}