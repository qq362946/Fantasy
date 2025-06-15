using System.IO;
using Fantasy.Assembly;
using Fantasy.Async;
using UnityEngine;

namespace Fantasy
{
    public class RefAssemblyMain : MonoBehaviour
    {
        private Scene _scene;
        private void Start()
        {
            StartAsync().Coroutine();
        }
        
        private void OnDestroy()
        {
            // 当Unity关闭或当前脚本销毁的时候，销毁这个Scene。
            // 这样网络和Fantasy的相关功能都会销毁掉了。
            // 这里只是展示一下如何销毁这个Scene的地方。
            // 但这里销毁的时机明显是不对的，应该放到一个全局的地方。
            _scene?.Dispose();
        }

        private async FTask StartAsync()
        {
            var refAssemblyA = LoadAssembly("RefAssemblyA");
            var refAssemblyB = LoadAssembly("RefAssemblyB");
            await Fantasy.Platform.Unity.Entry.Initialize(GetType().Assembly);
            _scene = await Scene.Create(SceneRuntimeMode.MainThread);
            await AssemblySystem.LoadAssembly(refAssemblyA);
            await AssemblySystem.LoadAssembly(refAssemblyB);
            await _scene.EventComponent.PublishAsync(new OnCreateScene(_scene));
        }
    
        private System.Reflection.Assembly LoadAssembly(string assemblyName)
        {
            var dllBytes = File.ReadAllBytes($"Assets/AssetBundle/Hotfix/{assemblyName}.dll.bytes");
            var pdbBytes = File.ReadAllBytes($"Assets/AssetBundle/Hotfix/{assemblyName}.pdb.bytes");
            return System.Reflection.Assembly.Load(dllBytes, pdbBytes);
        }
    }
}

