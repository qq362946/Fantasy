using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoleScene : BaseScene
{
    public RoleScene(){

    }

    public override void EnterScene()
    {
        mUIFacade.AddPanelToDict(StringManager.SelectRolePanel);
        mUIFacade.AddPanelToDict(StringManager.CreateRolePanel);

        
        base.EnterScene();
        
        // 打开SelectRolePanel
        mUIFacade.GetUIPanel(StringManager.SelectRolePanel).EnterPanel();
        mUIFacade.lastPanel = mUIFacade.GetUIPanel(StringManager.SelectRolePanel);
    }

}
