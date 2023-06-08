using Fantasy.Helper;
using UnityEngine;
using UnityEngine.Serialization;
// ReSharper disable MethodHasAsyncOverload

namespace Fantasy
{
    public class FantasyInitialize : MonoBehaviour
    {
        [FormerlySerializedAs("EditorModel")] 
        public bool editorModel;
        [FormerlySerializedAs("RemoteUpdatePath")] 
        public string remoteUpdatePath;

        private void Awake()
        {
            if (remoteUpdatePath.LastIndexOf('/') != remoteUpdatePath.Length - 1)
            {
                Log.Error($"RemotePath:{remoteUpdatePath} Must be used/ended");
                return;
            }

            Define.EditorModel = editorModel;
            Define.RemoteUpdatePath = remoteUpdatePath;
        }

        private void Start()
        {
            StartAsync().Coroutine();
        }

        private async FTask StartAsync()
        {
            DontDestroyOnLoad(gameObject);
            // 初始化框架
            Application.Initialize(AssemblyName.Core);
            // 初始化程序集
            await AssemblyLoadHelper.Initialize();
            // 执行入口事件
            await EventSystem.Instance.PublishAsync(new OnAppStart()
            {
                Scene = Scene.Create("Unity")
            });
        }

        private void Update()
        {
            ThreadSynchronizationContext.Main.Update();
            SingletonSystem.Update(); 
        }

        private void OnApplicationQuit()
        {
            EventSystem.Instance.Publish(new OnAppClosed());
            Application.Close();
        }
    }
}

