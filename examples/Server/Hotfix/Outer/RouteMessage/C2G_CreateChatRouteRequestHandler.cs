namespace Fantasy;

public sealed class C2G_CreateChatRouteRequestHandler : MessageRPC<C2G_CreateChatRouteRequest, G2C_CreateChatRouteResponse>
{
    protected override async FTask Run(Session session, C2G_CreateChatRouteRequest request, G2C_CreateChatRouteResponse response, Action reply)
    {
        // 首先需要找到一个需要建立Route的Scene的SceneConfig。
        // 例子演示的连接的ChatScene,所以这里我通过SceneConfigData拿到这个SceneConfig。
        // 如果是其他Scene，用法跟这个没有任何区别。
        var chatSceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Chat)[0];
        // 通过chatSceneConfig拿到这个Scene的RouteId
        var chatRouteId = chatSceneConfig.RouteId;
        // 通过Scene拿到当前Scene的NetworkMessagingComponent。
        // NetworkMessagingComponent是服务器之间通讯的唯一手段。
        var networkMessagingComponent = session.Scene.NetworkMessagingComponent;
        // 通过CallInnerRoute方法发送一个RPC消息给ChatScene上。
        // 任何一个实体的RunTimeId都可以做为RouteId使用。
        // 所以这个传递了一个session.RunTimeId，是方便Chat发送消息回Gate上。
        var routeResponse = (Chat2G_CreateRouteResponse)await networkMessagingComponent.CallInnerRoute(chatRouteId,
            new G2Chat_CreateRouteRequest()
            {
                GateRouteId = session.RunTimeId
            });
        if (routeResponse.ErrorCode != 0)
        {
            // 如果ErrorCode不是0表示请求的协议发生错误，应该提示给客户端。
            // 这里就不做这个了。
            return;
        }
        // 要实现Route协议的转发，需要给Session添加一个RouteComponent，这个非常重要。
        var routeComponent = session.AddComponent<RouteComponent>();
        // 需要再Examples/Config/NetworkProtocol/RouteType.Config里添加一个ChatRoute
        // 然后点击导表工具，会自动生成一个RouteType.cs文件。
        // 使用你定义的ChatRoute当routeType的参数传递进去。
        // routeResponse会返回一个ChatRouteId，这个就是Chat的RouteId。
        routeComponent.AddAddress((int)RouteType.ChatRoute,routeResponse.ChatRouteId);
        // 这些操作完成后，就完成了Route消息的建立。
        // 后面可以直接发送Route消息通过Gate自动中转给Chat了。
    }
}