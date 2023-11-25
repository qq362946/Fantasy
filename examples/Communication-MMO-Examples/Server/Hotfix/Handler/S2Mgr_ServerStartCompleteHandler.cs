using Fantasy;

namespace BestGame;

/// <summary>
/// 计算、记录区服其它服务器起服完成数
/// </summary>
public class S2Mgr_ServerStartCompleteHandler : Route<Scene,S2Mgr_ServerStartComplete>
{
    protected override async FTask Run(Scene scene,S2Mgr_ServerStartComplete message)
    {
        var serverMgr = scene.GetComponent<ServerMgr>();
        serverMgr.Number++;

        var serverList = SceneHelper.GetSceneByWorld(scene.World.Id);
        var realmList = SceneHelper.GetSceneByWorld(scene.World.Id,SceneType.Realm);
        if(serverMgr.Number>=serverList.Count()){
            Log.Info("----------本区起服完成");
            foreach(var realm in realmList){
                // 给Mgr发消息
                Log.Debug($"realm.EntityId :{realm.EntityId}");
                MessageHelper.SendInnerRoute(scene,realm.EntityId,new Mgr2R_MachineStartFinished{});
            }
        }

        await FTask.CompletedTask;
    }
}