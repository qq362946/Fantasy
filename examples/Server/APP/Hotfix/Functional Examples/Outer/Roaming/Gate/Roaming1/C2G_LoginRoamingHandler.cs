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
        var result = await session.TryCreateRoaming(1, 10000);
        // 根据返回的类型来做不同的处理
        switch (result.Status)
        {
            // 新创建的漫游
            case CreateRoamingStatus.NewCreated:        
            {
                var mapConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];
                var linkResponse = await result.Roaming.Link(session, mapConfig, RoamingType.MapRoamingType);
                if (linkResponse != 0)
                {
                    response.ErrorCode = linkResponse;
                    Log.Debug($"Map漫游创建失败 ErrorCode:{linkResponse}");
                    return;
                }
                Log.Debug("Map漫游创建成功");
                break;
            }
            // 已存在的漫游
            case CreateRoamingStatus.AlreadyExists:     
            {
                Log.Debug("使用已存在的漫游组件");
                var mapConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];
                var linkResponse = await result.Roaming.Link(session, mapConfig, RoamingType.MapRoamingType);
                if (linkResponse != 0)
                {
                    response.ErrorCode = linkResponse;
                    Log.Debug($"Map漫游创建失败 ErrorCode:{linkResponse}");
                    return;
                }
                Log.Debug("Map漫游重连成功");
                break;
            }
            // 存在的但roamingId不同的漫游
            case CreateRoamingStatus.SessionAlreadyHasRoaming:      
            {
                Log.Debug("错误：当前Session已经创建了不同roamingId的漫游组件");
                break;
            }
        }
    }
}