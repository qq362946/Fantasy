using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Model.Roaming;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;
using Fantasy.Platform.Net;

namespace Fantasy;

public sealed class C2G_LoginRoamingHandler : MessageRPC<C2G_LoginRoamingRequest, G2C_LoginRoamingResponse>
{
    protected override async FTask Run(Session session, C2G_LoginRoamingRequest request, G2C_LoginRoamingResponse response, Action reply)
    {
        // 给session创建一个漫游功能。
        var roamingComponent = await session.GetOrCreateRoaming(1, 10000);
        // 连接一个漫游类型
        var mapConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];
        var mapSceneAddress = mapConfig.Address;
        var linkResponse = await roamingComponent.Link(mapSceneAddress, session.Address, RoamingType.MapRoamingType);
        if (linkResponse != 0)
        {
            response.ErrorCode = linkResponse;
            Log.Debug($"Map漫游创建失败 ErrorCode:{linkResponse}");
            return;
        }
        Log.Debug("Map漫游创建成功");
    }
}