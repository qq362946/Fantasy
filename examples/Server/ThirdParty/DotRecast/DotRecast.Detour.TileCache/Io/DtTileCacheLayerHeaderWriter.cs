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

using System.IO;
using DotRecast.Core;

namespace DotRecast.Detour.TileCache.Io
{
    public class DtTileCacheLayerHeaderWriter
    {
        public void Write(BinaryWriter stream, DtTileCacheLayerHeader header, RcByteOrder order, bool cCompatibility)
        {
            RcIO.Write(stream, header.magic, order);
            RcIO.Write(stream, header.version, order);
            RcIO.Write(stream, header.tx, order);
            RcIO.Write(stream, header.ty, order);
            RcIO.Write(stream, header.tlayer, order);

            RcIO.Write(stream, header.bmin.X, order);
            RcIO.Write(stream, header.bmin.Y, order);
            RcIO.Write(stream, header.bmin.Z, order);
            RcIO.Write(stream, header.bmax.X, order);
            RcIO.Write(stream, header.bmax.Y, order);
            RcIO.Write(stream, header.bmax.Z, order);

            RcIO.Write(stream, (short)header.hmin, order);
            RcIO.Write(stream, (short)header.hmax, order);
            RcIO.Write(stream, (byte)header.width);
            RcIO.Write(stream, (byte)header.height);
            RcIO.Write(stream, (byte)header.minx);
            RcIO.Write(stream, (byte)header.maxx);
            RcIO.Write(stream, (byte)header.miny);
            RcIO.Write(stream, (byte)header.maxy);
            if (cCompatibility)
            {
                RcIO.Write(stream, (short)0, order); // C struct padding
            }
        }
    }
}