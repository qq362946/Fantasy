using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy;

public sealed class C2G_TestMemoryPackHandler : MessageRPC<C2G_TestMemoryPackRequest,G2C_TestMemoryPackResponse>
{
    protected override async FTask Run(Session session, C2G_TestMemoryPackRequest request, G2C_TestMemoryPackResponse response, Action reply)
    {
        reply();
        await FTask.CompletedTask;
    }
}