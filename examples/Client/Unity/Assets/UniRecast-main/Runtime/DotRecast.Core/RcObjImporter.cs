/*
recast4j Copyright (c) 2015-2019 Piotr Piastucki piotr@jtilia.org
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
using System.Globalization;
using System.IO;
using DotRecast.Core.Numerics;

namespace DotRecast.Core
{
    public static class RcObjImporter
    {
        public static RcObjImporterContext LoadContext(byte[] chunk)
        {
            RcObjImporterContext context = new RcObjImporterContext();
            try
            {
                using StreamReader reader = new StreamReader(new MemoryStream(chunk));
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    ReadLine(line, context);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }

            return context;
        }


        public static void ReadLine(string line, RcObjImporterContext context)
        {
            if (line.StartsWith("v"))
            {
                ReadVertex(line, context);
            }
            else if (line.StartsWith("f"))
            {
                ReadFace(line, context);
            }
        }

        private static void ReadVertex(string line, RcObjImporterContext context)
        {
            if (line.StartsWith("v "))
            {
                var vert = ReadVector3f(line);
                context.vertexPositions.Add(vert.X);
                context.vertexPositions.Add(vert.Y);
                context.vertexPositions.Add(vert.Z);
            }
        }

        private static RcVec3f ReadVector3f(string line)
        {
            string[] v = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (v.Length < 4)
            {
                throw new Exception("Invalid vector, expected 3 coordinates, found " + (v.Length - 1));
            }

            // fix - https://github.com/ikpil/DotRecast/issues/7
            return new RcVec3f(
                float.Parse(v[1], CultureInfo.InvariantCulture),
                float.Parse(v[2], CultureInfo.InvariantCulture),
                float.Parse(v[3], CultureInfo.InvariantCulture)
            );
        }

        private static void ReadFace(string line, RcObjImporterContext context)
        {
            string[] v = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (v.Length < 4)
            {
                throw new Exception("Invalid number of face vertices: 3 coordinates expected, found " + v.Length);
            }

            for (int j = 0; j < v.Length - 3; j++)
            {
                context.meshFaces.Add(ReadFaceVertex(v[1], context));
                for (int i = 0; i < 2; i++)
                {
                    context.meshFaces.Add(ReadFaceVertex(v[2 + j + i], context));
                }
            }
        }

        private static int ReadFaceVertex(string face, RcObjImporterContext context)
        {
            string[] v = face.Split("/");
            return GetIndex(int.Parse(v[0]), context.vertexPositions.Count);
        }

        private static int GetIndex(int posi, int size)
        {
            if (posi > 0)
            {
                posi--;
            }
            else if (posi < 0)
            {
                posi = size + posi;
            }
            else
            {
                throw new Exception("0 vertex index");
            }

            return posi;
        }
    }
}