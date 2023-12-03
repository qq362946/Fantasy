using UnityEngine;
using UnityEngine.UI;
public class SettingPanel : BasePanel
{
    public Button btn_back;
    public Button btn_Logout;
    protected override void Awake()
    {
        base.Awake();
        isPart = true;
    }

    void Start(){
        btn_Logout.onClick.SetListener(() => {
            UIFacade.Ins.EnterScene(new AccountScene());

            // >>process map 退出地图返回登录大厅
            // GameManager.sender.QuitWorld();
        });
        
        btn_back.onClick.SetListener(() => {
           base.BackToPanel();
        });
    }
}