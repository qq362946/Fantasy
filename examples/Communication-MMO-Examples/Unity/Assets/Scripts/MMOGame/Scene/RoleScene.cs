public class RoleScene : BaseScene
{
    public override void EnterScene()
    {
        base.EnterScene();
        // 添加UIPanel
        mUIFacade.AddPanelToDict(StringManager.SelectRolePanel);
        mUIFacade.AddPanelToDict(StringManager.CreateRolePanel);
        mUIFacade.InitUIPanelDict();
        
        // 打开SelectRolePanel
        var selectRolePanel = mUIFacade.GetUIPanel(StringManager.SelectRolePanel);
        selectRolePanel.EnterPanel();
        mUIFacade.lastPanel = selectRolePanel;
    }

}
