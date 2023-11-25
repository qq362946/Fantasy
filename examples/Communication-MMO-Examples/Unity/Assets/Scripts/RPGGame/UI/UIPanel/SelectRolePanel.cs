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

    public string josn;

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

            UIFacade.Instance.EnterScene(new MapScene());
        });

        btn_back.onClick.SetListener(() => {
            UIFacade.Instance.EnterScene(new AccountScene());

            // >>process account 大厅返回登录界面
            // GameManager.sender.Logout();
            // RPGManager.Instance.Back2LoginScene();
        });
    }
        
    public async FTask EnterMap()
    {
        // >>process map  进入地图
        // 进入地图请求(测试这个要先获取角色列表，不然网关账号缓存中没有角色)
        var enter = (G2C_EnterMapResponse) await GameManager.sender.Call(new C2G_EnterMapRequest(){
            MapNum = 12314, RoleId = 457749024540262403
        });
        Log.Info(enter.ToJson());
    }
        
    public async FTask LoadCharacters()
    {
        // >>process role 网络层请求加载账号下创建的角色
        // 获取角色列表
        var getRoles = (G2C_RoleListResponse) await GameManager.sender.Call(new C2G_RoleListRequest(){});
        Log.Info(getRoles.Items.ToJson());
    }

    public override void EnterPanel()
    {
        base.EnterPanel();

        var json = @"[
            {'AccountId':1,'RoleId':11232,'NickName':'骑士roubin','Class':'1PaladinMan'},
            {'AccountId':2,'RoleId':21122,'NickName':'幻想baobao','Class':'2SkeletonMage'},
            {'AccountId':3,'RoleId':31232,'NickName':'魔法baobao','Class':'3SkeletonWarrior'},
            {'AccountId':4,'RoleId':41123,'NickName':'黎明dada','Class':'3SkeletonWarrior'},
            ]";
            
        roles = JsonHelper.Deserialize<List<RoleInfo>>(json);
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
        
        // >>process role 请求网络层加载账号下创建的角色
        // GameManager.sender.LoadCharacters();

        // 默认选角相机位置
        mUIFacade.CamLocation(GameManager.Ins.location.select_camLocation);
        PreviewRole(roleIds[0]);
        uiTab.RefreshTab();
    }

    public override void ExitPanel()
    {
        base.ExitPanel();
        // 移除动态内容
        roleViewer.ClearPreview();
        uiTab.ClearTablist();
    }

    void PreviewRole(string roleId)
    {
        var prefab = roles.ToList().Find(p => p.RoleId == long.Parse(roleId));
        GameObject playerObj = roleViewer.ViewRole(prefab.Class,
            roleId,GameManager.Ins.location.select_spawnLoaction);
    }

}