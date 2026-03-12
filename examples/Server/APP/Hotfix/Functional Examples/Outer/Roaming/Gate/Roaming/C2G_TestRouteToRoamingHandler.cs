using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;

namespace Fantasy;

public sealed class C2G_TestRouteToRoamingHandler : Message<C2G_TestRouteToRoaming>
{
    protected override async FTask Run(Session session, C2G_TestRouteToRoaming message)
    {
        // 首先可以通过 session.TryGetRoaming 获取RoamingComponent
        
        if (!session.TryGetRoaming(out var roamingComponent))
        {
            return;
        }

        // 发送消息需要两个参数:
        // 1. roamingType: 漫游的类型。可以在RoamingType.MapRoamingType获得
        // 2. message: 需要发送给漫游场景的消息。
        
        // 发送普通消息可以通过 roamingComponent.Send();
        // 发送RPC消息可以通过 roamingComponent.Call();
        
        // 例子
        // 下面发送一个RPCRoute消息给Map所在的服务器中。

        var response = (Map2G_TestRouteMessageResponse)await roamingComponent.Call(RoamingType.MapRoamingType,
            new G2Map_TestRouteMessageRequest()
            {
                Tag = message.Tag
            });
        if (response.ErrorCode != 0)
        {
            Log.Debug($"发送消息给漫游终端失败 ErrorCode:{response.ErrorCode}response.ErrorCode");
            return;
        }
    }
}