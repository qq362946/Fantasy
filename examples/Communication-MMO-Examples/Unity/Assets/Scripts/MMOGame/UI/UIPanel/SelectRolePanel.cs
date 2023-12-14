using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Fantasy;

/// <summary>
/// 选角面板
/// </summary>
public class SelectRolePanel : BasePanel
{
    public Button btn_enterMap;
    public Button btn_Create;
    public Button btn_back;
    
    [HideInInspector]public UnitViewer roleViewer;
    public List<RoleInfo> roles;
    private UITab uiTab;
    private long selectRoleId;


    protected override void Awake()
    {
        base.Awake();
        roleViewer = gameObject.AddComponent<UnitViewer>();
    }

    void Start()
    {
        btn_Create.onClick.SetListener(() => {
            // >>process role  创建角色界面
            mUIFacade.GetUIPanel(StringManager.CreateRolePanel).EnterPanel();     
        });

        btn_enterMap.onClick.SetListener(async () => {
            // >>process map  进入地图
            await EnterMap();
        });

        btn_back.onClick.SetListener(() => {
            UIFacade.Ins.EnterScene(new AccountScene());

            // >>process account 大厅返回登录界面
            // GameManager.sender.Logout();
            // RPGManager.Instance.Back2LoginScene();
        });
    }
        
    public override void EnterPanel()
    {
        base.EnterPanel();
        GameFacade.Ins.CamLocation(GameManager.Ins.location.select_camLocation);

        // >>process role 网络层请求加载账号下创建的角色
        CallRoles().Coroutine(); 
    }

    public override void ExitPanel()
    {
        base.ExitPanel();
        // 移除动态内容
        roleViewer.ClearPreview(PoolType.Unit);
        uiTab?.ClearTablist();
    }

    public async FTask EnterMap()
    {
        var response = (G2C_EnterMapResponse) await Sender.Ins.Call(new C2G_EnterMapRequest(){
            RoleId = selectRoleId
        });

        UIFacade.Ins.EnterScene(new MapScene());

        // 创建本地玩家unit
        GameFacade.PlayerUnits.AddLocalUnit2Scene(response.RoleInfo);
    }
        
    public async FTask CallRoles()
    {
        var response =  (G2C_RoleListResponse) await Sender.Ins.Call(new C2G_RoleListRequest(){});
        
        // 按创建时间排序
        roles = response.RoleInfos.OrderByDescending(role => role.CreatedTime).ToList();
        Log.Info("取得角色列表: "+roles.ToJson());

        if(roles == null||roles.Count==0) return;

        CreatePreview();
    }

    void CreatePreview()
    {
        var roleNames = roles.Select(role => role.NickName).ToArray();
        var roleIds = roles.Select(role => role.RoleId.ToString()).ToArray(); // 池缓存keys

        // UITab组件创建职业列表
        uiTab = GetComponent<UITab>();
        GameObject[] list = uiTab.CreateTabList(roles.Count,Vector2.down,30f,
                                                roleIds,PreviewRole);
        // 自定义tab属性
        for(int i=0;i<list.Length;i++){
            UIRoleNameSlot slot = list[i].GetComponent<UIRoleNameSlot>();
            slot.nameText.text = roleNames[i];
        }

        // 预览角色
        PreviewRole(roleIds[0]);
        uiTab.RefreshTab();
    }

    void PreviewRole(string roleId)
    {
        selectRoleId = long.Parse(roleId);
        GameManager.Ins.RoleId = selectRoleId;
        
        var roleInfo = roles.ToList().Find(p => p.RoleId == selectRoleId);
        
        GameObject go = roleViewer.ViewUnit(roleInfo.ClassName,
            roleId,GameManager.Ins.location.select_spawnLoaction);

        // ==> 激活角色控制器
        go.GetComponent<GameEntity>().EnableController();
    }

}