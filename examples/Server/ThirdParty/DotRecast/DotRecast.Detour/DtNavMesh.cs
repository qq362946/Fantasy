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
using System.Collections.Generic;
using DotRecast.Core;
using DotRecast.Core.Collections;
using DotRecast.Core.Numerics;

namespace DotRecast.Detour
{
    using static DtDetour;

    /// A navigation mesh based on tiles of convex polygons.
    /// @ingroup detour
    public class DtNavMesh
    {
        private DtNavMeshParams m_params; //< Current initialization params. TODO: do not store this info twice.
        private RcVec3f m_orig; // < Origin of the tile (0,0)
        private float m_tileWidth; // < Dimensions of each tile.
        private float m_tileHeight; // < Dimensions of each tile.
        private int m_maxTiles; // < Max number of tiles.
        private int m_tileLutSize; //< Tile hash lookup size (must be pot).
        private int m_tileLutMask; // < Tile hash lookup mask.

        private DtMeshTile[] m_posLookup; //< Tile hash lookup.
        private DtMeshTile m_nextFree; //< Freelist of tiles.
        private DtMeshTile[] m_tiles; //< List of tiles.

        /** The maximum number of vertices per navigation polygon. */
        private int m_maxVertPerPoly;

        public DtStatus Init(DtNavMeshParams param, int maxVertsPerPoly)
        {
            m_params = param;
            m_orig = param.orig;
            m_tileWidth = param.tileWidth;
            m_tileHeight = param.tileHeight;

            // Init tiles
            m_maxVertPerPoly = maxVertsPerPoly;
            m_maxTiles = param.maxTiles;
            m_tileLutSize = DtUtils.NextPow2(param.maxTiles);
            if (0 == m_tileLutSize)
                m_tileLutSize = 1;
            m_tileLutMask = m_tileLutSize - 1;

            m_tiles = new DtMeshTile[m_maxTiles];
            m_posLookup = new DtMeshTile[m_tileLutSize];
            m_nextFree = null;
            for (int i = m_maxTiles - 1; i >= 0; --i)
            {
                m_tiles[i] = new DtMeshTile(i);
                m_tiles[i].salt = 1;
                m_tiles[i].next = m_nextFree;
                m_nextFree = m_tiles[i];
            }

            return DtStatus.DT_SUCCESS;
        }

        public DtStatus Init(DtMeshData data, int maxVertsPerPoly, int flags)
        {
            var option = GetNavMeshParams(data);
            DtStatus status = Init(option, maxVertsPerPoly);
            if (status.Failed())
                return status;

            return AddTile(data, flags, 0, out _);
        }

        private static DtNavMeshParams GetNavMeshParams(DtMeshData data)
        {
            DtNavMeshParams option = new DtNavMeshParams();
            option.orig = data.header.bmin;
            option.tileWidth = data.header.bmax.X - data.header.bmin.X;
            option.tileHeight = data.header.bmax.Z - data.header.bmin.Z;
            option.maxTiles = 1;
            option.maxPolys = data.header.polyCount;
            return option;
        }


        /// The maximum number of tiles supported by the navigation mesh.
        /// @return The maximum number of tiles supported by the navigation mesh.
        public int GetMaxTiles()
        {
            return m_maxTiles;
        }

        /// Returns pointer to tile in the tile array.
        public DtMeshTile GetTile(int i)
        {
            return m_tiles[i];
        }

        /// Gets the polygon reference for the tile's base polygon.
        ///  @param[in]	tile		The tile.
        /// @return The polygon reference for the base polygon in the specified tile.
        public long GetPolyRefBase(DtMeshTile tile)
        {
            if (tile == null)
            {
                return 0;
            }

            int it = tile.index;
            return EncodePolyId(tile.salt, it, 0);
        }

        private int AllocLink(DtMeshTile tile)
        {
            if (tile.linksFreeList == DT_NULL_LINK)
                return DT_NULL_LINK;

            int linkIdx = tile.linksFreeList;
            tile.linksFreeList = tile.links[linkIdx].next;
            return linkIdx;
        }

        private void FreeLink(DtMeshTile tile, int link)
        {
            tile.links[link].next = tile.linksFreeList;
            tile.linksFreeList = link;
        }

        /// Calculates the tile grid location for the specified world position.
        ///  @param[in]	pos  The world position for the query. [(x, y, z)]
        ///  @param[out]	tx		The tile's x-location. (x, y)
        ///  @param[out]	ty		The tile's y-location. (x, y)
        public void CalcTileLoc(RcVec3f pos, out int tx, out int ty)
        {
            tx = (int)MathF.Floor((pos.X - m_orig.X) / m_tileWidth);
            ty = (int)MathF.Floor((pos.Z - m_orig.Z) / m_tileHeight);
        }

        /// Gets the tile and polygon for the specified polygon reference.
        ///  @param[in]		ref		The reference for the a polygon.
        ///  @param[out]	tile	The tile containing the polygon.
        ///  @param[out]	poly	The polygon.
        /// @return The status flags for the operation.
        public DtStatus GetTileAndPolyByRef(long refs, out DtMeshTile tile, out DtPoly poly)
        {
            tile = null;
            poly = null;

            if (refs == 0)
            {
                return DtStatus.DT_FAILURE;
            }

            DecodePolyId(refs, out var salt, out var it, out var ip);
            if (it >= m_maxTiles)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            if (m_tiles[it].salt != salt || m_tiles[it].data == null || m_tiles[it].data.header == null)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            if (ip >= m_tiles[it].data.header.polyCount)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            tile = m_tiles[it];
            poly = m_tiles[it].data.polys[ip];

            return DtStatus.DT_SUCCESS;
        }

        /// @par
        ///
        /// @warning Only use this function if it is known that the provided polygon
        /// reference is valid. This function is faster than #getTileAndPolyByRef,
        /// but
        /// it does not validate the reference.
        /// Returns the tile and polygon for the specified polygon reference.
        ///  @param[in]		ref		A known valid reference for a polygon.
        ///  @param[out]	tile	The tile containing the polygon.
        ///  @param[out]	poly	The polygon.
        public void GetTileAndPolyByRefUnsafe(long refs, out DtMeshTile tile, out DtPoly poly)
        {
            DecodePolyId(refs, out var salt, out var it, out var ip);
            tile = m_tiles[it];
            poly = m_tiles[it].data.polys[ip];
        }

        /// Checks the validity of a polygon reference.
        ///  @param[in]	ref		The polygon reference to check.
        /// @return True if polygon reference is valid for the navigation mesh.
        public bool IsValidPolyRef(long refs)
        {
            if (refs == 0)
            {
                return false;
            }

            DecodePolyId(refs, out var salt, out var it, out var ip);
            if (it >= m_maxTiles)
            {
                return false;
            }

            if (m_tiles[it].salt != salt || m_tiles[it].data == null)
            {
                return false;
            }

            if (ip >= m_tiles[it].data.header.polyCount)
            {
                return false;
            }

            return true;
        }

        public ref readonly DtNavMeshParams GetParams()
        {
            return ref m_params;
        }


        // TODO: These methods are duplicates from dtNavMeshQuery, but are needed for off-mesh connection finding.
        /// Queries polygons within a tile.
        private int QueryPolygonsInTile(DtMeshTile tile, RcVec3f qmin, RcVec3f qmax, Span<long> polys, int maxPolys)
        {
            if (tile.data.bvTree != null)
            {
                int nodeIndex = 0;
                int end = tile.data.header.bvNodeCount;
                var tbmin = tile.data.header.bmin;
                var tbmax = tile.data.header.bmax;
                float qfac = tile.data.header.bvQuantFactor;
                // Calculate quantized box
                RcVec3i bmin;
                RcVec3i bmax;
                // dtClamp query box to world box.
                float minx = Math.Clamp(qmin.X, tbmin.X, tbmax.X) - tbmin.X;
                float miny = Math.Clamp(qmin.Y, tbmin.Y, tbmax.Y) - tbmin.Y;
                float minz = Math.Clamp(qmin.Z, tbmin.Z, tbmax.Z) - tbmin.Z;
                float maxx = Math.Clamp(qmax.X, tbmin.X, tbmax.X) - tbmin.X;
                float maxy = Math.Clamp(qmax.Y, tbmin.Y, tbmax.Y) - tbmin.Y;
                float maxz = Math.Clamp(qmax.Z, tbmin.Z, tbmax.Z) - tbmin.Z;
                // Quantize
                bmin.X = (int)(qfac * minx) & 0x7ffffffe;
                bmin.Y = (int)(qfac * miny) & 0x7ffffffe;
                bmin.Z = (int)(qfac * minz) & 0x7ffffffe;
                bmax.X = (int)(qfac * maxx + 1) | 1;
                bmax.Y = (int)(qfac * maxy + 1) | 1;
                bmax.Z = (int)(qfac * maxz + 1) | 1;

                // Traverse tree
                long @base = GetPolyRefBase(tile);
                int n = 0;
                while (nodeIndex < end)
                {
                    DtBVNode node = tile.data.bvTree[nodeIndex];
                    bool overlap = DtUtils.OverlapQuantBounds(ref bmin, ref bmax, ref node.bmin, ref node.bmax);
                    bool isLeafNode = node.i >= 0;

                    if (isLeafNode && overlap)
                    {
                        if (n < maxPolys)
                        {
                            polys[n++] = (@base | (long)node.i);
                        }
                    }

                    if (overlap || isLeafNode)
                    {
                        nodeIndex++;
                    }
                    else
                    {
                        int escapeIndex = -node.i;
                        nodeIndex += escapeIndex;
                    }
                }

                return n;
            }
            else
            {
                RcVec3f bmin = new RcVec3f();
                RcVec3f bmax = new RcVec3f();
                int n = 0;
                long @base = GetPolyRefBase(tile);
                for (int i = 0; i < tile.data.header.polyCount; ++i)
                {
                    DtPoly p = tile.data.polys[i];
                    // Do not return off-mesh connection polygons.
                    if (p.GetPolyType() == DtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                    {
                        continue;
                    }

                    // Calc polygon bounds.
                    int v = p.verts[0] * 3;
                    bmin = tile.data.verts.ToVec3(v);
                    bmax = tile.data.verts.ToVec3(v);
                    for (int j = 1; j < p.vertCount; ++j)
                    {
                        v = p.verts[j] * 3;
                        bmin = RcVec3f.Min(bmin, tile.data.verts.ToVec3(v));
                        bmax = RcVec3f.Max(bmax, tile.data.verts.ToVec3(v));
                    }

                    if (DtUtils.OverlapBounds(qmin, qmax, bmin, bmax))
                    {
                        if (n < maxPolys)
                        {
                            polys[n++] = (@base | (long)i);
                        }
                    }
                }

                return n;
            }
        }

        public DtStatus UpdateTile(DtMeshData data, int flags)
        {
            long refs = GetTileRefAt(data.header.x, data.header.y, data.header.layer);
            refs = RemoveTile(refs);
            return AddTile(data, flags, refs, out _);
        }

        /// @par
        ///
        /// The add operation will fail if the data is in the wrong format, the allocated tile
        /// space is full, or there is a tile already at the specified reference.
        ///
        /// The lastRef parameter is used to restore a tile with the same tile
        /// reference it had previously used.  In this case the #dtPolyRef's for the
        /// tile will be restored to the same values they were before the tile was 
        /// removed.
        ///
        /// The nav mesh assumes exclusive access to the data passed and will make
        /// changes to the dynamic portion of the data. For that reason the data
        /// should not be reused in other nav meshes until the tile has been successfully
        /// removed from this nav mesh.
        ///
        /// @see dtCreateNavMeshData, #removeTile
        /// Adds a tile to the navigation mesh.
        ///  @param[in]		data		Data for the new tile mesh. (See: #dtCreateNavMeshData)
        ///  @param[in]		dataSize	Data size of the new tile mesh.
        ///  @param[in]		flags		Tile flags. (See: #dtTileFlags)
        ///  @param[in]		lastRef		The desired reference for the tile. (When reloading a tile.) [opt] [Default: 0]
        ///  @param[out]	result		The tile reference. (If the tile was succesfully added.) [opt]
        /// @return The status flags for the operation. 
        public DtStatus AddTile(DtMeshData data, int flags, long lastRef, out long result)
        {
            result = 0;

            // Make sure the data is in right format.
            DtMeshHeader header = data.header;

            // Make sure the location is free.
            if (GetTileAt(header.x, header.y, header.layer) != null)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_ALREADY_OCCUPIED;
            }

            // Allocate a tile.
            DtMeshTile tile = null;
            if (lastRef == 0)
            {
                if (null != m_nextFree)
                {
                    tile = m_nextFree;
                    m_nextFree = tile.next;
                    tile.next = null;
                }
            }
            else
            {
                // Try to relocate the tile to specific index with same salt.
                int tileIndex = DecodePolyIdTile(lastRef);
                if (tileIndex >= m_maxTiles)
                {
                    return DtStatus.DT_FAILURE | DtStatus.DT_OUT_OF_MEMORY;
                }

                // Try to find the specific tile id from the free list.
                DtMeshTile target = m_tiles[tileIndex];
                DtMeshTile prev = null;
                tile = m_nextFree;

                while (null != tile && tile != target)
                {
                    prev = tile;
                    tile = tile.next;
                }

                // Could not find the correct location.
                if (tile != target)
                    return DtStatus.DT_FAILURE | DtStatus.DT_OUT_OF_MEMORY;

                // Remove from freelist
                if (null == prev)
                    m_nextFree = tile.next;
                else
                    prev.next = tile.next;

                // Restore salt.
                tile.salt = DecodePolyIdSalt(lastRef);
            }

            // Make sure we could allocate a tile.
            if (null == tile)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_OUT_OF_MEMORY;
            }

            // Insert tile into the position lut.
            int h = ComputeTileHash(header.x, header.y, m_tileLutMask);
            tile.next = m_posLookup[h];
            m_posLookup[h] = tile;


            // Patch header pointers.
            tile.data = data;
            tile.links = new DtLink[data.header.maxLinkCount];
            for (int i = 0; i < tile.links.Length; ++i)
            {
                tile.links[i] = new DtLink();
            }

            // If there are no items in the bvtree, reset the tree pointer.
            if (tile.data.bvTree != null && tile.data.bvTree.Length == 0)
            {
                tile.data.bvTree = null;
            }

            // Build links freelist
            tile.linksFreeList = 0;
            tile.links[data.header.maxLinkCount - 1].next = DT_NULL_LINK;
            for (int i = 0; i < data.header.maxLinkCount - 1; ++i)
                tile.links[i].next = i + 1;

            // Init tile.
            tile.flags = flags;

            ConnectIntLinks(tile);

            // Base off-mesh connections to their starting polygons and connect connections inside the tile.
            BaseOffMeshLinks(tile);
            ConnectExtOffMeshLinks(tile, tile, -1);

            // Create connections with neighbour tiles.
            const int MAX_NEIS = 32;
            DtMeshTile[] neis = new DtMeshTile[MAX_NEIS];
            int nneis;

            // Connect with layers in current tile.
            nneis = GetTilesAt(header.x, header.y, neis, MAX_NEIS);
            for (int j = 0; j < nneis; ++j)
            {
                if (neis[j] == tile)
                {
                    continue;
                }

                ConnectExtLinks(tile, neis[j], -1);
                ConnectExtLinks(neis[j], tile, -1);
                ConnectExtOffMeshLinks(tile, neis[j], -1);
                ConnectExtOffMeshLinks(neis[j], tile, -1);
            }

            // Connect with neighbour tiles.
            for (int i = 0; i < 8; ++i)
            {
                nneis = GetNeighbourTilesAt(header.x, header.y, i, neis, MAX_NEIS);
                for (int j = 0; j < nneis; ++j)
                {
                    ConnectExtLinks(tile, neis[j], i);
                    ConnectExtLinks(neis[j], tile, DtUtils.OppositeTile(i));
                    ConnectExtOffMeshLinks(tile, neis[j], i);
                    ConnectExtOffMeshLinks(neis[j], tile, DtUtils.OppositeTile(i));
                }
            }

            result = GetTileRef(tile);
            return DtStatus.DT_SUCCESS;
        }

        /// Removes the specified tile from the navigation mesh.
        /// @param[in] ref The reference of the tile to remove.
        /// @param[out] data Data associated with deleted tile.
        /// @param[out] dataSize Size of the data associated with deleted tile.
        ///
        /// This function returns the data for the tile so that, if desired,
        /// it can be added back to the navigation mesh at a later point.
        ///
        /// @see #addTile
        public long RemoveTile(long refs)
        {
            if (refs == 0)
            {
                return 0;
            }

            int tileIndex = DecodePolyIdTile(refs);
            int tileSalt = DecodePolyIdSalt(refs);
            if (tileIndex >= m_maxTiles)
            {
                throw new Exception("Invalid tile index");
            }

            DtMeshTile tile = m_tiles[tileIndex];
            if (tile.salt != tileSalt)
            {
                throw new Exception("Invalid tile salt");
            }

            // Remove tile from hash lookup.
            int h = ComputeTileHash(tile.data.header.x, tile.data.header.y, m_tileLutMask);
            DtMeshTile prev = null;
            DtMeshTile cur = m_posLookup[h];
            while (null != cur)
            {
                if (cur == tile)
                {
                    if (null != prev)
                        prev.next = cur.next;
                    else
                        m_posLookup[h] = cur.next;
                    break;
                }

                prev = cur;
                cur = cur.next;
            }

            // Remove connections to neighbour tiles.
            const int MAX_NEIS = 32;
            DtMeshTile[] neis = new DtMeshTile[MAX_NEIS];
            int nneis = 0;

            // Disconnect from other layers in current tile.
            nneis = GetTilesAt(tile.data.header.x, tile.data.header.y, neis, MAX_NEIS);
            for (int j = 0; j < nneis; ++j)
            {
                if (neis[j] == tile) continue;
                UnconnectLinks(neis[j], tile);
            }

            // Disconnect from neighbour tiles.
            for (int i = 0; i < 8; ++i)
            {
                nneis = GetNeighbourTilesAt(tile.data.header.x, tile.data.header.y, i, neis, MAX_NEIS);
                for (int j = 0; j < nneis; ++j)
                {
                    UnconnectLinks(neis[j], tile);
                }
            }

            // Reset tile.
            tile.data = null;
            tile.flags = 0;
            tile.links = null;
            tile.linksFreeList = DT_NULL_LINK;

            // Update salt, salt should never be zero.
            tile.salt = (tile.salt + 1) & ((1 << DT_SALT_BITS) - 1);
            if (tile.salt == 0)
            {
                tile.salt++;
            }

            // Add to free list.
            tile.next = m_nextFree;
            m_nextFree = tile;
            return GetTileRef(tile);
        }

        /// Builds internal polygons links for a tile.
        void ConnectIntLinks(DtMeshTile tile)
        {
            if (tile == null)
            {
                return;
            }

            long @base = GetPolyRefBase(tile);

            for (int i = 0; i < tile.data.header.polyCount; ++i)
            {
                DtPoly poly = tile.data.polys[i];
                poly.firstLink = DT_NULL_LINK;

                if (poly.GetPolyType() == DtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                {
                    continue;
                }

                // Build edge links backwards so that the links will be
                // in the linked list from lowest index to highest.
                for (int j = poly.vertCount - 1; j >= 0; --j)
                {
                    // Skip hard and non-internal edges.
                    if (poly.neis[j] == 0 || (poly.neis[j] & DT_EXT_LINK) != 0)
                    {
                        continue;
                    }

                    int idx = AllocLink(tile);
                    DtLink link = tile.links[idx];
                    link.refs = @base | (long)(poly.neis[j] - 1);
                    link.edge = (byte)j;
                    link.side = 0xff;
                    link.bmin = link.bmax = 0;
                    // Add to linked list.
                    link.next = poly.firstLink;
                    poly.firstLink = idx;
                }
            }
        }

        /// Removes external links at specified side.
        void UnconnectLinks(DtMeshTile tile, DtMeshTile target)
        {
            if (tile == null || target == null)
            {
                return;
            }

            int targetNum = DecodePolyIdTile(GetTileRef(target));

            for (int i = 0; i < tile.data.header.polyCount; ++i)
            {
                DtPoly poly = tile.data.polys[i];
                int j = poly.firstLink;
                int pj = DT_NULL_LINK;
                while (j != DT_NULL_LINK)
                {
                    if (DecodePolyIdTile(tile.links[j].refs) == targetNum)
                    {
                        // Remove link.
                        int nj = tile.links[j].next;
                        if (pj == DT_NULL_LINK)
                        {
                            poly.firstLink = nj;
                        }
                        else
                        {
                            tile.links[pj].next = nj;
                        }

                        FreeLink(tile, j);
                        j = nj;
                    }
                    else
                    {
                        // Advance
                        pj = j;
                        j = tile.links[j].next;
                    }
                }
            }
        }

        /// Builds external polygon links for a tile.
        void ConnectExtLinks(DtMeshTile tile, DtMeshTile target, int side)
        {
            if (tile == null)
            {
                return;
            }

            var connectPolys = new List<DtConnectPoly>();

            // Connect border links.
            for (int i = 0; i < tile.data.header.polyCount; ++i)
            {
                DtPoly poly = tile.data.polys[i];

                // Create new links.
                // short m = DT_EXT_LINK | (short)side;

                int nv = poly.vertCount;
                for (int j = 0; j < nv; ++j)
                {
                    // Skip non-portal edges.
                    if ((poly.neis[j] & DT_EXT_LINK) == 0)
                    {
                        continue;
                    }

                    int dir = poly.neis[j] & 0xff;
                    if (side != -1 && dir != side)
                    {
                        continue;
                    }

                    // Create new links
                    int va = poly.verts[j] * 3;
                    int vb = poly.verts[(j + 1) % nv] * 3;
                    int nnei = FindConnectingPolys(tile.data.verts, va, vb, target, DtUtils.OppositeTile(dir), ref connectPolys);
                    foreach (var connectPoly in connectPolys)
                    {
                        int idx = AllocLink(tile);
                        if (idx != DT_NULL_LINK)
                        {
                            DtLink link = tile.links[idx];
                            link.refs = connectPoly.refs;
                            link.edge = (byte)j;
                            link.side = (byte)dir;

                            link.next = poly.firstLink;
                            poly.firstLink = idx;

                            // Compress portal limits to a byte value.
                            if (dir == 0 || dir == 4)
                            {
                                float tmin = (connectPoly.tmin - tile.data.verts[va + 2])
                                             / (tile.data.verts[vb + 2] - tile.data.verts[va + 2]);
                                float tmax = (connectPoly.tmax - tile.data.verts[va + 2])
                                             / (tile.data.verts[vb + 2] - tile.data.verts[va + 2]);
                                if (tmin > tmax)
                                {
                                    float temp = tmin;
                                    tmin = tmax;
                                    tmax = temp;
                                }

                                link.bmin = (byte)MathF.Round(Math.Clamp(tmin, 0.0f, 1.0f) * 255.0f);
                                link.bmax = (byte)MathF.Round(Math.Clamp(tmax, 0.0f, 1.0f) * 255.0f);
                            }
                            else if (dir == 2 || dir == 6)
                            {
                                float tmin = (connectPoly.tmin - tile.data.verts[va])
                                             / (tile.data.verts[vb] - tile.data.verts[va]);
                                float tmax = (connectPoly.tmax - tile.data.verts[va])
                                             / (tile.data.verts[vb] - tile.data.verts[va]);
                                if (tmin > tmax)
                                {
                                    float temp = tmin;
                                    tmin = tmax;
                                    tmax = temp;
                                }

                                link.bmin = (byte)MathF.Round(Math.Clamp(tmin, 0.0f, 1.0f) * 255.0f);
                                link.bmax = (byte)MathF.Round(Math.Clamp(tmax, 0.0f, 1.0f) * 255.0f);
                            }
                        }
                    }
                }
            }
        }

        /// Builds external polygon links for a tile.
        void ConnectExtOffMeshLinks(DtMeshTile tile, DtMeshTile target, int side)
        {
            if (tile == null)
            {
                return;
            }

            // Connect off-mesh links.
            // We are interested on links which land from target tile to this tile.
            int oppositeSide = (side == -1) ? 0xff : DtUtils.OppositeTile(side);

            for (int i = 0; i < target.data.header.offMeshConCount; ++i)
            {
                DtOffMeshConnection targetCon = target.data.offMeshCons[i];
                if (targetCon.side != oppositeSide)
                {
                    continue;
                }

                DtPoly targetPoly = target.data.polys[targetCon.poly];
                // Skip off-mesh connections which start location could not be
                // connected at all.
                if (targetPoly.firstLink == DT_NULL_LINK)
                {
                    continue;
                }

                var ext = new RcVec3f()
                {
                    X = targetCon.rad,
                    Y = target.data.header.walkableClimb,
                    Z = targetCon.rad
                };

                // Find polygon to connect to.
                RcVec3f p = targetCon.pos[1];
                var refs = FindNearestPolyInTile(tile, p, ext, out var nearestPt);
                if (refs == 0)
                {
                    continue;
                }

                // findNearestPoly may return too optimistic results, further check
                // to make sure.

                if (RcMath.Sqr(nearestPt.X - p.X) + RcMath.Sqr(nearestPt.Z - p.Z) > RcMath.Sqr(targetCon.rad))
                {
                    continue;
                }

                // Make sure the location is on current mesh.
                target.data.verts[targetPoly.verts[1] * 3] = nearestPt.X;
                target.data.verts[targetPoly.verts[1] * 3 + 1] = nearestPt.Y;
                target.data.verts[targetPoly.verts[1] * 3 + 2] = nearestPt.Z;

                // Link off-mesh connection to target poly.
                int idx = AllocLink(target);
                DtLink link = target.links[idx];
                link.refs = refs;
                link.edge = 1;
                link.side = (byte)oppositeSide;
                link.bmin = link.bmax = 0;
                // Add to linked list.
                link.next = targetPoly.firstLink;
                targetPoly.firstLink = idx;

                // Link target poly to off-mesh connection.
                if ((targetCon.flags & DT_OFFMESH_CON_BIDIR) != 0)
                {
                    int tidx = AllocLink(tile);
                    int landPolyIdx = DecodePolyIdPoly(refs);
                    DtPoly landPoly = tile.data.polys[landPolyIdx];
                    link = tile.links[tidx];
                    link.refs = GetPolyRefBase(target) | (long)targetCon.poly;
                    link.edge = 0xff;
                    link.side = (byte)(side == -1 ? 0xff : side);
                    link.bmin = link.bmax = 0;
                    // Add to linked list.
                    link.next = landPoly.firstLink;
                    landPoly.firstLink = tidx;
                }
            }
        }

        /// Returns all polygons in neighbour tile based on portal defined by the segment.
        private int FindConnectingPolys(float[] verts, int va, int vb, DtMeshTile tile, int side, ref List<DtConnectPoly> cons)
        {
            if (tile == null)
                return 0;

            cons.Clear();

            RcVec2f amin = RcVec2f.Zero;
            RcVec2f amax = RcVec2f.Zero;
            CalcSlabEndPoints(verts, va, vb, ref amin, ref amax, side);
            float apos = GetSlabCoord(verts, va, side);

            // Remove links pointing to 'side' and compact the links array.
            RcVec2f bmin = RcVec2f.Zero;
            RcVec2f bmax = RcVec2f.Zero;
            int m = DT_EXT_LINK | side;
            int n = 0;
            long @base = GetPolyRefBase(tile);

            for (int i = 0; i < tile.data.header.polyCount; ++i)
            {
                DtPoly poly = tile.data.polys[i];
                int nv = poly.vertCount;
                for (int j = 0; j < nv; ++j)
                {
                    // Skip edges which do not point to the right side.
                    if (poly.neis[j] != m)
                    {
                        continue;
                    }

                    int vc = poly.verts[j] * 3;
                    int vd = poly.verts[(j + 1) % nv] * 3;
                    float bpos = GetSlabCoord(tile.data.verts, vc, side);
                    // Segments are not close enough.
                    if (MathF.Abs(apos - bpos) > 0.01f)
                    {
                        continue;
                    }

                    // Check if the segments touch.
                    CalcSlabEndPoints(tile.data.verts, vc, vd, ref bmin, ref bmax, side);

                    if (!OverlapSlabs(amin, amax, bmin, bmax, 0.01f, tile.data.header.walkableClimb))
                    {
                        continue;
                    }

                    // Add return value.
                    long refs = @base | (long)i;
                    float tmin = Math.Max(amin.X, bmin.X);
                    float tmax = Math.Min(amax.X, bmax.X);
                    cons.Add(new DtConnectPoly(refs, tmin, tmax));
                    n++;
                    break;
                }
            }

            return n;
        }

        private bool OverlapSlabs(RcVec2f amin, RcVec2f amax, RcVec2f bmin, RcVec2f bmax, float px, float py)
        {
            // Check for horizontal overlap.
            // The segment is shrunken a little so that slabs which touch
            // at end points are not connected.
            float minx = Math.Max(amin.X + px, bmin.X + px);
            float maxx = Math.Min(amax.X - px, bmax.X - px);
            if (minx > maxx)
            {
                return false;
            }

            // Check vertical overlap.
            float ad = (amax.Y - amin.Y) / (amax.X - amin.X);
            float ak = amin.Y - ad * amin.X;
            float bd = (bmax.Y - bmin.Y) / (bmax.X - bmin.X);
            float bk = bmin.Y - bd * bmin.X;
            float aminy = ad * minx + ak;
            float amaxy = ad * maxx + ak;
            float bminy = bd * minx + bk;
            float bmaxy = bd * maxx + bk;
            float dmin = bminy - aminy;
            float dmax = bmaxy - amaxy;

            // Crossing segments always overlap.
            if (dmin * dmax < 0)
            {
                return true;
            }

            // Check for overlap at endpoints.
            float thr = (py * 2) * (py * 2);
            if (dmin * dmin <= thr || dmax * dmax <= thr)
            {
                return true;
            }

            return false;
        }

        /// Builds internal polygons links for a tile.
        void BaseOffMeshLinks(DtMeshTile tile)
        {
            if (tile == null)
            {
                return;
            }

            long @base = GetPolyRefBase(tile);

            // Base off-mesh connection start points.
            for (int i = 0; i < tile.data.header.offMeshConCount; ++i)
            {
                DtOffMeshConnection con = tile.data.offMeshCons[i];
                DtPoly poly = tile.data.polys[con.poly];

                var ext = new RcVec3f()
                {
                    X = con.rad,
                    Y = tile.data.header.walkableClimb,
                    Z = con.rad,
                };

                // Find polygon to connect to.
                var refs = FindNearestPolyInTile(tile, con.pos[0], ext, out var nearestPt);
                if (refs == 0)
                {
                    continue;
                }

                RcVec3f[] p = con.pos; // First vertex
                // findNearestPoly may return too optimistic results, further check
                // to make sure.
                if (RcMath.Sqr(nearestPt.X - p[0].X) + RcMath.Sqr(nearestPt.Z - p[0].Z) > RcMath.Sqr(con.rad))
                {
                    continue;
                }

                // Make sure the location is on current mesh.
                tile.data.verts[poly.verts[0] * 3] = nearestPt.X;
                tile.data.verts[poly.verts[0] * 3 + 1] = nearestPt.Y;
                tile.data.verts[poly.verts[0] * 3 + 2] = nearestPt.Z;

                // Link off-mesh connection to target poly.
                int idx = AllocLink(tile);
                DtLink link = tile.links[idx];
                link.refs = refs;
                link.edge = 0;
                link.side = 0xff;
                link.bmin = link.bmax = 0;
                // Add to linked list.
                link.next = poly.firstLink;
                poly.firstLink = idx;

                // Start end-point is always connect back to off-mesh connection.
                int tidx = AllocLink(tile);
                int landPolyIdx = DecodePolyIdPoly(refs);
                DtPoly landPoly = tile.data.polys[landPolyIdx];
                link = tile.links[tidx];
                link.refs = @base | (long)con.poly;
                link.edge = 0xff;
                link.side = 0xff;
                link.bmin = link.bmax = 0;
                // Add to linked list.
                link.next = landPoly.firstLink;
                landPoly.firstLink = tidx;
            }
        }

        /**
     * Returns closest point on polygon.
     *
     * @param ref
     * @param pos
     * @return
     */
        RcVec3f ClosestPointOnDetailEdges(DtMeshTile tile, DtPoly poly, RcVec3f pos, bool onlyBoundary)
        {
            const int ANY_BOUNDARY_EDGE = (DtDetailTriEdgeFlags.DT_DETAIL_EDGE_BOUNDARY << 0) |
                                          (DtDetailTriEdgeFlags.DT_DETAIL_EDGE_BOUNDARY << 2) |
                                          (DtDetailTriEdgeFlags.DT_DETAIL_EDGE_BOUNDARY << 4);
            int ip = poly.index;
            float dmin = float.MaxValue;
            float tmin = 0;
            RcVec3f pmin = new RcVec3f();
            RcVec3f pmax = new RcVec3f();

            Span<RcVec3f> tempV = stackalloc RcVec3f[3];
            if (tile.data.detailMeshes != null)
            {
                ref DtPolyDetail pd = ref tile.data.detailMeshes[ip];
                for (int i = 0; i < pd.triCount; i++)
                {
                    int ti = (pd.triBase + i) * 4;
                    int[] tris = tile.data.detailTris;
                    if (onlyBoundary && (tris[ti + 3] & ANY_BOUNDARY_EDGE) == 0)
                    {
                        continue;
                    }

                    Span<RcVec3f> v = tempV;
                    for (int j = 0; j < 3; ++j)
                    {
                        if (tris[ti + j] < poly.vertCount)
                        {
                            int index = poly.verts[tris[ti + j]] * 3;
                            v[j] = new RcVec3f
                            {
                                X = tile.data.verts[index],
                                Y = tile.data.verts[index + 1],
                                Z = tile.data.verts[index + 2]
                            };
                        }
                        else
                        {
                            int index = (pd.vertBase + (tris[ti + j] - poly.vertCount)) * 3;
                            v[j] = new RcVec3f
                            {
                                X = tile.data.detailVerts[index],
                                Y = tile.data.detailVerts[index + 1],
                                Z = tile.data.detailVerts[index + 2]
                            };
                        }
                    }

                    for (int k = 0, j = 2; k < 3; j = k++)
                    {
                        if ((GetDetailTriEdgeFlags(tris[ti + 3], j) & DtDetailTriEdgeFlags.DT_DETAIL_EDGE_BOUNDARY) == 0
                            && (onlyBoundary || tris[ti + j] < tris[ti + k]))
                        {
                            // Only looking at boundary edges and this is internal, or
                            // this is an inner edge that we will see again or have already seen.
                            continue;
                        }

                        var d = DtUtils.DistancePtSegSqr2D(pos, v[j], v[k], out var t);
                        if (d < dmin)
                        {
                            dmin = d;
                            tmin = t;
                            pmin = v[j];
                            pmax = v[k];
                        }
                    }
                }
            }
            else
            {
                Span<RcVec3f> v = tempV.Slice(0, 2);
                for (int j = 0; j < poly.vertCount; ++j)
                {
                    int k = (j + 1) % poly.vertCount;
                    v[0].X = tile.data.verts[poly.verts[j] * 3];
                    v[0].Y = tile.data.verts[poly.verts[j] * 3 + 1];
                    v[0].Z = tile.data.verts[poly.verts[j] * 3 + 2];
                    v[1].X = tile.data.verts[poly.verts[k] * 3];
                    v[1].Y = tile.data.verts[poly.verts[k] * 3 + 1];
                    v[1].Z = tile.data.verts[poly.verts[k] * 3 + 2];

                    var d = DtUtils.DistancePtSegSqr2D(pos, v[0], v[1], out var t);
                    if (d < dmin)
                    {
                        dmin = d;
                        tmin = t;
                        pmin = v[0];
                        pmax = v[1];
                    }
                }
            }

            return RcVec3f.Lerp(pmin, pmax, tmin);
        }

        public bool GetPolyHeight(DtMeshTile tile, DtPoly poly, RcVec3f pos, out float height)
        {
            height = 0;

            // Off-mesh connections do not have detail polys and getting height
            // over them does not make sense.
            if (poly.GetPolyType() == DtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
            {
                return false;
            }

            int ip = poly.index;

            Span<float> verts = stackalloc float[m_maxVertPerPoly * 3];
            int nv = poly.vertCount;
            for (int i = 0; i < nv; ++i)
            {
                RcSpans.Copy(tile.data.verts, poly.verts[i] * 3, verts, i * 3, 3);
            }

            if (!DtUtils.PointInPolygon(pos, verts, nv))
            {
                return false;
            }

            // Find height at the location.
            Span<RcVec3f> tempV = stackalloc RcVec3f[3];
            if (tile.data.detailMeshes != null)
            {
                ref DtPolyDetail pd = ref tile.data.detailMeshes[ip];
                for (int j = 0; j < pd.triCount; ++j)
                {
                    int t = (pd.triBase + j) * 4;
                    Span<RcVec3f> v = tempV;
                    for (int k = 0; k < 3; ++k)
                    {
                        if (tile.data.detailTris[t + k] < poly.vertCount)
                        {
                            int index = poly.verts[tile.data.detailTris[t + k]] * 3;
                            v[k] = new RcVec3f
                            {
                                X = tile.data.verts[index],
                                Y = tile.data.verts[index + 1],
                                Z = tile.data.verts[index + 2]
                            };
                        }
                        else
                        {
                            int index = (pd.vertBase + (tile.data.detailTris[t + k] - poly.vertCount)) * 3;
                            v[k] = new RcVec3f
                            {
                                X = tile.data.detailVerts[index],
                                Y = tile.data.detailVerts[index + 1],
                                Z = tile.data.detailVerts[index + 2]
                            };
                        }
                    }

                    if (DtUtils.ClosestHeightPointTriangle(pos, v[0], v[1], v[2], out var h))
                    {
                        height = h;
                        return true;
                    }
                }
            }
            else
            {
                Span<RcVec3f> v = tempV;
                v[0].X = tile.data.verts[poly.verts[0] * 3];
                v[0].Y = tile.data.verts[poly.verts[0] * 3 + 1];
                v[0].Z = tile.data.verts[poly.verts[0] * 3 + 2];
                for (int j = 1; j < poly.vertCount - 1; ++j)
                {
                    for (int k = 0; k < 2; ++k)
                    {
                        v[k + 1].X = tile.data.verts[poly.verts[j + k] * 3];
                        v[k + 1].Y = tile.data.verts[poly.verts[j + k] * 3 + 1];
                        v[k + 1].Z = tile.data.verts[poly.verts[j + k] * 3 + 2];
                    }

                    if (DtUtils.ClosestHeightPointTriangle(pos, v[0], v[1], v[2], out var h))
                    {
                        height = h;
                        return true;
                    }
                }
            }

            // If all triangle checks failed above (can happen with degenerate triangles
            // or larger floating point values) the point is on an edge, so just select
            // closest. This should almost never happen so the extra iteration here is
            // ok.
            var closest = ClosestPointOnDetailEdges(tile, poly, pos, false);
            height = closest.Y;
            return true;
        }

        public void ClosestPointOnPoly(long refs, RcVec3f pos, out RcVec3f closest, out bool posOverPoly)
        {
            GetTileAndPolyByRefUnsafe(refs, out var tile, out var poly);
            closest = pos;

            if (GetPolyHeight(tile, poly, pos, out var h))
            {
                closest.Y = h;
                posOverPoly = true;
                return;
            }

            posOverPoly = false;

            // Off-mesh connections don't have detail polygons.
            if (poly.GetPolyType() == DtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
            {
                int i = poly.verts[0] * 3;
                var v0 = new RcVec3f(tile.data.verts[i], tile.data.verts[i + 1], tile.data.verts[i + 2]);
                i = poly.verts[1] * 3;
                var v1 = new RcVec3f(tile.data.verts[i], tile.data.verts[i + 1], tile.data.verts[i + 2]);
                DtUtils.DistancePtSegSqr2D(pos, v0, v1, out var t);
                closest = RcVec3f.Lerp(v0, v1, t);
                return;
            }

            // Outside poly that is not an offmesh connection.
            closest = ClosestPointOnDetailEdges(tile, poly, pos, true);
        }

        /// Find nearest polygon within a tile.
        private long FindNearestPolyInTile(DtMeshTile tile, RcVec3f center, RcVec3f halfExtents, out RcVec3f nearestPt)
        {
            nearestPt = RcVec3f.Zero;

            RcVec3f bmin = RcVec3f.Subtract(center, halfExtents);
            RcVec3f bmax = RcVec3f.Add(center, halfExtents);

            // Get nearby polygons from proximity grid.
            RcFixedArray256<long> fixedArrayPolys = new RcFixedArray256<long>();
            var polys = fixedArrayPolys.AsSpan();

            int polyCount = QueryPolygonsInTile(tile, bmin, bmax, polys, polys.Length);

            // Find nearest polygon amongst the nearby polygons.
            long nearest = 0;
            float nearestDistanceSqr = float.MaxValue;
            for (int i = 0; i < polyCount; ++i)
            {
                long refs = polys[i];
                float d;
                ClosestPointOnPoly(refs, center, out var closestPtPoly, out var posOverPoly);

                // If a point is directly over a polygon and closer than
                // climb height, favor that instead of straight line nearest point.
                RcVec3f diff = RcVec3f.Subtract(center, closestPtPoly);
                if (posOverPoly)
                {
                    d = MathF.Abs(diff.Y) - tile.data.header.walkableClimb;
                    d = d > 0 ? d * d : 0;
                }
                else
                {
                    d = diff.LengthSquared();
                }

                if (d < nearestDistanceSqr)
                {
                    nearestPt = closestPtPoly;
                    nearestDistanceSqr = d;
                    nearest = refs;
                }
            }

            return nearest;
        }

        private DtMeshTile GetTileAt(int x, int y, int layer)
        {
            // Find tile based on hash.
            int h = ComputeTileHash(x, y, m_tileLutMask);
            DtMeshTile tile = m_posLookup[h];
            while (null != tile)
            {
                if (null != tile.data &&
                    null != tile.data.header &&
                    tile.data.header.x == x &&
                    tile.data.header.y == y &&
                    tile.data.header.layer == layer)
                {
                    return tile;
                }

                tile = tile.next;
            }

            return null;
        }

        /// Returns neighbour tile based on side.
        private int GetNeighbourTilesAt(int x, int y, int side, DtMeshTile[] tiles, int maxTiles)
        {
            int nx = x, ny = y;
            switch (side)
            {
                case 0:
                    nx++;
                    break;
                case 1:
                    nx++;
                    ny++;
                    break;
                case 2:
                    ny++;
                    break;
                case 3:
                    nx--;
                    ny++;
                    break;
                case 4:
                    nx--;
                    break;
                case 5:
                    nx--;
                    ny--;
                    break;
                case 6:
                    ny--;
                    break;
                case 7:
                    nx++;
                    ny--;
                    break;
            }

            return GetTilesAt(nx, ny, tiles, maxTiles);
        }

        /// Returns neighbour tile based on side.
        public int GetTilesAt(int x, int y, DtMeshTile[] tiles, int maxTiles)
        {
            int n = 0;

            // Find tile based on hash.
            int h = ComputeTileHash(x, y, m_tileLutMask);
            DtMeshTile tile = m_posLookup[h];
            while (null != tile)
            {
                if (null != tile.data &&
                    null != tile.data.header &&
                    tile.data.header.x == x &&
                    tile.data.header.y == y)
                {
                    if (n < maxTiles)
                        tiles[n++] = tile;
                }

                tile = tile.next;
            }

            return n;
        }

        /// Returns neighbour tile based on side.
        public int GetTilesAt(int x, int y, Span<int> tiles, int maxTiles)
        {
            int n = 0;

            // Find tile based on hash.
            int h = ComputeTileHash(x, y, m_tileLutMask);
            DtMeshTile tile = m_posLookup[h];
            while (null != tile)
            {
                if (null != tile.data &&
                    null != tile.data.header &&
                    tile.data.header.x == x &&
                    tile.data.header.y == y)
                {
                    if (n < maxTiles)
                        tiles[n++] = tile.index;
                }

                tile = tile.next;
            }

            return n;
        }


        public long GetTileRefAt(int x, int y, int layer)
        {
            return GetTileRef(GetTileAt(x, y, layer));
        }

        public DtMeshTile GetTileByRef(long refs)
        {
            if (refs == 0)
            {
                return null;
            }

            int tileIndex = DecodePolyIdTile(refs);
            int tileSalt = DecodePolyIdSalt(refs);
            if (tileIndex >= m_maxTiles)
            {
                return null;
            }

            DtMeshTile tile = m_tiles[tileIndex];
            if (tile.salt != tileSalt)
            {
                return null;
            }

            return tile;
        }

        public long GetTileRef(DtMeshTile tile)
        {
            if (tile == null)
            {
                return 0;
            }

            return EncodePolyId(tile.salt, tile.index, 0);
        }

        /// Gets the endpoints for an off-mesh connection, ordered by "direction of travel".
        ///  @param[in]		prevRef		The reference of the polygon before the connection.
        ///  @param[in]		polyRef		The reference of the off-mesh connection polygon.
        ///  @param[out]	startPos	The start position of the off-mesh connection. [(x, y, z)]
        ///  @param[out]	endPos		The end position of the off-mesh connection. [(x, y, z)]
        /// @return The status flags for the operation.
        /// 
        /// @par
        ///
        /// Off-mesh connections are stored in the navigation mesh as special 2-vertex 
        /// polygons with a single edge. At least one of the vertices is expected to be 
        /// inside a normal polygon. So an off-mesh connection is "entered" from a 
        /// normal polygon at one of its endpoints. This is the polygon identified by 
        /// the prevRef parameter.
        public DtStatus GetOffMeshConnectionPolyEndPoints(long prevRef, long polyRef, ref RcVec3f startPos, ref RcVec3f endPos)
        {
            if (polyRef == 0)
            {
                return DtStatus.DT_FAILURE;
            }

            // Get current polygon
            DecodePolyId(polyRef, out var salt, out var it, out var ip);
            if (it >= m_maxTiles)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            if (m_tiles[it].salt != salt || m_tiles[it].data.header == null)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            DtMeshTile tile = m_tiles[it];
            if (ip >= tile.data.header.polyCount)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            DtPoly poly = tile.data.polys[ip];

            // Make sure that the current poly is indeed off-mesh link.
            if (poly.GetPolyType() != DtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
            {
                return DtStatus.DT_FAILURE;
            }

            // Figure out which way to hand out the vertices.
            int idx0 = 0, idx1 = 1;

            // Find link that points to first vertex.
            for (int i = poly.firstLink; i != DT_NULL_LINK; i = tile.links[i].next)
            {
                if (tile.links[i].edge == 0)
                {
                    if (tile.links[i].refs != prevRef)
                    {
                        idx0 = 1;
                        idx1 = 0;
                    }

                    break;
                }
            }

            startPos = tile.data.verts.ToVec3(poly.verts[idx0] * 3);
            endPos = tile.data.verts.ToVec3(poly.verts[idx1] * 3);

            return DtStatus.DT_SUCCESS;
        }

        public int GetMaxVertsPerPoly()
        {
            return m_maxVertPerPoly;
        }

        public bool IsAvailableTileCount()
        {
            return null != m_nextFree;
        }

        /// @{
        /// @name State Management
        /// These functions do not effect #dtTileRef or #dtPolyRef's. 
        /// Sets the user defined flags for the specified polygon.
        ///  @param[in]	ref		The polygon reference.
        ///  @param[in]	flags	The new flags for the polygon.
        /// @return The status flags for the operation.
        public DtStatus SetPolyFlags(long refs, int flags)
        {
            if (refs == 0)
            {
                return DtStatus.DT_FAILURE;
            }

            DecodePolyId(refs, out var salt, out var it, out var ip);
            if (it >= m_maxTiles)
            {
                return DtStatus.DT_INVALID_PARAM;
            }

            if (m_tiles[it].salt != salt || m_tiles[it].data == null || m_tiles[it].data.header == null)
            {
                return DtStatus.DT_INVALID_PARAM;
            }

            DtMeshTile tile = m_tiles[it];
            if (ip >= tile.data.header.polyCount)
            {
                return DtStatus.DT_INVALID_PARAM;
            }

            DtPoly poly = tile.data.polys[ip];

            // Change flags.
            poly.flags = flags;
            return DtStatus.DT_SUCCESS;
        }

        /// Gets the user defined flags for the specified polygon.
        ///  @param[in]		ref				The polygon reference.
        ///  @param[out]	resultFlags		The polygon flags.
        /// @return The status flags for the operation.
        public DtStatus GetPolyFlags(long refs, out int resultFlags)
        {
            resultFlags = 0;

            if (refs == 0)
            {
                return DtStatus.DT_FAILURE;
            }

            DecodePolyId(refs, out var salt, out var it, out var ip);
            if (it >= m_maxTiles)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            if (m_tiles[it].salt != salt || m_tiles[it].data == null || m_tiles[it].data.header == null)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            DtMeshTile tile = m_tiles[it];
            if (ip >= tile.data.header.polyCount)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            DtPoly poly = tile.data.polys[ip];

            resultFlags = poly.flags;

            return DtStatus.DT_SUCCESS;
        }

        /// Sets the user defined area for the specified polygon.
        ///  @param[in]	ref		The polygon reference.
        ///  @param[in]	area	The new area id for the polygon. [Limit: DT_MAX_AREAS]
        /// @return The status flags for the operation.
        public DtStatus SetPolyArea(long refs, char area)
        {
            if (refs == 0)
            {
                return DtStatus.DT_FAILURE;
            }

            DecodePolyId(refs, out var salt, out var it, out var ip);
            if (it >= m_maxTiles)
            {
                return DtStatus.DT_FAILURE;
            }

            if (m_tiles[it].salt != salt || m_tiles[it].data == null || m_tiles[it].data.header == null)
            {
                return DtStatus.DT_INVALID_PARAM;
            }

            DtMeshTile tile = m_tiles[it];
            if (ip >= tile.data.header.polyCount)
            {
                return DtStatus.DT_INVALID_PARAM;
            }

            DtPoly poly = tile.data.polys[ip];

            poly.SetArea(area);

            return DtStatus.DT_SUCCESS;
        }

        /// Gets the user defined area for the specified polygon.
        ///  @param[in]		ref			The polygon reference.
        ///  @param[out]	resultArea	The area id for the polygon.
        /// @return The status flags for the operation.
        public DtStatus GetPolyArea(long refs, out int resultArea)
        {
            resultArea = 0;

            if (refs == 0)
            {
                return DtStatus.DT_FAILURE;
            }

            DecodePolyId(refs, out var salt, out var it, out var ip);
            if (it >= m_maxTiles)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            if (m_tiles[it].salt != salt || m_tiles[it].data == null || m_tiles[it].data.header == null)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            DtMeshTile tile = m_tiles[it];
            if (ip >= tile.data.header.polyCount)
            {
                return DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
            }

            DtPoly poly = tile.data.polys[ip];
            resultArea = poly.GetArea();

            return DtStatus.DT_SUCCESS;
        }

        public RcVec3f GetPolyCenter(long refs)
        {
            RcVec3f center = RcVec3f.Zero;

            var status = GetTileAndPolyByRef(refs, out var tile, out var poly);
            if (status.Succeeded())
            {
                for (int i = 0; i < poly.vertCount; ++i)
                {
                    int v = poly.verts[i] * 3;
                    center.X += tile.data.verts[v];
                    center.Y += tile.data.verts[v + 1];
                    center.Z += tile.data.verts[v + 2];
                }

                float s = 1.0f / poly.vertCount;
                center.X *= s;
                center.Y *= s;
                center.Z *= s;
            }

            return center;
        }

        public void ComputeBounds(out RcVec3f bmin, out RcVec3f bmax)
        {
            bmin = new RcVec3f(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            bmax = new RcVec3f(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int t = 0; t < GetMaxTiles(); ++t)
            {
                DtMeshTile tile = GetTile(t);
                if (tile != null && tile.data != null)
                {
                    for (int i = 0; i < tile.data.verts.Length; i += 3)
                    {
                        bmin.X = Math.Min(bmin.X, tile.data.verts[i]);
                        bmin.Y = Math.Min(bmin.Y, tile.data.verts[i + 1]);
                        bmin.Z = Math.Min(bmin.Z, tile.data.verts[i + 2]);
                        bmax.X = Math.Max(bmax.X, tile.data.verts[i]);
                        bmax.Y = Math.Max(bmax.Y, tile.data.verts[i + 1]);
                        bmax.Z = Math.Max(bmax.Z, tile.data.verts[i + 2]);
                    }
                }
            }
        }
    }
}