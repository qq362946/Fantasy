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
using System.IO;
using System.IO.Compression;

using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace DotRecast.Detour.Extras.Unity.Astar
{
    public class DtPathfindingRecastGraphMetaReader
    {
        public DtPathfindingRecastGraphMeta Read(ZipArchive file, string filename)
        {
            ZipArchiveEntry entry = file.GetEntry(filename);
            using StreamReader reader = new StreamReader(entry.Open());


            var json = reader.ReadToEnd();

            // fixed : version 표기는 문자열이여야 한다
            string pattern = @"(\d+\.\d+\.\d+),";
            string replacement = "\"$1\",";
            var regex = new Regex(pattern);
            json = regex.Replace(json, replacement);

            var meta = JsonConvert.DeserializeObject<DtPathfindingRecastGraphMeta>(json);
            if (!meta.IsSupportedType())
            {
                throw new ArgumentException("Unsupported graph type " + string.Join(", ", meta.typeNames));
            }

            if (!meta.IsSupportedVersion())
            {
                throw new ArgumentException("Unsupported version " + meta.version);
            }

            return meta;
        }
    }
}