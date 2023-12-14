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

        btn_Submit.onClick.SetListener(async() => {
            string nickName = Regex.Replace(inputName.text, @"\s", "");
            if(nickName =="") return;

            Player player = classViewer.current.GetComponent<Player>();

            // >>process role 请求网络层创建角色
            await CreateRole(nickName,player.ClassName,player.ConfigId);
        });
    }

    public override void EnterPanel()
    {
        base.EnterPanel();
        // 默认预览职业与预览相机位置
        GameFacade.Ins.CamLocation(GameManager.Ins.location.create_camLoaction);

        CreatePreview();
    }

    public override void ExitPanel()
    {
        base.ExitPanel();

        // 移除动态内容
        classViewer.ClearPreview(PoolType.Unit);
        uiTab?.ClearTablist();
    }

    public async FTask CreateRole(string name,string className,int configId = 0)
    {
        var response = (G2C_CreateRoleResponse) await Sender.Ins.Call(new C2G_CreateRoleRequest(){
            NickName = name,Sex = 1,Class = className, UnitConfigId = configId
        });

        Response.Ins.CreateRoleResponse(response.ErrorCode);
        Log.Info("创建角色成功:"+response.RoleInfo.ToJson());

        if(response.ErrorCode ==ErrorCode.Success)
        {
            // >>process role 创建角色成功返回选角界面
            await TimerScheduler.Instance.Core.WaitAsync(3000);
            BackToPanel();
        }
    }

    void CreatePreview()
    {
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

        // 预览职业
        PreviewClass(classes[0].ClassName);
        uiTab.RefreshTab();
    }

    void PreviewClass(string className)
    {
        var go = classViewer.ViewClass(className,GameManager.Ins.location.create_spawnLoaction);
        info.text = classViewer.current.gameObject.GetComponent<Player>().toolTip;

        // ==> 激活职业预览角色控制器
        go.GetComponent<GameEntity>().EnableController();
    }
    
    public override void ResetPanel()
    {
        inputName.text = "";
    }

}