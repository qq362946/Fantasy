using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fantasy.Editor;
using Fantasy.Helper;
using Fantasy.Model;
using UnityEngine;
using UnityEngine.Serialization;

namespace Fantasy
{
    public class Entry : MonoBehaviour
    {
        private void Awake()
        {
            // Define.RemoteUpdatePath说明下
            // 一般是域名、为了开发方便编辑器下可以设置一个远程更新地址
            // 但打包线上还是要走域名比较好、也可以通过热更更换这个地址
#if UNITY_EDITOR || UNITY_EDITOR_64
            Define.EditorModel = FantasySettingsScriptableObject.Instance.editorModel;
            Define.RemoteUpdatePath = FantasySettingsScriptableObject.Instance.remoteUpdatePath;
#else
            Define.EditorModel = false;
            Define.RemoteUpdatePath = "http://127.0.0.1";
#endif
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
            // AssetBundle初始化
            await AssetBundleHelper.Initialize();
            // 加载界面、用于远程服务器更新资源
            await LoadingHelper.Start();
            // 加载Model程序集
            AssemblyLoadHelper.LoadModelDll();
            // 执行入口事件
            await EventSystem.Instance.PublishAsync(new OnAppStart()
            {
                ClientScene = Scene.Create("Unity")
            });
        }

        private void Update()
        {
            ThreadSynchronizationContext.Main.Update();
            SingletonSystem.Update(); 
        }

        private void OnApplicationQuit()
        {
            EventSystem.Instance?.Publish(new OnAppClosed());
            Application.Close();
        }
    }
}

