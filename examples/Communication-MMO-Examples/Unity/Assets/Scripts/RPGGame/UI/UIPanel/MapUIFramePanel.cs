using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 选角面板
/// </summary>
public class MapUIFramePanel : BasePanel
{
    public Button btn_setting;

    public GameObject chatContent;
    
    // public UIAbility uIAbility;
    // public UIMessageSlot messageSlot;
    public KeyCode[] activationKeys = {KeyCode.M, KeyCode.M};
    public GameObject bigMapPanel;
    protected override void Awake()
    {
        base.Awake();
    }

    void Start(){
        btn_setting.onClick.SetListener(() => {
            mUIFacade.GetUIPanel(StringManager.SettingPanel).EnterPanel();     
        });
    }

    void Update(){
        if (Utils.AnyKeyDown(activationKeys)){
            if(!bigMapPanel.activeSelf) bigMapPanel.SetActive(true);
            else bigMapPanel.SetActive(false);
        }
    }
}