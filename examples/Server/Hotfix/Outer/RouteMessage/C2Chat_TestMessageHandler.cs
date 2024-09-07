using Fantasy.Async;
using Fantasy.Network.Interface;

namespace Fantasy;

public sealed class C2Chat_TestMessageHandler : Route<ChatUnit, C2Chat_TestMessage>
{
    protected override async FTask Run(ChatUnit entity, C2Chat_TestMessage message)
    {
        Log.Debug($"C2Chat_TestMessageHandler.c2Chat_TestMessage: {message}");
        await FTask.CompletedTask;
    }
}