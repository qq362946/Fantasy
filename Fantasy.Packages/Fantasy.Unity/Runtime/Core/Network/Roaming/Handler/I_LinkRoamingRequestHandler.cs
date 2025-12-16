#if FANTASY_NET
using System;
using Fantasy.Async;
using Fantasy.InnerMessage;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.Roaming.Handler;

internal sealed class I_LinkRoamingRequestHandler : AddressRPC<Scene, I_LinkRoamingRequest, I_LinkRoamingResponse>
{
    protected override async FTask Run(Scene scene, I_LinkRoamingRequest request, I_LinkRoamingResponse response, Action reply)
    {
        var (errorCode, roamingTerminal) = scene.TerminusComponent.Create(
            request.RoamingId, request.RoamingType,
            request.ForwardSessionAddress, request.SceneAddress);
        
        if (errorCode != 0)
        {
            response.ErrorCode = errorCode;
            return;
        }

        var requestArgs = request.Args;
        response.TerminusId = roamingTerminal.TerminusId;
        reply();
        await scene.EventComponent.PublishAsync(new OnCreateTerminus(scene, roamingTerminal, requestArgs));
    }
}
#endif
