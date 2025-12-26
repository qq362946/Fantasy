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

using System.IO;
using DotRecast.Core;

namespace DotRecast.Detour.Dynamic.Io
{
    public class DtVoxelFileWriter
    {
        private readonly IRcCompressor _compressor;

        public DtVoxelFileWriter(IRcCompressor compressor)
        {
            _compressor = compressor;
        }

        public void Write(BinaryWriter stream, DtVoxelFile f, bool compression)
        {
            Write(stream, f, DtVoxelFile.PREFERRED_BYTE_ORDER, compression);
        }

        public void Write(BinaryWriter stream, DtVoxelFile f, RcByteOrder byteOrder, bool compression)
        {
            RcIO.Write(stream, DtVoxelFile.MAGIC, byteOrder);
            RcIO.Write(stream, DtVoxelFile.VERSION_EXPORTER_RECAST4J | (compression ? DtVoxelFile.VERSION_COMPRESSION_LZ4 : 0), byteOrder);
            RcIO.Write(stream, f.walkableRadius, byteOrder);
            RcIO.Write(stream, f.walkableHeight, byteOrder);
            RcIO.Write(stream, f.walkableClimb, byteOrder);
            RcIO.Write(stream, f.walkableSlopeAngle, byteOrder);
            RcIO.Write(stream, f.cellSize, byteOrder);
            RcIO.Write(stream, f.maxSimplificationError, byteOrder);
            RcIO.Write(stream, f.maxEdgeLen, byteOrder);
            RcIO.Write(stream, f.minRegionArea, byteOrder);
            RcIO.Write(stream, f.regionMergeArea, byteOrder);
            RcIO.Write(stream, f.vertsPerPoly, byteOrder);
            RcIO.Write(stream, f.buildMeshDetail);
            RcIO.Write(stream, f.detailSampleDistance, byteOrder);
            RcIO.Write(stream, f.detailSampleMaxError, byteOrder);
            RcIO.Write(stream, f.useTiles);
            RcIO.Write(stream, f.tileSizeX, byteOrder);
            RcIO.Write(stream, f.tileSizeZ, byteOrder);
            RcIO.Write(stream, f.rotation.X, byteOrder);
            RcIO.Write(stream, f.rotation.Y, byteOrder);
            RcIO.Write(stream, f.rotation.Z, byteOrder);
            RcIO.Write(stream, f.bounds[0], byteOrder);
            RcIO.Write(stream, f.bounds[1], byteOrder);
            RcIO.Write(stream, f.bounds[2], byteOrder);
            RcIO.Write(stream, f.bounds[3], byteOrder);
            RcIO.Write(stream, f.bounds[4], byteOrder);
            RcIO.Write(stream, f.bounds[5], byteOrder);
            RcIO.Write(stream, f.tiles.Count, byteOrder);
            foreach (DtVoxelTile t in f.tiles)
            {
                WriteTile(stream, t, byteOrder, compression);
            }
        }

        public void WriteTile(BinaryWriter stream, DtVoxelTile tile, RcByteOrder byteOrder, bool compression)
        {
            RcIO.Write(stream, tile.tileX, byteOrder);
            RcIO.Write(stream, tile.tileZ, byteOrder);
            RcIO.Write(stream, tile.width, byteOrder);
            RcIO.Write(stream, tile.depth, byteOrder);
            RcIO.Write(stream, tile.borderSize, byteOrder);
            RcIO.Write(stream, tile.boundsMin.X, byteOrder);
            RcIO.Write(stream, tile.boundsMin.Y, byteOrder);
            RcIO.Write(stream, tile.boundsMin.Z, byteOrder);
            RcIO.Write(stream, tile.boundsMax.X, byteOrder);
            RcIO.Write(stream, tile.boundsMax.Y, byteOrder);
            RcIO.Write(stream, tile.boundsMax.Z, byteOrder);
            RcIO.Write(stream, tile.cellSize, byteOrder);
            RcIO.Write(stream, tile.cellHeight, byteOrder);
            byte[] bytes = tile.spanData;
            if (compression)
            {
                bytes = _compressor.Compress(bytes);
            }

            RcIO.Write(stream, bytes.Length, byteOrder);
            stream.Write(bytes);
        }
    }
}