using Fantasy.Async;
using Fantasy.Network.Interface;

namespace Fantasy;

public sealed class C2Chat_TestRequestPushMessageHandler : Route<ChatUnit, C2Chat_TestRequestPushMessage>
{
    protected override async FTask Run(ChatUnit chatUnit, C2Chat_TestRequestPushMessage message)
    {
        // 因为没有服务器的相关的逻辑，所以制作了一个协议来触发服务器发送消息给客户端的环境。
        // 使用当前Scene.NetworkMessagingComponent.SendInnerRoute发送消息给客户端。
        // 只需要把消息发送给创建链接的Gate上就会自动转发消息到客户端上。
        // 因为chatUnit.GateRouteId是在G2Chat_CreateRouteRequestHandler方法里记录的所以直接使用这个就可以了。

        chatUnit.Scene.NetworkMessagingComponent.SendInnerRoute(chatUnit.GateRouteId, new Chat2C_PushMessage()
        {
            Tag = "Hi Route Chat2C_PushMessage"
        });

        await FTask.CompletedTask;
    }
}