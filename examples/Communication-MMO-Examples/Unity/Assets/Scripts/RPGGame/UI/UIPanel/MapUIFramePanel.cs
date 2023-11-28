using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 地图场景框架面板
/// </summary>
public class MapUIFramePanel : BasePanel
{
    public Button btn_setting;

    public GameObject chatContent;

    [HideInInspector]public UnitViewer unitViewer;
    
    // public UIAbility uIAbility;
    // public UIMessageSlot messageSlot;
    public KeyCode[] activationKeys = {KeyCode.M, KeyCode.M};
    public GameObject bigMapPanel;
    protected override void Awake()
    {
        base.Awake();
        unitViewer = gameObject.AddComponent<UnitViewer>();
    }

    void Start(){
        btn_setting.onClick.SetListener(() => {
            mUIFacade.GetUIPanel(StringManager.SettingPanel).EnterPanel();     
        });
    }

    public override void ExitPanel()
    {
        base.ExitPanel();
        // 重置相机
        mUIFacade.ReSetMMOCamera();
        // 移除unitViewer动态内容
        unitViewer.ClearPreview();
    }

    public GameObject AddUnit2Viewer(string className,string roleId,Transform point){
        return unitViewer.ViewUnit(className,roleId,point);
    }

    void Update(){
        if (Utils.AnyKeyDown(activationKeys)){
            if(!bigMapPanel.activeSelf) bigMapPanel.SetActive(true);
            else bigMapPanel.SetActive(false);
        }
    }
}