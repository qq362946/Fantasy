using Fantasy.Async;
using Fantasy.Event;
using Fantasy.Network.Roaming;

namespace Fantasy;

public sealed class OnCreateTerminus_LinkUnit : AsyncEventSystem<OnCreateTerminus>
{
    protected override async FTask Handler(OnCreateTerminus self)
    {
        var scene = self.Scene;
        var terminus = self.Terminus;

        switch (scene.SceneType)
        {
            case SceneType.Map:
            {
                if (self.Args != null)
                {
                    // 因为demo里还有其他漫游的测试用例，所以OnCreateTerminus事件会有多个地方监听。
                    // 因为逻辑不同会导致出错，所以这样参数不传递参数来过滤下
                    return;
                }
                // 创建一个新的Unit
                var playerUnit = PlayerUnitFactory.CreatePlayer(scene, "Fantasy");
                // 关联到Terminus后，下次接收消息会发送到playerUnit上
                await terminus.LinkTerminusEntity(playerUnit, true);
                // 添加到管理器中
                PlayerUnitManageHelper.AddPlayerUnit(playerUnit, PlayerUnitManageHelper.AddPlayerUnitNotify.SyncEveryone);
                break;
            }
            default:
            {
                Log.Error("暂不支持");
                break;
            }
        }
    }
}