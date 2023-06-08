using Fantasy.Core;
using Fantasy.Core.Network;

namespace Fantasy.Hotfix.Handler;

public sealed class H_C2L_TestMessageHandler : Message<H_C2L_TestMessage>
{
    protected override async FTask Run(Session session, H_C2L_TestMessage message)
    {
        Log.Debug($"H_C2L_TestMessageHandler Server:{session.Scene.RouteId} {message.ToJson()}");

        await FTask.CompletedTask;
    }
}