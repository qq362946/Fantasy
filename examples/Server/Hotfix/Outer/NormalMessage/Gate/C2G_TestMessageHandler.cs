using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy.Gate;

public sealed class C2G_TestMessageHandler : Message<C2G_TestMessage>
{
    protected override async FTask Run(Session session, C2G_TestMessage message)
    {
        Log.Debug($"Receive C2G_TestMessage Tag={message.Tag}");
        await FTask.CompletedTask;
    }
}