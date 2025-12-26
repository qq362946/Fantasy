using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy.Gate;

public sealed class C2G_TestRequestPushMessageHandler : Message<C2G_TestRequestPushMessage>
{
    protected override async FTask Run(Session session, C2G_TestRequestPushMessage message)
    {
        // 因为没有服务器的相关的逻辑，所以制作了一个协议来触发服务器发送消息给客户端的环境。
        // 使用当前会话的Session.Send发送消息给客户端。
        // 如果需要群发，你可以用一个容器保存起来，发送的时候遍历这个容器调用Send方法就可以了。
        session.Send(new G2C_PushMessage()
        {
            Tag = "Hi G2C_PushMessage"
        });
        await FTask.CompletedTask;
    }
}
