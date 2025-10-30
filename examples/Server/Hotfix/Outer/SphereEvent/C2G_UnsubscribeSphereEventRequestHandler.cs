using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Fantasy;

public sealed class C2G_UnsubscribeSphereEventRequestHandler : MessageRPC<C2G_UnsubscribeSphereEventRequest, G2C_UnsubscribeSphereEventResponse>
{
    protected override FTask Run(Session session, C2G_UnsubscribeSphereEventRequest request, G2C_UnsubscribeSphereEventResponse response, Action reply)
    {
        throw new NotImplementedException();
    }
}