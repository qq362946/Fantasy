using System;
using System.IO;
using System.Text;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Recast.Toolset;
using DotRecast.Recast.Toolset.Builder;
using DotRecast.Recast.Toolset.Geom;
using UnityEngine;

namespace UniRecast.Core
{
    public readonly struct UniRcMesh
    {
        public readonly string Name;
        public readonly RcVec3f[] Vertices;
        public readonly int[] Faces;

        public UniRcMesh(string name, RcVec3f[] vertices, int[] faces)
        {
            Name = name;
            Vertices = vertices;
            Faces = faces;
        }

        public bool IsEmpty()
        {
            if (null == Vertices)
                return true;

            if (0 >= Vertices.Length)
                return true;

            return false;
        }

        public string ToGeomObjString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"# UniRecast {UniRcConst.Version}");
            sb.AppendLine($"o {Name}");

            // 버텍스 기록
            foreach (var vertex in Vertices)
            {
                sb.AppendLine($"v {vertex.X:F6} {vertex.Y:F6} {vertex.Z:F6}");
            }

            // 면 기록
            for (int i = 0; i < Faces.Length; i += 3)
            {
                int index0 = Faces[i + 0] + 1;
                int index1 = Faces[i + 1] + 1;
                int index2 = Faces[i + 2] + 1;
                sb.AppendLine($"f {index0} {index1} {index2}");
            }

            return sb.ToString();
        }

        public DtNavMesh Build(RcNavMeshBuildSettings setting)
        {
            var vertices = new float[Vertices.Length * 3];
            for (int i = 0; i < Vertices.Length; ++i)
            {
                vertices[i * 3 + 0] = Vertices[i].X;
                vertices[i * 3 + 1] = Vertices[i].Y;
                vertices[i * 3 + 2] = Vertices[i].Z;
            }

            var geom = new DemoInputGeomProvider(vertices, Faces);
            var builder = new TileNavMeshBuilder();
            var result = builder.Build(geom, setting);
            return result.NavMesh;
        }

        public void SaveFile(string path = "")
        {
            var beginTicks = RcFrequency.Ticks;
            var workingDir = Directory.GetCurrentDirectory();
            var filePath = Path.Combine(workingDir, path, $"{Name}.obj");
            var objStr = ToGeomObjString();

            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8, 1024 * 64))
            {
                sw.Write(objStr);
            }

            var elapsedTicks = RcFrequency.Ticks - beginTicks;
            Debug.Log($"save to {filePath} {elapsedTicks / TimeSpan.TicksPerMillisecond} ms");
        }
    }
}