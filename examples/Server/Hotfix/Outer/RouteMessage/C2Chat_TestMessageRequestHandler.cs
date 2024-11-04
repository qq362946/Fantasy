using System;
using Fantasy.Async;
using Fantasy.Network.Interface;

namespace Fantasy;

public sealed class C2Chat_TestMessageRequestHandler : RouteRPC<ChatUnit, C2Chat_TestMessageRequest, Chat2C_TestMessageResponse>
{
    protected override async FTask Run(ChatUnit entity, C2Chat_TestMessageRequest request, Chat2C_TestMessageResponse response, Action reply)
    {
        Log.Debug($"C2Chat_TestMessageRequestHandler request = {request}");
        response.Tag = "Hello RouteRPC";
        await FTask.CompletedTask;
    }
}