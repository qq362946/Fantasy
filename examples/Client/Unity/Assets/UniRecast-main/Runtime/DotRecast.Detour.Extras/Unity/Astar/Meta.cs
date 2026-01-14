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
using System.Text.RegularExpressions;

namespace DotRecast.Detour.Extras.Unity.Astar
{
    public class Meta
    {
        public const string TYPENAME_RECAST_GRAPH = "Pathfinding.RecastGraph";
        public const string MIN_SUPPORTED_VERSION = "4.0.6";
        public const string UPDATED_STRUCT_VERSION = "4.1.0";
        public static readonly Regex VERSION_PATTERN = new Regex(@"(\d+)\.(\d+)\.(\d+)");
        public string version { get; set; }
        public int graphs { get; set; }
        public string[] guids { get; set; }
        public string[] typeNames { get; set; }

        public bool IsSupportedVersion()
        {
            return IsVersionAtLeast(MIN_SUPPORTED_VERSION);
        }

        public bool IsVersionAtLeast(string minVersion)
        {
            int[] actual = ParseVersion(version);
            int[] minSupported = ParseVersion(minVersion);
            for (int i = 0; i < Math.Min(actual.Length, minSupported.Length); i++)
            {
                if (actual[i] > minSupported[i])
                {
                    return true;
                }
                else if (minSupported[i] > actual[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static int[] ParseVersion(string version)
        {
            Match m = VERSION_PATTERN.Match(version);
            if (m.Success)
            {
                int[] v = new int[m.Groups.Count - 1];
                for (int i = 0; i < v.Length; i++)
                {
                    v[i] = int.Parse(m.Groups[i + 1].Value);
                }

                return v;
            }

            throw new ArgumentException("Invalid version format: " + version);
        }

        public bool IsSupportedType()
        {
            foreach (string t in typeNames)
            {
                if (t == TYPENAME_RECAST_GRAPH)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
