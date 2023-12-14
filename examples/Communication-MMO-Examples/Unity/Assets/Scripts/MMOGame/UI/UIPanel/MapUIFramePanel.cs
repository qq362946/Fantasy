using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 地图场景框架面板
/// </summary>
public class MapUIFramePanel : BasePanel
{
    public Button btn_setting;
    public GameObject chatContent;
    
    public KeyCode[] activationKeys = {KeyCode.M, KeyCode.M};
    public GameObject bigMapPanel;
    

    void Start(){
        btn_setting.onClick.SetListener(() => {
            mUIFacade.GetUIPanel(StringManager.SettingPanel).EnterPanel();     
        });
    }

    public override void ExitPanel()
    {
        base.ExitPanel();
        // 重置相机
        GameFacade.Ins.ReSetMMOCamera();
    }

    void Update(){
        if (Utils.AnyKeyDown(activationKeys)){
            if(!bigMapPanel.activeSelf) bigMapPanel.SetActive(true);
            else bigMapPanel.SetActive(false);
        }
    }
}