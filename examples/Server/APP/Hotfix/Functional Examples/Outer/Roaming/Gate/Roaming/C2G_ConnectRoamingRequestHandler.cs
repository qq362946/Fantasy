using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Model.Roaming;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;
using Fantasy.Platform.Net;
using Fantasy.Roaming;

namespace Fantasy;

public sealed class C2G_ConnectRoamingRequestHandler : MessageRPC<C2G_ConnectRoamingRequest, G2C_ConnectRoamingResponse>
{
    protected override async FTask Run(Session session, C2G_ConnectRoamingRequest request, G2C_ConnectRoamingResponse response, Action reply)
    {
        // 给session创建一个漫游功能。
        var result = await session.TryCreateRoaming(1, 10000);

        switch (result.Status)
        {
            // 新创建的漫游
            case CreateRoamingStatus.NewCreated:        
            {
                // 通过SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0]拿到Map场景的配置信息
                // 如果需要协议漫游其他Scene可以在配置中查找要漫游的服务器。
                // 可以同时漫游多个Scene，但每个Scene的漫游都有一个固定的类型，不能重复。
                // 这个RoamingType是通过RoamingType.Config文件中定义的。
                // RouteType.Config文件位置在你定义的网络文件协议文件夹下。如果找不到RoamingType.Config文件，可以运行下导出协议工具导出一个协议后会自动创建。
                // 该示例工程下文件位置在Config/NetworkProtocol/RoamingType.Config
                // 执行完后漫游会自动把Session绑定到Map场景上。
                // 后面发送该类型的消息到Session上会自动转发给Map场景。
                var mapConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];
                using var args = Entity.Create<MaoRoamingArgs>(session.Scene);
                args.Tag = "HI";
                var linkResponse = await result.Roaming.Link(session, mapConfig, RoamingType.MapRoamingType, args);
                if (linkResponse != 0)
                {
                    response.ErrorCode = linkResponse;
                    Log.Debug($"Map漫游创建失败 ErrorCode:{linkResponse}");
                    return;
                }
                // 同样，你可以创建多个漫游的场景，但每个场景的AddressId和RoamingType不能重复。
                // 这里创建Chat场景的漫游。
                var chatConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Chat)[0];
                linkResponse = await result.Roaming.Link(session, chatConfig, RoamingType.ChatRoamingType);
                if (linkResponse != 0)
                {
                    response.ErrorCode = linkResponse;
                    Log.Debug($"Chat漫游创建失败 ErrorCode:{linkResponse}");
                    return;
                }
                // 如果你觉的每次创建一个场景的漫游都麻烦，你可以利用RoamingType.RoamingTypes遍历创建。
                // 但这样的会把你在RoamingType.Config定义的都创建出来
                foreach (var roamingType in RoamingType.RoamingTypes)
                {
                    // 这里添加roaming.Link的方法进行创建。
                }
                Log.Debug("漫游创建成功");
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
                var chatConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Chat)[0];
                linkResponse = await result.Roaming.Link(session, chatConfig, RoamingType.ChatRoamingType);
                if (linkResponse != 0)
                {
                    response.ErrorCode = linkResponse;
                    Log.Debug($"Chat漫游创建失败 ErrorCode:{linkResponse}");
                    return;
                }
                Log.Debug("漫游创建成功1111");
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