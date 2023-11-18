using Fantasy.Core.Network;
using Fantasy;

namespace BestGame;

/// <summary>
/// 起服完成后，给Mgr发消息
/// </summary>
public class OnServerStartComplete : AsyncEventSystem<Fantasy.OnServerStartComplete>
{
    public override async FTask Handler(Fantasy.OnServerStartComplete self)
    {
        // mgr每区服只有1个，用List[0]取得
        var mgrScene = SceneHelper.GetSceneByWorld(self.Server.Scene.World.Id,SceneType.Mgr)[0];

        // 给Mgr发消息
        MessageHelper.SendInnerRoute(self.Server.Scene,mgrScene.EntityId,new S2Mgr_ServerStartComplete{});
        

        await FTask.CompletedTask;
    }
}