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
using System.Threading.Tasks;
using DotRecast.Core;
using DotRecast.Core.Collections;
using DotRecast.Detour.Dynamic.Colliders;
using DotRecast.Detour.Dynamic.Io;
using DotRecast.Recast;

namespace DotRecast.Detour.Dynamic
{
    public class DtDynamicNavMesh
    {
        public const int MAX_VERTS_PER_POLY = 6;
        public readonly DtDynamicNavMeshConfig config;
        private readonly RcBuilder builder;
        private readonly Dictionary<long, DtDynamicTile> _tiles = new Dictionary<long, DtDynamicTile>();
        private readonly RcContext _context;
        private readonly DtNavMeshParams navMeshParams;
        private readonly BlockingCollection<IDtDaynmicTileJob> updateQueue = new BlockingCollection<IDtDaynmicTileJob>();
        private readonly RcAtomicLong currentColliderId = new RcAtomicLong(0);
        private DtNavMesh _navMesh;
        private bool _dirty = true;

        public DtDynamicNavMesh(DtVoxelFile voxelFile)
        {
            config = new DtDynamicNavMeshConfig(voxelFile.useTiles, voxelFile.tileSizeX, voxelFile.tileSizeZ, voxelFile.cellSize);
            config.walkableHeight = voxelFile.walkableHeight;
            config.walkableRadius = voxelFile.walkableRadius;
            config.walkableClimb = voxelFile.walkableClimb;
            config.walkableSlopeAngle = voxelFile.walkableSlopeAngle;
            config.maxSimplificationError = voxelFile.maxSimplificationError;
            config.maxEdgeLen = voxelFile.maxEdgeLen;
            config.minRegionArea = voxelFile.minRegionArea;
            config.regionMergeArea = voxelFile.regionMergeArea;
            config.vertsPerPoly = voxelFile.vertsPerPoly;
            config.buildDetailMesh = voxelFile.buildMeshDetail;
            config.detailSampleDistance = voxelFile.detailSampleDistance;
            config.detailSampleMaxError = voxelFile.detailSampleMaxError;
            builder = new RcBuilder();
            navMeshParams = new DtNavMeshParams();
            navMeshParams.orig.X = voxelFile.bounds[0];
            navMeshParams.orig.Y = voxelFile.bounds[1];
            navMeshParams.orig.Z = voxelFile.bounds[2];
            navMeshParams.tileWidth = voxelFile.useTiles ? voxelFile.cellSize * voxelFile.tileSizeX : voxelFile.bounds[3] - voxelFile.bounds[0];
            navMeshParams.tileHeight = voxelFile.useTiles ? voxelFile.cellSize * voxelFile.tileSizeZ : voxelFile.bounds[5] - voxelFile.bounds[2];
            navMeshParams.maxTiles = voxelFile.tiles.Count;
            navMeshParams.maxPolys = 0x8000;
            foreach (var t in voxelFile.tiles)
            {
                _tiles.Add(LookupKey(t.tileX, t.tileZ), new DtDynamicTile(t));
            }

            ;
            _context = new RcContext();
        }

        public DtNavMesh NavMesh()
        {
            return _navMesh;
        }

        /**
     * Voxel queries require checkpoints to be enabled in {@link DynamicNavMeshConfig}
     */
        public DtVoxelQuery VoxelQuery()
        {
            return new DtVoxelQuery(navMeshParams.orig, navMeshParams.tileWidth, navMeshParams.tileHeight, LookupHeightfield);
        }

        private RcHeightfield LookupHeightfield(int x, int z)
        {
            return GetTileAt(x, z)?.checkpoint.heightfield;
        }

        public long AddCollider(IDtCollider collider)
        {
            long cid = currentColliderId.IncrementAndGet();
            updateQueue.Add(new DtDynamicTileColliderAdditionJob(cid, collider, GetTiles(collider.Bounds())));
            return cid;
        }

        public void RemoveCollider(long colliderId)
        {
            updateQueue.Add(new DtDynamicTileColliderRemovalJob(colliderId, GetTilesByCollider(colliderId)));
        }


        private HashSet<DtDynamicTile> ProcessQueue()
        {
            var items = ConsumeQueue();
            foreach (var item in items)
            {
                Process(item);
            }

            return items.SelectMany(i => i.AffectedTiles()).ToHashSet();
        }

        private List<IDtDaynmicTileJob> ConsumeQueue()
        {
            List<IDtDaynmicTileJob> items = new List<IDtDaynmicTileJob>();
            while (updateQueue.TryTake(out var item))
            {
                items.Add(item);
            }

            return items;
        }

        private void Process(IDtDaynmicTileJob item)
        {
            foreach (var tile in item.AffectedTiles())
            {
                item.Process(tile);
            }
        }

        // Perform full build of the navmesh
        public void Build()
        {
            ProcessQueue();
            Rebuild(_tiles.Values);
        }

        // Perform full build concurrently using the given {@link ExecutorService}
        public bool Build(TaskFactory executor)
        {
            ProcessQueue();
            return Rebuild(_tiles.Values, executor);
        }


        // Perform incremental update of the navmesh
        public bool Update()
        {
            return Rebuild(ProcessQueue());
        }

        // Perform incremental update concurrently using the given {@link ExecutorService}
        public bool Update(TaskFactory executor)
        {
            return Rebuild(ProcessQueue(), executor);
        }

        private bool Rebuild(ICollection<DtDynamicTile> tiles)
        {
            foreach (var tile in tiles)
                Rebuild(tile);

            return UpdateNavMesh();
        }

        private bool Rebuild(ICollection<DtDynamicTile> tiles, TaskFactory executor)
        {
            var tasks = tiles
                .Select(tile => executor.StartNew(() => Rebuild(tile)))
                .ToArray();

            Task.WaitAll(tasks);
            return UpdateNavMesh();
        }

        private ICollection<DtDynamicTile> GetTiles(float[] bounds)
        {
            if (bounds == null)
            {
                return _tiles.Values;
            }

            int minx = (int)MathF.Floor((bounds[0] - navMeshParams.orig.X) / navMeshParams.tileWidth) - 1;
            int minz = (int)MathF.Floor((bounds[2] - navMeshParams.orig.Z) / navMeshParams.tileHeight) - 1;
            int maxx = (int)MathF.Floor((bounds[3] - navMeshParams.orig.X) / navMeshParams.tileWidth) + 1;
            int maxz = (int)MathF.Floor((bounds[5] - navMeshParams.orig.Z) / navMeshParams.tileHeight) + 1;
            List<DtDynamicTile> tiles = new List<DtDynamicTile>();
            for (int z = minz; z <= maxz; ++z)
            {
                for (int x = minx; x <= maxx; ++x)
                {
                    DtDynamicTile tile = GetTileAt(x, z);
                    if (tile != null && IntersectsXZ(tile, bounds))
                    {
                        tiles.Add(tile);
                    }
                }
            }

            return tiles;
        }

        private bool IntersectsXZ(DtDynamicTile tile, float[] bounds)
        {
            return tile.voxelTile.boundsMin.X <= bounds[3] && tile.voxelTile.boundsMax.X >= bounds[0] &&
                   tile.voxelTile.boundsMin.Z <= bounds[5] && tile.voxelTile.boundsMax.Z >= bounds[2];
        }

        private List<DtDynamicTile> GetTilesByCollider(long cid)
        {
            return _tiles.Values.Where(t => t.ContainsCollider(cid)).ToList();
        }

        private void Rebuild(DtDynamicTile tile)
        {
            DtNavMeshCreateParams option = new DtNavMeshCreateParams();
            option.walkableHeight = config.walkableHeight;
            _dirty = _dirty | tile.Build(builder, config, _context);
        }

        private bool UpdateNavMesh()
        {
            if (_dirty)
            {
                _dirty = false;

                DtNavMesh navMesh = new DtNavMesh();
                navMesh.Init(navMeshParams, MAX_VERTS_PER_POLY);

                foreach (var t in _tiles.Values)
                {
                    t.AddTo(navMesh);
                }

                _navMesh = navMesh;
                return true;
            }

            return false;
        }

        private DtDynamicTile GetTileAt(int x, int z)
        {
            return _tiles.TryGetValue(LookupKey(x, z), out var tile)
                ? tile
                : null;
        }

        private long LookupKey(long x, long z)
        {
            return (z << 32) | x;
        }

        public List<DtVoxelTile> VoxelTiles()
        {
            return _tiles.Values.Select(t => t.voxelTile).ToList();
        }

        public List<RcBuilderResult> RecastResults()
        {
            return _tiles.Values.Select(t => t.recastResult).ToList();
        }

        public void NavMesh(DtNavMesh mesh)
        {
            _tiles.Values.ForEach(t =>
            {
                const int MAX_NEIS = 32;
                DtMeshTile[] tiles = new DtMeshTile[MAX_NEIS];
                int nneis = mesh.GetTilesAt(t.voxelTile.tileX, t.voxelTile.tileZ, tiles, MAX_NEIS);
                if (0 < nneis)
                {
                    t.SetMeshData(tiles[0].data);
                }
            });
            _navMesh = mesh;
            _dirty = false;
        }
    }
}