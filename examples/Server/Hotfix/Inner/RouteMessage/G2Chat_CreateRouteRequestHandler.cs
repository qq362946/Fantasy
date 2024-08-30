namespace Fantasy;

public sealed class G2Chat_CreateRouteRequestHandler : RouteRPC<Scene, G2Chat_CreateRouteRequest, Chat2G_CreateRouteResponse>
{
    protected override async FTask Run(Scene scene, G2Chat_CreateRouteRequest request, Chat2G_CreateRouteResponse response, Action reply)
    {
        // 接收到Gate消息后，首先建立一个实体用来后面接收Route消息。
        // 这里就拿ChatUnit来做这个。
        var chatUnit = Entity.Create<ChatUnit>(scene, true, true);
        // 把Gate传递过来的RouteId保存住，以便后面可以直接给Gate发送消息。
        // 例如断线等操作，都可以通过这个GateRouteId发送到Gate的Session上。
        chatUnit.GateRouteId = request.GateRouteId;
        // 把chatUnit的RunTimeId发送给Gate。
        // 正如之前所说任何实体的RunTimeId都可以当做RouteId使用。
        response.ChatRouteId = chatUnit.RunTimeId;
        await FTask.CompletedTask;
    }
}