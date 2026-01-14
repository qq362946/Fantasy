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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DotRecast.Core;
using DotRecast.Core.Collections;
using DotRecast.Detour.Dynamic.Colliders;
using DotRecast.Detour.Dynamic.Io;
using DotRecast.Recast;

namespace DotRecast.Detour.Dynamic
{
    public class DtDynamicTile
    {
        public readonly DtVoxelTile voxelTile;
        public DtDynamicTileCheckpoint checkpoint;
        public RcBuilderResult recastResult;
        private DtMeshData meshData;
        private readonly ConcurrentDictionary<long, IDtCollider> colliders = new ConcurrentDictionary<long, IDtCollider>();
        private bool dirty = true;
        private long id;

        public DtDynamicTile(DtVoxelTile voxelTile)
        {
            this.voxelTile = voxelTile;
        }

        public bool Build(RcBuilder builder, DtDynamicNavMeshConfig config, RcContext context)
        {
            if (dirty)
            {
                RcHeightfield heightfield = BuildHeightfield(config, context);
                RcBuilderResult r = BuildRecast(builder, config, voxelTile, heightfield, context);
                DtNavMeshCreateParams option = NavMeshCreateParams(voxelTile.tileX, voxelTile.tileZ, voxelTile.cellSize,
                    voxelTile.cellHeight, config, r);
                meshData = DtNavMeshBuilder.CreateNavMeshData(option);
                return true;
            }

            return false;
        }

        private RcHeightfield BuildHeightfield(DtDynamicNavMeshConfig config, RcContext context)
        {
            ICollection<long> rasterizedColliders = checkpoint != null
                ? checkpoint.colliders as ICollection<long>
                : RcImmutableArray<long>.Empty;

            RcHeightfield heightfield = checkpoint != null
                ? checkpoint.heightfield
                : voxelTile.Heightfield();

            foreach (var (cid, c) in colliders)
            {
                if (!rasterizedColliders.Contains(cid))
                {
                    heightfield.bmax.Y = Math.Max(heightfield.bmax.Y, c.Bounds()[4] + heightfield.ch * 2);
                    c.Rasterize(heightfield, context);
                }
            }

            if (config.enableCheckpoints)
            {
                checkpoint = new DtDynamicTileCheckpoint(heightfield, colliders.Keys.ToHashSet());
            }

            return heightfield;
        }

        private RcBuilderResult BuildRecast(RcBuilder builder, DtDynamicNavMeshConfig config, DtVoxelTile vt,
            RcHeightfield heightfield, RcContext context)
        {
            RcConfig rcConfig = new RcConfig(
                config.useTiles, config.tileSizeX, config.tileSizeZ,
                vt.borderSize,
                RcPartitionType.OfValue(config.partition),
                vt.cellSize, vt.cellHeight,
                config.walkableSlopeAngle, config.walkableHeight, config.walkableRadius, config.walkableClimb,
                config.minRegionArea, config.regionMergeArea,
                config.maxEdgeLen, config.maxSimplificationError,
                Math.Min(DtDynamicNavMesh.MAX_VERTS_PER_POLY, config.vertsPerPoly),
                config.detailSampleDistance, config.detailSampleMaxError,
                true, true, true, default, true);
            RcBuilderResult r = builder.Build(context, vt.tileX, vt.tileZ, null, rcConfig, heightfield, false);
            if (config.keepIntermediateResults)
            {
                recastResult = r;
            }

            return r;
        }

        public void AddCollider(long cid, IDtCollider collider)
        {
            colliders[cid] = collider;
            dirty = true;
        }

        public bool ContainsCollider(long cid)
        {
            return colliders.ContainsKey(cid);
        }

        public void RemoveCollider(long colliderId)
        {
            if (colliders.TryRemove(colliderId, out var collider))
            {
                dirty = true;
                checkpoint = null;
            }
        }

        private DtNavMeshCreateParams NavMeshCreateParams(int tilex, int tileZ, float cellSize, float cellHeight,
            DtDynamicNavMeshConfig config, RcBuilderResult rcResult)
        {
            RcPolyMesh m_pmesh = rcResult.Mesh;
            RcPolyMeshDetail m_dmesh = rcResult.MeshDetail;
            DtNavMeshCreateParams option = new DtNavMeshCreateParams();
            for (int i = 0; i < m_pmesh.npolys; ++i)
            {
                m_pmesh.flags[i] = 1;
            }

            option.tileX = tilex;
            option.tileZ = tileZ;
            option.verts = m_pmesh.verts;
            option.vertCount = m_pmesh.nverts;
            option.polys = m_pmesh.polys;
            option.polyAreas = m_pmesh.areas;
            option.polyFlags = m_pmesh.flags;
            option.polyCount = m_pmesh.npolys;
            option.nvp = m_pmesh.nvp;
            if (m_dmesh != null)
            {
                option.detailMeshes = m_dmesh.meshes;
                option.detailVerts = m_dmesh.verts;
                option.detailVertsCount = m_dmesh.nverts;
                option.detailTris = m_dmesh.tris;
                option.detailTriCount = m_dmesh.ntris;
            }

            option.walkableHeight = config.walkableHeight;
            option.walkableRadius = config.walkableRadius;
            option.walkableClimb = config.walkableClimb;
            option.bmin = m_pmesh.bmin;
            option.bmax = m_pmesh.bmax;
            option.cs = cellSize;
            option.ch = cellHeight;
            option.buildBvTree = true;

            option.offMeshConCount = 0;
            option.offMeshConVerts = Array.Empty<float>();
            option.offMeshConRad = Array.Empty<float>();
            option.offMeshConDir = Array.Empty<int>();
            option.offMeshConAreas = Array.Empty<int>();
            option.offMeshConFlags = Array.Empty<int>();
            option.offMeshConUserID = Array.Empty<int>();
            return option;
        }

        public void AddTo(DtNavMesh navMesh)
        {
            if (meshData != null)
            {
                navMesh.AddTile(meshData, 0, 0, out var id);
            }
            else
            {
                navMesh.RemoveTile(id);
                id = 0;
            }
        }

        public void SetMeshData(DtMeshData data)
        {
            this.meshData = data;
        }
    }
}