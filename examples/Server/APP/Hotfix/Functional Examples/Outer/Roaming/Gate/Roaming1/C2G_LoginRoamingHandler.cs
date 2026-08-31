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
        // 首次连接选择Map，重连使用漫游连接中保存的目标Scene地址。
        var linkResuilt = roamingComponent.IsLinked(RoamingType.MapRoamingType)
            ? await roamingComponent.Link(RoamingType.MapRoamingType)
            : await roamingComponent.Link(
                SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0].Address,
                RoamingType.MapRoamingType);
        if (linkResuilt != 0)
        {
            response.ErrorCode = linkResuilt;
            Log.Debug($"Map漫游创建失败 ErrorCode:{linkResuilt}");
            return;
        }
        Log.Debug("Map漫游创建成功");
    }
}
