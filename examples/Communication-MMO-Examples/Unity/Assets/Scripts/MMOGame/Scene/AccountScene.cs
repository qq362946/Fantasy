using UnityEngine;

public class AccountScene : BaseScene
{
    public override void EnterScene()
    {
        base.EnterScene();
        // 添加UIPanel
        mUIFacade.AddPanelToDict(StringManager.LoginPanel);
        mUIFacade.AddPanelToDict(StringManager.RegisterPanel);
        mUIFacade.AddPanelToDict(StringManager.ServerListPanel);
        mUIFacade.InitUIPanelDict();
        

        // 打开LoginPanel
        var loginPanel = mUIFacade.GetUIPanel(StringManager.LoginPanel);
        loginPanel.EnterPanel();
        mUIFacade.lastPanel = loginPanel;

        // 打开登录注册界面,相机动画
        Camera.main.GetComponent<Animator>().enabled = true;
    }

    public override void ExitScene()
    {
        base.ExitScene();

        // 关闭登录注册界面,相机动画
        Camera.main.GetComponent<Animator>().enabled = false;
    }
}
