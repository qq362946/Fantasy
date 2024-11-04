using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Examples.RouteMessage
{
    public sealed class Chat2C_PushMessageHandler : Message<Chat2C_PushMessage>
    {
        protected override async FTask Run(Session session, Chat2C_PushMessage message)
        {
            // 接收服务器推送过来的消息。
            Log.Debug($"接收Chat服务器推送的Route消息 Chat2C_PushMessage Tag:{message.Tag}");
            await FTask.CompletedTask;
        }
    }
}