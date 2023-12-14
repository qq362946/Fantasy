using System;
using System.Collections.Generic;
using Fantasy;
using UnityEngine;

using BestGame;

public class Entry : MonoBehaviour
{
    void Start()
    {
        // 框架初始化
        Sender.Ins.GameNetStart();

        // 把当前工程的程序集装载到框架中、这样框架才会正常的操作
        // 装载后例如网络协议等一些框架提供的功能就可以使用了
        AssemblyManager.Load(AssemblyName.AssemblyCSharp, GetType().Assembly);

        // 进入登录场景
        UIFacade.Ins.EnterScene(new AccountScene());
        UIFacade.Ins.lastScene = UIFacade.Ins.currentScene;
    }
}
