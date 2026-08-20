#if FANTASY_NET
using System;
using Fantasy.Async;
using Fantasy.InnerMessage;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;

namespace Fantasy.Roaming.Handler;

/// <summary>
/// 从目标 Scene 解除 roamingId 对应的 Terminus。
/// </summary>
internal sealed class I_UnLinkRoamingRequestHandler : AddressRPC<Scene, I_UnLinkRoamingRequest, I_UnLinkRoamingResponse>
{
    protected override async FTask Run(Scene scene, I_UnLinkRoamingRequest request, I_UnLinkRoamingResponse response, Action reply)
    {
        if (!scene.TerminusComponent.TryGetTerminus(request.RoamingId, out var terminus))
        {
            // Terminus 已经不存在，断开目标已经达成。
            return;
        }
        
        if (terminus.OwnerRoamingRuntimeId != request.OwnerRoamingRuntimeId)
        {
            // Terminus 已经被新的 Gate 接管。
            // 旧 Gate 的延迟销毁请求不能删除新 owner 的 Terminus。
            return;
        }
        
        await scene.TerminusComponent.RemoveTerminusAsync(
            DisposeTerminusType.UnLink,
            request.RoamingId,
            request.DisposeRoaming);
    }
}
#endif
