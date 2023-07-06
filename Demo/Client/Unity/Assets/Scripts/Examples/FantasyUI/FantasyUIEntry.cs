using System;
using Fantasy;
using Fantasy.Core;
using Fantasy.Helper;
using UnityEngine;

public class FantasyUIEntry : MonoBehaviour
{
    public Scene Scene;

    public void Awake()
    {
        // 框架初始化
        Scene = Fantasy.Entry.Initialize();
        AssemblyManager.Load(AssemblyName.AssemblyCSharp, GetType().Assembly);
        // 添加UI组件
        var uiComponent = Scene.AddComponent<UIComponent>();
        // 初始化UI组件
        uiComponent.Initialize(1920, 1080, addAudioListener: false);
        // 添加音效管理组件
        Scene.AddComponent<AudioManageComponent>();
        // 显示LoginUI
        uiComponent.AddComponent<LoginUI>();
    }
}