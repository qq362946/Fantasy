using System.IO;
using Fantasy.Async;
using UnityEngine;

namespace Fantasy
{
    public class RefAssemblyMain : MonoBehaviour
    {
        private void Start()
        {
            StartAsync().Coroutine();
        }

        private async FTask StartAsync()
        {
            var refAssemblyA = LoadAssembly("RefAssemblyA");
            var refAssemblyB = LoadAssembly("RefAssemblyB");
            Fantasy.Platform.Unity.Entry.Initialize(GetType().Assembly, refAssemblyA, refAssemblyB);
            _ = await Scene.Create(SceneRuntimeType.MainThread);
        }
    
        private System.Reflection.Assembly LoadAssembly(string assemblyName)
        {
            var dllBytes = File.ReadAllBytes($"Assets/AssetBundle/Hotfix/{assemblyName}.dll.bytes");
            var pdbBytes = File.ReadAllBytes($"Assets/AssetBundle/Hotfix/{assemblyName}.pdb.bytes");
            return System.Reflection.Assembly.Load(dllBytes, pdbBytes);
        }
    }
}

