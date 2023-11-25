using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AccountScene : UIScene
{
    public AccountScene(){ 

    }

    public override void EnterScene()
    {
        mUIFacade.AddPanelToDict(StringManager.LoginPanel);
        mUIFacade.AddPanelToDict(StringManager.RegisterPanel);
        mUIFacade.AddPanelToDict(StringManager.ServerListPanel);
        base.EnterScene();

        // 打开LoginPanel
        mUIFacade.GetUIPanel(StringManager.LoginPanel).EnterPanel();
        mUIFacade.lastPanel = mUIFacade.GetUIPanel(StringManager.LoginPanel);
        Camera.main.GetComponent<Animator>().enabled = true;
    }

    public override void ExitScene()
    {
        base.ExitScene();

        // 关闭登录注册界面,相机动画
        Camera.main.GetComponent<Animator>().enabled = false;
    }
}
