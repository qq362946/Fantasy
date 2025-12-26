using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Examples.NormalMessage
{
    public sealed class G2C_PushMessageHandler : Message<G2C_PushMessage>
    {
        protected override async FTask Run(Session session, G2C_PushMessage message)
        {
            // 接收服务器推送过来的消息。
            Log.Debug($"接收服务器推送的消息 G2C_PushMessageHandler Tag:{message.Tag}");
            await FTask.CompletedTask;
        }
    }
}