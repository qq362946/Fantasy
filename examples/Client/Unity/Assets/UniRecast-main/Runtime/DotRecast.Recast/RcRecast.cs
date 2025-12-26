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

using System;
using DotRecast.Core;
using DotRecast.Core.Numerics;

namespace DotRecast.Recast
{
    public static class RcRecast
    {
        /// Represents the null area.
        /// When a data element is given this value it is considered to no longer be 
        /// assigned to a usable area.  (E.g. It is un-walkable.)
        public const int RC_NULL_AREA = 0;

        /// The default area id used to indicate a walkable polygon. 
        /// This is also the maximum allowed area id, and the only non-null area id 
        /// recognized by some steps in the build process. 
        public const int RC_WALKABLE_AREA = 63;

        /// The value returned by #rcGetCon if the specified direction is not connected
        /// to another span. (Has no neighbor.)
        public const int RC_NOT_CONNECTED = 0x3f;

        /// Defines the number of bits allocated to rcSpan::smin and rcSpan::smax.
        public const int RC_SPAN_HEIGHT_BITS = 20;

        /// Defines the maximum value for rcSpan::smin and rcSpan::smax.
        public const int RC_SPAN_MAX_HEIGHT = (1 << RC_SPAN_HEIGHT_BITS) - 1;

        /// The number of spans allocated per span spool.
        /// @see rcSpanPool
        public const int RC_SPANS_PER_POOL = 2048;
        
        // Must be 255 or smaller (not 256) because layer IDs are stored as
        // a byte where 255 is a special value.
        public const int RC_MAX_LAYERS = RC_NOT_CONNECTED;
        public const int RC_MAX_NEIS = 16;

        /// Heighfield border flag.
        /// If a heightfield region ID has this bit set, then the region is a border
        /// region and its spans are considered unwalkable.
        /// (Used during the region and contour build process.)
        /// @see rcCompactSpan::reg
        public const int RC_BORDER_REG = 0x8000;

        /// Polygon touches multiple regions.
        /// If a polygon has this region ID it was merged with or created
        /// from polygons of different regions during the polymesh
        /// build step that removes redundant border vertices.
        /// (Used during the polymesh and detail polymesh build processes)
        /// @see rcPolyMesh::regs
        public const int RC_MULTIPLE_REGS = 0;

        // Border vertex flag.
        /// If a region ID has this bit set, then the associated element lies on
        /// a tile border. If a contour vertex's region ID has this bit set, the
        /// vertex will later be removed in order to match the segments and vertices
        /// at tile boundaries.
        /// (Used during the build process.)
        /// @see rcCompactSpan::reg, #rcContour::verts, #rcContour::rverts
        public const int RC_BORDER_VERTEX = 0x10000;

        /// Area border flag.
        /// If a region ID has this bit set, then the associated element lies on
        /// the border of an area.
        /// (Used during the region and contour build process.)
        /// @see rcCompactSpan::reg, #rcContour::verts, #rcContour::rverts
        public const int RC_AREA_BORDER = 0x20000;

        /// Applied to the region id field of contour vertices in order to extract the region id.
        /// The region id field of a vertex may have several flags applied to it. So the
        /// fields value can't be used directly.
        /// @see rcContour::verts, rcContour::rverts
        public const int RC_CONTOUR_REG_MASK = 0xffff;

        /// A value which indicates an invalid index within a mesh.
        /// @note This does not necessarily indicate an error.
        /// @see rcPolyMesh::polys
        public const int RC_MESH_NULL_IDX = 0xffff;

        public const int RC_LOG_WARNING = 1;

        private static readonly int[] DirOffsetX = { -1, 0, 1, 0, };
        private static readonly int[] DirOffsetY = { 0, 1, 0, -1 };
        private static readonly int[] DirForOffset = { 3, 0, -1, 2, 1 };

        /// Sets the neighbor connection data for the specified direction.
        /// @param[in]		span			The span to update.
        /// @param[in]		direction		The direction to set. [Limits: 0 <= value < 4]
        /// @param[in]		neighborIndex	The index of the neighbor span.
        public static void SetCon(RcCompactSpanBuilder span, int direction, int neighborIndex)
        {
            int shift = direction * 6;
            int con = span.con;
            span.con = (con & ~(0x3f << shift)) | ((neighborIndex & 0x3f) << shift);
        }

        /// Gets neighbor connection data for the specified direction.
        /// @param[in]		span		The span to check.
        /// @param[in]		direction	The direction to check. [Limits: 0 <= value < 4]
        /// @return The neighbor connection data for the specified direction, or #RC_NOT_CONNECTED if there is no connection.
        public static int GetCon(ref RcCompactSpan s, int dir)
        {
            int shift = dir * 6;
            return (s.con >> shift) & 0x3f;
        }

        /// Gets the standard width (x-axis) offset for the specified direction.
        /// @param[in]		direction		The direction. [Limits: 0 <= value < 4]
        /// @return The width offset to apply to the current cell position to move in the direction.
        public static int GetDirOffsetX(int dir)
        {
            return DirOffsetX[dir & 0x03];
        }

        // TODO (graham): Rename this to rcGetDirOffsetZ
        /// Gets the standard height (z-axis) offset for the specified direction.
        /// @param[in]		direction		The direction. [Limits: 0 <= value < 4]
        /// @return The height offset to apply to the current cell position to move in the direction.
        public static int GetDirOffsetY(int dir)
        {
            return DirOffsetY[dir & 0x03];
        }

        /// Gets the direction for the specified offset. One of x and y should be 0.
        /// @param[in]		offsetX		The x offset. [Limits: -1 <= value <= 1]
        /// @param[in]		offsetZ		The z offset. [Limits: -1 <= value <= 1]
        /// @return The direction that represents the offset.
        public static int GetDirForOffset(int x, int y)
        {
            return DirForOffset[((y + 1) << 1) + x];
        }

        public static void CalcBounds(float[] verts, int nv, float[] bmin, float[] bmax)
        {
            for (int i = 0; i < 3; i++)
            {
                bmin[i] = verts[i];
                bmax[i] = verts[i];
            }

            for (int i = 1; i < nv; ++i)
            {
                for (int j = 0; j < 3; j++)
                {
                    bmin[j] = Math.Min(bmin[j], verts[i * 3 + j]);
                    bmax[j] = Math.Max(bmax[j], verts[i * 3 + j]);
                }
            }
            // Calculate bounding box.
        }

        public static void CalcGridSize(RcVec3f bmin, RcVec3f bmax, float cs, out int sizeX, out int sizeZ)
        {
            sizeX = (int)((bmax.X - bmin.X) / cs + 0.5f);
            sizeZ = (int)((bmax.Z - bmin.Z) / cs + 0.5f);
        }


        public static void CalcTileCount(RcVec3f bmin, RcVec3f bmax, float cs, int tileSizeX, int tileSizeZ, out int tw, out int td)
        {
            CalcGridSize(bmin, bmax, cs, out var gw, out var gd);
            tw = (gw + tileSizeX - 1) / tileSizeX;
            td = (gd + tileSizeZ - 1) / tileSizeZ;
        }

        /// @par
        ///
        /// Modifies the area id of all triangles with a slope below the specified value.
        ///
        /// See the #rcConfig documentation for more information on the configuration parameters.
        ///
        /// @see rcHeightfield, rcClearUnwalkableTriangles, rcRasterizeTriangles
        public static int[] MarkWalkableTriangles(RcContext ctx, float walkableSlopeAngle, float[] verts, int[] tris, int nt, RcAreaModification areaMod)
        {
            int[] areas = new int[nt];
            float walkableThr = MathF.Cos(walkableSlopeAngle / 180.0f * MathF.PI);
            RcVec3f norm = new RcVec3f();
            for (int i = 0; i < nt; ++i)
            {
                int tri = i * 3;
                RcVec3f v0 = verts.ToVec3(tris[tri + 0] * 3);
                RcVec3f v1 = verts.ToVec3(tris[tri + 1] * 3);
                RcVec3f v2 = verts.ToVec3(tris[tri + 2] * 3);
                CalcTriNormal(v0, v1, v2, ref norm);
                // Check if the face is walkable.
                if (norm.Y > walkableThr)
                    areas[i] = areaMod.Apply(areas[i]);
            }

            return areas;
        }

        public static void CalcTriNormal(RcVec3f v0, RcVec3f v1, RcVec3f v2, ref RcVec3f norm)
        {
            var e0 = v1 - v0;
            var e1 = v2 - v0;
            norm = RcVec3f.Cross(e0, e1);
            norm = RcVec3f.Normalize(norm);
        }


        /// @par
        ///
        /// Only sets the area id's for the unwalkable triangles. Does not alter the
        /// area id's for walkable triangles.
        ///
        /// See the #rcConfig documentation for more information on the configuration parameters.
        ///
        /// @see rcHeightfield, rcClearUnwalkableTriangles, rcRasterizeTriangles
        public static void ClearUnwalkableTriangles(RcContext ctx, float walkableSlopeAngle, float[] verts, int nv, int[] tris, int nt, int[] areas)
        {
            float walkableThr = MathF.Cos(walkableSlopeAngle / 180.0f * MathF.PI);

            RcVec3f norm = new RcVec3f();

            for (int i = 0; i < nt; ++i)
            {
                int tri = i * 3;
                RcVec3f v0 = verts.ToVec3(tris[tri + 0] * 3);
                RcVec3f v1 = verts.ToVec3(tris[tri + 1] * 3);
                RcVec3f v2 = verts.ToVec3(tris[tri + 2] * 3);
                CalcTriNormal(v0, v1, v2, ref norm);
                // Check if the face is walkable.
                if (norm.Y <= walkableThr)
                    areas[i] = RC_NULL_AREA;
            }
        }
    }
}