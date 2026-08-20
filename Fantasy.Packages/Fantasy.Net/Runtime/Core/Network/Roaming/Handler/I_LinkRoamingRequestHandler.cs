#if FANTASY_NET
using System;
using Fantasy.Async;
using Fantasy.InnerMessage;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.Roaming.Handler;

/// <summary>
/// 在目标 Scene 创建或恢复 Terminus，并返回当前可路由的 TerminusId。
/// </summary>
internal sealed class I_LinkRoamingRequestHandler : AddressRPC<Scene, I_LinkRoamingRequest, I_LinkRoamingResponse>
{
    protected override async FTask Run(Scene scene, I_LinkRoamingRequest request, I_LinkRoamingResponse response, Action reply)
    {
        var (errorCode, roamingTerminal) = await scene.TerminusComponent.Create(
            scene,
            request.RoamingId,
            request.RoamingType,
            request.ForwardSessionAddress,
            request.SceneAddress,
            request.OwnerRoamingRuntimeId,
            request.Args);

        if (errorCode != 0)
        {
            response.ErrorCode = errorCode;
            return;
        }

        response.TerminusId = roamingTerminal.TerminusId;
    }
}
#endif
