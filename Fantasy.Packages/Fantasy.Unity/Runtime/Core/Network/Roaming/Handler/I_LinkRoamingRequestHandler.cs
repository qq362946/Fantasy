#if FANTASY_NET
using Fantasy.Async;
using Fantasy.InnerMessage;
using Fantasy.Network.Interface;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.Roaming.Handler;

public sealed class I_LinkRoamingRequestHandler : RouteRPC<Scene, I_LinkRoamingRequest, I_LinkRoamingResponse>
{
    protected override async FTask Run(Scene scene, I_LinkRoamingRequest request, I_LinkRoamingResponse response, Action reply)
    {
        var (errorCode, roamingTerminal) = await scene.TerminusComponent.Create(
            request.RoamingId, request.RoamingType,
            request.ForwardSessionRouteId, request.SceneRouteId);
        
        if (errorCode != 0)
        {
            response.ErrorCode = errorCode;
            return;
        }

        response.TerminusId = roamingTerminal.TerminusId;
    }
}
#endif
