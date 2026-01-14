using DotRecast.Core.Numerics;

namespace DotRecast.Detour
{
    public static class DtDetour
    {
        /// The maximum number of vertices per navigation polygon.
        /// @ingroup detour
        public const int DT_VERTS_PER_POLYGON = 6;
        
        /** A magic number used to detect compatibility of navigation tile data. */
        public const int DT_NAVMESH_MAGIC = 'D' << 24 | 'N' << 16 | 'A' << 8 | 'V';

        /** A version number used to detect compatibility of navigation tile data. */
        public const int DT_NAVMESH_VERSION = 7;

        public const int DT_NAVMESH_VERSION_RECAST4J_FIRST = 0x8807;
        public const int DT_NAVMESH_VERSION_RECAST4J_NO_POLY_FIRSTLINK = 0x8808;
        public const int DT_NAVMESH_VERSION_RECAST4J_32BIT_BVTREE = 0x8809;
        public const int DT_NAVMESH_VERSION_RECAST4J_LAST = 0x8809;

        /** A magic number used to detect the compatibility of navigation tile states. */
        public const int DT_NAVMESH_STATE_MAGIC = 'D' << 24 | 'N' << 16 | 'M' << 8 | 'S';

        /** A version number used to detect compatibility of navigation tile states. */
        public const int DT_NAVMESH_STATE_VERSION = 1;

        public const int DT_SALT_BITS = 16;
        public const int DT_TILE_BITS = 28;
        public const int DT_POLY_BITS = 20;

        /// A flag that indicates that an entity links to an external entity.
        /// (E.g. A polygon edge is a portal that links to another polygon.)
        public const int DT_EXT_LINK = 0x8000;

        /// A value that indicates the entity does not link to anything.
        public const int DT_NULL_LINK = unchecked((int)0xffffffff);
        
        public const int DT_NODE_PARENT_BITS = 24;
        public const int DT_NODE_STATE_BITS = 2;
        public const int DT_MAX_STATES_PER_NODE = 1 << DT_NODE_STATE_BITS; // number of extra states per node. See dtNode::state
        
        /// A flag that indicates that an off-mesh connection can be traversed in
        /// both directions. (Is bidirectional.)
        public const int DT_OFFMESH_CON_BIDIR = 1;

        /// The maximum number of user defined area ids.
        public const int DT_MAX_AREAS = 64;

        /// Limit raycasting during any angle pahfinding
        /// The limit is given as a multiple of the character radius
        public const float DT_RAY_CAST_LIMIT_PROPORTIONS = 50.0f;

        /// @{
        /// @name Encoding and Decoding
        /// These functions are generally meant for internal use only.
        /// Derives a standard polygon reference.
        ///  @note This function is generally meant for internal use only.
        ///  @param[in]	salt	The tile's salt value.
        ///  @param[in]	it		The index of the tile.
        ///  @param[in]	ip		The index of the polygon within the tile.
        public static long EncodePolyId(int salt, int it, int ip)
        {
            return (((long)salt) << (DT_POLY_BITS + DT_TILE_BITS)) | ((long)it << DT_POLY_BITS) | (long)ip;
        }

        /// Decodes a standard polygon reference.
        /// @note This function is generally meant for internal use only.
        /// @param[in] ref The polygon reference to decode.
        /// @param[out] salt The tile's salt value.
        /// @param[out] it The index of the tile.
        /// @param[out] ip The index of the polygon within the tile.
        /// @see #encodePolyId
        public static void DecodePolyId(long refs, out int salt, out int it, out int ip)
        {
            long saltMask = (1L << DT_SALT_BITS) - 1;
            long tileMask = (1L << DT_TILE_BITS) - 1;
            long polyMask = (1L << DT_POLY_BITS) - 1;
            salt = (int)((refs >> (DT_POLY_BITS + DT_TILE_BITS)) & saltMask);
            it = (int)((refs >> DT_POLY_BITS) & tileMask);
            ip = (int)(refs & polyMask);
        }

        /// Extracts a tile's salt value from the specified polygon reference.
        /// @note This function is generally meant for internal use only.
        /// @param[in] ref The polygon reference.
        /// @see #encodePolyId
        public static int DecodePolyIdSalt(long refs)
        {
            long saltMask = (1L << DT_SALT_BITS) - 1;
            return (int)((refs >> (DT_POLY_BITS + DT_TILE_BITS)) & saltMask);
        }

        /// Extracts the tile's index from the specified polygon reference.
        /// @note This function is generally meant for internal use only.
        /// @param[in] ref The polygon reference.
        /// @see #encodePolyId
        public static int DecodePolyIdTile(long refs)
        {
            long tileMask = (1L << DT_TILE_BITS) - 1;
            return (int)((refs >> DT_POLY_BITS) & tileMask);
        }

        /// Extracts the polygon's index (within its tile) from the specified
        /// polygon reference.
        /// @note This function is generally meant for internal use only.
        /// @param[in] ref The polygon reference.
        /// @see #encodePolyId
        public static int DecodePolyIdPoly(long refs)
        {
            long polyMask = (1L << DT_POLY_BITS) - 1;
            return (int)(refs & polyMask);
        }

        public static int ComputeTileHash(int x, int y, int mask)
        {
            uint h1 = 0x8da6b343; // Large multiplicative constants;
            uint h2 = 0xd8163841; // here arbitrarily chosen primes
            uint n = h1 * (uint)x + h2 * (uint)y;
            return (int)(n & mask);
        }

        public static float GetSlabCoord(float[] verts, int va, int side)
        {
            if (side == 0 || side == 4)
            {
                return verts[va];
            }
            else if (side == 2 || side == 6)
            {
                return verts[va + 2];
            }

            return 0;
        }

        public static void CalcSlabEndPoints(float[] verts, int va, int vb, ref RcVec2f bmin, ref RcVec2f bmax, int side)
        {
            if (side == 0 || side == 4)
            {
                if (verts[va + 2] < verts[vb + 2])
                {
                    bmin.X = verts[va + 2];
                    bmin.Y = verts[va + 1];
                    bmax.X = verts[vb + 2];
                    bmax.Y = verts[vb + 1];
                }
                else
                {
                    bmin.X = verts[vb + 2];
                    bmin.Y = verts[vb + 1];
                    bmax.X = verts[va + 2];
                    bmax.Y = verts[va + 1];
                }
            }
            else if (side == 2 || side == 6)
            {
                if (verts[va + 0] < verts[vb + 0])
                {
                    bmin.X = verts[va + 0];
                    bmin.Y = verts[va + 1];
                    bmax.X = verts[vb + 0];
                    bmax.Y = verts[vb + 1];
                }
                else
                {
                    bmin.X = verts[vb + 0];
                    bmin.Y = verts[vb + 1];
                    bmax.X = verts[va + 0];
                    bmax.Y = verts[va + 1];
                }
            }
        }

        /// Get flags for edge in detail triangle.
        /// @param[in]	triFlags		The flags for the triangle (last component of detail vertices above).
        /// @param[in]	edgeIndex		The index of the first vertex of the edge. For instance, if 0,
        ///								returns flags for edge AB.
        public static int GetDetailTriEdgeFlags(int triFlags, int edgeIndex)
        {
            return (triFlags >> (edgeIndex * 2)) & 0x3;
        }
    }
}