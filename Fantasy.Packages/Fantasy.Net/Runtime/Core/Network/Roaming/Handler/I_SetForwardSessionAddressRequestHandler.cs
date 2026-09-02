#if FANTASY_NET
using Fantasy.Async;
using Fantasy.InnerMessage;
using Fantasy.Network;
using Fantasy.Network.Interface;
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace

namespace Fantasy.Roaming.Handler;

/// <summary>
/// 在客户端重连后更新目标 Terminus 的 Session 地址并恢复转发。
/// </summary>
internal sealed class I_SetForwardSessionAddressRequestHandler : AddressRPC<Scene, I_SetForwardSessionAddressRequest, I_SetForwardSessionAddressResponse>
{
    protected override async FTask Run(Scene scene, I_SetForwardSessionAddressRequest request, I_SetForwardSessionAddressResponse response, Action reply)
    {
        if (!scene.TerminusComponent.TryGetTerminus(request.RoamingId, out var terminus))
        {
            response.ErrorCode = InnerErrorCode.ErrSetForwardSessionAddressNotFoundTerminus;
            return;
        }
        
        if (terminus.OwnerRoamingRuntimeId != request.OwnerRoamingRuntimeId)
        {
            response.ErrorCode = InnerErrorCode.ErrRoamingOwnerChanged;
            return;
        }

        // 地址和开关在同一条 Scene 消息中顺序更新，避免恢复转发后仍指向旧 Session。
        terminus.StopForwarding = false;
        terminus.ForwardSessionAddress = request.ForwardSessionAddress;
        await FTask.CompletedTask;
    }
}
#endif
