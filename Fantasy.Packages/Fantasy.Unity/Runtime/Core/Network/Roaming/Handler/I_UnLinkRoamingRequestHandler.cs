#if FANTASY_NET
using System;
using Fantasy.Async;
using Fantasy.InnerMessage;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;

namespace Fantasy.Roaming.Handler;

internal sealed class I_UnLinkRoamingRequestHandler : AddressRPC<Scene, I_UnLinkRoamingRequest, I_UnLinkRoamingResponse>
{
    protected override async FTask Run(Scene scene, I_UnLinkRoamingRequest request, I_UnLinkRoamingResponse response, Action reply)
    {
        await scene.TerminusComponent.RemoveTerminusAsync(
            DisposeTerminusType.UnLink, request.RoamingId, request.DisposeRoaming);
    }
}
#endif
