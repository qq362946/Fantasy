using UnityEngine;
using Fantasy;

public partial class Response
{
    CreateRolePanel create;

    public void CreateRoleResponse(uint Error)
    {
        if(create ==null)
            create = UIFacade.Ins.GetUIPanel(StringManager.CreateRolePanel) as CreateRolePanel;


        // 角色名已经存在
        if (Error == ErrorCode.Error_RegisterAccountAlreayRegister)
        {
            create.ResetPanel();
            create.prompt.text = StringManager.CreateRoleFailed;
            return;
        }
        
        // 创建角色成功
        // >>process role 返回选角界面
        if(Error == ErrorCode.Success){
            create.prompt.text = StringManager.CreateRoleSuccess;
        } 
    }
}

