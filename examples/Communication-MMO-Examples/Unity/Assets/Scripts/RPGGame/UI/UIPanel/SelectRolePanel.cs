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
    
    [HideInInspector]public RoleViewer roleViewer;
    public List<RoleInfo> roles;
    private UITab uiTab;
    private long selectRoleId;


    protected override void Awake()
    {
        base.Awake();
        roleViewer = gameObject.AddComponent<RoleViewer>();
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
            UIFacade.Instance.EnterScene(new AccountScene());

            // >>process account 大厅返回登录界面
            // GameManager.sender.Logout();
            // RPGManager.Instance.Back2LoginScene();
        });
    }
        
    public override void EnterPanel()
    {
        base.EnterPanel();
        mUIFacade.CamLocation(GameManager.Ins.location.select_camLocation);

        // >>process role 网络层请求加载账号下创建的角色
        CallRoles().Coroutine(); 
    }

    public override void ExitPanel()
    {
        base.ExitPanel();
        // 移除动态内容
        roleViewer.ClearPreview();
        uiTab?.ClearTablist();
    }

    public async FTask EnterMap()
    {
        var response = (G2C_EnterMapResponse) await GameManager.sender.Call(new C2G_EnterMapRequest(){
            RoleId = selectRoleId
        });

        // 角色是以`UV+RoleId+职业类名`作为key缓存的,也作为go.name
        Player.localName = "UV"+response.RoleInfo.RoleId+response.RoleInfo.Class;
        
        UIFacade.Instance.EnterScene(new MapScene());
    }
        
    public async FTask CallRoles()
    {
        var response =  (G2C_RoleListResponse) await GameManager.sender.Call(new C2G_RoleListRequest(){});
        
        // 按创建时间排序
        roles = response.Items.OrderByDescending(role => role.CreatedTime).ToList();
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
        var prefab = roles.ToList().Find(p => p.RoleId == selectRoleId);
        
        GameObject go = roleViewer.ViewRole(prefab.Class,
            roleId,GameManager.Ins.location.select_spawnLoaction);
    }

}