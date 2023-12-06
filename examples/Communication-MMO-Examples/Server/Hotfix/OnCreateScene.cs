using Fantasy;
using BestGame;

/// <summary>
/// 当Scene创建时需要干什么
/// </summary>
public class OnCreateScene : AsyncEventSystem<Fantasy.OnCreateScene>
{
    public override async FTask Handler(Fantasy.OnCreateScene self)
    {
        // Fantasy服务器是以Scene为单位的、所以Scene下有什么组件都可以自己添加定义
        // OnCreateScene这个事件就是给开发者使用的
        // 比如Address协议这里、我就是做了一个管理Address地址的一个组件挂在到Address这个Scene下面了
        // 比如Map下你需要一些自定义组件、你也可以在这里操作
        var scene = self.Scene;

        scene.AddComponent<GuiderComponent>();

        switch (scene.SceneType)
        {
            case SceneType.Addressable:
            {
                // 挂载管理Address地址组件
                scene.AddComponent<AddressableManageComponent>();
                break;
            }
            case SceneType.Mgr:
            {
                scene.AddComponent<ServerMgr>();
                break;
            }
            case SceneType.Gate:
            {
                // 网关sessionKey组件
                scene.AddComponent<SessionKeyComponent>();
                // 网关账号管理组件
                scene.AddComponent<GateAccountManager>();
                // 用户名缓存，检查
                scene.AddComponent<NameCheckComponent>();
                break;
            }
            case SceneType.Map:
            {
                // Unit管理组件
                scene.AddComponent<UnitManager>();
                // 添加AOI组件
                scene.AddComponent<AOIComponent>();
                break;
            }
            case SceneType.Realm:
            {
                // 认证帐号管理组件
                scene.AddComponent<AccountManager>();
                break;
            }
        }
        

        await FTask.CompletedTask;
    }
}