using Fantasy.Async;
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
        // 这个功能很重要，这个组件是整个Roaming系统最核心的组件，这个组件会处理Roaming协议
        // 这个功能会处理Roaming协议，所以创建这个是必须的。
        // CreateRoaming需要支持三个参数:
        // roamingId:这个参数是RoamingId，RoamingId是Roaming的唯一标识，不能重复。
        // 指定了这个RoamingId后，服务器其他漫游终端的Id会是你设置的RoamingId。
        // 这样操作方便统一管理漫游协议。
        // 一般这个RoamingId是一个角色的Id，这样方便管理。
        // isAutoDispose:是否在Session断开的时候自动断开漫游功能。
        // delayRemove:如果开启了自定断开漫游功能需要设置一个延迟多久执行断开。
        // 这里没有角色的Id，所以这里使用1来代替。
        // isAutoDispose我选择自动断开，这个断开的时机是Session断开后执行。
        // delayRemove断开漫游功能后，Session会自动断开，所以这里设置延迟1000毫秒执行断开。
        // 这里创建的漫游功能会自动处理Roaming协议，所以不需要手动处理Roaming协议。
        var roaming = session.CreateRoaming(1, true, 1000);
        // 通过SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0]拿到Map场景的配置信息
        // 如果需要协议漫游其他Scene可以在配置中查找要漫游的服务器。
        // 可以同时漫游多个Scene，但每个Scene的漫游都有一个固定的类型，不能重复。
        var mapConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];
        // 通过RoamingComponent.Link(session, mapConfig, 1, 1)链接Map场景
        // 第一个参数是Session，第二个参数是Map场景的配置信息，第三个参数是Map场景的RouteId，第四个参数是Map场景的RoamingType。
        // 这个RoamingType是通过RoamingType.Config文件中定义的。
        // RouteType.Config文件位置在你定义的网络文件协议文件夹下。如果找不到RoamingType.Config文件，可以运行下导出协议工具导出一个协议后会自动创建。
        // 该示例工程下文件位置在Config/NetworkProtocol/RoamingType.Config
        // 执行完后漫游会自动把Session绑定到Map场景上。
        // 后面发送该类型的消息到Session上会自动转发给Map场景。
        var linkResponse = await roaming.Link(session, mapConfig, RoamingType.MapRoamingType);
        if (linkResponse != 0)
        {
            response.ErrorCode = linkResponse;
            return;
        }
        // 同样，你可以创建多个漫游的场景，但每个场景的RouteId和RoamingType不能重复。
        // 这里创建Chat场景的漫游。
        var chatConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Chat)[0];
        linkResponse = await roaming.Link(session, chatConfig, RoamingType.ChatRoamingType);
        if (linkResponse != 0)
        {
            response.ErrorCode = linkResponse;
            return;
        }
        // 如果你觉的每次创建一个场景的漫游都麻烦，你可以利用RoamingType.RoamingTypes遍历创建。
        // 但这样的会把你在RoamingType.Config定义的都创建出来
        foreach (var roamingType in RoamingType.RoamingTypes)
        {
            // 这里添加roaming.Link的方法进行创建。
        }
    }
}