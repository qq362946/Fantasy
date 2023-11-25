using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fantasy;

/// <summary>
/// 新建角色面板
/// </summary>
public class CreateRolePanel : BasePanel
{
    public Text prompt;
    public Button btn_Submit;
    public Button btn_back;
    public Text info;
    public InputField inputName;

    // 玩家职业类
    private Player[] classes;
    [HideInInspector]public ClassViewer classViewer;
    private UITab uiTab;

    protected override void Awake()
    {
        base.Awake();
        backToPanel = StringManager.SelectRolePanel;

        classViewer = gameObject.AddComponent<ClassViewer>();
        classes = GameManager.Ins.playerClasses.ToArray();
        Log.Info("classes: " + classes.Length+" | "+classes[0].ClassName);
    }

    void Start()
    {
        btn_back.onClick.SetListener(() => {
            // >>process role 返回选角界面
            base.BackToPanel();
        });

        btn_Submit.onClick.SetListener(() => {
            string nickName = Regex.Replace(inputName.text, @"\s", "");
            if(nickName =="") return;

            Player player = classViewer.current.GetComponent<Player>();

            // >>process role 请求网络层创建角色
            // GameManager.sender.CreateCharacter(nickName,player.ClassName);
        });
    }

    public async FTask CreateRole(string name,int role){
        // >>process role 网络层请求创建角色
        // 创建角色请求
        var create = (G2C_RoleCreateResponse) await GameManager.sender.Call(new C2G_RoleCreateRequest(){
            NickName = "roubin2",Sex = 1,Class = "Magic", UnitConfigId = 12012
        });
        Log.Info(create.RoleInfo.ToJson());
    }

    public override void EnterPanel()
    {
        base.EnterPanel();

        var classNames = classes.Select(player => player.ClassName).ToArray();
        
        // UITab组件创建职业列表
        uiTab = GetComponent<UITab>();
        GameObject[] list = uiTab.CreateTabList(classes.Length,Vector2.down,55f,
                                                classNames,PreviewClass);
        // 自定义tab属性
        for(int i=0;i<list.Length;i++){
            UIRoleClassSlot slot = list[i].GetComponent<UIRoleClassSlot>();
            slot.nameText.text = classes[i].nickName;
            slot.image.sprite = classes[i].portraitIcon;
        }

        // 默认预览职业与预览相机位置
        mUIFacade.CamLocation(GameManager.Ins.location.create_camLoaction);
        PreviewClass(classes[0].ClassName);
        uiTab.RefreshTab();
    }

    public override void ExitPanel()
    {
        base.ExitPanel();

        // 移除动态内容
        classViewer.ClearPreview();
        uiTab.ClearTablist();
    }

    void PreviewClass(string className)
    {
        classViewer.ViewClass(className,GameManager.Ins.location.create_spawnLoaction);
        info.text = classViewer.current.gameObject.GetComponent<Player>().toolTip;
    }
    
    public override void ResetPanel()
    {
        inputName.text = "";
    }

}