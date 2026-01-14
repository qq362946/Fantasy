using System.Collections.Generic;
using System.IO;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Detour.Io;
using UnityEngine;
using UnityEngine.Rendering;

namespace UniRecast.Core
{
    // https://github.com/highfidelity/unity-to-hifi-exporter/blob/master/Assets/UnityToHiFiExporter/Editor/TerrainObjExporter.cs
    public static class UniRcExtensions
    {
        public static RcVec3f ToRightHand(this Vector3 v)
        {
            return new RcVec3f(-v.x, v.y, v.z);
        }

        public static UniRcNavMeshSurfaceTarget ToUniRcSurfaceSource(this MeshFilter meshFilter)
        {
            return new UniRcNavMeshSurfaceTarget(meshFilter.name, meshFilter.sharedMesh, meshFilter.transform.localToWorldMatrix);
        }

        public static UniRcNavMeshSurfaceTarget ToUniRcSurfaceSource(this Terrain terrain)
        {
            var mesh = terrain.terrainData.ToMesh(terrain.transform.position);
            return new UniRcNavMeshSurfaceTarget(terrain.name, mesh, terrain.transform.localToWorldMatrix);
        }

        public static UniRcNavMeshSurfaceTarget ToCombinedNavMeshSurfaceTarget(this IList<UniRcNavMeshSurfaceTarget> sources, string name)
        {
            CombineInstance[] combineInstances = new CombineInstance[sources.Count];

            for (int i = 0; i < sources.Count; i++)
            {
                combineInstances[i].mesh = sources[i].GetMesh();
                combineInstances[i].transform = sources[i].GetMatrix4();
            }

            // Combine meshes with UInt32 index format
            Mesh combinedMesh = new Mesh();
            combinedMesh.indexFormat = IndexFormat.UInt32;
            combinedMesh.CombineMeshes(combineInstances, true, true, false);

            // // debug code
            // // Create a new GameObject to hold the combined mesh
            // var combinedMeshObject = new GameObject(name);
            // var meshFilter = combinedMeshObject.AddComponent<MeshFilter>();
            // var meshRenderer = combinedMeshObject.AddComponent<MeshRenderer>();
            //
            // // Assign the combined mesh to the new GameObject
            // meshFilter.sharedMesh = combinedMesh;

            return new UniRcNavMeshSurfaceTarget(name, combinedMesh, Matrix4x4.identity);
        }

        public static Mesh ToMesh(this TerrainData terrainData, Vector3 terrainPos)
        {
            int w = terrainData.heightmapResolution;
            int h = terrainData.heightmapResolution;
            Vector3 meshScale = terrainData.size;
            int tRes = (int)Mathf.Pow(2, 1);
            meshScale = new Vector3(meshScale.x / (w - 1) * tRes, meshScale.y, meshScale.z / (h - 1) * tRes);
            float[,] heights = terrainData.GetHeights(0, 0, w, h);

            w = (w - 1) / tRes + 1;
            h = (h - 1) / tRes + 1;

            Vector3[] vertices = new Vector3[w * h];
            int[] triangles = new int[(w - 1) * (h - 1) * 6];

            for (int z = 0; z < h; z++)
            {
                for (int x = 0; x < w; x++)
                {
                    vertices[z * w + x] = Vector3.Scale(meshScale, new Vector3(x, heights[z * tRes, x * tRes], z));
                }
            }

            int index = 0;

            // Build triangle indices: 3 indices into vertex array for each triangle
            for (int z = 0; z < h - 1; z++)
            {
                for (int x = 0; x < w - 1; x++)
                {
                    // For each grid cell output two triangles
                    triangles[index++] = (z * w) + x;
                    triangles[index++] = ((z + 1) * w) + x;
                    triangles[index++] = (z * w) + x + 1;

                    triangles[index++] = ((z + 1) * w) + x;
                    triangles[index++] = ((z + 1) * w) + x + 1;
                    triangles[index++] = (z * w) + x + 1;
                }
            }

            // Create a new mesh
            // Assign vertices and triangles to the mesh
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;

            // Calculate normals and other mesh attributes if needed
            mesh.RecalculateNormals();

            return mesh;
        }

        public static void SaveNavMeshFile(this DtNavMesh navMesh, string fileName)
        {
            var navMeshFileName = $"{fileName}.bytes";
            using var fs = new FileStream(navMeshFileName, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);

            DtMeshSetWriter writer = new DtMeshSetWriter();
            writer.Write(bw, navMesh, RcByteOrder.BIG_ENDIAN, true);
        }
    }
}